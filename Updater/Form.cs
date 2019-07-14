using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Xml.Serialization;
using Ionic.Zip;
using Newtonsoft.Json;

namespace UnityModManagerNet.Updater
{
    public partial class UpdaterForm : Form
    {
        private const string UpdateZipFile = "DearUnityModManager.zip";
        private const string ConfigFile = "UnityModManagerConfig.xml";
        private const string ManagerName = "UnityModManager";
        private const string ManagerFile = "UnityModManager.dll";
        private const string ManagerAppName = "DearUnityModManager";
        private const string ManagerAppFile = "DearUnityModManager.exe";
        private const string UpdaterAppFile = "DUMMUpdater.exe";
        private const string CacheFilePostfix = ".cache";

        private readonly HashSet<string> _updaterFileNames = new HashSet<string>
        {
            UpdaterAppFile,
            "Ionic.Zip.dll",
            "Newtonsoft.Json.dll"
        };
        private readonly Dictionary<string, string> _updaterFiles = new Dictionary<string, string>();

        public UpdaterForm()
        {
            InitializeComponent();
            Start();
        }

        public void DoFormClosed()
        {
            foreach (var file in _updaterFiles)
                if (File.Exists(file.Key))
                {
                    File.Copy(file.Key, file.Value, true);
                    File.Delete(file.Key);
                }
        }

        public void Start()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            if (!Utils.HasNetworkConnection())
            {
                status.Text = $"无网络连接！";
                return;
            }
            try
            {
                Config config;
                using (var stream = File.OpenRead(ConfigFile))
                {
                    var serializer = new XmlSerializer(typeof(Config));
                    config = serializer.Deserialize(stream) as Config;
                }
                if (config == null || string.IsNullOrEmpty(config.Repository))
                {
                    status.Text = $"解析配置文件“{ConfigFile}”失败！";
                    return;
                }
                if (File.Exists(UpdateZipFile))
                {
                    File.Delete(UpdateZipFile);
                }
                string result = null;
                using (var wc = new WebClient())
                {
                    wc.Encoding = Encoding.UTF8;
                    result = wc.DownloadString(new Uri(config.Repository));
                }
                var repository = JsonConvert.DeserializeObject<Repository>(result);
                if (repository == null || repository.Releases.Length == 0)
                {
                    status.Text = $"解析仓库配置文件“{config.Repository}”失败！";
                    return;
                }
                var release = repository.Releases.FirstOrDefault(x => x.Id == ManagerName);
                if (File.Exists(ManagerFile))
                {
                    var managerAssembly = Assembly.ReflectionOnlyLoad(File.ReadAllBytes(ManagerFile));
                    if (Utils.ParseVersion(release?.Version) <= managerAssembly.GetName().Version)
                    {
                        status.Text = $"无可用的更新。";
                        return;
                    }
                }
                status.Text = $"正在下载新版本v{release?.Version}……";
                using (var wc = new WebClient())
                {
                    wc.Encoding = Encoding.UTF8;
                    wc.DownloadProgressChanged += Wc_DownloadProgressChanged;
                    wc.DownloadFileCompleted += Wc_DownloadFileCompleted;
                    wc.DownloadFileAsync(new Uri(release?.DownloadUrl), UpdateZipFile);
                }
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
                return;
            }
            if (e.Cancelled) return;
            var success = false;
            try
            {
                foreach (var p in Process.GetProcessesByName(ManagerAppName))
                {
                    status.Text = "正在等待MOD管理器关闭……";
                    p.CloseMainWindow();
                    p.WaitForExit();
                }
                var root = Environment.CurrentDirectory.Replace(ManagerAppName, "");
                using (var zip = ZipFile.Read(UpdateZipFile))
                {
                    foreach (var entry in zip.EntriesSorted)
                    {
                        if (entry.IsDirectory)
                        {
                            Directory.CreateDirectory(Path.Combine(root, entry.FileName));
                        }
                        else
                        {
                            var path = Path.Combine(root, entry.FileName);
                            Directory.CreateDirectory(Path.GetDirectoryName(path));
                            var name = Path.GetFileName(entry.FileName);
                            if (_updaterFileNames.Contains(name))
                            {
                                _updaterFiles[path += CacheFilePostfix] = path;
                                continue;
                            }
                            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
                            {
                                entry.Extract(fs);
                            }
                        }
                    }
                }
                status.Text = "已完成。";
                success = true;
            }
            catch (Exception ex)
            {
                status.Text = ex.Message;
            }
            if (File.Exists(UpdateZipFile))
            {
                File.Delete(UpdateZipFile);
            }
            if (!success) return;
            if (!Utils.IsUnixPlatform() && Process.GetProcessesByName(ManagerAppName).Length == 0)
            {
                if (File.Exists(ManagerAppFile))
                {
                    SetForegroundWindow(Process.Start(ManagerAppFile).MainWindowHandle);
                }
            }
            Application.Exit();
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetForegroundWindow(IntPtr hwnd);
    }
}
