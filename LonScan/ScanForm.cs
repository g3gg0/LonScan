using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace LonScan
{
    public partial class ScanForm : Form
    {
        private readonly Config Config;
        private readonly LonNetwork Network;
        private Dictionary<int, int> Packets = new Dictionary<int, int>();
        private readonly Dictionary<int, string> AddressGuesses = new Dictionary<int, string>();
        private readonly Dictionary<int, ListViewItem> Items = new Dictionary<int, ListViewItem>();

        public ScanForm(LonNetwork network, Config config)
        {
            InitializeComponent();

            lstDevices.MouseClick += LstDevices_MouseClick;
            lstDevices.MouseDoubleClick += LstDevices_MouseDoubleClick;

            foreach (var dev in config.DeviceConfigs)
            {
                foreach (int addr in dev.Addresses)
                {
                    if(AddressGuesses.ContainsKey(addr))
                    {
                        AddressGuesses[addr] += ", " + dev.Name;
                    }
                    else
                    {
                        AddressGuesses.Add(addr, dev.Name);
                    }
                }
            }
            AddressGuesses.Add(127, "MES-WiFi");

            Config = config;
            Network = network;
            Network.OnReceive += DataReceived;

            QueryDevices();
        }

        private void QueryDevices()
        {
            LonPPdu pduEnable = new LonPPdu
            {
                NPDU = new LonNPdu
                {
                    Address = new LonAddressSubnet
                    {
                        SourceSubnet = 1,
                        SourceNode = Config.SourceNode,
                        DestinationSubnet = 1
                    },
                    DomainLength = LonNPdu.LonNPduDomainLength.Bits_8,
                    Domain = 0x54,
                    PDU = new LonSPdu
                    {
                        SPDUType = LonSPdu.LonSPduType.Request,
                        APDU = new LonAPduNetworkManagement
                        {
                            Code = LonAPdu.LonAPduNMType.RespondToQuery,
                            Data = new byte[] { 1 }
                        }
                    }
                }
            };
            LonPPdu pduDisable = new LonPPdu
            {
                NPDU = new LonNPdu
                {
                    Address = new LonAddressSubnet
                    {
                        SourceSubnet = 1,
                        SourceNode = Config.SourceNode,
                        DestinationSubnet = 1
                    },
                    DomainLength = LonNPdu.LonNPduDomainLength.Bits_8,
                    Domain = 0x54,
                    PDU = new LonSPdu
                    {
                        SPDUType = LonSPdu.LonSPduType.Request,
                        APDU = new LonAPduNetworkManagement
                        {
                            Code = LonAPdu.LonAPduNMType.RespondToQuery,
                            Data = new byte[] { 0 }
                        }
                    }
                }
            };

            LonPPdu pduQuery = new LonPPdu
            {
                NPDU = new LonNPdu
                {
                    Address = new LonAddressSubnet
                    {
                        SourceSubnet = 1,
                        SourceNode = Config.SourceNode,
                        DestinationSubnet = 1
                    },
                    DomainLength = LonNPdu.LonNPduDomainLength.Bits_8,
                    Domain = 0x54,
                    PDU = new LonSPdu
                    {
                        SPDUType = LonSPdu.LonSPduType.Request,
                        APDU = new LonAPduNetworkManagement
                        {
                            Code = LonAPdu.LonAPduNMType.QueryId,
                            Data = new byte[] { 1 }
                        }
                    }
                }
            };

            Thread SendThread = new Thread(() =>
            {
                Network.SendMessage(pduEnable, (p) => { }, -1);
                Thread.Sleep(500);
                Network.SendMessage(pduQuery, (p) => { }, -1);
                Thread.Sleep(500);
                Network.SendMessage(pduDisable, (p) => { }, -1);
            });
            SendThread.Start();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            Network.OnReceive -= DataReceived;
            base.OnClosing(e);
        }


        private void LstDevices_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ListViewItem item = lstDevices.GetItemAt(e.X, e.Y);

            if (item == null)
            {
                return;
            }
            int address = Items.Where(p => p.Value == item).First().Key;

            ContextMenu contextMenu = new ContextMenu();

            var candid = Config.DeviceConfigs.Where(d => d.ProgramId == Items[address].SubItems[4].Text);
            if (candid.Count() == 1)
            {
                LonDeviceConfig cfg = candid.First();
                LonDevice dev = new LonDevice(cfg.Name, address, Network, cfg);
                AddDevice(dev);
            }
        }

        private void LstDevices_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                ListViewItem item = lstDevices.GetItemAt(e.X, e.Y);

                if (item == null)
                {
                    return;
                }
                int address = Items.Where(p => p.Value == item).First().Key;

                ContextMenu contextMenu = new ContextMenu();

                var candid = Config.DeviceConfigs.Where(d => d.ProgramId == Items[address].SubItems[4].Text);
                if (candid.Count() > 0)
                {
                    foreach (LonDeviceConfig cfg in candid)
                    {
                        contextMenu.MenuItems.Add(new MenuItem("Add #" + address + " as " + cfg.Name, (s, ev) =>
                        {
                            LonDevice dev = new LonDevice(cfg.Name, address, Network, cfg);
                            AddDevice(dev);
                        }));
                    }
                }
                else
                {
                    foreach (LonDeviceConfig cfg in Config.DeviceConfigs)
                    {
                        contextMenu.MenuItems.Add(new MenuItem("Add #" + address + " as " + cfg.Name, (s, ev) =>
                        {
                            LonDevice dev = new LonDevice(cfg.Name, address, Network, cfg);
                            AddDevice(dev);
                        }));
                    }
                }

                contextMenu.Show(this, PointToClient(MousePosition));
            }
        }

        private void AddDevice(LonDevice dev)
        {
            (MdiParent as LonScannerMain).AddDevice(dev);
        }

        public void DataReceived(LonPPdu pdu)
        {
            BeginInvoke(new Action(() =>
            {
                if (pdu.NPDU != null)
                {
                    int address = (int)pdu.NPDU.Address.SourceNode;
                    string possible = "";

                    if(address == Config.SourceNode)
                    {
                        possible = "(this tool)";
                    }
                    else if (AddressGuesses.ContainsKey(address))
                    {
                        possible = "Guess: " + AddressGuesses[address];
                    }

                    if (!Packets.ContainsKey(address))
                    {
                        var item = new ListViewItem(new string[] { "" + address, "" + 0, possible, "", "" });
                        Items.Add(address, item);
                        Packets.Add(address, 0);
                        lstDevices.Items.Add(item);
                    }

                    if (pdu.NPDU.PDU is LonSPdu spdu)
                    {
                        if (spdu.APDU is LonAPduGenericApplication apdu)
                        {
                            if (apdu.Code == 0x21)
                            {
                                byte[] neuron = new byte[6];
                                byte[] prog_str = new byte[8];

                                Array.Copy(apdu.Data, 0, neuron, 0, neuron.Length);
                                Array.Copy(apdu.Data, 6, prog_str, 0, prog_str.Length);
                                Items[address].SubItems[3].Text = BitConverter.ToString(neuron).Replace("-", "");
                                Items[address].SubItems[4].Text = BitConverter.ToString(prog_str).Replace("-", "");

                                var candid = Config.DeviceConfigs.Where(d => d.ProgramId == Items[address].SubItems[4].Text);
                                if (candid.Count() > 0)
                                {
                                    possible = "";
                                    foreach (var cand in candid)
                                    {
                                        possible += cand.Name + ", ";
                                    }
                                    Items[address].SubItems[2].Text = "Match: " + possible.Trim(new[] { ' ', ',' });
                                }
                            }
                        }
                    }

                    Packets[address]++;
                    Items[address].SubItems[1].Text = "" + Packets[address];
                    Items.Values.ToList().ForEach(x => x.Selected = false);
                    Items[address].Selected = true;
                }
            }));
        }
    }
}
