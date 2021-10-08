using Ionic.Zip;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace UnityModManagerNet.Installer
{
    public partial class DownloadExtraFiles : Form
    {
        const string downloadFile = "ExtraFiles.zip";
        string gamePath;

        public DownloadExtraFiles()
        {
            InitializeComponent();
        }

        public DownloadExtraFiles(string url, string gamePath)
        {
            this.gamePath = gamePath;
            InitializeComponent();
            Start(url);
        }

        public void Start(string url)
        {
            if (string.IsNullOrEmpty(gamePath))
            {
                status.Text = "请选择游戏目录。";
                ConsoleInstaller.Log.Print(status.Text);
                return;
            }

            try
            {
                status.Text = "下载中……";
                ConsoleInstaller.Log.Print(status.Text);
                using var wc = new WebClient();
                wc.Encoding = Encoding.UTF8;
                wc.DownloadProgressChanged += Wc_DownloadProgressChanged;
                wc.DownloadFileCompleted += Wc_DownloadFileCompleted;
                wc.DownloadFileAsync(new Uri(url), downloadFile);
            }
            catch (Exception e)
            {
                status.Text = e.Message;
            }
        }

        private void Wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
        }

        private void Wc_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                status.Text = e.Error.Message;
                ConsoleInstaller.Log.Print(e.Error.Message);
                return;
            }
            if (!e.Cancelled)
            {
                var success = false;
                try
                {
                    using (var zip = ZipFile.Read(downloadFile))
                    {
                        foreach (var entry in zip.EntriesSorted)
                        {
                            if (entry.IsDirectory)
                            {
                                Directory.CreateDirectory(Path.Combine(gamePath, entry.FileName));
                            }
                            else
                            {
                                Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(gamePath, entry.FileName)));
                                using FileStream fs = new FileStream(Path.Combine(gamePath, entry.FileName), FileMode.Create, FileAccess.Write);
                                entry.Extract(fs);
                            }
                        }
                    }

                    status.Text = "下载完毕。";
                    ConsoleInstaller.Log.Print(status.Text);
                    success = true;
                }
                catch (Exception ex)
                {
                    status.Text = ex.Message;
                    ConsoleInstaller.Log.Print(ex.Message);
                }

                if (File.Exists(downloadFile))
                {
                    File.Delete(downloadFile);
                }

                if (success)
                {
                    DialogResult = DialogResult.OK;
                    Close();
                }
            }
        }
    }
}
