using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

using Ping = System.Net.NetworkInformation.Ping;

namespace UnityModManagerNet
{
    public partial class UnityModManager
    {
        private const string RepositoryUrl = "www.nexusmods.com";

        private static void CheckModUpdates()
        {
            Logger.Log("正在检查MOD新版本……");

            if (!HasNetworkConnection())
            {
                Logger.Log("无法访问网站，请检查防火墙或代理设置！");
                return;
            }

            var urls = new HashSet<string>();

            foreach (var modEntry in ModEntries)
                if (!string.IsNullOrEmpty(modEntry.Info.Repository))
                    urls.Add(modEntry.Info.Repository);

            if (urls.Count <= 0) return;
            foreach (var url in urls)
                UI.Instance.StartCoroutine(unityVersion < new System.Version(5, 4)
                    ? DownloadString_5_3(url, ParseRepository)
                    : DownloadString(url, ParseRepository));
        }

        private static void ParseRepository(string json, string url)
        {
            if (string.IsNullOrEmpty(json)) return;

            try
            {
                var repository = json.FromJson<Repository>();
                if (repository?.Releases == null || repository.Releases.Length <= 0) return;
                foreach (var release in repository.Releases)
                    if (!string.IsNullOrEmpty(release.Id) && !string.IsNullOrEmpty(release.Version))
                    {
                        var modEntry = FindMod(release.Id);
                        if (modEntry == null) continue;
                        var ver = ParseVersion(release.Version);
                        if (modEntry.Version < ver &&
                            (modEntry.NewestVersion == null || modEntry.NewestVersion < ver))
                            modEntry.NewestVersion = ver;
                    }
            }
            catch (Exception e)
            {
                Logger.Log($"从网站“{url}”检查MOD新版本时发生了错误。");
                Logger.Log(e.Message);
            }
        }

        public static bool HasNetworkConnection()
        {
            try
            {
                using (var ping = new Ping())
                {
                    return ping.Send(RepositoryUrl, 1000)?.Status == IPStatus.Success;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return false;
        }

        private static IEnumerator DownloadString(string url, UnityAction<string, string> handler)
        {
            var www = UnityWebRequest.Get(url);
            yield return www?.SendWebRequest();

            var ver = ParseVersion(Application.unityVersion);
            var isError = typeof(UnityWebRequest).GetMethod(ver.Major >= 2017 ? "get_isNetworkError" : "get_isError");
            if (isError == null || (bool)isError.Invoke(www, null))
            {
                Logger.Log(www?.error);
                Logger.Log($"Error downloading '{url}'.");
                yield break;
            }

            handler(www?.downloadHandler.text, url);
        }

        private static IEnumerator DownloadString_5_3(string url, UnityAction<string, string> handler)
        {
            var www = new WWW(url);
            yield return www;

            if (!string.IsNullOrEmpty(www.error))
            {
                Logger.Log(www.error);
                Logger.Log($"从网站“{url}”检查MOD新版本时发生了错误。");
                yield break;
            }

            handler(www.text, url);
        }
    }
}