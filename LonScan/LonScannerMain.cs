using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;

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

        private void ToolBarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            toolStrip.Visible = toolBarToolStripMenuItem.Checked;
        }

        private void StatusBarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            statusStrip.Visible = statusBarToolStripMenuItem.Checked;
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
    }
}
