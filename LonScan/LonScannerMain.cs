using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

/*
   Icons:
    https://www.flaticon.com/free-icon/target_3094446
    https://pixabay.com/vectors/add-cross-green-maths-plus-symbol-159647/
    https://pixabay.com/vectors/icons-technology-devices-1312802/

    convert dev.png  -bordercolor white -border 0 \( -clone 0 -resize 16x16 \)           \( -clone 0 -resize 32x32 \)           \( -clone 0 -resize 48x48 \)           \( -clone 0 -resize 64x64 \)           -delete 0 -alpha off -colors 256 dev.ico
 */


namespace LonScan
{
    public partial class LonScannerMain : Form
    {
        public LonNetwork Network;
        public Config Config;
        private Thread LatencyThread;
        private bool Exiting = false;

        public LonScannerMain()
        {
            InitializeComponent();

            Text = Text + "  " + typeof(LonScannerMain).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

            LoadConfig();

            try
            {
                Network = new LonNetwork(Config);
                Network.Start();
            }
            catch(Exception ex)
            { 
                MessageBox.Show("Failed to bring up network: " + ex.Message);
                throw ex;
            }

            LatencyThread = new Thread(LatencyThreadMain);
            LatencyThread.Start();
        }

        private void LatencyThreadMain()
        {
            while (!Exiting)
            {
                long unixTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

                LonPPdu pdu = new LonPPdu
                {
                    NPDU = new LonNPdu
                    {
                        Address = new LonAddressNode
                        {
                            SourceSubnet = Config.SourceSubnet,
                            SourceNode = Config.SourceNode,
                            DestinationSubnet = Config.SourceSubnet,
                            DestinationNode = Config.SourceNode
                        },
                        DomainLength = LonNPdu.LonNPduDomainLength.Bits_8,
                        Domain = 0x54,
                        PDU = new LonSPdu
                        {
                            SPDUType = LonSPdu.LonSPduType.Response,
                            APDU = new LonAPduGenericApplication
                            {
                                Code = 1,
                                Data = BitConverter.GetBytes(unixTime)
                            }
                        }
                    }
                };

                double latency = -1;

                bool success = Network.SendMessage(pdu, (p) =>
                {
                    if (!(p.NPDU.PDU is LonSPdu spdu))
                    {
                        return;
                    }

                    if (!(spdu.APDU is LonAPduGenericApplication apdu) || apdu.Code != 1 || apdu.Data.Length != 8)
                    {
                        return;
                    }

                    long ts = BitConverter.ToInt64(apdu.Data, 0);
                    DateTimeOffset ofs = DateTimeOffset.FromUnixTimeMilliseconds(ts);

                    TimeSpan delta = DateTimeOffset.Now - ofs;
                    latency = delta.TotalMilliseconds;

                }, Config.LatencyCheckTime, 1);

                Thread.Sleep(Config.LatencyCheckTime);

                int loss = (Network.PacketsTimedOut * 100) / Network.PacketsSent;
                string latStr = "-timeout-";
                if (latency > 0)
                {
                    latStr = latency.ToString("0") + " ms";
                }
                string stats = "Latency: " + latStr + ", " + loss + "% loss, Sent: " + Network.PacketsSent + " Received: " + Network.PacketsReceived + " Timeouts: " + Network.PacketsTimedOut;

                Color col = Color.Black;

                if (loss > 20 || latency > Config.PacketTimeout / Config.PacketRetries)
                {
                    col = Color.Red;
                }

                BeginInvoke(new Action(() =>
                {
                    toolStripLatency.Text = stats;
                    toolStripLatency.ForeColor = col;
                }));
            }
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
            Exiting = true;
            Network.Stop();
            LatencyThread.Join();
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
            DeviceForm form = new DeviceForm(dev) { MdiParent = this };
            form.Show();
        }

        private void btnScan_Click(object sender, EventArgs e)
        {
            ScanForm form = new ScanForm(Network, Config) { MdiParent = this };
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
                        Address = new LonAddressGroup
                        {
                            SourceSubnet = 1,
                            SourceNode = 126,
                            DestinationGroup = (uint)group
                        },
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
            //string msg = "01 09 01 DA 01 8F 54 01 0E 25 06 23 06 02 C8 00 40 95 03 CD 63";
            //string msg = "01 19 01 DA 01 8F 54 0F 0E 24 06 21 04 02 C8 00 40";
            string msg = "01 19 01 DA 01 8F 54 0F 0B 02 01";
            byte[] data = msg.Split(' ').Select(b => Convert.ToByte(b, 16)).ToArray();

            var pdu = LonPPdu.FromData(data, 0, data.Length);
            //pdu.NPDU.DestinationNode = 10;
            //pdu.NPDU.SourceSubnet = 1;
            //pdu.NPDU.DestinationSubnet= 1;

            string text = PacketForge.ToString(pdu);
            Console.WriteLine(text);

            //pdu.NPDU.PDU = (pdu.NPDU.PDU as LonSPdu).APDU;

            bool success = Network.SendMessage(pdu, (p) =>
                {
                });
            return;

            /*
            for (int group = 0; group < 3
            ; group++)
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

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                foreach (string s in dlg.FileNames)
                {
                    try
                    {
                        XifFile xif = new XifFile(s);

                        if (Config.DeviceConfigs.Any(c => c.Name == xif.DeviceName))
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

        private void packetForgeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PacketForgeDlg form = new PacketForgeDlg(Network, Config) { MdiParent = this };
            form.Show();
        }
    }
}
