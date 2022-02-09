using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
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

            for(int nv = 0; nv <= Device.Config.NvMap.Keys.Max(); nv++)
            {
                ListViewItem item;

                if (Device.Config.NvMap.ContainsKey(nv))
                {
                    item = new ListViewItem(new string[13] { nv.ToString("X2"), Device.Config.NvMap[nv].Name, Device.Config.NvMap[nv].Description, "", "", "", "", "", "", "", "", "", "" });
                }
                else
                {
                    item = new ListViewItem(new string[13] { nv.ToString("X2"), "(unused)", "", "", "", "", "", "", "", "", "", "", "" });
                }
                listView1.Items.Add(item);
            }

            Device.OnNvUpdate += OnNvUpdate;
            Device.OnUpdate += OnUpdate;
            Device.StartNvScan();
        }

        private void OnUpdate(LonDevice device)
        {
            try
            {
                this.BeginInvoke(new Action(() =>
                {
                    txtFirmwareDate.Text = Device.ConfigDate;
                    txtFirmwareVer.Text = Device.ConfigVersion;
                    txtFirmwareProd.Text = Device.ConfigProduct;
                }));
            }
            catch (Exception)
            {

            }
        }

        private void OnNvUpdate(LonDevice device, int nv_recv, string hex, string value)
        {
            try
            {
                this.BeginInvoke(new Action(() =>
                {
                    while (listView1.Items.Count <= nv_recv)
                    {
                        var item = new ListViewItem(new string[13] { listView1.Items.Count.ToString("X2"), "", "", "", "", "", "", "", "", "", "", "", "" });
                        listView1.Items.Add(item);
                    }
                    var curItem = listView1.Items[nv_recv];
                    int col = 3;
                    curItem.SubItems[col++].Text = hex;
                    curItem.SubItems[col++].Text = value;

                    if (Device.NvConfigTable.ContainsKey(nv_recv))
                    {
                        curItem.SubItems[col++].Text = Device.NvConfigTable[nv_recv].Bound ? "yes" : "no";
                        curItem.SubItems[col++].Text = Device.NvConfigTable[nv_recv].Priority ? "yes" : "no";
                        curItem.SubItems[col++].Text = Device.NvConfigTable[nv_recv].Direction.ToString();
                        curItem.SubItems[col++].Text = Device.NvConfigTable[nv_recv].NetVarSelector.ToString();
                        curItem.SubItems[col++].Text = Device.NvConfigTable[nv_recv].Turnaround ? "yes" : "no";
                        curItem.SubItems[col++].Text = Device.NvConfigTable[nv_recv].Service.ToString();
                        curItem.SubItems[col++].Text = Device.NvConfigTable[nv_recv].Secure ? "yes" : "no";
                        curItem.SubItems[col++].Text = Device.NvConfigTable[nv_recv].Address.ToString();

                        if(Device.NvConfigTable[nv_recv].Bound)
                        {
                            if (Device.NvConfigTable[nv_recv].Direction == LonAPdu.LonAPduDirection.In)
                            {
                                curItem.BackColor = Color.LightGreen;
                            }
                            else
                            {
                                curItem.BackColor = Color.LightPink;
                            }
                        }
                    }
                    listView1.Items[nv_recv].Tag = nv_recv;

                    listView1.SelectedIndices.Clear();
                    listView1.SelectedIndices.Add(nv_recv);
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
                                DestinationNode = Address
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

        private void btnDumpFlash_Click(object sender, EventArgs e)
        {
            ContextMenu menu = new ContextMenu();

            menu.MenuItems.Add(new MenuItem("Firmware", (s, a) =>
            {
                DumpMemory(0x0000, 0x4000, "Firmware");
            }));
            menu.MenuItems.Add(new MenuItem("Application", (s, a) =>
            {
                DumpMemory(0x4000, 0xE800, "Application");
            }));
            menu.MenuItems.Add(new MenuItem("RAM", (s, a) =>
            {
                DumpMemory(0xE800, 0xF000, "RAM");
            }));
            menu.MenuItems.Add(new MenuItem("EEPROM", (s, a) =>
            {
                DumpMemory(0xF000, 0xF200, "EEPROM");
            }));

            menu.Show(this, PointToClient(MousePosition));
        }

        private void DumpMemory(uint start, uint end, string desc)
        {
            Thread readThread = new Thread(() =>
            {
                Device.StopNvScan();
                byte[] mem = Device.ReadMemory(start, (int)(end-start), (p) =>
                {
                    BeginInvoke(new Action(() =>
                    {
                        progressBar1.Value = (int)(p * 100);
                    }));
                });

                Device.StartNvScan();

                BeginInvoke(new Action(() =>
                {
                    if (mem != null && mem.Length > 0)
                    {
                        progressBar1.Value = 0;
                        SaveFileDialog saveFileDialog = new SaveFileDialog();
                        saveFileDialog.Filter = "Binary files (*.bin)|*.bin|All files (*.*)|*.*";
                        saveFileDialog.Title = "Save neuron's " + desc + " as...";
                        saveFileDialog.FileName = desc + ".bin";

                        if (saveFileDialog.ShowDialog() == DialogResult.OK)
                        {
                            try
                            {
                                File.WriteAllBytes(saveFileDialog.FileName, mem);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("Failed to save");
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("Failed to read memory");
                    }
                }));
            });
            readThread.Start();
        }
    }
}
