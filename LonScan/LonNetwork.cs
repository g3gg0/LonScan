using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace LonScan
{
    public class LonNetwork
    {
        public bool ThreadStop = false;
        private Thread ReceiveThread;

        public delegate void ResponseCallback(LonPPdu ppdu);
        private int NextTransaction = 0;
        private Dictionary<int, ResponseCallback> PendingRequests = new Dictionary<int, ResponseCallback>();
        private readonly string RemoteAddress;
        private readonly IPAddress Remote;
        private readonly IPEndPoint RemoteEndpoint;
        private readonly Socket SendSocket;
        private readonly UdpClient ReceiveClient;
        private bool Stopped;
        internal Action<LonPPdu> OnReceive;

        public LonNetwork(string remoteAddress, int remoteReceivePort = 3333, int remoteSendPort = 3334)
        {
            RemoteAddress = remoteAddress;
            Remote = IPAddress.Parse(RemoteAddress);
            RemoteEndpoint = new IPEndPoint(Remote, remoteSendPort);
            SendSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            ReceiveClient = new UdpClient(remoteReceivePort);

            OnReceive += (p) => { };
        }

        public void Start()
        {
            ThreadStop = false;
            ReceiveThread = new Thread(ThreadReceive);
            ReceiveThread.Start();
        }

        /// <summary>
        /// Send a message to the network, waiting for the response
        /// </summary>
        /// <param name="data">payload to send to the network</param>
        /// <param name="response">callback to be called upon a response</param>
        /// <param name="waitTime">time to wait for the response in ms, 0 = wait forever, -1 = do not wait at all</param>
        /// <returns>Returns true when a response arrived in time, false when waiting timed out.</returns>
        public bool SendMessage(LonPPdu pdu, ResponseCallback response, int waitTime = 2000)
        {
            int trans;

            while (PendingRequests.Count > 0)
            {
                Thread.Sleep(100);
                if (Stopped)
                {
                    return false;
                }
            }

            lock (PendingRequests)
            {
                trans = NextTransaction;

                NextTransaction++;
                NextTransaction %= 8;

                PendingRequests.Add(trans, response);
            }

            /* replace transaction ID */
            if (pdu.NPDU.PDU is LonSPdu spdu)
            {
                spdu.TransNo = (uint)trans;
            }

            SendSocket.SendTo(pdu.FrameBytes, RemoteEndpoint);

            DateTime start = DateTime.Now;

            if (waitTime > 0)
            {
                lock (PendingRequests)
                {
                    while ((DateTime.Now - start).TotalMilliseconds < waitTime || waitTime == 0)
                    {
                        Monitor.Wait(PendingRequests, 50);

                        if (!PendingRequests.ContainsKey(trans))
                        {
                            return true;
                        }
                        if (Stopped)
                        {
                            return false;
                        }
                    }
                    PendingRequests.Remove(trans);
                }
            }

            return false;
        }

        void ThreadReceive()
        {
            IPEndPoint receiveEndpoint = new IPEndPoint(IPAddress.Any, 0);

            ReceiveClient.Client.ReceiveBufferSize = 1024 * 1024;

            while (!ThreadStop)
            {
                byte[] bytes = ReceiveClient.Receive(ref receiveEndpoint);

                /* only parse packets, no status responses */
                if (bytes[0] != 2)
                {
                    continue;
                }
                /* ignore packets with CRC errors */
                if (bytes[1] != 0)
                {
                    continue;
                }
                /* ignore packets which are too short */
                if (bytes.Length < 8)
                {
                    continue;
                }

                try
                {
                    LonPPdu parsed = LonPPdu.FromData(bytes, 6, bytes.Length - 8);
                    PacketReceived(parsed);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to parse packet: " + BitConverter.ToString(bytes).Replace("-", " "));
                    Console.WriteLine(ex);
                }
            }

            ReceiveClient.Close();
        }

        private void PacketReceived(LonPPdu pdu)
        {
            OnReceive(pdu);

            if (!(pdu.NPDU.PDU is LonSPdu spdu))
            {
                return;
            }

            lock (PendingRequests)
            {
                if (spdu.SPDUType == LonSPdu.LonSPduType.Response && PendingRequests.ContainsKey((int)spdu.TransNo))
                {
                    PendingRequests[(int)spdu.TransNo]?.Invoke(pdu);
                    PendingRequests.Remove((int)spdu.TransNo);
                    Monitor.Pulse(PendingRequests);
                }
            }
        }

        public void Stop()
        {
            Stopped = true;
            if (ReceiveThread != null)
            {
                ThreadStop = true;
                ReceiveThread.Join(500);
            }
            PendingRequests.Clear();
        }
    }
}
