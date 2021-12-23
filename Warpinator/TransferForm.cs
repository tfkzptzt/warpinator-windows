﻿using System;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Warpinator.Controls;

namespace Warpinator
{
    public partial class TransferForm : Form
    {
        readonly Remote remote;
        string[] selectedFiles;

        public TransferForm(Remote r)
        {
            InitializeComponent();

            remote = r ?? throw new ArgumentNullException("Remote must not be null");

            this.Activated += (s, e) => { remote.IncomingTransferFlag = false; };
            UpdateLabels();
            UpdateTransfers();
        }

        IntPtr hIcon = IntPtr.Zero;
        internal void UpdateLabels()
        {
            pictureUser.Image = remote.Picture;
            if (remote.Picture != null)
            {
                var oldIcon = hIcon;
                hIcon = remote.Picture.GetHicon();
                this.Icon = Icon.FromHandle(hIcon);
                if (oldIcon != IntPtr.Zero)
                    Utils.User32.DestroyIcon(oldIcon);
            }
            lblDisplayName.Text = remote.DisplayName;
            lblUserString.Text = remote.UserName + "@" + remote.Hostname;
            lblAddress.Text = remote.Address + ":" + remote.Port;
            lblStatus.Text = remote.Status.ToString();
            if ((remote.Status == RemoteStatus.DISCONNECTED || remote.Status == RemoteStatus.ERROR) && !remote.ServiceAvailable)
                lblStatus.Text += ", unavailable";
            btnReconnect.Visible = remote.Status == RemoteStatus.DISCONNECTED || remote.Status == RemoteStatus.ERROR;
            //pictureStatus

            this.Text = lblUserString.Text;
        }

        internal void UpdateTransfers()
        {
            flowLayoutTransfers.Controls.Clear();
            foreach (var t in remote.Transfers)
            {
                var p = new TransferPanel(t);
                //p.;
                flowLayoutTransfers.Controls.Add(p);
                p.Width = flowLayoutTransfers.ClientSize.Width - 10;
                p.Show();
            }
        }
     
        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Title = "Pick files to send",
                Multiselect = true
            };
            var res = ofd.ShowDialog();
            if (res == DialogResult.OK)
            {
                selectedFiles = ofd.FileNames;
                txtFile.Text = String.Join("; ", selectedFiles.Select((f) => Path.GetFileName(f)));
            }
        }

        private void BtnBrowseDir_Click(object sender, EventArgs e)
        {
            var d = new OpenFoldersDialog
            {
                Title = "Pick folders to send",
                MultiSelect = true
            };
            if (d.Show())
            {
                selectedFiles = d.FileNames;
                txtFile.Text = String.Join("; ", selectedFiles.Select((f) => Path.GetFileName(f)));
            }
        }

        private void BtnSend_Click(object sender, EventArgs e)
        {
            if (selectedFiles == null || selectedFiles.Length == 0)
            {
                MessageBox.Show("No files were selected");
                return;
            }
            Transfer t = new Transfer()
            {
                FilesToSend = selectedFiles.ToList(),
                RemoteUUID = remote.UUID
            };
            t.PrepareSend();
            remote.Transfers.Add(t);
            UpdateTransfers();

            remote.StartSendTransfer(t);
        }

        private void FlowLayoutPanel_ClientSizeChanged(object sender, EventArgs e)
        {
            foreach (Control c in flowLayoutTransfers.Controls)
            {
                c.Width = flowLayoutTransfers.ClientSize.Width - 10;
            }
        }

        private void BtnDlDir_Click(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", String.Format("/n, /e, \"{0}\"", Properties.Settings.Default.DownloadDir));
        }

        private void BtnReconnect_Click(object sender, EventArgs e)
        {
            remote.Connect();
        }

        private void DropTargets_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            else e.Effect = DragDropEffects.None;
        }

        private void FlowLayoutTransfers_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var data = (string[])e.Data.GetData(DataFormats.FileDrop);
                var bak = selectedFiles;
                selectedFiles = data;
                BtnSend_Click(null, null);
                selectedFiles = bak;
            }
        }

        private void TxtFile_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                selectedFiles = (string[])e.Data.GetData(DataFormats.FileDrop);
                txtFile.Text = String.Join("; ", selectedFiles.Select((f) => Path.GetFileName(f)));
            }
        }
    }
}
