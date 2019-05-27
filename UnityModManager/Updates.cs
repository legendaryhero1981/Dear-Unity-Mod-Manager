using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace UnityModManagerNet
{
    public partial class UnityModManager
    {
        private static void CheckModUpdates()
        {
            Logger.Log("正在检查MOD新版本……");

            if (!HasNetworkConnection())
            {
                Logger.Log("无法访问网站，请检查防火墙或代理设置！");
                return;
            }

            var urls = new HashSet<string>();

            foreach (var modEntry in modEntries)
            {
                if (!string.IsNullOrEmpty(modEntry.Info.Repository))
                {
                    urls.Add(modEntry.Info.Repository);
                }
            }

            if (urls.Count > 0)
            {
                foreach (var url in urls)
                {
                    UI.Instance.StartCoroutine(DownloadString(url, ParseRepository));
                }
            }
        }

        private static void ParseRepository(string json, string url)
        {
            if (string.IsNullOrEmpty(json))
            {
                return;
            }

            try
            {
                var repository = JsonUtility.FromJson<Repository>(json);
                if (repository != null && repository.Releases != null && repository.Releases.Length > 0)
                {
                    foreach (var release in repository.Releases)
                    {
                        if (!string.IsNullOrEmpty(release.Id) && !string.IsNullOrEmpty(release.Version))
                        {
                            var modEntry = FindMod(release.Id);
                            if (modEntry != null)
                            {
                                var ver = ParseVersion(release.Version);
                                if (modEntry.Version < ver && (modEntry.NewestVersion == null || modEntry.NewestVersion < ver))
                                {
                                    modEntry.NewestVersion = ver;
                                }
                            }
                        }
                    }
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
                using (var ping = new System.Net.NetworkInformation.Ping())
                {
                    return ping.Send("raw.githubusercontent.com", 3000).Status == IPStatus.Success;
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

            yield return www.SendWebRequest();

            MethodInfo isError;

            var ver = ParseVersion(Application.unityVersion);
            if (ver.Major >= 2017)
            {
                isError = typeof(UnityWebRequest).GetMethod("get_isNetworkError");
            }
            else
            {
                isError = typeof(UnityWebRequest).GetMethod("get_isError");
            }

            if (isError == null || (bool)isError.Invoke(www, null))
            {
                Logger.Log(www.error);
                Logger.Log($"从网站“{url}”检查MOD新版本时发生了错误。");
                yield break;
            }

            handler(www.downloadHandler.text, url);
        }
    }
}
