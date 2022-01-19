using Newtonsoft.Json;
using System;
using System.Threading;

namespace LonScan
{
    public class LonDevice
    {
        public readonly string Name;
        public readonly int Address;
        public LonDeviceConfig Config;

        [JsonIgnore]
        public readonly LonNetwork Network;
        [JsonIgnore]
        private bool ThreadStop;
        [JsonIgnore]
        private Thread ScanThread = null;

        public delegate void NvUpdateCallback(LonDevice device, int nv, string name, string desc, string hex, string value);
        public event NvUpdateCallback OnNvUpdate;


        public LonDevice(string name, int address, LonNetwork network, LonDeviceConfig config)
        {
            Name = name;
            Address = address;
            Network = network;
            Config = config;

            OnNvUpdate += LonDevice_OnNvUpdate;
        }

        private void LonDevice_OnNvUpdate(LonDevice device, int nv, string name, string desc, string hex, string value)
        {
        }

        public void StartNvScan()
        {
            ThreadStop = false;
            ScanThread = new Thread(() =>
            {
                while (true)
                {
                    foreach (var info in Config.NvMap)
                    {
                        int nvIndex = info.Key;

                        if (ThreadStop)
                        {
                            return;
                        }
                        
                        LonPPdu pdu = new LonPPdu
                        {
                            NPDU = new LonNPdu
                            {
                                AddressFormat = LonNPdu.LonNPduAddressFormat.SubnetNode,
                                SourceSubnet = 1,
                                SourceNode = 126,
                                DomainLength = LonNPdu.LonNPduDomainLength.Bits_8,
                                Domain = 0x54,
                                DestinationSubnet = 1,
                                DestinationNode = (uint)Address,
                                PDU = new LonSPdu
                                {
                                    SPDUType = LonSPdu.LonSPduType.Request,
                                    APDU = new LonAPduNetworkManagement
                                    {
                                        Code = (int)LonAPdu.LonAPduNMType.NetworkVariableValueFetch,
                                        Data = new byte[] { (byte)info.Key }
                                    }
                                }
                            }
                        };

                        bool success = Network.SendMessage(pdu, (p) =>
                        {
                            MessageReceived(p);
                        });
                        /*
                        LonPPdu pduTbl = new LonPPdu
                        {
                            NPDU = new LonNPdu
                            {
                                AddressFormat = LonNPdu.LonNPduAddressFormat.SubnetNode,
                                SourceSubnet = 1,
                                SourceNode = 126,
                                DomainLength = LonNPdu.LonNPduDomainLength.Bits_8,
                                Domain = 0x54,
                                DestinationSubnet = 1,
                                DestinationNode = (uint)Address,
                                PDU = new LonSPdu
                                {
                                    SPDUType = LonSPdu.LonSPduType.Request,
                                    APDU = new LonAPduNetworkManagement
                                    {
                                        Code = (int)LonAPdu.LonAPduNMType.QueryNetworkVariableConfig,
                                        Data = new byte[] { (byte)nvIndex }
                                    }
                                }
                            }
                        };
                        LonPPdu pduType = new LonPPdu
                        {
                            NPDU = new LonNPdu
                            {
                                AddressFormat = LonNPdu.LonNPduAddressFormat.SubnetNode,
                                SourceSubnet = 1,
                                SourceNode = 126,
                                DomainLength = LonNPdu.LonNPduDomainLength.Bits_8,
                                Domain = 0x54,
                                DestinationSubnet = 1,
                                DestinationNode = (uint)Address,
                                PDU = new LonSPdu
                                {
                                    SPDUType = LonSPdu.LonSPduType.Request,
                                    APDU = new LonAPduNetworkManagement
                                    {
                                        Code = (int)LonAPdu.LonAPduNMType.QueryStandardNetworkVariableType,
                                        Data = new byte[] { (byte)nvIndex, 0, 16 }
                                    }
                                }
                            }
                        };

                        string cfgString = "";
                        string typeString = "";

                        Network.SendMessage(pduTbl, (p) =>
                        {
                            var cfg = LonAPdu.NVConfig.FromData((p.NPDU.PDU as LonSPdu).APDU.Data, 0);
                            cfgString = cfg.ToString();
                        });
                        Network.SendMessage(pduType, (p) =>
                        {
                            typeString = BitConverter.ToString((p.NPDU.PDU as LonSPdu).APDU.Data);
                        });

                        Console.WriteLine(nvIndex.ToString().PadLeft(2) + "# " + Config.NvMap[nvIndex].Name.PadRight(16) + " | " + cfgString + " " + typeString);
                        */
                        Thread.Sleep(50);
                    }
                }
            });
            ScanThread.Start();
        }

        public void StopNvScan()
        {
            if (ScanThread != null)
            {
                ThreadStop = true;
                ScanThread.Join(500);
            }
        }

        void MessageReceived(LonPPdu pdu)
        {
            if (pdu.NPDU.SourceNode != Address)
            {
                return;
            }

            if (!(pdu.NPDU.PDU is LonSPdu spdu))
            {
                return;
            }

            if (!(spdu.APDU is LonAPduGenericApplication apdu))
            {
                return;
            }

            if ((apdu.Code & 0x1F) != (uint)LonAPdu.LonAPduNMType.NetworkVariableValueFetch)
            {
                return;
            }

            int nv_recv = apdu.Data[0];
            NvInfo info = Config.NvMap[nv_recv];

            string hex = "";

            for (int pos = 1; pos < apdu.Data.Length; pos++)
            {
                hex += apdu.Data[pos].ToString("X2");
                if ((pos % 2) == 1)
                {
                    hex += " ";
                }
            }

            string value = LonStandardTypes.ToString(apdu.Data, 1, apdu.Data.Length - 1, info.Type);

            OnNvUpdate(this, nv_recv, info.Name, info.Description, hex, value);
        }
    }
}
