using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Linq;
using System.Windows.Forms;

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

            Remote = null;

            if (IPAddress.TryParse(Config.RemoteAddress, out var ipAddress))
            {
                Remote = ipAddress;
            }
            else
            {
                try
                {
                    var hostAddresses = Dns.GetHostAddresses(Config.RemoteAddress);
                    if (hostAddresses.Length == 0)
                    {
                        MessageBox.Show($"Warning: Unable to resolve hostname: {Config.RemoteAddress}. Using broadcast address.", "Resolve error");
                    }
                    else
                    {
                        Remote = hostAddresses[0];
                    }
                }
                catch(Exception ex)
                {
                    MessageBox.Show($"Warning: Unable to resolve hostname: {Config.RemoteAddress}", "Resolve error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            if (Remote == null)
            {
                var allIf = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(n => n.OperationalStatus == OperationalStatus.Up);

                // Get default gateway
                var defaultGateway =
                    allIf.SelectMany(n => n.GetIPProperties()?.GatewayAddresses)
                    .FirstOrDefault(g => g?.Address != null && g.Address.AddressFamily.ToString() == "InterNetwork");

                if (defaultGateway == null)
                {
                    throw new ArgumentException("No default gateway found.");
                }

                // Find network interface with default gateway
                var networkInterface = allIf.Where(n => n.GetIPProperties().GatewayAddresses.Contains(defaultGateway))
                    .FirstOrDefault();

                if (networkInterface == null)
                {
                    throw new ArgumentException("No network interface with default gateway found.");
                }

                // Get IP and subnet mask
                var ipInfo = networkInterface.GetIPProperties().UnicastAddresses
                    .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
                    .FirstOrDefault();

                if (ipInfo == null)
                {
                    throw new ArgumentException("No IPv4 address found for network interface.");
                }

                // Calculate broadcast address
                byte[] ipAdressBytes = ipInfo.Address.GetAddressBytes();
                byte[] subnetMaskBytes = ipInfo.IPv4Mask.GetAddressBytes();

                Remote = GetBroadcastAddress(ipInfo.Address, ipInfo.IPv4Mask);
            }

            if (IsBroadcastAddress(Remote))
            {
                MessageBox.Show("Warning: Using broadcast address can lead to high latency or drop rate!" + Environment.NewLine + "Please specify the IP address in LonScan.cfg, field 'RemoteAddress'", "Broadcast Address Warning",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            RemoteEndpoint = new IPEndPoint(Remote, Config.RemoteSendPort);
            SendSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            ReceiveClient = new UdpClient(Config.RemoteReceivePort);

            OnReceive += (p) => { };
        }

        private bool IsBroadcastAddress(IPAddress ipAddress)
        {
            foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                foreach (var unicastIPAddressInformation in networkInterface.GetIPProperties().UnicastAddresses)
                {
                    if (unicastIPAddressInformation.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        var broadcastAddress = GetBroadcastAddress(unicastIPAddressInformation.Address, unicastIPAddressInformation.IPv4Mask);
                        if (ipAddress.Equals(broadcastAddress))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private IPAddress GetBroadcastAddress(IPAddress ipAddress, IPAddress subnetMask)
        {
            byte[] ipAddressBytes = ipAddress.GetAddressBytes();
            byte[] subnetMaskBytes = subnetMask.GetAddressBytes();

            if (ipAddressBytes.Length != subnetMaskBytes.Length)
            {
                throw new ArgumentException("Lengths of IP address and subnet mask do not match.");
            }

            byte[] broadcastAddressBytes = new byte[ipAddressBytes.Length];
            for (int i = 0; i < broadcastAddressBytes.Length; i++)
            {
                broadcastAddressBytes[i] = (byte)(ipAddressBytes[i] | (subnetMaskBytes[i] ^ 255));
            }

            return new IPAddress(broadcastAddressBytes);
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
