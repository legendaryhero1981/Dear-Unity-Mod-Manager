using HarmonyLib;
using System;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace UnityModManagerNet;

public class Injector
{
    private static bool startUiWithManager;

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

        Fixes.Apply();

        if (!string.IsNullOrEmpty(UnityModManager.Config.UIStartingPoint) && UnityModManager.Config.UIStartingPoint != UnityModManager.Config.StartingPoint)
        {
            if (TryGetEntryPoint(UnityModManager.Config.UIStartingPoint, out var @class, out var method, out var place))
            {
                var usePrefix = (place == "before");
                var harmony = new HarmonyLib.Harmony(nameof(UnityModManager));
                var prefix = typeof(Injector).GetMethod(nameof(Prefix_Show), BindingFlags.Static | BindingFlags.NonPublic);
                var postfix = typeof(Injector).GetMethod(nameof(Postfix_Show), BindingFlags.Static | BindingFlags.NonPublic);
                harmony.Patch(method, usePrefix ? new HarmonyMethod(prefix) : null, !usePrefix ? new HarmonyMethod(postfix) : null);
            }
            else
            {
                UnityModManager.OpenUnityFileLog();
                return;
            }
        }
        else
            startUiWithManager = true;

        if (!string.IsNullOrEmpty(UnityModManager.Config.StartingPoint))
        {
            if (!doorstop && UnityModManager.Config.StartingPoint == UnityModManager.Config.EntryPoint)
            {
                UnityModManager.Start();
                if (startUiWithManager)
                    RunUI();
            }
            else
            {
                if (TryGetEntryPoint(UnityModManager.Config.StartingPoint, out var @class, out var method, out var place))
                {
                    var usePrefix = (place == "before");
                    var harmony = new Harmony(nameof(UnityModManager));
                    var prefix = typeof(Injector).GetMethod(nameof(Prefix_Start), BindingFlags.Static | BindingFlags.NonPublic);
                    var postfix = typeof(Injector).GetMethod(nameof(Postfix_Start), BindingFlags.Static | BindingFlags.NonPublic);
                    harmony.Patch(method, usePrefix ? new HarmonyMethod(prefix) : null, !usePrefix ? new HarmonyMethod(postfix) : null);
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
        else if (startUiWithManager)
        {
            UnityModManager.Logger.Error($"不能载入UI！UIStartingPoint节点没有定义！");
            UnityModManager.OpenUnityFileLog();
            return;
        }
        else
            UnityModManager.Start();

        if (!string.IsNullOrEmpty(UnityModManager.Config.TextureReplacingPoint))
        {
            if (TryGetEntryPoint(UnityModManager.Config.TextureReplacingPoint, out var @class, out var method, out var place))
            {
                var usePrefix = (place == "before");
                var harmony = new HarmonyLib.Harmony(nameof(UnityModManager));
                var prefix = typeof(Injector).GetMethod(nameof(Prefix_TextureReplacing), BindingFlags.Static | BindingFlags.NonPublic);
                var postfix = typeof(Injector).GetMethod(nameof(Postfix_TextureReplacing), BindingFlags.Static | BindingFlags.NonPublic);
                harmony.Patch(method, usePrefix ? new HarmonyMethod(prefix) : null, !usePrefix ? new HarmonyMethod(postfix) : null);
            }
            else
            {
                UnityModManager.OpenUnityFileLog();
            }
        }

        if (!string.IsNullOrEmpty(UnityModManager.Config.SessionStartPoint))
        {
            if (TryGetEntryPoint(UnityModManager.Config.SessionStartPoint, out var @class, out var method, out var place))
            {
                var usePrefix = (place == "before");
                var harmony = new HarmonyLib.Harmony(nameof(UnityModManager));
                var prefix = typeof(Injector).GetMethod(nameof(Prefix_SessionStart), BindingFlags.Static | BindingFlags.NonPublic);
                var postfix = typeof(Injector).GetMethod(nameof(Postfix_SessionStart), BindingFlags.Static | BindingFlags.NonPublic);
                harmony.Patch(method, usePrefix ? new HarmonyMethod(prefix) : null, !usePrefix ? new HarmonyMethod(postfix) : null);
            }
            else
            {
                UnityModManager.Config.SessionStartPoint = null;
                UnityModManager.OpenUnityFileLog();
            }
        }

        if (!string.IsNullOrEmpty(UnityModManager.Config.SessionStopPoint))
        {
            if (TryGetEntryPoint(UnityModManager.Config.SessionStopPoint, out var @class, out var method, out var place))
            {
                var usePrefix = (place == "before");
                var harmony = new HarmonyLib.Harmony(nameof(UnityModManager));
                var prefix = typeof(Injector).GetMethod(nameof(Prefix_SessionStop), BindingFlags.Static | BindingFlags.NonPublic);
                var postfix = typeof(Injector).GetMethod(nameof(Postfix_SessionStop), BindingFlags.Static | BindingFlags.NonPublic);
                harmony.Patch(method, usePrefix ? new HarmonyMethod(prefix) : null, !usePrefix ? new HarmonyMethod(postfix) : null);
            }
            else
            {
                UnityModManager.Config.SessionStopPoint = null;
                UnityModManager.OpenUnityFileLog();
            }
        }
    }

    static void RunUI()
    {
        if (!UnityModManager.UI.Load())
            UnityModManager.Logger.Error($"不能加载UI！");
        UnityModManager.UI.Instance.FirstLaunch();
    }

    static void Prefix_Start()
    {
        UnityModManager.Start();
        if (startUiWithManager)
            RunUI();
    }

    static void Postfix_Start()
    {
        UnityModManager.Start();
        if (startUiWithManager)
            RunUI();
    }

    static void Prefix_Show()
    {
        if (!UnityModManager.UI.Load())
        {
            UnityModManager.Logger.Error($"不能加载UI！");
        }
        if (!UnityModManager.UI.Instance)
        {
            UnityModManager.Logger.Error("类 UnityModManager.UI 不存在！");
            return;
        }
        UnityModManager.UI.Instance.FirstLaunch();
    }

    static void Postfix_Show()
    {
        if (!UnityModManager.UI.Load())
        {
            UnityModManager.Logger.Error($"不能加载UI！");
        }
        if (!UnityModManager.UI.Instance)
        {
            UnityModManager.Logger.Error("类 UnityModManager.UI 不存在！");
            return;
        }
        UnityModManager.UI.Instance.FirstLaunch();
    }

    static void Prefix_TextureReplacing()
    {
        //UnityModManager.ApplySkins();
    }

    static void Postfix_TextureReplacing()
    {
        //UnityModManager.ApplySkins();
    }
    static void Prefix_SessionStart()
    {
        foreach (var mod in UnityModManager.ModEntries)
        {
            if (mod.Active && mod.OnSessionStart != null)
            {
                try
                {
                    mod.OnSessionStart.Invoke(mod);
                }
                catch (Exception e)
                {
                    mod.Logger.LogException("OnSessionStart", e);
                }
            }
        }
    }

    static void Postfix_SessionStart()
    {
        Prefix_SessionStart();
    }

    static void Prefix_SessionStop()
    {
        foreach (var mod in UnityModManager.ModEntries)
        {
            if (mod.Active && mod.OnSessionStop != null)
            {
                try
                {
                    mod.OnSessionStop.Invoke(mod);
                }
                catch (Exception e)
                {
                    mod.Logger.LogException("OnSessionStop", e);
                }
            }
        }
    }

    static void Postfix_SessionStop()
    {
        Prefix_SessionStop();
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

        try
        {
            var asm = Assembly.Load(assemblyName);
            return TryGetEntryPoint(asm, str, out foundClass, out foundMethod, out insertionPlace);
        }
        catch (Exception e)
        {
            UnityModManager.Logger.Error($"文件“{assemblyName}”加载失败！");
            UnityModManager.Logger.LogException(e);
        }

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