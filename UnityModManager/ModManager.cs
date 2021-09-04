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

namespace UnityModManagerNet
{
    public partial class UnityModManager
    {
        private static readonly Version VER_0 = new();
        private static readonly Version VER_0_13 = new(0, 13);
        private static readonly ModuleDefMD thisModuleDef = ModuleDefMD.Load(typeof(UnityModManager).Module);
        private static bool forbidDisableMods;
        public static readonly List<ModEntry> ModEntries = new();
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
        ///     Contains version of UMM
        /// </summary>
        public static Version Version { get; } = typeof(UnityModManager).Assembly.GetName().Version;
        public static string modsPath { get; private set; }
        [Obsolete("请使用modsPath！OldModsPath与0.13之前版本的mod兼容。")]
        public static string OldModsPath = "";
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
            var name = args.LoadedAssembly.GetName().Name;
            if (name != "Assembly-CSharp" && name != "assembly_valheim") return;
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
                Logger.Error(@"UnityModeManager文件夹只能位于\Game\*Data\Managed\目录中！此文件夹在安装后通过DearUnityModManager.exe自动创建。");
            Config = GameInfo.Load();
            if (Config == null) return false;
            Logger.Log($"游戏名称：{Config.Name}。");
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
            if (!UI.Load()) Logger.Error("不能加载UI！");
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
                    using var writer = new StreamWriter(filepath);
                    var serializer = new XmlSerializer(typeof(T), attributes);
                    serializer.Serialize(writer, data);
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
                if (!File.Exists(filepath)) return t;
                try
                {
                    using var stream = File.OpenRead(filepath);
                    var serializer = new XmlSerializer(typeof(T));
                    var result = (T)serializer.Deserialize(stream);
                    return result;
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
                    using var stream = File.OpenRead(filepath);
                    var serializer = new XmlSerializer(typeof(T), attributes);
                    var result = (T)serializer.Deserialize(stream);
                    return result;
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
            /// <summary>
            ///  [0.22.5.31]
            /// </summary>
            public string[] LoadAfter;

            public bool Equals(ModInfo other)
            {
                return Id.Equals(other?.Id);
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
            public readonly ModInfo Info;
            public readonly ModLogger Logger;
            private readonly Dictionary<long, MethodInfo> _mCache = new();
            /// <summary>
            ///     Path to mod folder
            /// </summary>
            public readonly string Path;
            /// <summary>
            ///     Version of a mod
            /// </summary>
            public readonly Version Version;
            /// <summary>
            ///     Newest version  of a mod
            /// </summary>
            public Version NewestVersion;
            /// <summary>
            ///     Required UMM version
            /// </summary>
            public readonly Version ManagerVersion;
            /// <summary>
            ///     Required game version [0.15.0]
            /// </summary>
            public readonly Version GameVersion;
            /// <summary>
            ///     Required mods
            /// </summary>
            public readonly Dictionary<string, Version> Requirements = new();
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
            ///     [0.20.0.11]
            /// </summary>
            public readonly ConcurrentStack<Action<ModEntry>> OnModActions = new();
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
            public Assembly Assembly { get; private set; }
            //public ModSettings Settings = null;
            /// <summary>
            ///     Show button to reload the mod [0.14.0]
            /// </summary>
            public bool CanReload { get; private set; }
            public bool Started { get; private set; }
            public bool ErrorOnLoading { get; private set; }
            /// <summary>
            ///     If OnToggle exists
            /// </summary>
            public bool Toggleable => OnToggle != null;
            /// <summary>
            ///     If Assembly is loaded [0.13.1]
            /// </summary>
            public bool Loaded => Assembly != null;
            /// <summary>
            /// List of mods after which this mod should be loaded [0.22.5.31]
            /// </summary>
            public readonly List<string> LoadAfter = new();

            public bool HasUpdate = false;
            private bool _mFirstLoading = true;
            private int _mReloaderCount;

            public ModEntry(ModInfo info, string path)
            {
                Info = info;
                Path = path;
                Logger = new ModLogger(Info.Id);
                Version = ParseVersion(info.Version);
                ManagerVersion = !string.IsNullOrEmpty(info.ManagerVersion)
                    ? ParseVersion(info.ManagerVersion) : !string.IsNullOrEmpty(Config.MinimalManagerVersion) ? ParseVersion(Config.MinimalManagerVersion)
                        : new Version();
                GameVersion = !string.IsNullOrEmpty(info.GameVersion) ? ParseVersion(info.GameVersion) : new Version();

                if (info.Requirements == null || info.Requirements.Length <= 0) return;

                var regex = new Regex(@"(.*)-(\d+\.\d+\.\d+).*");
                foreach (var id in info.Requirements)
                {
                    var match = regex.Match(id);
                    if (match.Success)
                    {
                        Requirements.Add(match.Groups[1].Value, ParseVersion(match.Groups[2].Value));
                        continue;
                    }
                    if (!Requirements.ContainsKey(id)) Requirements.Add(id, null);
                }

                if (info.LoadAfter != null && info.LoadAfter.Length > 0) LoadAfter.AddRange(info.LoadAfter);
            }

            private bool _mActive;
            public bool Active
            {
                get => _mActive;
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
                            if (_mActive)
                                return;

                            if (OnToggle == null || OnToggle(this, true))
                            {
                                _mActive = true;
                                Logger.Log("已激活MOD！");
                                GameScripts.OnModToggle(this, true);
                            }
                            else
                            {
                                Logger.Log("激活MOD失败！");
                            }
                        }
                        else if (!forbidDisableMods)
                        {
                            if (!_mActive || OnToggle == null || !OnToggle(this, false))
                                return;
                            _mActive = false;
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
                if (Loaded) return !ErrorOnLoading;

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
                        if (!mod.Active) Logger.Log($"依赖的MOD“{id}”已被禁用！");
                    }

                if (LoadAfter.Count > 0)
                    foreach (var id in LoadAfter)
                    {
                        var mod = FindMod(id);
                        if (mod == null)
                        {
                            Logger.Log($"可选的MOD“{id}”不存在。");
                            continue;
                        }

                        if (mod.Active || !mod.Enabled) continue;
                        mod.Active = true;
                        if (!mod.Active) Logger.Log($"可选的MOD“{id}”已启用，但未激活。");
                    }

                if (ErrorOnLoading) return false;

                var assemblyPath = System.IO.Path.Combine(Path, Info.AssemblyName);
                var pdbPath = assemblyPath.Replace(".dll", ".pdb");

                if (File.Exists(assemblyPath))
                {
                    try
                    {
                        var assemblyCachePath = assemblyPath;
                        var pdbCachePath = pdbPath;
                        var cacheExists = false;

                        if (_mFirstLoading)
                        {
                            var fi = new FileInfo(assemblyPath);
                            var hash = (ushort)((long)fi.LastWriteTimeUtc.GetHashCode() + UnityModManager.Version.GetHashCode() +
                                                 ManagerVersion.GetHashCode()).GetHashCode();
                            assemblyCachePath = $"{assemblyPath}.{hash}.cache";
                            pdbCachePath = assemblyCachePath + ".pdb";
                            cacheExists = File.Exists(assemblyCachePath);

                            if (!cacheExists)
                                foreach (var filepath in Directory.GetFiles(Path, "*.cache*"))
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
                            if (_mFirstLoading)
                            {
                                if (!cacheExists)
                                {
                                    var hasChanges = false;
                                    var modDef = ModuleDefMD.Load(File.ReadAllBytes(assemblyPath));
                                    foreach (var item in modDef.GetAssemblyRefs())
                                    {
                                        if (!item.FullName.StartsWith("0Harmony, Version=1.")) continue;
                                        item.Name = "0Harmony-1.2";
                                        hasChanges = true;
                                    }
                                    if (hasChanges)
                                        modDef.Write(assemblyCachePath);
                                    else
                                        File.Copy(assemblyPath, assemblyCachePath, true);
                                    if (File.Exists(pdbPath))
                                        File.Copy(pdbPath, pdbCachePath, true);
                                }
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
                                var modDef = ModuleDefMD.Load(File.ReadAllBytes(assemblyPath));
                                modDef.Assembly.Name += ++_mReloaderCount;
                                using var buf = new MemoryStream();
                                modDef.Write(buf);
                                Assembly = File.Exists(pdbPath) ? Assembly.Load(buf.ToArray(), File.ReadAllBytes(pdbPath)) : Assembly.Load(buf.ToArray());
                            }
                        }
                        else
                        {
                            if (!cacheExists)
                            {
                                var hasChanges = false;
                                var modDef = ModuleDefMD.Load(File.ReadAllBytes(assemblyPath));
                                foreach (var item in modDef.GetTypeRefs())
                                    if (item.FullName == "UnityModManagerNet.UnityModManager")
                                    {
                                        item.ResolutionScope = new AssemblyRefUser(thisModuleDef.Assembly);
                                        hasChanges = true;
                                    }
                                foreach (var item in modDef.GetMemberRefs().Where(member => member.IsFieldRef))
                                    if (item.Name == "modsPath" && item.Class.FullName == "UnityModManagerNet.UnityModManager")
                                    {
                                        item.Name = "OldModsPath";
                                        hasChanges = true;
                                    }
                                foreach (var item in modDef.GetAssemblyRefs())
                                {
                                    if (!item.FullName.StartsWith("0Harmony, Version=1.")) continue;
                                    item.Name = "0Harmony-1.2";
                                    hasChanges = true;
                                }
                                if (hasChanges)
                                    modDef.Write(assemblyCachePath);
                                else
                                    File.Copy(assemblyPath, assemblyCachePath, true);
                            }

                            Assembly = Assembly.LoadFile(assemblyCachePath);
                        }

                        _mFirstLoading = false;
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
                if (!Started || !CanReload) return;

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
                    _mActive = false;

                try
                {
                    if (!Active && (OnUnload == null || OnUnload.Invoke(this)))
                    {
                        _mCache.Clear();
                        var accessCacheType = typeof(HarmonyLib.Traverse).Assembly.GetType("HarmonyLib.AccessCache");
                        var accessCache = typeof(HarmonyLib.Traverse).GetField("Cache", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null);
                        string[] fields = { "declaredFields", "declaredProperties", "declaredMethods", "inheritedFields", "inheritedProperties", "inheritedMethods" };
                        foreach (var field in fields)
                        {
                            var accessCacheDict = (System.Collections.IDictionary)accessCacheType.GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(accessCache);
                            accessCacheDict?.Clear();
                        }
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

                if (_mCache.TryGetValue(key, out var methodInfo)) return methodInfo;
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
                _mCache[key] = methodInfo;
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