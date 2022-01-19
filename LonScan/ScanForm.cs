using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
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
            AddressGuesses.Add(126, "(this tool)");

            Config = config;
            Network = network;
            Network.OnReceive += DataReceived;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            Network.OnReceive -= DataReceived;
            base.OnClosing(e);
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

                foreach (LonDeviceConfig cfg in Config.DeviceConfigs)
                {
                    contextMenu.MenuItems.Add(new MenuItem("Add #" + address + " as " + cfg.Name, (s, ev) =>
                    {
                        LonDevice dev = new LonDevice(cfg.Name, address, Network, cfg);
                        AddDevice(dev);
                    }));
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
                    int address = (int)pdu.NPDU.SourceNode;

                    string possible = "";

                    if (AddressGuesses.ContainsKey(address))
                    {
                        possible = AddressGuesses[address];
                    }

                    if (!Packets.ContainsKey(address))
                    {
                        var item = new ListViewItem(new string[] { "" + address, "" + 0, possible });
                        Items.Add(address, item);
                        Packets.Add(address, 0);
                        lstDevices.Items.Add(item);
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
