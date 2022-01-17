using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LonScan
{
    public partial class HexForm : Form
    {
        public byte[] Data = null;

        public HexForm(byte[] data = null)
        {
            InitializeComponent();
            if (data != null)
            {
                Data = data;
                string hex = BitConverter.ToString(data).Replace("-", " ");
                txtHex.Text = hex;
            }
        }

        public HexForm(string data)
        {
            InitializeComponent();
            if (data != null)
            {
                Data = StringToByteArray(data.Replace(" ", ""));
                string hex = BitConverter.ToString(Data).Replace("-", " ");
                txtHex.Text = hex;
            }
        }

        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        private void btnWrite_Click(object sender, EventArgs e)
        {
            string hex = txtHex.Text.Replace(" ", "");

            if((hex.Length % 2) != 0)
            {
                return;
            }

            Data = StringToByteArray(hex);

            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
