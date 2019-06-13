using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.Net;
using System.Net.NetworkInformation;

namespace UnityModManagerNet.Installer
{
    public partial class UnityModManagerForm : Form
    {
        private readonly Dictionary<GameInfo, HashSet<UnityModManager.Repository.Release>> repositories = new Dictionary<GameInfo, HashSet<UnityModManager.Repository.Release>>();
        private static readonly string _repositoryUrl = "raw.githubusercontent.com";

        private void CheckModUpdates()
        {
            if (selectedGame == null)
                return;

            if (!HasNetworkConnection())
            {
                return;
            }

            if (!repositories.ContainsKey(selectedGame))
                repositories.Add(selectedGame, new HashSet<UnityModManager.Repository.Release>());

            var urls = new HashSet<string>();
            foreach (var mod in mods)
            {
                if (!string.IsNullOrEmpty(mod.Repository))
                {
                    urls.Add(mod.Repository);
                }
            }

            if (urls.Count > 0)
            {
                foreach (var url in urls)
                {
                    try
                    {
                        using (var wc = new WebClient())
                        {
                            wc.Encoding = System.Text.Encoding.UTF8;
                            wc.DownloadStringCompleted += (sender, e) => { ModUpdates_DownloadStringCompleted(sender, e, selectedGame, url); };
                            wc.DownloadStringAsync(new Uri(url));
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Print(e.Message);
                        Log.Print($"从网站“{url}”检查MOD新版本失败！");
                    }
                }
            }
        }

        private void ModUpdates_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e, GameInfo game, string url)
        {
            if (e.Error != null)
            {
                Log.Print(e.Error.Message);
                return;
            }

            if (!e.Cancelled && !string.IsNullOrEmpty(e.Result) && repositories.ContainsKey(game))
            {
                try
                {
                    var repository = JsonConvert.DeserializeObject<UnityModManager.Repository>(e.Result);
                    if (repository == null || repository.Releases == null || repository.Releases.Length == 0)
                        return;

                    listMods.Invoke((MethodInvoker)delegate
                    {
                        foreach (var v in repository.Releases)
                        {
                            repositories[game].Add(v);
                        }
                        if (selectedGame == game)
                            RefreshModList();
                    });
                }
                catch (Exception ex)
                {
                    Log.Print(ex.Message);
                    Log.Print($"从网站“{url}”检查MOD新版本失败！");
                }
            }
        }

        private void CheckLastVersion()
        {
            if (string.IsNullOrEmpty(config.Repository))
                return;

            Log.Print("正在检查MOD管理器的新版本……");

            if (!HasNetworkConnection())
            {
                Log.Print("无法访问网站，请检查防火墙或代理设置！");
                return;
            }

            try
            {
                using (var wc = new WebClient())
                {
                    wc.Encoding = System.Text.Encoding.UTF8;
                    wc.DownloadStringCompleted += LastVersion_DownloadStringCompleted;
                    wc.DownloadStringAsync(new Uri(config.Repository));
                }
            }
            catch (Exception e)
            {
                Log.Print(e.Message);
                Log.Print($"下载MOD新版本失败！");
            }
        }

        private void LastVersion_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                Log.Print(e.Error.Message);
                return;
            }

            if (!e.Cancelled && !string.IsNullOrEmpty(e.Result))
            {
                try
                {
                    var repository = JsonConvert.DeserializeObject<UnityModManager.Repository>(e.Result);
                    if (repository == null || repository.Releases == null || repository.Releases.Length == 0)
                        return;

                    var release = repository.Releases.FirstOrDefault(x => x.Id == nameof(UnityModManager));
                    if (release != null && !string.IsNullOrEmpty(release.Version))
                    {
                        var ver = Utils.ParseVersion(release.Version);
                        if (version < ver)
                        {
                            btnDownloadUpdate.Text = $"下载v{release.Version}";
                            Log.Print($"有可用的新版本。");
                        }
                        else
                        {
                            Log.Print($"没有可用的新版本。");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Print(ex.Message);
                    Log.Print($"检查MOD新版本时出错！");
                }
            }
        }

        public static bool HasNetworkConnection()
        {
            try
            {
                using (var ping = new Ping())
                {
                    var reply = ping.Send(_repositoryUrl, 1000);
                    return reply?.Status == IPStatus.Success;
                }
            }
            catch (Exception e)
            {
                Log.Print(e.Message);
            }

            return false;
        }
    }
}