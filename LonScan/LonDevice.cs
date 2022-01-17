using Newtonsoft.Json;
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
                                    APDU = LonAPdu.GenerateNMNVFetch((byte)info.Key)
                                }
                            }
                        };

                        bool success = Network.SendMessage(pdu, (p) =>
                        {
                            MessageReceived(p);
                        });

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
