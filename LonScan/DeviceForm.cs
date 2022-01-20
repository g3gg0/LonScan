using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;

namespace LonScan
{
    public partial class DeviceForm : Form
    {
        private int Address = 10;
        private Thread ScanThread = null;
        private LonDevice Device = null;
        private int MemoryAddress = 0;


        public DeviceForm(LonDevice device)
        {
            InitializeComponent();

            Text = "Device view: " + device.Name + " (Address " + device.Address + ")";
            Device = device;

            Device.OnNvUpdate += OnNvUpdate;
            Device.StartNvScan();
        }

        private void OnNvUpdate(LonDevice device, int nv_recv, string name, string desc, string hex, string value)
        {
            try
            {
                this.BeginInvoke(new Action(() =>
                {
                    while (listView1.Items.Count <= nv_recv)
                    {
                        listView1.Items.Add(listView1.Items.Count.ToString("X2"));
                        listView1.Items[listView1.Items.Count - 1].SubItems.Add("");
                        listView1.Items[listView1.Items.Count - 1].SubItems.Add("");
                        listView1.Items[listView1.Items.Count - 1].SubItems.Add("");
                        listView1.Items[listView1.Items.Count - 1].SubItems.Add("");
                    }
                    listView1.Items[nv_recv].SubItems[1].Text = name;
                    listView1.Items[nv_recv].SubItems[2].Text = desc;
                    listView1.Items[nv_recv].SubItems[3].Text = hex;
                    listView1.Items[nv_recv].SubItems[4].Text = value;
                    listView1.Items[nv_recv].Tag = nv_recv;
                }));
            }
            catch (Exception)
            {

            }
        }

        private void ScanMemory()
        {
            Thread t = new Thread(()=>
            {
                MemoryAddress = 0x2306;

                while (MemoryAddress < 0x2400)
                {
                    LonPPdu pdu = new LonPPdu
                    {
                        NPDU = new LonNPdu
                        {
                            Address = new LonAddressNode
                            {
                                SourceSubnet = 1,
                                SourceNode = 126,
                                DestinationSubnet = 1,
                                DestinationNode = (uint)Address
                            },

                            DomainLength = LonNPdu.LonNPduDomainLength.Bits_8,
                            Domain = 0x54,
                            PDU = new LonSPdu
                            {
                                SPDUType = LonSPdu.LonSPduType.Request,
                                APDU = LonAPduNetworkManagement.GenerateNMMemoryRead(0, (uint)MemoryAddress, 16)
                            }
                        }
                    };

                    bool success = Device.Network.SendMessage(pdu, (p) =>
                    {
                        //Console.WriteLine("M: " + Address.ToString("X4") + "  " + BitConverter.ToString(p.FrameBytes).Replace("-", " "));
                    });

                    if (success)
                    {
                        MemoryAddress += 0x10;
                    }
                }
            });
            ScanThread.Abort();
            t.Start();
        }


        protected override void OnClosing(CancelEventArgs e)
        {
            if(ScanThread != null)
            {
                ScanThread.Abort();
                ScanThread = null;
            }
            Device.StopNvScan();
            base.OnClosing(e);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
        }
    }
}
