﻿using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Windows.Forms;
using UnityModManagerNet.ConsoleInstaller;

namespace UnityModManagerNet.Installer
{
    public partial class UnityModManagerForm
    {
        private readonly Dictionary<GameInfo, HashSet<UnityModManager.Repository.Release>> _repositories = new();
        private const string RepositoryUrl = "raw.githubusercontent.com";

        private void CheckModUpdates()
        {
            if (selectedGame == null || !HasNetworkConnection()) return;

            if (!_repositories.ContainsKey(selectedGame))
                _repositories.Add(selectedGame, new HashSet<UnityModManager.Repository.Release>());

            var urls = new HashSet<string>();
            foreach (var mod in _mods.Where(mod => !string.IsNullOrEmpty(mod.Repository)))
            {
                urls.Add(mod.Repository);
            }

            if (urls.Count <= 0) return;
            foreach (var url in urls)
            {
                try
                {
                    using var wc = new WebClient {Encoding = System.Text.Encoding.UTF8};
                    wc.DownloadStringCompleted += (sender, e) => { ModUpdates_DownloadStringCompleted(sender, e, selectedGame, url); };
                    wc.DownloadStringAsync(new Uri(url));
                }
                catch (Exception e)
                {
                    ConsoleInstaller.Log.Print(e.Message);
                    ConsoleInstaller.Log.Print($"从网站“{url}”检查MOD新版本失败！");
                }
            }
        }

        private void ModUpdates_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e, GameInfo game, string url)
        {
            if (e.Error != null)
            {
                ConsoleInstaller.Log.Print(e.Error.Message);
                return;
            }

            if (e.Cancelled || string.IsNullOrEmpty(e.Result) || !_repositories.ContainsKey(game)) return;
            try
            {
                var repository = JsonConvert.DeserializeObject<UnityModManager.Repository>(e.Result);
                if (repository == null || repository.Releases == null || repository.Releases.Length == 0)
                    return;

                listMods.Invoke((MethodInvoker)delegate
                {
                    foreach (var v in repository.Releases)
                    {
                        _repositories[game].Add(v);
                    }
                    if (selectedGame == game)
                        RefreshModList();
                });
            }
            catch (Exception ex)
            {
                ConsoleInstaller.Log.Print(ex.Message);
                ConsoleInstaller.Log.Print($"从网站“{url}”检查MOD新版本失败！");
            }
        }

        private void CheckLastVersion()
        {
            if (string.IsNullOrEmpty(config.Repository))
                return;

            ConsoleInstaller.Log.Print("正在检查MOD管理器的新版本……");

            if (!HasNetworkConnection())
            {
                ConsoleInstaller.Log.Print("无法访问网站，请检查防火墙或代理设置！");
                return;
            }

            try
            {
                using var wc = new WebClient {Encoding = System.Text.Encoding.UTF8};
                wc.DownloadStringCompleted += LastVersion_DownloadStringCompleted;
                wc.DownloadStringAsync(new Uri(config.Repository));
            }
            catch (Exception e)
            {
                ConsoleInstaller.Log.Print(e.Message);
                ConsoleInstaller.Log.Print($"下载MOD新版本失败！");
            }
        }

        private void LastVersion_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                ConsoleInstaller.Log.Print(e.Error.Message);
                return;
            }

            if (e.Cancelled || string.IsNullOrEmpty(e.Result)) return;
            try
            {
                var repository = JsonConvert.DeserializeObject<UnityModManager.Repository>(e.Result);
                if (repository?.Releases == null || repository.Releases.Length == 0) return;

                var release = repository.Releases.FirstOrDefault(x => x.Id == nameof(UnityModManager));
                if (release == null || string.IsNullOrEmpty(release.Version)) return;

                var ver = ConsoleInstaller.Utils.ParseVersion(release.Version);
                if (version < ver)
                {
                    btnDownloadUpdate.Text = $@"下载V{release.Version}";
                    ConsoleInstaller.Log.Print($"有可用的新版本。");
                }
                else ConsoleInstaller.Log.Print($"没有可用的新版本。");
            }
            catch (Exception ex)
            {
                ConsoleInstaller.Log.Print(ex.Message);
                ConsoleInstaller.Log.Print($"检查MOD新版本时出错！");
            }
        }

        public static bool HasNetworkConnection()
        {
            try
            {
                using var ping = new Ping();
                var reply = ping.Send(RepositoryUrl, 3000);
                return reply?.Status == IPStatus.Success;
            }
            catch (Exception e)
            {
                ConsoleInstaller.Log.Print(e.Message);
            }

            return false;
        }
    }
}