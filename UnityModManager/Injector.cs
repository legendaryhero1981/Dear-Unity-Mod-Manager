using System;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using Harmony12;

namespace UnityModManagerNet
{
    public class Injector
    {
        static bool usePrefix = false;

        public static void Run(bool doorstop = false)
        {
            try
            {
                _Run(doorstop);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                UnityModManager.OpenUnityFileLog();
            }
        }

        private static void _Run(bool doorstop)
        {
            Console.WriteLine();
            UnityModManager.Logger.Log("正在注入……");

            if (!UnityModManager.Initialize())
            {
                UnityModManager.Logger.Log($"初始化数据出错，注入已被取消！");
                UnityModManager.OpenUnityFileLog();
                return;
            }

            if (!string.IsNullOrEmpty(UnityModManager.Config.StartingPoint))
            {
                if (!doorstop && UnityModManager.Config.StartingPoint == UnityModManager.Config.EntryPoint)
                {
                    UnityModManager.Start();
                }
                else
                {
                    if (TryGetEntryPoint(UnityModManager.Config.StartingPoint, out var @class, out var method, out var place))
                    {
                        usePrefix = (place == "before");
                        var harmony = HarmonyInstance.Create(nameof(UnityModManager));
                        var prefix = typeof(Injector).GetMethod(nameof(Prefix_Start), BindingFlags.Static | BindingFlags.NonPublic);
                        var postfix = typeof(Injector).GetMethod(nameof(Postfix_Start), BindingFlags.Static | BindingFlags.NonPublic);
                        harmony.Patch(method, new HarmonyMethod(prefix), new HarmonyMethod(postfix));
                        UnityModManager.Logger.Log("注入成功！");
                    }
                    else
                    {
                        UnityModManager.Logger.Log("注入失败！");
                        UnityModManager.OpenUnityFileLog();
                        return;
                    }
                }
            }
            else
            {
                UnityModManager.Start();
            }

            if (!string.IsNullOrEmpty(UnityModManager.Config.UIStartingPoint))
            {
                if (TryGetEntryPoint(UnityModManager.Config.UIStartingPoint, out var @class, out var method, out var place))
                {
                    usePrefix = (place == "before");
                    var harmony = HarmonyInstance.Create(nameof(UnityModManager));
                    var prefix = typeof(Injector).GetMethod(nameof(Prefix_Show), BindingFlags.Static | BindingFlags.NonPublic);
                    var postfix = typeof(Injector).GetMethod(nameof(Postfix_Show), BindingFlags.Static | BindingFlags.NonPublic);
                    harmony.Patch(method, new HarmonyMethod(prefix), new HarmonyMethod(postfix));
                }
                else
                {
                    UnityModManager.OpenUnityFileLog();
                    return;
                }
            }
            else if (UnityModManager.UI.Instance)
            {
                UnityModManager.UI.Instance.FirstLaunch();
            }
        }

        static void Prefix_Start()
        {
            if (usePrefix)
                UnityModManager.Start();
        }

        static void Postfix_Start()
        {
            if (!usePrefix)
                UnityModManager.Start();
        }

        static void Prefix_Show()
        {
            if (usePrefix && UnityModManager.UI.Instance)
                UnityModManager.UI.Instance.FirstLaunch();
        }

        static void Postfix_Show()
        {
            if (!usePrefix && UnityModManager.UI.Instance)
                UnityModManager.UI.Instance.FirstLaunch();
        }

        internal static bool TryGetEntryPoint(string str, out Type foundClass, out MethodInfo foundMethod, out string insertionPlace)
        {
            foundClass = null;
            foundMethod = null;
            insertionPlace = null;

            if (!TryParseEntryPoint(str, out string assemblyName, out _, out _, out _)) return false;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.ManifestModule.Name == assemblyName)
                {
                    return TryGetEntryPoint(assembly, str, out foundClass, out foundMethod, out insertionPlace);
                }
            }
            UnityModManager.Logger.Error($"找不到Assembly文件“{assemblyName}”！");

            return false;

        }

        internal static bool TryGetEntryPoint(Assembly assembly, string str, out Type foundClass, out MethodInfo foundMethod, out string insertionPlace)
        {
            foundClass = null;
            foundMethod = null;

            if (!TryParseEntryPoint(str, out _, out var className, out var methodName, out insertionPlace))
            {
                return false;
            }

            foundClass = assembly.GetType(className);
            if (foundClass == null)
            {
                UnityModManager.Logger.Error($"找不到类名称“{className}”！");
                return false;
            }

            foundMethod = foundClass.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            if (foundMethod != null) return true;
            UnityModManager.Logger.Error($"找不到方法名称“{methodName}”！");
            return false;

        }

        internal static bool TryParseEntryPoint(string str, out string assembly, out string @class, out string method, out string insertionPlace)
        {
            assembly = string.Empty;
            @class = string.Empty;
            method = string.Empty;
            insertionPlace = string.Empty;

            var regex = new Regex(@"(?:(?<=\[)(?'assembly'.+(?>\.dll))(?=\]))|(?:(?'class'[\w|\.]+)(?=\.))|(?:(?<=\.)(?'func'\w+))|(?:(?<=\:)(?'mod'\w+))", RegexOptions.IgnoreCase);
            var matches = regex.Matches(str);
            var groupNames = regex.GetGroupNames();

            if (matches.Count > 0)
            {
                foreach (Match match in matches)
                {
                    foreach (var group in groupNames)
                    {
                        if (!match.Groups[@group].Success) continue;
                        switch (@group)
                        {
                            case "assembly":
                                assembly = match.Groups[@group].Value;
                                break;
                            case "class":
                                @class = match.Groups[@group].Value;
                                break;
                            case "func":
                                method = match.Groups[@group].Value;
                                if (method == "ctor")
                                    method = ".ctor";
                                else if (method == "cctor")
                                    method = ".cctor";
                                break;
                            case "mod":
                                insertionPlace = match.Groups[@group].Value.ToLower();
                                break;
                        }
                    }
                }
            }

            var hasError = false;

            if (string.IsNullOrEmpty(assembly))
            {
                hasError = true;
                UnityModManager.Logger.Error("找不到Assembly名称！");
            }

            if (string.IsNullOrEmpty(@class))
            {
                hasError = true;
                UnityModManager.Logger.Error("找不到类名称！");
            }

            if (string.IsNullOrEmpty(method))
            {
                hasError = true;
                UnityModManager.Logger.Error("找不到方法名称！");
            }

            if (hasError)
            {
                UnityModManager.Logger.Error($"解析入口点字符串“{str}”失败！");
                return false;
            }

            return true;
        }
    }
}
