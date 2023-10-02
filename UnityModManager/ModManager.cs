using dnlib.DotNet;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

using UnityEngine;
using Debug = UnityEngine.Debug;

namespace UnityModManagerNet;

public partial class UnityModManager
{
    private static readonly Version VER_0 = new();
    private static readonly Version VER_0_13 = new(0, 13);
    private static readonly Version VER_2018_2 = new(2018, 2);
    private static readonly ModuleDefMD thisModuleDef = ModuleDefMD.Load(typeof(UnityModManager).Module);
    private static bool forbidDisableMods;
    /// <summary>
    /// List of all mods
    /// </summary>
    public static readonly List<ModEntry> ModEntries = new();
    /// <summary>
    /// Path to Mods folder
    /// </summary>
    public static string modsPath { get; private set; }
    internal static bool started;
    internal static bool initialized;
    /// <summary>
    ///     Contains version of UnityEngine
    /// </summary>
    public static Version unityVersion { get; private set; }
    /// <summary>
    ///     Contains version of a game, if configured [0.15.0]
    /// </summary>
    public static Version gameVersion { get; private set; } = new();
    /// <summary>
    /// [0.27.0]
    /// </summary>
    internal static readonly List<TextureReplacer.Skin> allSkins = new();
    /// <summary>
    /// Does the OnSessionStart support [0.27.0]
    /// </summary>
    public static bool IsSupportOnSessionStart => !string.IsNullOrEmpty(Config.SessionStartPoint);
    /// <summary>
    /// Does the OnSessionStop support [0.27.0]
    /// </summary>
    public static bool IsSupportOnSessionStop => !string.IsNullOrEmpty(Config.SessionStopPoint);
    /// <summary>
    /// [0.26.0]
    /// </summary>
    public delegate void ToggleModsListen(ModEntry modEntry, bool result);
    /// <summary>
    /// [0.26.0]
    /// </summary>
    public static event ToggleModsListen toggleModsListen;
    /// <summary>
    ///     Contains version of UMM
    /// </summary>
    public static Version Version { get; } = typeof(UnityModManager).Assembly.GetName().Version;
    [Obsolete("请使用modsPath！OldModsPath与0.13之前版本的mod兼容。")]
    public static string OldModsPath = "";
    internal static Param Params { get; set; }
    internal static GameInfo Config { get; set; }
    /// <summary>
    ///     [0.20.0.12]
    /// </summary>
    internal static Action FreezeUI = () => { }, UnFreezeUI = () => { };

    public static void Main()
    {
        AppDomain.CurrentDomain.AssemblyLoad += OnLoad;
    }

    private static void OnLoad(object sender, AssemblyLoadEventArgs args)
    {
        var name = args.LoadedAssembly.GetName().Name;
        if (name != "Assembly-CSharp" && name != "GH.Runtime" && name != "AtomGame" && name != "Game") return;
        AppDomain.CurrentDomain.AssemblyLoad -= OnLoad;
        Injector.Run(true);
    }

    public static bool Initialize()
    {
        if (initialized) return true;
        initialized = true;
        Logger.Clear();
        Logger.Log("正在初始化数据……");
        Logger.Log($"版本：{Version}。");
        try
        {
            Logger.Log(
                $"操作系统：{Environment.OSVersion} {Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE")}。");
            Logger.Log($".Net框架版本：{Environment.Version}。");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
        unityVersion = ParseVersion(Application.unityVersion);
        Logger.Log($"Unity引擎版本：{unityVersion}。");
        if (!Assembly.GetExecutingAssembly().Location.Contains($"Managed{Path.DirectorySeparatorChar}UnityModManager"))
            Logger.Error(@$"在目录{Assembly.GetExecutingAssembly().Location}中发现重复文件！UnityModeManager文件夹只能位于\Game\*Data\Managed\目录中！此文件夹在安装后通过DearUnityModManager.exe自动创建。");
        Config = GameInfo.Load();
        if (Config == null) return false;
        Logger.Log($"游戏名称：{Config.Name}。");
        Logger.NativeLog($"IsSupportOnSessionStart: {IsSupportOnSessionStart}.");
        Logger.NativeLog($"IsSupportOnSessionStop: {IsSupportOnSessionStop}.");
        Params = Param.Load();
        modsPath = Path.Combine(Environment.CurrentDirectory, Config.ModsDirectory);
        if (!Directory.Exists(modsPath))
        {
            var modsPath2 = Path.Combine(Path.GetDirectoryName(Environment.CurrentDirectory) ?? string.Empty, Config.ModsDirectory);

            if (Directory.Exists(modsPath2))
                modsPath = modsPath2;
            else
                Directory.CreateDirectory(modsPath);
        }
        Logger.Log($"Mods路径：{modsPath}。");
        OldModsPath = modsPath;
        KeyBinding.Initialize();
        AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

        return true;
    }

    private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
    {
        var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == args.Name);
        if (assembly != null) return assembly;

        string filename = null;
        if (args.Name.StartsWith("0Harmony12"))
            filename = "0Harmony12.dll";
        else if (args.Name.StartsWith("0Harmony, Version=1.") || args.Name.StartsWith("0Harmony-1.2"))
            filename = "0Harmony-1.2.dll";
        else if (args.Name.StartsWith("0Harmony, Version=2."))
            filename = "0Harmony.dll";

        if (filename == null) return null;
        var filepath = Path.Combine(Path.GetDirectoryName(typeof(UnityModManager).Assembly.Location) ?? string.Empty, filename);
        if (!File.Exists(filepath)) return null;

        try
        {
            return Assembly.LoadFile(filepath);
        }
        catch (Exception e)
        {
            Logger.Error(e.ToString());
        }

        return null;
    }

    public static void Start()
    {
        try
        {
            _Start();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            OpenUnityFileLog();
        }
    }

    private static void _Start()
    {
        if (!Initialize())
        {
            Logger.Log("初始化数据出错，启用已被取消！");
            OpenUnityFileLog();
            return;
        }

        if (started)
        {
            Logger.Log("MOD已经启用，无需再次启用！");
            return;
        }

        started = true;
        ParseGameVersion();
        GameScripts.Init(ModEntries);
        GameScripts.OnBeforeLoadMods();

        if (Directory.Exists(modsPath))
        {
            Logger.Log("正在解析Mods……");
            var mods = new Dictionary<string, ModEntry>();
            var countMods = 0;
            foreach (var dir in Directory.GetDirectories(modsPath))
            {
                var jsonPath = Path.Combine(dir, Config.ModInfo);
                if (!File.Exists(jsonPath))
                    jsonPath = Path.Combine(dir, Config.ModInfo.ToLower());
                if (!File.Exists(jsonPath)) continue;
                countMods++;
                Logger.Log($"正在解析文件“{jsonPath}”……");
                ModEntry modEntry = null;
                try
                {
                    var modInfo = TinyJson.JSONParser.FromJson<ModInfo>(File.ReadAllText(jsonPath));
                    if (string.IsNullOrEmpty(modInfo.Id))
                    {
                        Logger.Error("Id为空！");
                        continue;
                    }
                    if (mods.ContainsKey(modInfo.Id))
                    {
                        Logger.Error($"Id“{modInfo.Id}”已经被另一个MOD占用！");
                        continue;
                    }
                    if (string.IsNullOrEmpty(modInfo.AssemblyName) && File.Exists(Path.Combine(dir, modInfo.Id + ".dll")))
                        modInfo.AssemblyName = modInfo.Id + ".dll";
                    modEntry = new ModEntry(modInfo, dir + Path.DirectorySeparatorChar);
                    mods.Add(modInfo.Id, modEntry);
                }
                catch (Exception exception)
                {
                    Logger.Error($"解析文件“{jsonPath}”失败！");
                    Debug.LogException(exception);
                }
                var trFolder = Path.Combine(dir, "TextureReplacer");
                if (Directory.Exists(trFolder))
                {
                    foreach (string skinDir in Directory.GetDirectories(trFolder))
                    {
                        try
                        {
                            string trJsonPath = Path.Combine(skinDir, "skin.json");
                            TextureReplacer.Skin skin;
                            if (File.Exists(trJsonPath))
                            {
                                skin = TinyJson.JSONParser.FromJson<TextureReplacer.Skin>(File.ReadAllText(trJsonPath));
                            }
                            else
                            {
                                skin = new TextureReplacer.Skin() { Name = new DirectoryInfo(skinDir).Name };
                            }
                            skin.modEntry = modEntry;
                            skin.textures = new Dictionary<string, TextureReplacer.Skin.texture>();
                            modEntry?.Skins.Add(skin);

                            foreach (string file in Directory.GetFiles(skinDir))
                            {
                                if (file.EndsWith("skin.json"))
                                {
                                }
                                else if (file.EndsWith(".png") || file.EndsWith(".jpg"))
                                {
                                    skin.textures[Path.GetFileNameWithoutExtension(file)] = new TextureReplacer.Skin.texture { Path = file };
                                }
                                else
                                {
                                    Logger.Log($"Unsupported file format for '{file}'.");
                                }
                            }
                        }
                        catch (Exception exception)
                        {
                            Logger.Error($"Error");
                            Debug.LogException(exception);
                        }
                    }
                }
            }
            if (mods.Count > 0)
            {
                Logger.Log("正在排序Mods……");
                TopoSort(mods);
                Params.ReadModParams();
                Logger.Log("正在加载Mods……");
                foreach (var mod in ModEntries)
                    if (!mod.Enabled)
                        mod.Logger.Log($"MOD“{mod.Info.Id}”已被禁用！");
                    else
                        mod.Active = true;
            }
            //ApplySkins();
            Logger.Log($"Mods解析完成！成功加载了{ModEntries.Count(x => !x.ErrorOnLoading)}/{countMods}的MOD！{Environment.NewLine}");
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(x => x.ManifestModule.Name == "UnityModManager.dll");
            if (assemblies.Count() > 1)
            {
                Logger.Error($"检测到UnityModManager.dll的额外副本！");
                foreach (var ass in assemblies)
                {
                    Logger.Log($"- {ass.CodeBase}");
                }
                Console.WriteLine();
            }
        }
        else
        {
            Logger.Log($"目录“{modsPath}”不存在！");
        }

        GameScripts.OnAfterLoadMods();
        //if (!UI.Load()) Logger.Error("不能加载UI！");
    }

    static MethodInfo GetTexturePropertyNames = typeof(Material).GetMethod("GetTexturePropertyNames", new Type[] { typeof(List<string>) });
    static List<string> texturePropertyNames = new List<string>();

    internal static void ApplySkins()
    {
        if (unityVersion < VER_2018_2)
            return;

        Logger.Log($"Replacing textures.");

        var materials = Resources.FindObjectsOfTypeAll<Material>();

        foreach (var skin in allSkins)
        {
            if (skin.Conditions.IsEmpty)
            {
                foreach (var mat in materials)
                {
                    texturePropertyNames.Clear();
                    GetTexturePropertyNames.Invoke(mat, new object[] { texturePropertyNames });

                    foreach (var p in texturePropertyNames)
                    {
                        var tex = mat.GetTexture(p);
                        if (tex && !string.IsNullOrEmpty(tex.name) && tex is Texture2D tex2d)
                        {
                            foreach (var kv in skin.textures)
                            {
                                if (tex.name == kv.Key)
                                {
                                    mat.SetTexture(p, kv.Value.Texture);
                                    Logger.Log($"Replaced texture '{tex.name}' in material '{mat.name}'.");
                                    if (!kv.Value.Previous)
                                        kv.Value.Previous = tex2d;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    private static void ParseGameVersion()
    {
        if (string.IsNullOrEmpty(Config.GameVersionPoint)) return;

        try
        {
            Logger.Log("正在解析游戏版本……");

            var version = TryGetValueFromDllPoint(Config.GameVersionPoint)?.ToString();
            if (version == null) return;

            Logger.Log($"已找到游戏版本字符串为“{version}”！");

            gameVersion = ParseVersion(version);
            Logger.Log($"已检测游戏版本为“{gameVersion}”！");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            OpenUnityFileLog();
        }
    }

    private static object TryGetValueFromDllPoint(string point)
    {
        var regex = new Regex(@"^\[(.+\.dll)\](\w+)((?:\.\w+(?:\(\))?)+)$", RegexOptions.IgnoreCase);
        var match = regex.Match(point);

        if (!match.Success)
        {
            Logger.Error($"DLL入口点格式错误：“{point}”！");
            return null;
        }

        var dll = match.Groups[1].Value;
        var path = match.Groups[2].Value;
        var subpaths = match.Groups[3].Value.Trim('.').Split('.');

        var asm = Assembly.Load(dll);
        if (asm == null)
        {
            Logger.Error($"找不到动态链接库文件“{dll}”！");
            return null;
        }

        var cls = asm.GetType(path);
        var i = 0;

        for (; i < subpaths.Length; i++)
        {
            var pathElement = subpaths[i];

            if (pathElement.EndsWith("()")) break;

            path += "." + pathElement;
            var newCls = asm.GetType(path);
            if (newCls != null) cls = newCls;
            else if (cls != null) break;
        }

        if (cls == null)
        {
            Logger.Error($"找不到类“{path}”！");
            return null;
        }
        else if (i == subpaths.Length)
        {
            Logger.Error($"无法提供值，因为“{path}”是类型！");
            return null;
        }

        object instance = null;

        for (var first = i; i < subpaths.Length; i++)
        {
            var pathElement = subpaths[i];

            if (pathElement.EndsWith("()"))
            {
                pathElement = pathElement.Substring(0, pathElement.Length - 2);
            }

            if (!GetValueFromMember(cls, ref instance, pathElement, i == first)) return null;

            if (instance == null)
            {
                Logger.Error($"“{cls.FullName}.{pathElement}”返回了空引用！");
                return null;
            }

            cls = instance.GetType();
        }

        return instance;
    }

    private static bool GetValueFromMember(Type cls, ref object instance, string name, bool _static)
    {
        var flags = BindingFlags.Public | BindingFlags.NonPublic | (_static ? BindingFlags.Static : BindingFlags.FlattenHierarchy | BindingFlags.Instance);

        var field = cls.GetField(name, flags);
        if (field != null)
        {
            instance = field.GetValue(instance);
            return true;
        }

        var property = cls.GetProperty(name, flags);
        if (property != null)
        {
            instance = property.GetValue(instance, null);
            return true;
        }

        var method = cls.GetMethod(name, flags, null, Type.EmptyTypes, null);
        if (method != null)
        {
            instance = method.Invoke(instance, null);
            return true;
        }

        Logger.Error($"类“{cls.FullName}”中不包含{(_static ? "静态" : "非静态")}成员“{name}”");
        return false;
    }

    private static void DFS(string id, IReadOnlyDictionary<string, ModEntry> mods)
    {
        if (ModEntries.Any(m => m.Info.Id == id)) return;
        foreach (var req in mods[id].Requirements.Keys.Where(mods.ContainsKey)) DFS(req, mods);
        foreach (var req in mods[id].LoadAfter.Where(mods.ContainsKey)) DFS(req, mods);
        ModEntries.Add(mods[id]);
    }

    private static void TopoSort(Dictionary<string, ModEntry> mods)
    {
        foreach (var id in mods.Keys) DFS(id, mods);
    }

    public static ModEntry FindMod(string id)
    {
        return ModEntries.FirstOrDefault(x => x.Info.Id == id);
    }

    public static Version GetVersion()
    {
        return Version;
    }

    public static void SaveSettingsAndParams()
    {
        Params.Save();
        foreach (var mod in ModEntries.Where(mod => mod.Active && mod.OnSaveGUI != null))
        {
            try
            {
                mod.OnSaveGUI(mod);
            }
            catch (Exception e)
            {
                mod.Logger.LogException("OnSaveGUI", e);
            }
        }
    }
}
/// <summary>
///     Copies a value from an old assembly to a new one [0.14.0]
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class SaveOnReloadAttribute : Attribute
{
}

/// <summary>
///     Allows reloading [0.14.1]
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class EnableReloadingAttribute : Attribute
{
}