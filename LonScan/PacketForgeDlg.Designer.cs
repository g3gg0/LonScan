namespace LonScan
{
    partial class PacketForgeDlg
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PacketForgeDlg));
            this.cmbFrameBytes = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.txtTxDump = new System.Windows.Forms.TextBox();
            this.btnTx = new System.Windows.Forms.Button();
            this.txtRxDump = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.SuspendLayout();
            // 
            // cmbFrameBytes
            // 
            this.cmbFrameBytes.FormattingEnabled = true;
            this.cmbFrameBytes.Location = new System.Drawing.Point(76, 18);
            this.cmbFrameBytes.Name = "cmbFrameBytes";
            this.cmbFrameBytes.Size = new System.Drawing.Size(551, 21);
            this.cmbFrameBytes.TabIndex = 0;
            this.cmbFrameBytes.TextChanged += new System.EventHandler(this.cmbFrameBytes_TextChanged);
            this.cmbFrameBytes.KeyDown += new System.Windows.Forms.KeyEventHandler(this.cmbFrameBytes_KeyDown);
            this.cmbFrameBytes.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.cmbFrameBytes_KeyPress);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 21);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(64, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Frame bytes";
            // 
            // txtTxDump
            // 
            this.txtTxDump.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtTxDump.Font = new System.Drawing.Font("Lucida Console", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtTxDump.Location = new System.Drawing.Point(3, 16);
            this.txtTxDump.Multiline = true;
            this.txtTxDump.Name = "txtTxDump";
            this.txtTxDump.ReadOnly = true;
            this.txtTxDump.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtTxDump.Size = new System.Drawing.Size(397, 437);
            this.txtTxDump.TabIndex = 2;
            // 
            // btnTx
            // 
            this.btnTx.Location = new System.Drawing.Point(633, 17);
            this.btnTx.Name = "btnTx";
            this.btnTx.Size = new System.Drawing.Size(75, 23);
            this.btnTx.TabIndex = 3;
            this.btnTx.Text = "Send";
            this.btnTx.UseVisualStyleBackColor = true;
            this.btnTx.Click += new System.EventHandler(this.btnTx_Click);
            // 
            // txtRxDump
            // 
            this.txtRxDump.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtRxDump.Font = new System.Drawing.Font("Lucida Console", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtRxDump.Location = new System.Drawing.Point(3, 16);
            this.txtRxDump.Multiline = true;
            this.txtRxDump.Name = "txtRxDump";
            this.txtRxDump.ReadOnly = true;
            this.txtRxDump.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtRxDump.Size = new System.Drawing.Size(393, 437);
            this.txtRxDump.TabIndex = 2;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.txtTxDump);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox1.Location = new System.Drawing.Point(0, 0);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(403, 456);
            this.groupBox1.TabIndex = 4;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "TX Packet structure";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.txtRxDump);
            this.groupBox2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox2.Location = new System.Drawing.Point(0, 0);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(399, 456);
            this.groupBox2.TabIndex = 5;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "RX Packet structure";
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.label1);
            this.groupBox3.Controls.Add(this.cmbFrameBytes);
            this.groupBox3.Controls.Add(this.btnTx);
            this.groupBox3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox3.Location = new System.Drawing.Point(4, 4);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(806, 44);
            this.groupBox3.TabIndex = 6;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Forge a packet";
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(4, 4);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.groupBox1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.groupBox2);
            this.splitContainer1.Size = new System.Drawing.Size(806, 456);
            this.splitContainer1.SplitterDistance = 403;
            this.splitContainer1.TabIndex = 7;
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainer2.IsSplitterFixed = true;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.groupBox3);
            this.splitContainer2.Panel1.Padding = new System.Windows.Forms.Padding(4);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.splitContainer1);
            this.splitContainer2.Panel2.Padding = new System.Windows.Forms.Padding(4);
            this.splitContainer2.Size = new System.Drawing.Size(814, 520);
            this.splitContainer2.SplitterDistance = 52;
            this.splitContainer2.TabIndex = 8;
            // 
            // PacketForgeDlg
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(814, 520);
            this.Controls.Add(this.splitContainer2);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(740, 300);
            this.Name = "PacketForgeDlg";
            this.Text = "Packet Forge";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ComboBox cmbFrameBytes;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtTxDump;
        private System.Windows.Forms.Button btnTx;
        private System.Windows.Forms.TextBox txtRxDump;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.SplitContainer splitContainer2;
    }
}