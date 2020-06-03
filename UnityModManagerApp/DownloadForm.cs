using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace UnityModManagerNet.Installer
{
    public partial class DownloadForm : Form
    {
        public UnityModManager.Repository.Release Release;
        public string TempFilepath { get; private set; }

        public DownloadForm()
        {
            InitializeComponent();
        }

        public DownloadForm(UnityModManager.Repository.Release release)
        {
            this.Release = release;
            InitializeComponent();
            Start();
        }

        private void Start()
        {
            try
            {
                var dir = Path.Combine(Path.GetTempPath(), "DearUnityModManager");

                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                TempFilepath = Path.Combine(dir, $"{Release.Id}.zip");
                status.Text = $@"正在下载 {Release.Id} {Release.Version} ……";
                using var wc = new WebClient {Encoding = Encoding.UTF8};
                wc.DownloadProgressChanged += Wc_DownloadProgressChanged;
                wc.DownloadFileCompleted += Wc_DownloadFileCompleted;
                wc.DownloadFileAsync(new Uri(Release.DownloadUrl), TempFilepath);
            }
            catch (Exception e)
            {
                status.Text = e.Message;
                Log.Print(e.Message);
            }
        }

        private void Wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
        }

        private void Wc_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                status.Text = e.Error.Message;
                Log.Print(e.Error.Message);
                return;
            }

            if (e.Cancelled) return;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
