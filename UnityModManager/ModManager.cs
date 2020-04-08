using dnlib.DotNet;

using Harmony12;

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

namespace UnityModManagerNet
{
    public partial class UnityModManager
    {
        private static readonly Version VER_0 = new Version();
        private static readonly Version VER_0_13 = new Version(0, 13);
        private static readonly ModuleDefMD thisModuleDef = ModuleDefMD.Load(typeof(UnityModManager).Module);
        private static bool forbidDisableMods;
        public static readonly List<ModEntry> ModEntries = new List<ModEntry>();
        internal static bool started;
        internal static bool initialized;
        /// <summary>
        ///     Contains version of UnityEngine
        /// </summary>
        public static Version unityVersion { get; private set; }
        /// <summary>
        ///     Contains version of a game, if configured [0.15.0]
        /// </summary>
        public static Version gameVersion { get; private set; } = new Version();
        /// <summary>
        ///     Contains version of UMM
        /// </summary>
        public static Version version { get; } = typeof(UnityModManager).Assembly.GetName().Version;
        internal static string modsPath { get; private set; }
        internal static Param Params { get; set; }
        internal static GameInfo Config { get; set; }
        /// <summary>
        ///     [0.20.0.12]
        /// </summary>
        internal static Action FreezeUI = () => { }, UnFreezeUI = () => { };

        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyLoad += OnLoad;
        }

        private static void OnLoad(object sender, AssemblyLoadEventArgs args)
        {
            if (args.LoadedAssembly.FullName !=
                "Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null") return;
            AppDomain.CurrentDomain.AssemblyLoad -= OnLoad;
            Injector.Run(true);
        }

        public static bool Initialize()
        {
            if (initialized)
                return true;
            initialized = true;
            Logger.Clear();
            Logger.Log("正在初始化数据……");
            Logger.Log($"版本：{version}。");
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
            Config = GameInfo.Load();
            if (Config == null) return false;
            Logger.Log($"游戏名称：{Config.Name}。");
            Params = Param.Load();
            modsPath = Path.Combine(Environment.CurrentDirectory, Config.ModsDirectory);
            if (!Directory.Exists(modsPath))
            {
                var modsPath2 = Path.Combine(Path.GetDirectoryName(Environment.CurrentDirectory), Config.ModsDirectory);

                if (Directory.Exists(modsPath2))
                    modsPath = modsPath2;
                else
                    Directory.CreateDirectory(modsPath);
            }
            Logger.Log($"Mods路径：{modsPath}。");
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            return true;
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == args.Name);
            if (assembly != null)
                return assembly;
            if (!args.Name.StartsWith("0Harmony,")) return null;
            var regex = new Regex(@"Version=(\d+\.\d+)");
            var match = regex.Match(args.Name);
            if (!match.Success) return null;
            var ver = match.Groups[1].Value;
            var filepath = Path.Combine(Path.GetDirectoryName(typeof(UnityModManager).Assembly.Location),
                $"0Harmony-{ver}.dll");
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
            if (!string.IsNullOrEmpty(Config.GameVersionPoint))
                try
                {
                    Logger.Log("正在解析游戏版本……");
                    if (Injector.TryParseEntryPoint(Config.GameVersionPoint, out var assembly, out var className,
                        out var methodName, out _))
                    {
                        var asm = Assembly.Load(assembly);
                        if (asm == null)
                        {
                            Logger.Error($"找不到Assembly文件“{assembly}”！");
                            goto Next;
                        }

                        var foundClass = asm.GetType(className);
                        if (foundClass == null)
                        {
                            Logger.Error($"找不到类名称“{className}”！");
                            goto Next;
                        }

                        var foundMethod = foundClass.GetMethod(methodName,
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                        if (foundMethod == null)
                        {
                            var foundField = foundClass.GetField(methodName,
                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                            if (foundField != null)
                            {
                                gameVersion = ParseVersion(foundField.GetValue(null).ToString());
                                Logger.Log($"已检测游戏版本“{gameVersion}”！");
                            }
                            else
                            {
                                Logger.Error($"找不到方法名称“{methodName}”！");
                            }

                            goto Next;
                        }

                        gameVersion = ParseVersion(foundMethod.Invoke(null, null).ToString());
                        Logger.Log($"已检测游戏版本“{gameVersion}”！");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    OpenUnityFileLog();
                }

            Next:
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
                    if (!File.Exists(Path.Combine(dir, Config.ModInfo)))
                        jsonPath = Path.Combine(dir, Config.ModInfo.ToLower());

                    if (!File.Exists(jsonPath)) continue;
                    countMods++;
                    Logger.Log($"正在解析文件“{jsonPath}”……");
                    try
                    {
                        var modInfo = File.ReadAllText(jsonPath).FromJson<ModInfo>();
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
                        if (string.IsNullOrEmpty(modInfo.AssemblyName))
                            modInfo.AssemblyName = modInfo.Id + ".dll";
                        var modEntry = new ModEntry(modInfo, dir + Path.DirectorySeparatorChar);
                        mods.Add(modInfo.Id, modEntry);
                    }
                    catch (Exception exception)
                    {
                        Logger.Error($"解析文件“{jsonPath}”失败！");
                        Debug.LogException(exception);
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
                Logger.Log(
                    $"Mods解析完成！成功加载了{ModEntries.Count(x => !x.ErrorOnLoading)}/{countMods}的MOD！{Environment.NewLine}");
                var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(x => x.ManifestModule.Name == "UnityModManager.dll");
                if (assemblies.Count() > 1)
                {
                    Logger.Error($"检测到UnityModManager.dll的额外副本！");
                    foreach(var ass in assemblies)
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
            if (!UI.Load()) Logger.Error("不能加载UI！");
        }

        private static void DFS(string id, IReadOnlyDictionary<string, ModEntry> mods)
        {
            if (ModEntries.Any(m => m.Info.Id == id)) return;
            foreach (var req in mods[id].Requirements.Keys.Where(mods.ContainsKey)) DFS(req, mods);
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
            return version;
        }

        public static void SaveSettingsAndParams()
        {
            Params.Save();
            foreach (var mod in ModEntries)
            {
                if (!mod.Active || mod.OnSaveGUI == null) continue;
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

        public class Repository
        {
            public Release[] Releases;

            [Serializable]
            public class Release : IEquatable<Release>
            {
                public string DownloadUrl;
                public string Id;
                public string Version;

                public bool Equals(Release other)
                {
                    return Id.Equals(other.Id);
                }

                public override bool Equals(object obj)
                {
                    if (ReferenceEquals(null, obj)) return false;
                    return obj is Release obj2 && Equals(obj2);
                }

                public override int GetHashCode()
                {
                    return Id.GetHashCode();
                }
            }
        }

        public class ModSettings
        {
            public virtual void Save(ModEntry modEntry)
            {
                Save(this, modEntry);
            }

            public virtual string GetPath(ModEntry modEntry)
            {
                return Path.Combine(modEntry.Path, "Settings.xml");
            }

            public static void Save<T>(T data, ModEntry modEntry) where T : ModSettings, new()
            {
                Save(data, modEntry, null);
            }

            /// <summary>
            ///     [0.20.0]
            /// </summary>
            public static void Save<T>(T data, ModEntry modEntry, XmlAttributeOverrides attributes)
                where T : ModSettings, new()
            {
                var filepath = data.GetPath(modEntry);
                try
                {
                    using (var writer = new StreamWriter(filepath))
                    {
                        var serializer = new XmlSerializer(typeof(T), attributes);
                        serializer.Serialize(writer, data);
                    }
                }
                catch (Exception e)
                {
                    modEntry.Logger.Error($"保存文件“{filepath}”失败！");
                    modEntry.Logger.LogException(e);
                }
            }

            public static T Load<T>(ModEntry modEntry) where T : ModSettings, new()
            {
                var t = new T();
                var filepath = t.GetPath(modEntry);
                if (File.Exists(filepath))
                    try
                    {
                        using (var stream = File.OpenRead(filepath))
                        {
                            var serializer = new XmlSerializer(typeof(T));
                            var result = (T)serializer.Deserialize(stream);
                            return result;
                        }
                    }
                    catch (Exception e)
                    {
                        modEntry.Logger.Error($"读取文件“{filepath}”失败！");
                        modEntry.Logger.LogException(e);
                    }
                return t;
            }

            public static T Load<T>(ModEntry modEntry, XmlAttributeOverrides attributes) where T : ModSettings, new()
            {
                var t = new T();
                var filepath = t.GetPath(modEntry);
                if (!File.Exists(filepath)) return t;
                try
                {
                    using (var stream = File.OpenRead(filepath))
                    {
                        var serializer = new XmlSerializer(typeof(T), attributes);
                        var result = (T)serializer.Deserialize(stream);
                        return result;
                    }
                }
                catch (Exception e)
                {
                    modEntry.Logger.Error($"Can't read {filepath}.");
                    modEntry.Logger.LogException(e);
                }

                return t;
            }
        }

        public class ModInfo : IEquatable<ModInfo>
        {
            public string Author;
            public string AssemblyName;
            public string DisplayName;
            public string EntryMethod;
            public string GameVersion;
            public string HomePage;
            public string Id;
            /// <summary>
            ///     Used for RoR2 game [0.17.0]
            /// </summary>
            [NonSerialized] public bool IsCheat = true;
            public string ManagerVersion;
            public string Repository;
            public string[] Requirements;
            public string Version;
            /// <summary>
            ///  [0.20.0.15]
            /// </summary>
            public string FreezeUI;

            public bool Equals(ModInfo other)
            {
                return Id.Equals(other.Id);
            }

            public static implicit operator bool(ModInfo exists)
            {
                return exists != null;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                return obj is ModInfo modInfo && Equals(modInfo);
            }

            public override int GetHashCode()
            {
                return Id.GetHashCode();
            }
        }

        public partial class ModEntry
        {
            /// <summary>
            ///     Required game version [0.15.0]
            /// </summary>
            public readonly Version GameVersion;

            public readonly ModInfo Info;

            public readonly ModLogger Logger;

            /// <summary>
            ///     Required UMM version
            /// </summary>
            public readonly Version ManagerVersion;

            /// <summary>
            ///     Path to mod folder
            /// </summary>
            public readonly string Path;

            /// <summary>
            ///     Required mods
            /// </summary>
            public readonly Dictionary<string, Version> Requirements = new Dictionary<string, Version>();

            /// <summary>
            ///     Version of a mod
            /// </summary>
            public readonly Version Version;

            /// <summary>
            ///     Displayed in UMM UI. Add <color></color> tag to change colors. Can be used when custom verification game version
            ///     [0.15.0]
            /// </summary>
            public string CustomRequirements = string.Empty;

            /// <summary>
            ///     UI checkbox
            /// </summary>
            public bool Enabled = true;

            /// <summary>
            ///     Not used
            /// </summary>
            public bool HasUpdate = false;

            private bool mActive;

            private readonly Dictionary<long, MethodInfo> mCache = new Dictionary<long, MethodInfo>();

            private bool mFirstLoading = true;

            /// <summary>
            ///     Not used
            /// </summary>
            public Version NewestVersion;

            /// <summary>
            ///     [0.20.0.11]
            /// </summary>
            public readonly ConcurrentStack<Action<ModEntry>> OnModActions = new ConcurrentStack<Action<ModEntry>>();

            /// <summary>
            ///     Called by MonoBehaviour.FixedUpdate [0.13.0]
            /// </summary>
            public Action<ModEntry, float> OnFixedUpdate;

            /// <summary>
            /// Called by MonoBehaviour.OnGUI when mod options are visible.
            /// </summary>
            public Action<ModEntry> OnGUI;

            /// <summary>
            /// Called by MonoBehaviour.OnGUI, always [0.21.0]
            /// </summary>
            public Action<ModEntry> OnFixedGUI;

            /// <summary>
            ///     Called when closing mod GUI [0.16.0]
            /// </summary>
            public Action<ModEntry> OnHideGUI;

            /// <summary>
            ///     Called by MonoBehaviour.LateUpdate [0.13.0]
            /// </summary>
            public Action<ModEntry, float> OnLateUpdate;

            /// <summary>
            ///     Called when the game closes
            /// </summary>
            public Action<ModEntry> OnSaveGUI;

            /// <summary>
            ///     Called when opening mod GUI [0.16.0]
            /// </summary>
            public Action<ModEntry> OnShowGUI;

            /// <summary>
            ///     Called to activate / deactivate the mod
            /// </summary>
            public Func<ModEntry, bool, bool> OnToggle;

            /// <summary>
            ///     Called to unload old data for reloading mod [0.14.0]
            /// </summary>
            public Func<ModEntry, bool> OnUnload;

            /// <summary>
            ///     Called by MonoBehaviour.Update [0.13.0]
            /// </summary>
            public Action<ModEntry, float> OnUpdate;

            public ModEntry(ModInfo info, string path)
            {
                Info = info;
                Path = path;
                Logger = new ModLogger(Info.Id);
                Version = ParseVersion(info.Version);
                ManagerVersion = !string.IsNullOrEmpty(info.ManagerVersion)
                    ? ParseVersion(info.ManagerVersion)
                    : new Version();
                GameVersion = !string.IsNullOrEmpty(info.GameVersion) ? ParseVersion(info.GameVersion) : new Version();

                if (info.Requirements == null || info.Requirements.Length <= 0) return;
                var regex = new Regex(@"(.*)-(\d\.\d\.\d).*");
                foreach (var id in info.Requirements)
                {
                    var match = regex.Match(id);
                    if (match.Success)
                    {
                        Requirements.Add(match.Groups[1].Value, ParseVersion(match.Groups[2].Value));
                        continue;
                    }

                    if (!Requirements.ContainsKey(id))
                        Requirements.Add(id, null);
                }
            }

            public Assembly Assembly { get; private set; }

            //public ModSettings Settings = null;

            /// <summary>
            ///     Show button to reload the mod [0.14.0]
            /// </summary>
            public bool CanReload { get; private set; }

            public bool Started { get; private set; }

            public bool ErrorOnLoading { get; private set; }
            //public bool Enabled => Enabled;

            /// <summary>
            ///     If OnToggle exists
            /// </summary>
            public bool Toggleable => OnToggle != null;

            /// <summary>
            ///     If Assembly is loaded [0.13.1]
            /// </summary>
            public bool Loaded => Assembly != null;

            public bool Active
            {
                get => mActive;
                set
                {
                    if (value && !Loaded)
                    {
                        var stopwatch = Stopwatch.StartNew();
                        Load();
                        Logger.NativeLog($"加载MOD耗时{stopwatch.ElapsedMilliseconds / 1000f:f2}秒！");
                        return;
                    }

                    if (!Started || ErrorOnLoading)
                        return;
                    try
                    {
                        if (value)
                        {
                            if (mActive)
                                return;

                            if (OnToggle == null || OnToggle(this, true))
                            {
                                mActive = true;
                                Logger.Log("已启用MOD！");
                                GameScripts.OnModToggle(this, true);
                            }
                            else
                            {
                                Logger.Log("启用MOD失败！");
                            }
                        }
                        else if (!forbidDisableMods)
                        {
                            if (!mActive || OnToggle == null || !OnToggle(this, false))
                                return;
                            mActive = false;
                            Logger.Log("已禁用MOD！");
                            GameScripts.OnModToggle(this, false);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.LogException("OnToggle", e);
                    }
                }
            }

            public bool Load()
            {
                if (Loaded)
                    return !ErrorOnLoading;
                ErrorOnLoading = false;
                Logger.Log($"MOD版本“{Info.Version}”已加载！");
                if (string.IsNullOrEmpty(Info.AssemblyName))
                {
                    ErrorOnLoading = true;
                    Logger.Error($"“{nameof(Info.AssemblyName)}”为空！");
                }

                if (string.IsNullOrEmpty(Info.EntryMethod))
                {
                    ErrorOnLoading = true;
                    Logger.Error($"“{nameof(Info.EntryMethod)}”为空！");
                }

                if (!string.IsNullOrEmpty(Info.ManagerVersion))
                    if (ManagerVersion > GetVersion())
                    {
                        ErrorOnLoading = true;
                        Logger.Error($"MOD管理器版本必须不低于“{Info.ManagerVersion}”！");
                    }

                if (!string.IsNullOrEmpty(Info.GameVersion))
                    if (gameVersion != VER_0 && GameVersion > gameVersion)
                    {
                        ErrorOnLoading = true;
                        Logger.Error($"游戏版本必须不低于“{Info.GameVersion}”！");
                    }

                if (Requirements.Count > 0)
                    foreach (var item in Requirements)
                    {
                        var id = item.Key;
                        var mod = FindMod(id);
                        if (mod == null)
                        {
                            ErrorOnLoading = true;
                            Logger.Error($"找不到依赖的MOD“{id}”！");
                            continue;
                        }

                        if (item.Value != null && item.Value > mod.Version)
                        {
                            ErrorOnLoading = true;
                            Logger.Error($"依赖的MOD“{id}”版本必须不低于“{item.Value}”！");
                            continue;
                        }

                        if (mod.Active) continue;
                        mod.Enabled = true;
                        mod.Active = true;
                        if (!mod.Active)
                            Logger.Log($"依赖的MOD“{id}”已被禁用！");
                    }

                if (ErrorOnLoading)
                    return false;
                var assemblyPath = System.IO.Path.Combine(Path, Info.AssemblyName);
                if (File.Exists(assemblyPath))
                {
                    try
                    {
                        var assemblyCachePath = assemblyPath;
                        var cacheExists = false;
                        if (mFirstLoading)
                        {
                            var fi = new FileInfo(assemblyPath);
                            var hash = (ushort)((long)fi.LastWriteTimeUtc.GetHashCode() + version.GetHashCode() +
                                                 ManagerVersion.GetHashCode()).GetHashCode();
                            assemblyCachePath = $"{assemblyPath}.{hash}.cache";
                            cacheExists = File.Exists(assemblyCachePath);
                            if (!cacheExists)
                                foreach (var filepath in Directory.GetFiles(Path, "*.cache"))
                                    try
                                    {
                                        File.Delete(filepath);
                                    }
                                    catch (Exception)
                                    {
                                    }
                        }

                        if (ManagerVersion >= VER_0_13)
                        {
                            if (mFirstLoading)
                            {
                                if (!cacheExists) File.Copy(assemblyPath, assemblyCachePath, true);
                                Assembly = Assembly.LoadFile(assemblyCachePath);
                                foreach (var type in Assembly.GetTypes())
                                {
                                    if (!type.GetCustomAttributes(typeof(EnableReloadingAttribute), true).Any())
                                        continue;
                                    CanReload = true;
                                    break;
                                }
                            }
                            else
                            {
                                Assembly = Assembly.Load(File.ReadAllBytes(assemblyPath));
                            }
                        }
                        else
                        {
                            if (!cacheExists)
                            {
                                var modDef = ModuleDefMD.Load(File.ReadAllBytes(assemblyPath));
                                foreach (var item in modDef.GetTypeRefs())
                                    if (item.FullName == "UnityModManagerNet.UnityModManager")
                                        item.ResolutionScope = new AssemblyRefUser(thisModuleDef.Assembly);
                                modDef.Write(assemblyCachePath);
                            }

                            Assembly = Assembly.LoadFile(assemblyCachePath);
                        }

                        mFirstLoading = false;
                    }
                    catch (Exception exception)
                    {
                        ErrorOnLoading = true;
                        Logger.Error($"读取文件“{assemblyPath}”失败！");
                        Logger.LogException(exception);
                        return false;
                    }

                    try
                    {
                        var param = new object[] { this };
                        var types = new[] { typeof(ModEntry) };
                        if (FindMethod(Info.EntryMethod, types, false) == null)
                        {
                            param = null;
                            types = null;
                        }

                        if (!Invoke(Info.EntryMethod, out var result, param, types) ||
                            result != null && (bool)result == false)
                        {
                            ErrorOnLoading = true;
                            Logger.Log($"加载入口方法“{Info.EntryMethod}”失败！");
                        }
                    }
                    catch (Exception e)
                    {
                        ErrorOnLoading = true;
                        Logger.Log(e.ToString());
                        return false;
                    }

                    Started = true;
                    if (ErrorOnLoading) return false;
                    Active = true;
                    return true;
                }

                ErrorOnLoading = true;
                Logger.Error($"找不到文件“{assemblyPath}”！");
                return false;
            }

            internal void Reload()
            {
                if (!Started || !CanReload)
                    return;
                try
                {
                    var assemblyPath = System.IO.Path.Combine(Path, Info.AssemblyName);
                    var reflAssembly = Assembly.ReflectionOnlyLoad(File.ReadAllBytes(assemblyPath));
                    if (reflAssembly.GetName().Version == Assembly.GetName().Version)
                    {
                        Logger.Log("不需要重载，此版本与前一版本完全相同！");
                        return;
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e.ToString());
                    return;
                }

                OnSaveGUI?.Invoke(this);
                Logger.Log("重载中……");
                if (Toggleable)
                {
                    var b = forbidDisableMods;
                    forbidDisableMods = false;
                    Active = false;
                    forbidDisableMods = b;
                }
                else
                {
                    mActive = false;
                }

                try
                {
                    if (!Active && (OnUnload == null || OnUnload.Invoke(this)))
                    {
                        mCache.Clear();
                        typeof(Traverse).GetField("Cache", BindingFlags.Static | BindingFlags.NonPublic)
                            ?.SetValue(null, new AccessCache());
                        typeof(Harmony.Traverse).GetField("Cache", BindingFlags.Static | BindingFlags.NonPublic)
                            ?.SetValue(null, new Harmony.AccessCache());
                        var oldAssembly = Assembly;
                        Assembly = null;
                        Started = false;
                        ErrorOnLoading = false;
                        OnToggle = null;
                        OnGUI = null;
                        OnShowGUI = null;
                        OnHideGUI = null;
                        OnSaveGUI = null;
                        OnUnload = null;
                        OnUpdate = null;
                        OnFixedUpdate = null;
                        OnLateUpdate = null;
                        CustomRequirements = null;
                        if (!Load()) return;
                        var allTypes = oldAssembly.GetTypes();
                        foreach (var type in allTypes)
                        {
                            var t = Assembly.GetType(type.FullName);
                            if (t == null) continue;
                            foreach (var field in type.GetFields(
                                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                            {
                                if (!field.GetCustomAttributes(typeof(SaveOnReloadAttribute), true).Any()) continue;
                                var f = t.GetField(field.Name);
                                if (f == null) continue;
                                Logger.Log($"已复制字段“{field.DeclaringType?.Name}.{field.Name}”。");
                                try
                                {
                                    if (field.FieldType != f.FieldType)
                                    {
                                        if (field.FieldType.IsEnum && f.FieldType.IsEnum)
                                            f.SetValue(null, Convert.ToInt32(field.GetValue(null)));
                                    }
                                    else
                                    {
                                        f.SetValue(null, field.GetValue(null));
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error(ex.ToString());
                                }
                            }
                        }

                        return;
                    }

                    if (Active)
                    {
                        Logger.Log("必须禁用。");
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e.ToString());
                }

                Logger.Log("已取消重载！");
            }

            public bool Invoke(string namespaceClassnameMethodname, out object result, object[] param = null,
                Type[] types = null)
            {
                result = null;
                try
                {
                    var methodInfo = FindMethod(namespaceClassnameMethodname, types);
                    if (methodInfo != null)
                    {
                        result = methodInfo.Invoke(null, param);
                        return true;
                    }
                }
                catch (Exception exception)
                {
                    Logger.Error($"尝试调用方法“{namespaceClassnameMethodname}”失败！");
                    Logger.LogException(exception);
                }

                return false;
            }

            private MethodInfo FindMethod(string namespaceClassnameMethodname, Type[] types, bool showLog = true)
            {
                long key = namespaceClassnameMethodname.GetHashCode();
                if (types != null) key = types.Aggregate(key, (current, val) => current + val.GetHashCode());

                if (mCache.TryGetValue(key, out var methodInfo)) return methodInfo;
                if (Assembly != null)
                {
                    string classString = null;
                    string methodString = null;
                    var pos = namespaceClassnameMethodname.LastIndexOf('.');
                    if (pos != -1)
                    {
                        classString = namespaceClassnameMethodname.Substring(0, pos);
                        methodString = namespaceClassnameMethodname.Substring(pos + 1);
                    }
                    else
                    {
                        if (showLog)
                            Logger.Error($"方法名称“{namespaceClassnameMethodname}”错误！");

                        goto Exit;
                    }

                    var type = Assembly.GetType(classString);
                    if (type != null)
                    {
                        if (types == null)
                            types = new Type[0];

                        methodInfo = type.GetMethod(methodString,
                            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, types,
                            null);
                        if (methodInfo == null && showLog)
                            Logger.Log(types.Length > 0
                                ? $"未找到方法“{namespaceClassnameMethodname}[{string.Join(", ", types.Select(x => x.Name).ToArray())}]”！"
                                : $"未找到方法“{namespaceClassnameMethodname}”！");
                    }
                    else if (showLog)
                    {
                        Logger.Error($"未找到类“{classString}”！");
                    }
                }
                else if (showLog)
                {
                    UnityModManager.Logger.Error($"不能找到方法“{namespaceClassnameMethodname}”，MOD“{Info.Id}”未加载！");
                }

                Exit:
                mCache[key] = methodInfo;
                return methodInfo;
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
}