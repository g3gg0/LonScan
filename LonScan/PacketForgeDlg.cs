using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LonScan
{
    public partial class PacketForgeDlg : Form
    {
        private readonly LonNetwork Network;
        private readonly Config Config;

        public PacketForgeDlg(LonNetwork network, Config config)
        {
            Network = network;
            Config = config;
            InitializeComponent();

            foreach (string str in Config.PacketForgeTemplates)
            {
                cmbFrameBytes.Items.Add(str);
            }
        }

        private void cmbFrameBytes_KeyPress(object sender, KeyPressEventArgs e)
        {
            UpdatePacket();
        }

        private void UpdatePacket()
        {
            byte[] data = GetBytes();

            if (data.Length < 6)
            {
                return;
            }

            var pdu = LonPPdu.FromData(data, 0, data.Length);

            txtTxDump.Text = BitConverter.ToString(pdu.FrameBytes).Replace("-", " ") + Environment.NewLine + Environment.NewLine + PacketForge.ToString(pdu);

            if (pdu.NPDU.Address.SourceNode != Network.SourceNode)
            {
                txtRxDump.Text = "Please set source node to 0x" + Network.SourceNode.ToString("X2") + " (" + Network.SourceNode + ") else we will receive no reply.";
            }
            else
            {
                txtRxDump.Text = "";
            }
        }

        private void btnTx_Click(object sender, EventArgs e)
        {
            SendPacket();
        }

        private void cmbFrameBytes_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Enter)
            {
                SendPacket();
                e.SuppressKeyPress = true;
            }
        }

        private void cmbFrameBytes_TextChanged(object sender, EventArgs e)
        {
            UpdatePacket();
        }

        private void SendPacket()
        {
            byte[] data = GetBytes();

            if (cmbFrameBytes.Text.Contains("#"))
            {
                SaveSequence(cmbFrameBytes.Text);
            }
            cmbFrameBytes.Text = BitConverter.ToString(data).Replace("-", " ");


            Thread t = new Thread(() =>
            {
                var pdu = LonPPdu.FromData(data, 0, data.Length);
                bool wait = pdu.NPDU.Address.SourceNode == Network.SourceNode;

                if (wait)
                {
                    BeginInvoke(new Action(() =>
                    {
                        txtRxDump.Text = "(Waiting " + Config.PacketForgeTimeout + "ms for response...)";
                    }));
                }

                bool success = Network.SendMessage(pdu, (r) =>
                {
                    BeginInvoke(new Action(() =>
                    {
                        txtRxDump.Text = BitConverter.ToString(r.FrameBytes).Replace("-", " ") + Environment.NewLine + Environment.NewLine + PacketForge.ToString(r);
                    }));
                }, wait ? Config.PacketForgeTimeout : -1);

                if (!success)
                {
                    BeginInvoke(new Action(() =>
                    {
                        txtRxDump.Text = "(Response timed out)";
                    }));
                }
            });

            t.Start();
        }

        private void SaveSequence(string text)
        {
            if (!Config.PacketForgeTemplates.Contains(text))
            {
                cmbFrameBytes.Items.Add(text);
                Config.PacketForgeTemplates.Add(text);
                Config.Save();
            }
        }

        public byte[] GetBytes()
        {
            try
            {
                string msg = cmbFrameBytes.Text.Split('#')[0].Replace(" ", "");
                return Enumerable.Range(0, msg.Length) .Where(x => x % 2 == 0)
                                 .Select(x => Convert.ToByte(msg.Substring(x, 2), 16)) .ToArray();
            }
            catch (Exception ex)
            {
            }

            return new byte[0];
        }
    }
}
