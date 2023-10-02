extern alias PathfinderKingmaker;

using dnlib.DotNet;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace UnityModManagerNet
{
    public partial class UnityModManager
    {
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
            /// <summary>
            /// Does the mod use a dll [0.26.0]
            /// </summary>
            public bool HasAssembly => !string.IsNullOrEmpty(Info.AssemblyName) || !string.IsNullOrEmpty(Info.EntryMethod);
            /// <summary>
            /// [0.27.0]
            /// </summary>
            internal readonly List<TextureReplacer.Skin> Skins = new();
            /// <summary>
            /// [0.26.0]
            /// </summary>
            public delegate void ToggleModsListen(ModEntry modEntry, bool result);
            /// <summary>
            /// [0.26.0]
            /// </summary>
            public static event ToggleModsListen toggleModsListen;
            /// <summary>
            /// Called by SessionStartPoint usually after all loaded data
            /// Must be preconfigured
            /// Check UnityModManager.IsSupportOnSessionStart before 
            /// [0.27.0]
            /// </summary>
            public Action<ModEntry> OnSessionStart = null;
            /// <summary>
            /// Called by SessionStopPoint
            /// Must be preconfigured
            /// Check UnityModManager.IsSupportOnSessionStop before 
            /// [0.27.0]
            /// </summary>
            public Action<ModEntry> OnSessionStop = null;
            /// <summary>
            ///     Show button to reload the mod [0.14.0]
            /// </summary>
            public bool CanReload { get; private set; }
            public bool Started { get; private set; }
            public bool ErrorOnLoading { get; private set; }
            /// <summary>
            /// Return TRUE if OnToggle exists
            /// </summary>
            public bool Toggleable => OnToggle != null || !HasAssembly;
            /// <summary>
            /// Return TRUE if Assembly is loaded [0.13.1]
            /// </summary>
            public bool Loaded => Assembly != null || !HasAssembly && Started;
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
            /// <summary>
            /// Activates or deactivates the mod by calling OnToggle if present
            /// </summary>
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
                                toggleModsListen?.Invoke(this, true);
                            }
                            else
                            {
                                Logger.Log("激活MOD失败！");
                                Logger.NativeLog($"执行方法OnToggle(true)失败！");
                            }
                        }
                        else if (!forbidDisableMods)
                        {
                            if (!_mActive)
                                return;
                            if (OnToggle != null && OnToggle(this, false) || !HasAssembly)
                            {
                                _mActive = false;
                                Logger.Log($"已禁用MOD！");
                                GameScripts.OnModToggle(this, false);
                                toggleModsListen?.Invoke(this, false);
                            }
                            else if (OnToggle != null)
                            {
                                Logger.NativeLog($"执行方法OnToggle(true)失败！");
                            }
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

                if (!string.IsNullOrEmpty(Info.AssemblyName) && string.IsNullOrEmpty(Info.EntryMethod))
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

                if (!HasAssembly)
                {
                    Started = true;
                    Active = true;
                    return true;
                }

                var assemblyPath = System.IO.Path.Combine(Path, Info.AssemblyName);
                var pdbPath = assemblyPath.Replace(".dll", ".pdb");

                var replacedAssemblyPath = string.Empty;
                var commandArgs = Environment.GetCommandLineArgs();
                var idx = Array.IndexOf(commandArgs, $"--umm-{Info.Id}-assembly-path");
                if (idx != -1 && commandArgs.Length > idx + 1)
                {
                    replacedAssemblyPath = assemblyPath = commandArgs[idx + 1];
                }

                if (File.Exists(assemblyPath))
                {
                    if (!string.IsNullOrEmpty(replacedAssemblyPath))
                    {
                        try
                        {
                            Assembly = Assembly.LoadFile(assemblyPath);
                            _mFirstLoading = false;
                        }
                        catch (Exception exception)
                        {
                            ErrorOnLoading = true;
                            this.Logger.Error($"Error loading file '{assemblyPath}'.");
                            this.Logger.LogException(exception);
                            return false;
                        }
                    }
                    else
                    {
                        try
                        {
                            var assemblyCachePath = assemblyPath;
                            var pdbCachePath = pdbPath;
                            var cacheExists = false;

                            if (_mFirstLoading)
                            {
                                var fi = new FileInfo(assemblyPath);
                                var hash = (ushort)((long)fi.LastWriteTimeUtc.GetHashCode() + Version.GetHashCode() +
                                                    ManagerVersion.GetHashCode()).GetHashCode();
                                assemblyCachePath = assemblyPath + $".{hash}.cache";
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
                                    Assembly = File.Exists(pdbPath)
                                        ? Assembly.Load(buf.ToArray(), File.ReadAllBytes(pdbPath))
                                        : Assembly.Load(buf.ToArray());
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
                                        if (item.Name == "modsPath" &&
                                            item.Class.FullName == "UnityModManagerNet.UnityModManager")
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
                }
                else
                {
                    ErrorOnLoading = true;
                    Logger.Error($"找不到文件“{assemblyPath}”！");
                }
                return false;
            }

            internal void LoadSkins()
            {
                foreach (var skin in Skins)
                {
                    if (!allSkins.Contains(skin))
                        allSkins.Add(skin);

                    foreach (var kv in skin.textures)
                    {
                        var tex = Utils.LoadTexture(kv.Value.Path);
                        kv.Value.Texture = tex;
                    }
                }
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
            /// <summary>
            /// Looks for a word match within boundaries and ignores case [0.26.0]
            /// </summary>
            public bool HasContentType(string str)
            {
                return !string.IsNullOrEmpty(Info.ContentType) && new Regex($@"\b{str}\b", RegexOptions.IgnoreCase).IsMatch(Info.ContentType);
            }
        }
    }
}