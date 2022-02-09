using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;

namespace LonScan
{
    public class LonDevice
    {
        public readonly string Name;
        public readonly int Address;
        public LonDeviceConfig Config;
        public Dictionary<int, LonAPdu.NvConfig> NvConfigTable = new Dictionary<int, LonAPdu.NvConfig>();

        public readonly LonNetwork Network;
        private bool ThreadStop;
        private Thread ScanThread = null;

        public delegate void NvUpdateCallback(LonDevice device, int nv, string hex, string value);
        public event NvUpdateCallback OnNvUpdate;
        public delegate void UpdateCallback(LonDevice device);
        public event UpdateCallback OnUpdate;

        public byte[] ConfigMem = new byte[0x30];

        public string ConfigVersion;
        public string ConfigDate;
        public string ConfigProduct;

        public LonDevice(string name, int address, LonNetwork network, LonDeviceConfig config)
        {
            Name = name;
            Address = address;
            Network = network;
            Config = config;

            OnNvUpdate += (d, n, h, v) => { };
            OnUpdate += (d) => { };
        }

        public byte[] ReadMemory(uint address, int length, Action<decimal> progress = null)
        {
            byte[] result = new byte[0];

            for (uint off = 0; off < length; off += 0x10)
            {
                LonPPdu pdu = new LonPPdu
                {
                    NPDU = new LonNPdu
                    {
                        Address = new LonAddressNode
                        {
                            SourceSubnet = -1,
                            SourceNode = -1, /* automatically set */
                            DestinationSubnet = 1,
                            DestinationNode = Address
                        },
                        DomainLength = LonNPdu.LonNPduDomainLength.Bits_8,
                        Domain = 0x54,
                        PDU = new LonSPdu
                        {
                            SPDUType = LonSPdu.LonSPduType.Request,
                            APDU = LonAPduNetworkManagement.GenerateNMMemoryRead(0, address + off, 0x10)
                        }
                    }
                };
                bool success = Network.SendMessage(pdu, (p) =>
                {
                    LonSPdu spdu = (p.NPDU.PDU as LonSPdu);
                    if (spdu != null && spdu.APDU is LonAPduGenericApplication)
                    {
                        LonAPduGenericApplication apdu = (spdu.APDU as LonAPduGenericApplication);

                        /* mem read successful */
                        if ((apdu.Code & 0x20) != 0 && (apdu.Code & 0x1F) == (int)LonAPduNetworkManagement.LonAPduNMType.ReadMemory && apdu.Payload.Length > 0)
                        {
                            Array.Resize(ref result, (int)(off + apdu.Payload.Length));
                            Array.Copy(apdu.Payload, 0, result, off, apdu.Payload.Length);
                        }
                    }
                }, 5000, 5);

                if(progress != null)
                {
                    progress((decimal)result.Length / length);
                }

                if(!success)
                {
                    break;
                }
            }

            return result;
        }


        public void StartNvScan()
        {
            ThreadStop = false;
            ScanThread = new Thread(() =>
            {
                bool versionFetched = false;

                while (true)
                {
                    if(!versionFetched)
                    {
                        for (uint addr = 0; addr < ConfigMem.Length; addr += 0x10)
                        {
                            LonPPdu pdu = new LonPPdu
                            {
                                NPDU = new LonNPdu
                                {
                                    Address = new LonAddressNode
                                    {
                                        SourceSubnet = -1,
                                        SourceNode = -1, /* automatically set */
                                        DestinationSubnet = 1,
                                        DestinationNode = Address
                                    },
                                    DomainLength = LonNPdu.LonNPduDomainLength.Bits_8,
                                    Domain = 0x54,
                                    PDU = new LonSPdu
                                    {
                                        SPDUType = LonSPdu.LonSPduType.Request,
                                        APDU = LonAPduNetworkManagement.GenerateNMMemoryRead(0, 0x4000 + addr, 0x10)
                                    }
                                }
                            };
                            bool success = Network.SendMessage(pdu, (p) =>
                            {
                                LonSPdu spdu = (p.NPDU.PDU as LonSPdu);
                                if (spdu != null && spdu.APDU is LonAPduGenericApplication)
                                {
                                    LonAPduGenericApplication apdu = (spdu.APDU as LonAPduGenericApplication);

                                    /* mem read successful */
                                    if((apdu.Code & 0x20) != 0 && (apdu.Code & 0x1F) == (int)LonAPduNetworkManagement.LonAPduNMType.ReadMemory && apdu.Payload.Length == 0x10)
                                    {
                                        Array.Copy(apdu.Payload, 0, ConfigMem, addr, 0x10);
                                    }
                                }
                            }, 5000, 5);
                        }

                        versionFetched = true;
                        UpdateConfigStrings();
                    }

                    foreach (var info in Config.NvMap)
                    {
                        int nvIndex = info.Key;

                        if (ThreadStop)
                        {
                            return;
                        }

                        if(!NvConfigTable.ContainsKey(nvIndex))
                        {
                            LonPPdu nvpdu = new LonPPdu
                            {
                                NPDU = new LonNPdu
                                {
                                    Address = new LonAddressNode
                                    {
                                        SourceSubnet = -1,
                                        SourceNode = -1, /* automatically set */
                                        DestinationSubnet = 1,
                                        DestinationNode = Address
                                    },
                                    DomainLength = LonNPdu.LonNPduDomainLength.Bits_8,
                                    Domain = 0x54,
                                    PDU = new LonSPdu
                                    {
                                        SPDUType = LonSPdu.LonSPduType.Request,
                                        APDU = new LonAPduNetworkManagement
                                        {
                                            Code = LonAPdu.LonAPduNMType.QueryNetworkVariableConfig,
                                            Data = new byte[] { (byte)nvIndex }
                                        }
                                    }
                                }
                            };

                            Network.SendMessage(nvpdu, (p) =>
                            {
                                LonSPdu spdu = (p.NPDU.PDU as LonSPdu);
                                if (spdu != null && spdu.APDU is LonAPduGenericApplication)
                                {
                                    LonAPduGenericApplication apdu = (spdu.APDU as LonAPduGenericApplication);

                                    /* mem read successful */
                                    if ((apdu.Code & 0x20) != 0 && (apdu.Code & 0x1F) == (int)LonAPduNetworkManagement.LonAPduNMType.QueryNetworkVariableConfig)
                                    {
                                        LonAPdu.NvConfig cfg = LonAPdu.NvConfig.FromData(apdu.Data);
                                        NvConfigTable.Add(nvIndex, cfg);
                                    }
                                }
                            });
                        }
                        
                        LonPPdu pdu = new LonPPdu
                        {
                            NPDU = new LonNPdu
                            {
                                Address = new LonAddressNode
                                {
                                    SourceSubnet = -1,
                                    SourceNode = -1, /* automatically set */
                                    DestinationSubnet = 1,
                                    DestinationNode = Address
                                },
                                DomainLength = LonNPdu.LonNPduDomainLength.Bits_8,
                                Domain = 0x54,
                                PDU = new LonSPdu
                                {
                                    SPDUType = LonSPdu.LonSPduType.Request,
                                    APDU = new LonAPduNetworkManagement
                                    {
                                        Code = LonAPdu.LonAPduNMType.NetworkVariableValueFetch,
                                        Data = new byte[] { (byte)info.Key }
                                    }
                                }
                            }
                        };

                        bool success = Network.SendMessage(pdu, (p) =>
                        {
                            MessageReceived(p);
                        });
                    }
                }
            });
            ScanThread.Start();
        }

        private void UpdateConfigStrings()
        {
            int pos = 3;

            ConfigVersion = GetString(ConfigMem, ref pos);
            ConfigDate = GetString(ConfigMem, ref pos);
            ConfigProduct = GetString(ConfigMem, ref pos);

            OnUpdate(this);
        }

        private string GetString(byte[] buf, ref int pos)
        {
            int length = 0;

            for(int lenChk = pos; lenChk < buf.Length; lenChk++)
            {
                if(buf[lenChk] == 0)
                {
                    break;
                }
                length++;
            }

            byte[] strBuf = new byte[length];
            Array.Copy(buf, pos, strBuf, 0, length);

            pos += length + 1;

            string str = Encoding.ASCII.GetString(strBuf);

            return str;
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
            if (pdu.NPDU.Address.ForNode(Address))
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

            OnNvUpdate(this, nv_recv, hex, value);
        }
    }
}
