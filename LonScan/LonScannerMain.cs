using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;

/*
   Icons:
    https://www.flaticon.com/free-icon/target_3094446
    https://pixabay.com/vectors/add-cross-green-maths-plus-symbol-159647/
 
 */


namespace LonScan
{
    public partial class LonScannerMain : Form
    {
        public LonNetwork Network;
        public Config Config;

        public LonScannerMain()
        {
            InitializeComponent();

            LoadConfig();

            Network = new LonNetwork(Config.RemoteAddress, Config.RemoteReceivePort, Config.RemoteSendPort);
            Network.Start();
        }

        private void LoadConfig()
        {
            if (!File.Exists(Config.ConfigFile))
            {
                Config = new Config();
                Config.Save();
            }

            Config = Config.Load();

            if (Config == null)
            {
                MessageBox.Show("Config invalid, using default config", "Config file error");
                Config = new Config();
                Config.Save();
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            Network.Stop();
            base.OnClosing(e);
        }

        private void ExitToolsStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void CascadeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LayoutMdi(MdiLayout.Cascade);
        }

        private void TileVerticalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LayoutMdi(MdiLayout.TileVertical);
        }

        private void TileHorizontalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LayoutMdi(MdiLayout.TileHorizontal);
        }

        private void ArrangeIconsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LayoutMdi(MdiLayout.ArrangeIcons);
        }

        private void CloseAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form childForm in MdiChildren)
            {
                childForm.Close();
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            ContextMenu contextMenu = new ContextMenu();

            foreach (LonDeviceConfig cfg in Config.DeviceConfigs)
            {
                foreach (int addr in cfg.Addresses)
                {
                    contextMenu.MenuItems.Add(new MenuItem(cfg.Name + " (Address #" + addr + ")", (s, ev) =>
                        {
                            LonDevice dev = new LonDevice((s as MenuItem).Text, addr, Network, cfg);
                            AddDevice(dev);
                        }));
                }
            }

            contextMenu.Show(this, PointToClient(MousePosition));
        }

        internal void AddDevice(LonDevice dev)
        {
            DeviceForm form = new DeviceForm(dev);
            form.MdiParent = this;
            form.Show();
        }

        private void btnScan_Click(object sender, EventArgs e)
        {
            ScanForm form = new ScanForm(Network, Config);

            form.MdiParent = this;
            form.Show();
        }

        private void updateSystemTimeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            byte[] time = new byte[7];

            time[0] = (byte)(now.Year >> 8);
            time[1] = (byte)(now.Year >> 0);
            time[2] = (byte)(now.Month);
            time[3] = (byte)(now.Day);
            time[4] = (byte)(now.Hour);
            time[5] = (byte)(now.Minute);
            time[6] = (byte)(now.Second);

            for (int group = 0; group < 3; group++)
            {
                LonPPdu pdu = new LonPPdu
                {
                    NPDU = new LonNPdu
                    {
                        AddressFormat = LonNPdu.LonNPduAddressFormat.Group,
                        SourceSubnet = 1,
                        SourceNode = 126,
                        DestinationGroup = (uint)group,
                        DomainLength = LonNPdu.LonNPduDomainLength.Bits_8,
                        Domain = 0x54,
                        PDU = new LonAPduNetworkVariable
                        {
                            Selector = 0x100,
                            Data = time
                        }
                    }
                };
                /* 00 00 00 03 08 31 00    
                 * 
                            int year = GetSigned(data, offset + 0, 1);
                            uint month = GetUnsigned(data, offset + 2, 1);
                            uint day = GetUnsigned(data, offset + 3, 1);
                            uint hour = GetUnsigned(data, offset + 4, 1);
                            uint minute = GetUnsigned(data, offset + 5, 1);
                            uint second = GetUnsigned(data, offset + 6, 1);
                */

                bool success = Network.SendMessage(pdu, (p) =>
                {
                });
            }
        }

        private void TestFunc_Click(object sender, EventArgs e)
        {
            /*
            for (int group = 0; group < 3; group++)
            {
                LonPPdu pdu = new LonPPdu
                {
                    NPDU = new LonNPdu
                    {
                        AddressFormat = LonNPdu.LonNPduAddressFormat.Group,
                        SourceSubnet = 1,
                        SourceNode = 126,
                        DestinationGroup = (uint)group,
                        DomainLength = LonNPdu.LonNPduDomainLength.Bits_8,
                        Domain = 0x54,
                        PDU = new LonAPduNetworkManagement
                        {
                            Code = (int)LonAPdu.LonAPduNMType.
                            Data = time
                        }
                    }
                };

                bool success = Network.SendMessage(pdu, (p) =>
                {
                });
            */
        }

        private void ladeXIFToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "XIF files (*.xif)|*.xif|All files (*.*)|*.*";
            dlg.RestoreDirectory = true;
            dlg.Multiselect = true;

            if(dlg.ShowDialog() == DialogResult.OK)
            {
                foreach(string s in dlg.FileNames)
                {
                    try
                    {
                        XifFile xif = new XifFile(s);

                        if(Config.DeviceConfigs.Any(c => c.Name == xif.DeviceName))
                        {
                            continue;
                        }
                        Config.AddDeviceConfig(xif.ToDeviceConfig());
                    }
                    catch
                    {

                    }
                }
                Config.Save();
            }
        }
    }
}
