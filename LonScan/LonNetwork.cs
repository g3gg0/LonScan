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
        private readonly Dictionary<int, ResponseCallback> PendingRequests = new Dictionary<int, ResponseCallback>();
        private readonly Config Config;
        private readonly IPAddress Remote;
        private readonly IPEndPoint RemoteEndpoint;
        private readonly Socket SendSocket;
        private readonly UdpClient ReceiveClient;
        private int NextTransaction = 0;
        private bool Stopped;
        internal Action<LonPPdu> OnReceive;
        internal int SourceNode => Config.SourceNode;
        internal DateTime LastPacket = DateTime.Now;

        public int PacketsSent;
        public int PacketsReceived;
        public int PacketsTimedOut;

        public LonNetwork(Config config)
        {
            Config = config;
            Remote = IPAddress.Parse(config.RemoteAddress);
            RemoteEndpoint = new IPEndPoint(Remote, config.RemoteSendPort);
            SendSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            ReceiveClient = new UdpClient(config.RemoteReceivePort);

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
        public bool SendMessage(LonPPdu pdu, ResponseCallback response, int waitTime = -2, int maxTries = 0)
        {
            int trans;

            if (maxTries <= 0)
            {
                maxTries = Config.PacketRetries;
            }
            if (waitTime == -2)
            {
                waitTime = Config.PacketTimeout;
            }

            /* update source subnet/address if needed */
            if (pdu.NPDU.Address.SourceSubnet == -1)
            {
                pdu.NPDU.Address.SourceSubnet = Config.SourceSubnet;
            }
            if (pdu.NPDU.Address.SourceNode == -1)
            {
                pdu.NPDU.Address.SourceNode = Config.SourceNode;
            }

            for (int retry = 0; retry < maxTries; retry++)
            {
                lock (PendingRequests)
                {
                    /* delay transmission to prevent flooding */
                    while ((DateTime.Now - LastPacket).TotalMilliseconds < Config.PacketDelay)
                    {
                        Thread.Sleep(5);
                    }

                    do
                    {
                        if (PendingRequests.Count > 5)
                        {
                            Monitor.Wait(PendingRequests, 10);
                        }

                        trans = NextTransaction;
                        NextTransaction++;
                        NextTransaction %= 8;

                    } while (PendingRequests.ContainsKey(trans));

                    if (waitTime >= 0)
                    {
                        PendingRequests.Add(trans, response);
                    }

                    /* replace transaction ID */
                    if (pdu.NPDU.PDU is LonTransPdu lonpdu)
                    {
                        lonpdu.TransNo = (uint)trans;
                    }

                    LastPacket = DateTime.Now;
                    PacketsSent++;
                    SendSocket.SendTo(pdu.FrameBytes, RemoteEndpoint);

                    var sendTime = LastPacket;
                    if (waitTime >= 0)
                    {
                        while ((DateTime.Now - sendTime).TotalMilliseconds < (waitTime / maxTries) || waitTime == 0)
                        {
                            Monitor.Wait(PendingRequests, 10);

                            if (!PendingRequests.ContainsKey(trans))
                            {
                                PacketsReceived++;
                                return true;
                            }
                            if (Stopped)
                            {
                                return false;
                            }
                        }
                        PacketsTimedOut++;
                        PendingRequests.Remove(trans);
                    }
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
            Console.WriteLine(BitConverter.ToString(pdu.FrameBytes).Replace("-", " "));
            Console.WriteLine();
            Console.WriteLine(PacketForge.ToString(pdu));

            OnReceive(pdu);

            if (!pdu.NPDU.Address.ForNode(Config.SourceNode))
            {
                return;
            }
            if (!pdu.NPDU.Address.ForSubnet(Config.SourceSubnet))
            {
                return;
            }

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
