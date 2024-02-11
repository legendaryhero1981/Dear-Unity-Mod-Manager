using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace UnityModManagerNet.ConsoleInstaller
{
    internal class UnityModManagerConsole
    {
        private const string REG_PATH = @"HKEY_CURRENT_USER\Software\UnityModManager";
        public static readonly Version VER_0_22 = new(0, 22);
        internal static readonly Version HARMONY_VER = new(2, 0);

        private const string DLL_UMM = "UnityModManager.dll";
        private static readonly Dictionary<int, string> UmmDlls = new()
        {
            {2018,"UnityModManager2018.dll"},
            {2019,"UnityModManager2019.dll"},
            {2021,"UnityModManager2021.dll"}
        };

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetForegroundWindow(IntPtr hwnd);

        [Flags]
        private enum Actions { Install = 1, Update = 2, Delete = 4, Restore = 8 }

        private static void Main(string[] args)
        {
            if (CheckApplicationAlreadyRunning(out var process))
            {
                if (!Utils.IsUnixPlatform())
                    SetForegroundWindow(process.MainWindowHandle);
                return;
            }

            Init();
            Console.ReadKey();
        }

        private static bool CheckApplicationAlreadyRunning(out Process result)
        {
            result = null;
            var id = Process.GetCurrentProcess().Id;
            var name = Process.GetCurrentProcess().ProcessName;
            foreach (var p in Process.GetProcessesByName(name))
            {
                if (p.Id == id) continue;
                result = p;
                return true;
            }
            return false;
        }

        [Flags]
        private enum LibIncParam { Normal = 0, Skip = 1, Minimal_lt_0_22 = 2 }

        private static readonly Dictionary<string, LibIncParam> libraryFiles = new()
        {
            { "background.jpg", LibIncParam.Normal },
            { "0Harmony.dll", LibIncParam.Normal },
            { "0Harmony12.dll", LibIncParam.Minimal_lt_0_22 },
            { "0Harmony-1.2.dll", LibIncParam.Minimal_lt_0_22 },
            { "dnlib.dll", LibIncParam.Normal },
            { "System.Xml.dll", LibIncParam.Normal },
            { DLL_UMM, LibIncParam.Normal }
        };

        private static List<string> libraryPaths;

        private static Config config;
        private static Param param;
        private static Version version;

        private static string gamePath;
        private static string managedPath;
        private static string managerPath;
        private static string entryAssemblyPath;
        private static string injectedEntryAssemblyPath;
        private static string managerAssemblyPath;
        private static string entryPoint;
        private static string injectedEntryPoint;
        private static string gameExePath;
        private static string unityPlayerPath;
        private static string doorstopFilename = "winhttp.dll";
        private static string doorstopConfigFilename = "doorstop_config.ini";
        private static string doorstopPath;
        private static string doorstopConfigPath;

        private static ModuleDefMD assemblyDef;
        private static ModuleDefMD injectedAssemblyDef;
        private static ModuleDefMD managerDef;

        private static GameInfo selectedGame;
        private static Param.GameParam selectedGameParams;

        private static void Init()
        {
            Log.Init<Log>();
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            if (!Utils.IsUnixPlatform())
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var registry = asm.GetType("Microsoft.Win32.Registry");
                    if (registry == null) continue;
                    var getValue = registry.GetMethod("GetValue", new Type[] { typeof(string), typeof(string), typeof(object) });
                    if (getValue != null)
                    {
                        var exePath = getValue.Invoke(null, new object[] { REG_PATH, "ExePath", string.Empty }) as string;
                        if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
                        {
                            var setValue = registry.GetMethod("SetValue", new Type[] { typeof(string), typeof(string), typeof(object) });
                            if (setValue != null)
                            {
                                setValue.Invoke(null, new object[] { REG_PATH, "ExePath", Path.Combine(Application.StartupPath, "UnityModManager.exe") });
                                setValue.Invoke(null, new object[] { REG_PATH, "Path", Application.StartupPath });
                            }
                        }
                    }
                    break;
                }
            }

            version = typeof(UnityModManager).Assembly.GetName().Version;
            config = Config.Load();
            param = Param.Load();

            if (config is { GameInfo: { Length: > 0 } })
            {
                config.GameInfo = config.GameInfo.OrderBy(x => x.Name).ToArray();

                if (!string.IsNullOrEmpty(param.LastSelectedGame))
                {
                    selectedGame = config.GameInfo.FirstOrDefault(x => x.Name == param.LastSelectedGame);
                    selectedGameParams = param.GetGameParam(selectedGame);
                    gamePath = selectedGameParams.Path;
                    if (File.Exists(Path.Combine(gamePath, "UnityPlayer.dll")))
                        unityPlayerPath = Path.Combine(gamePath, "UnityPlayer.dll");
                }

                if (selectedGame != null)
                {
                    Log.Print($"已选择游戏“{selectedGame}”。是否要更改所选内容？是：<按y或Y键>否：<按其它键>");
                    var k = Console.ReadKey();
                    if (k.Key == ConsoleKey.Y)
                        SelectGame();
                }
                else
                    SelectGame();
            }
            else
            {
                Log.Print($"解析配置文件“{Config.filename}”时出错！");
                Close();
                return;
            }

            ReadGameAssets();
        }

        private static void SelectGame()
        {
            var i = 1;
            foreach (var g in config.GameInfo)
            {
                Log.Print($"{i++} — {g}");
            }
            Log.Print($"请输入一个序号，取值范围为：1-{config.GameInfo.Length}。");
        ReadAgain:
            Log.Print("序号：");
            var l = Console.ReadLine();
            if (string.IsNullOrEmpty(l))
            {
                Close();
                return;
            }
            if (int.TryParse(l, out var n))
            {
                if (n > 0 && n <= config.GameInfo.Length)
                {
                    selectedGame = config.GameInfo[n - 1];
                }
                else
                {
                    Log.Print($"请输入一个序号，取值范围为：1-{config.GameInfo.Length}。");
                    goto ReadAgain;
                }
            }
            else
            {
                Log.Print($"序号必须为自然数！");
                goto ReadAgain;
            }
            Log.Print($"选择的游戏为“{selectedGame}”。");
            if (!IsValid(selectedGame))
            {
                Close();
            }
            else
            {
                selectedGameParams = param.GetGameParam(selectedGame);
                param.LastSelectedGame = selectedGame.Name;
            }
        }

        private static void Close()
        {
            Log.Print("终止。");
            Process.GetCurrentProcess().CloseMainWindow();
        }

        private static bool IsValid(GameInfo gameInfo)
        {
            var ignoreFields = new List<string>
            {
                nameof(GameInfo.GameExe),
                nameof(GameInfo.GameName),
                nameof(GameInfo.GameVersionPoint),
                nameof(GameInfo.GameScriptName),
                nameof(GameInfo.StartingPoint),
                nameof(GameInfo.UIStartingPoint),
                nameof(GameInfo.TextureReplacingPoint),
                nameof(GameInfo.SessionStartPoint),
                nameof(GameInfo.SessionStopPoint),
                nameof(GameInfo.OldPatchTarget),
                nameof(GameInfo.Comment),
                nameof(GameInfo.MinimalManagerVersion),
                nameof(GameInfo.FixBlackUI),
                nameof(GameInfo.ExtraFilesUrl)
            };

            var prefix = (!string.IsNullOrEmpty(gameInfo.Name) ? $"[{gameInfo.Name}]" : "[?]");
            var hasError = false;
            foreach (var field in typeof(GameInfo).GetFields())
            {
                if (field.IsStatic || !field.IsPublic || ignoreFields.Exists(x => x == field.Name)) continue;
                var value = field.GetValue(gameInfo);
                if (value != null && value.ToString() != "") continue;
                hasError = true;
                Log.Print($"配置节点“{prefix}”的子节点“{field.Name}”为空！");
            }

            return !hasError && (string.IsNullOrEmpty(gameInfo.EntryPoint) || Utils.TryParseEntryPoint(gameInfo.EntryPoint, out _)) && (string.IsNullOrEmpty(gameInfo.StartingPoint) || Utils.TryParseEntryPoint(gameInfo.StartingPoint, out _)) && (string.IsNullOrEmpty(gameInfo.UIStartingPoint) || Utils.TryParseEntryPoint(gameInfo.UIStartingPoint, out _)) && (string.IsNullOrEmpty(gameInfo.TextureReplacingPoint) || Utils.TryParseEntryPoint(gameInfo.TextureReplacingPoint, out _)) && (string.IsNullOrEmpty(gameInfo.SessionStartPoint) || Utils.TryParseEntryPoint(gameInfo.SessionStartPoint, out _)) && (string.IsNullOrEmpty(gameInfo.SessionStopPoint) || Utils.TryParseEntryPoint(gameInfo.SessionStopPoint, out _)) && (string.IsNullOrEmpty(gameInfo.OldPatchTarget) || Utils.TryParseEntryPoint(gameInfo.OldPatchTarget, out _));
        }

        private static void ReadGameAssets()
        {
            gamePath = "";
            if (string.IsNullOrEmpty(selectedGameParams.Path) || !Directory.Exists(selectedGameParams.Path))
            {
                var result = Utils.FindGameFolder(selectedGame.Folder);
                if (string.IsNullOrEmpty(result))
                {
                    Log.Print($"游戏目录“{selectedGame.Folder}”不存在！");
                    SelectGameFolder();
                }
                else
                {
                    selectedGameParams.Path = result;
                    Log.Print($"已检查到游戏安装目录为“{result}”，要改变目录吗？是：<按y或Y键>否：<按其它键>");
                    var k = Console.ReadKey(true);
                    if (k.Key == ConsoleKey.Y)
                        SelectGameFolder();
                }
            }

            if (!Utils.IsDirectoryWritable(selectedGameParams.Path))
            {
                SelectGameFolder();
            }

            if (!Utils.IsUnixPlatform() && !Directory.GetFiles(selectedGameParams.Path, "*.exe", SearchOption.TopDirectoryOnly).Any())
            {
                Log.Print("请选择exe文件所在的游戏目录。");
                SelectGameFolder();
            }

            if (Utils.IsMacPlatform() && !selectedGameParams.Path.EndsWith(".app"))
            {
                Log.Print("请选择名称以“.app”结尾的游戏目录。");
                SelectGameFolder();
            }

            if (!string.IsNullOrEmpty(selectedGame.Comment))
            {
                Log.Print(selectedGame.Comment);
            }

            param.Sync(config.GameInfo);
            param.Save();
            Utils.TryParseEntryPoint(selectedGame.EntryPoint, out var assemblyName);
            gamePath = selectedGameParams.Path;
            if (File.Exists(Path.Combine(gamePath, "UnityPlayer.dll")))
                unityPlayerPath = Path.Combine(gamePath, "UnityPlayer.dll");

            if (File.Exists(Path.Combine(gamePath, "GameAssembly.dll")))
            {
                Log.Print("尚不支持使用IL2CPP编译的游戏版本！");
                return;
            }

            managedPath = Utils.FindManagedFolder(gamePath);
            if (managedPath == null)
            {
                Log.Print("请选择目录名称以“Data”结尾的游戏目录。");
                return;
            }

            Log.Print($"检测到Managed目录路径为“{managedPath}”。");
            managerPath = Path.Combine(managedPath, nameof(UnityModManager));
            entryAssemblyPath = Path.Combine(managedPath, assemblyName);
            managerAssemblyPath = Path.Combine(managerPath, typeof(UnityModManager).Module.Name);
            entryPoint = selectedGame.EntryPoint;
            injectedEntryPoint = selectedGame.EntryPoint;

            if (File.Exists(Path.Combine(managedPath, "System.Xml.dll")))
                libraryFiles["System.Xml.dll"] = LibIncParam.Skip;

            var gameSupportVersion = !string.IsNullOrEmpty(selectedGame.MinimalManagerVersion) ? Utils.ParseVersion(selectedGame.MinimalManagerVersion) : VER_0_22;
            libraryPaths = new List<string>();
            foreach (var item in libraryFiles.Where(item => (item.Value & LibIncParam.Skip) <= 0 && ((item.Value & LibIncParam.Minimal_lt_0_22) <= 0 || gameSupportVersion < VER_0_22)))
                libraryPaths.Add(Path.Combine(managerPath, item.Key));

            if (!string.IsNullOrEmpty(selectedGame.GameExe))
            {
                if (selectedGame.GameExe.Contains('*'))
                    foreach (var file in new DirectoryInfo(gamePath).GetFiles(selectedGame.GameExe, SearchOption.TopDirectoryOnly))
                        selectedGame.GameExe = file.Name;
                gameExePath = Path.Combine(gamePath, selectedGame.GameExe);
            }
            else
                gameExePath = string.Empty;

            var path = new DirectoryInfo(Application.StartupPath).FullName;
            if (path.StartsWith(gamePath))
            {
                Log.Print("DUMM目录不能放在游戏主目录及其子目录下，请先关闭DUMM，再将DUMM目录移动到单独的目录下重试！");
                return;
            }
        Refresh:
            doorstopPath = Path.Combine(gamePath, doorstopFilename);
            doorstopConfigPath = Path.Combine(gamePath, doorstopConfigFilename);
            injectedEntryAssemblyPath = entryAssemblyPath;
            assemblyDef = null;
            injectedAssemblyDef = null;
            managerDef = null;

            try
            {
                assemblyDef = ModuleDefMD.Load(File.ReadAllBytes(entryAssemblyPath));
            }
            catch (Exception e)
            {
                Log.Print(e.ToString() + Environment.NewLine + entryAssemblyPath);
                Close();
                return;
            }

            var useOldPatchTarget = false;
            GameInfo.filepathInGame = Path.Combine(managerPath, "Config.xml");
            if (File.Exists(GameInfo.filepathInGame))
            {
                var gameConfig = GameInfo.ImportFromGame();
                if (gameConfig == null || !Utils.TryParseEntryPoint(gameConfig.EntryPoint, out assemblyName))
                {
                    Close();
                    return;
                }
                injectedEntryPoint = gameConfig.EntryPoint;
                injectedEntryAssemblyPath = Path.Combine(managedPath, assemblyName);
            }
            else if (!string.IsNullOrEmpty(selectedGame.OldPatchTarget))
            {
                if (!Utils.TryParseEntryPoint(selectedGame.OldPatchTarget, out assemblyName))
                {
                    Close();
                    return;
                }
                useOldPatchTarget = true;
                injectedEntryPoint = selectedGame.OldPatchTarget;
                injectedEntryAssemblyPath = Path.Combine(managedPath, assemblyName);
            }

            try
            {
                injectedAssemblyDef = injectedEntryAssemblyPath == entryAssemblyPath ? assemblyDef : ModuleDefMD.Load(File.ReadAllBytes(injectedEntryAssemblyPath));
                if (File.Exists(managerAssemblyPath))
                    managerDef = ModuleDefMD.Load(File.ReadAllBytes(managerAssemblyPath));
            }
            catch (Exception e)
            {
                Log.Print(e.ToString() + Environment.NewLine + injectedEntryAssemblyPath + Environment.NewLine + managerAssemblyPath);
                Close();
                return;
            }

            var disabledMethods = new List<InstallType>();
            var unavailableMethods = new List<InstallType>();
            var managerType = typeof(UnityModManager);
            var starterType = typeof(Injection.UnityModManagerStarter);
        Rescan:
            var v0_12_Installed = injectedAssemblyDef.Types.FirstOrDefault(x => x.Name == managerType.Name);
            var newWayInstalled = injectedAssemblyDef.Types.FirstOrDefault(x => x.Name == starterType.Name);
            var hasInjectedAssembly = v0_12_Installed != null || newWayInstalled != null;

            if (useOldPatchTarget && !hasInjectedAssembly)
            {
                useOldPatchTarget = false;
                injectedEntryPoint = selectedGame.EntryPoint;
                injectedEntryAssemblyPath = entryAssemblyPath;
                injectedAssemblyDef = assemblyDef;
                goto Rescan;
            }

            if (Utils.IsUnixPlatform() || !File.Exists(gameExePath))
            {
                unavailableMethods.Add(InstallType.DoorstopProxy);
                selectedGameParams.InstallType = InstallType.Assembly;
            }
            else if (File.Exists(doorstopPath))
            {
                disabledMethods.Add(InstallType.Assembly);
                selectedGameParams.InstallType = InstallType.DoorstopProxy;
            }

            if (hasInjectedAssembly)
            {
                disabledMethods.Add(InstallType.DoorstopProxy);
                selectedGameParams.InstallType = InstallType.Assembly;
            }

            managerDef ??= injectedAssemblyDef;
            Actions actions = 0;
            if (selectedGameParams.InstallType == InstallType.Assembly && Utils.IsDirty(injectedAssemblyDef) && File.Exists($"{injectedEntryAssemblyPath}.original_"))
            {
                actions |= Actions.Restore;
            }

            var managerInstalled = managerDef.Types.FirstOrDefault(x => x.Name == managerType.Name);
            if (managerInstalled != null && (hasInjectedAssembly || selectedGameParams.InstallType == InstallType.DoorstopProxy))
            {
                Version version2;
                if (v0_12_Installed != null)
                {
                    var versionString = managerInstalled.Fields.First(x => x.Name == nameof(UnityModManager.Version)).Constant.Value.ToString();
                    version2 = Utils.ParseVersion(versionString);
                }
                else
                    version2 = managerDef.Assembly.Version;

                if (version > version2 && v0_12_Installed == null)
                    actions |= Actions.Update;

                Log.Print($"MOD管理器v{version2}已经以安装类型【{selectedGameParams.InstallType}】安装到游戏【{selectedGame}】中。");
                actions |= Actions.Delete;
            }
            else
            {
                Log.Print($"MOD管理器v{version}尚未安装到游戏【{selectedGame}】中。");
                actions |= Actions.Install;
            }

            Log.Print("输入命令键或按Enter键退出。");
            for (var i = (int)Actions.Install; i <= (int)Actions.Restore; i <<= 1)
            {
                if (actions.HasFlag((Actions)i))
                {
                    Log.Print($"{((Actions)i).ToString().First().ToString().ToUpper()} — {(Actions)i}");
                }
            }
        ReadAgain:
            Log.Print("命令键：");
            var c = Console.ReadLine();
            if (string.IsNullOrEmpty(c))
            {
                Close();
                return;
            }
            c = c.ToLower();
            if (c == "i" && actions.HasFlag(Actions.Install))
            {
                Log.Print($"已选择安装方法为[{selectedGameParams.InstallType}]。是否要更改所选内容？是：<按y或Y键>否：<按其它键>");
                var k = Console.ReadKey(true);
                if (k.Key == ConsoleKey.Y)
                {
                    var i = 1;
                    for (var t = InstallType.Assembly; t <= InstallType.DoorstopProxy; t++)
                    {
                        if (unavailableMethods.Contains(t) || disabledMethods.Contains(t))
                            continue;

                        Log.Print($"{i++} — {t}");
                    }
                ReadAgain2:
                    Log.Print("命令键：");
                    c = Console.ReadLine();
                    if (string.IsNullOrEmpty(c))
                    {
                        Close();
                        return;
                    }
                    var changed = false;
                    i = 1;
                    for (var t = InstallType.DoorstopProxy; t < InstallType.Count; t++)
                    {
                        if (unavailableMethods.Contains(t) || disabledMethods.Contains(t))
                            continue;
                        if (c == i.ToString())
                        {
                            selectedGameParams.InstallType = t;
                            changed = true;
                            param.Save();
                            break;
                        }
                        i++;
                    }
                    if (!changed)
                        goto ReadAgain2;
                }
                if (!TestWritePermissions())
                {
                    Close();
                    return;
                }
                if (!TestCompatibility())
                {
                    Close();
                    return;
                }

                var modsPath = Path.Combine(gamePath, selectedGame.ModsDirectory);
                if (!Directory.Exists(modsPath))
                    Directory.CreateDirectory(modsPath);

                if (selectedGameParams.InstallType == InstallType.DoorstopProxy)
                    InstallDoorstop(Actions.Install);
                else
                    InjectAssembly(Actions.Install, assemblyDef);

                goto Refresh;
            }
            if (c == "u" && actions.HasFlag(Actions.Update))
            {
                Log.Print($"已选择安装方法为[{selectedGameParams.InstallType}]。");

                if (!TestWritePermissions())
                {
                    Close();
                    return;
                }
                if (!TestCompatibility())
                {
                    Close();
                    return;
                }

                var modsPath = Path.Combine(gamePath, selectedGame.ModsDirectory);
                if (!Directory.Exists(modsPath))
                    Directory.CreateDirectory(modsPath);

                if (selectedGameParams.InstallType == InstallType.DoorstopProxy)
                    InstallDoorstop(Actions.Install);
                else
                    InjectAssembly(Actions.Install, assemblyDef);

                goto Refresh;
            }
            if (c == "d" && actions.HasFlag(Actions.Delete))
            {
                Log.Print($"已选择安装方法为[{selectedGameParams.InstallType}]。");

                if (!TestWritePermissions())
                {
                    Close();
                    return;
                }

                if (selectedGameParams.InstallType == InstallType.DoorstopProxy)
                    InstallDoorstop(Actions.Delete);
                else
                    InjectAssembly(Actions.Delete, injectedAssemblyDef);

                goto Refresh;
            }
            if (c == "r" && actions.HasFlag(Actions.Restore))
            {
                if (selectedGameParams.InstallType == InstallType.Assembly)
                {
                    var injectedEntryAssemblyPath = Path.Combine(managedPath, injectedAssemblyDef.Name);
                    var originalAssemblyPath = $"{injectedEntryAssemblyPath}.original_";
                    RestoreOriginal(injectedEntryAssemblyPath, originalAssemblyPath);
                }
                goto Refresh;
            }
            goto ReadAgain;
        }

        private static void SelectGameFolder()
        {
            Log.Print(@"请输入游戏目录的完整路径，例如：С:\Program Files\Steam\steamapps\common\游戏目录");
        ReadAgain:
            Log.Print("游戏路径：");
            var l = Console.ReadLine();
            if (string.IsNullOrEmpty(l))
            {
                Close();
                return;
            }
            l = l.Replace("\"", string.Empty);
            l = l.Replace("'", string.Empty);
            if (!Directory.Exists(l))
            {
                Log.Print($"游戏路径“{l}”不存在！");
                goto ReadAgain;
            }
            selectedGameParams.Path = l;
        }

        private static bool TestWritePermissions()
        {
            var success = true;

            if (selectedGameParams.InstallType == InstallType.DoorstopProxy)
            {
                success &= Utils.RemoveReadOnly(doorstopPath);
                success &= Utils.RemoveReadOnly(doorstopConfigPath);
            }
            else
            {
                success &= Utils.RemoveReadOnly(entryAssemblyPath);
                if (injectedEntryAssemblyPath != entryAssemblyPath)
                    success &= Utils.RemoveReadOnly(injectedEntryAssemblyPath);
            }

            if (Directory.Exists(managerPath))
                success = Directory.GetFiles(managerPath).Aggregate(success, (current, f) => current & Utils.RemoveReadOnly(f));

            if (!success) return false;

            success &= Utils.IsDirectoryWritable(managedPath);
            success &= Utils.IsFileWritable(managerAssemblyPath);
            success &= Utils.IsFileWritable(GameInfo.filepathInGame);
            success = libraryPaths.Aggregate(success, (current, file) => current & Utils.IsFileWritable(file));

            if (selectedGameParams.InstallType == InstallType.DoorstopProxy)
            {
                success &= Utils.IsFileWritable(doorstopPath);
                success &= Utils.IsFileWritable(doorstopConfigPath);
            }
            else
            {
                success &= Utils.IsFileWritable(entryAssemblyPath);
                if (injectedEntryAssemblyPath != entryAssemblyPath)
                    success &= Utils.IsFileWritable(injectedEntryAssemblyPath);
            }

            return success;
        }

        private static bool TestCompatibility()
        {
            try
            {
                foreach (var f in new DirectoryInfo(gamePath).GetFiles("0Harmony.dll", SearchOption.AllDirectories))
                {
                    if (f.FullName.EndsWith(Path.Combine("UnityModManager", "0Harmony.dll"))) continue;
                    var domain = AppDomain.CreateDomain("0Harmony", null, null, null, false);
                    var asm = domain.Load(File.ReadAllBytes(f.FullName));
                    AppDomain.Unload(domain);
                    if (asm.GetName().Version < HARMONY_VER)
                    {
                        Log.Print($"游戏有额外的0Harmony.dll类库文件在路径“{f.FullName}”中，这可能与DUMM不兼容，建议删除。");
                        return false;
                    }
                    Log.Print($"游戏有额外的0Harmony.dll类库文件在路径“{f.FullName}”中。");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return true;
        }

        private static bool InstallDoorstop(Actions action, bool write = true)
        {
            var gameConfigPath = GameInfo.filepathInGame;

            var success = false;
            switch (action)
            {
                case Actions.Install:
                    try
                    {
                        Log.Print("=======================================");

                        if (!Directory.Exists(managerPath))
                            Directory.CreateDirectory(managerPath);

                        Utils.MakeBackup(doorstopPath);
                        Utils.MakeBackup(doorstopConfigPath);
                        Utils.MakeBackup(libraryPaths);

                        if (!InstallDoorstop(Actions.Delete, false))
                        {
                            Log.Print("安装管理器模块到游戏失败，不能卸载上一个版本！");
                            goto EXIT;
                        }

                        Log.Print($"正在复制文件到游戏……");
                        Log.Print($"  '{doorstopFilename}'");
                        File.Copy(doorstopFilename, doorstopPath, true);
                        Log.Print($"  '{doorstopConfigFilename}'");
                        var relativeManagerAssemblyPath = managerAssemblyPath.Substring(gamePath.Length).Trim(Path.DirectorySeparatorChar);
                        File.WriteAllText(doorstopConfigPath, "[General]" + Environment.NewLine + "enabled = true" + Environment.NewLine + "target_assembly = " + relativeManagerAssemblyPath);
                        DoActionLibraries(Actions.Install);
                        DoActionGameConfig(Actions.Install);
                        Log.Print("安装管理器模块到游戏成功！");
                        success = true;
                    }
                    catch (Exception e)
                    {
                        Log.Print(e.ToString());
                        Utils.RestoreBackup(doorstopPath);
                        Utils.RestoreBackup(doorstopConfigPath);
                        Utils.RestoreBackup(libraryPaths);
                        Utils.RestoreBackup(gameConfigPath);
                        Log.Print("安装失败！");
                    }
                    break;
                case Actions.Delete:
                    try
                    {
                        if (write)
                        {
                            Log.Print("=======================================");
                        }

                        Utils.MakeBackup(gameConfigPath);
                        if (write)
                        {
                            Utils.MakeBackup(doorstopPath);
                            Utils.MakeBackup(doorstopConfigPath);
                            Utils.MakeBackup(libraryPaths);
                        }

                        Log.Print($"正在从游戏目录删除文件……");
                        Log.Print($"  '{doorstopFilename}'");
                        File.Delete(doorstopPath);
                        Log.Print($"  '{doorstopConfigFilename}'");
                        File.Delete(doorstopConfigPath);

                        if (write)
                        {
                            DoActionLibraries(Actions.Delete);
                            DoActionGameConfig(Actions.Delete);
                            Log.Print("从游戏目录删除文件成功！");
                        }

                        success = true;
                    }
                    catch (Exception e)
                    {
                        Log.Print(e.ToString());
                        if (write)
                        {
                            Utils.RestoreBackup(doorstopPath);
                            Utils.RestoreBackup(doorstopConfigPath);
                            Utils.RestoreBackup(libraryPaths);
                            Utils.RestoreBackup(gameConfigPath);
                            Log.Print("从游戏目录删除文件失败！");
                        }
                    }
                    break;
            }

        EXIT:
            if (!write) return success;
            Utils.DeleteBackup(doorstopPath);
            Utils.DeleteBackup(doorstopConfigPath);
            Utils.DeleteBackup(libraryPaths);
            Utils.DeleteBackup(gameConfigPath);

            return success;
        }

        private static bool InjectAssembly(Actions action, ModuleDefMD assemblyDef, bool write = true)
        {
            var managerType = typeof(UnityModManager);
            var starterType = typeof(Injection.UnityModManagerStarter);
            var gameConfigPath = GameInfo.filepathInGame;
            var assemblyPath = Path.Combine(managedPath, assemblyDef.Name);
            var originalAssemblyPath = $"{assemblyPath}.original_";
            var success = false;

            switch (action)
            {
                case Actions.Install:
                    {
                        try
                        {
                            Log.Print("=======================================");

                            if (!Directory.Exists(managerPath))
                                Directory.CreateDirectory(managerPath);

                            Utils.MakeBackup(assemblyPath);
                            Utils.MakeBackup(libraryPaths);

                            if (!Utils.IsDirty(assemblyDef))
                            {
                                File.Copy(assemblyPath, originalAssemblyPath, true);
                                Utils.MakeDirty(assemblyDef);
                            }

                            if (!InjectAssembly(Actions.Delete, injectedAssemblyDef, assemblyDef != injectedAssemblyDef))
                            {
                                Log.Print("安装管理器模块到游戏失败，不能卸载上一个版本！");
                                goto EXIT;
                            }

                            Log.Print($"正在注入文件“{Path.GetFileName(assemblyPath)}”……");

                            if (!Utils.TryGetEntryPoint(assemblyDef, entryPoint, out var methodDef, out var insertionPlace, true))
                                goto EXIT;

                            var starterDef = ModuleDefMD.Load(starterType.Module);
                            var starter = starterDef.Types.First(x => x.Name == starterType.Name);
                            starterDef.Types.Remove(starter);
                            assemblyDef.Types.Add(starter);

                            var instr = OpCodes.Call.ToInstruction(starter.Methods.First(x => x.Name == nameof(Injection.UnityModManagerStarter.Start)));

                            if (insertionPlace == "before")
                                methodDef.Body.Instructions.Insert(0, instr);
                            else
                                methodDef.Body.Instructions.Insert(methodDef.Body.Instructions.Count - 1, instr);

                            assemblyDef.Write(assemblyPath);
                            DoActionLibraries(Actions.Install);
                            DoActionGameConfig(Actions.Install);
                            Log.Print("安装管理器模块到游戏成功！");
                            success = true;
                        }
                        catch (Exception e)
                        {
                            Log.Print(e.ToString());
                            Utils.RestoreBackup(assemblyPath);
                            Utils.RestoreBackup(libraryPaths);
                            Utils.RestoreBackup(gameConfigPath);
                            Log.Print("安装管理器模块到游戏失败！");
                        }
                    }
                    break;
                case Actions.Delete:
                    {
                        try
                        {
                            if (write)
                                Log.Print("=======================================");

                            Utils.MakeBackup(gameConfigPath);
                            var v0_12_Installed = assemblyDef.Types.FirstOrDefault(x => x.Name == managerType.Name);
                            var newWayInstalled = assemblyDef.Types.FirstOrDefault(x => x.Name == starterType.Name);

                            if (v0_12_Installed != null || newWayInstalled != null)
                            {
                                if (write)
                                {
                                    Utils.MakeBackup(assemblyPath);
                                    Utils.MakeBackup(libraryPaths);
                                }

                                Log.Print("正在从游戏卸载管理器模块……");
                                var instr = OpCodes.Call.ToInstruction(newWayInstalled != null ? newWayInstalled.Methods.First(x => x.Name == nameof(Injection.UnityModManagerStarter.Start)) : v0_12_Installed.Methods.First(x => x.Name == nameof(UnityModManager.Start)));

                                if (!string.IsNullOrEmpty(injectedEntryPoint))
                                {
                                    if (!Utils.TryGetEntryPoint(assemblyDef, injectedEntryPoint, out var methodDef, out _, true))
                                        goto EXIT;

                                    for (var i = 0; i < methodDef.Body.Instructions.Count; i++)
                                    {
                                        if (methodDef.Body.Instructions[i].OpCode != instr.OpCode ||
                                            methodDef.Body.Instructions[i].Operand != instr.Operand) continue;
                                        methodDef.Body.Instructions.RemoveAt(i);
                                        break;
                                    }
                                }

                                assemblyDef.Types.Remove(newWayInstalled ?? v0_12_Installed);

                                if (!Utils.IsDirty(assemblyDef))
                                    Utils.MakeDirty(assemblyDef);

                                if (write)
                                {
                                    assemblyDef.Write(assemblyPath);
                                    DoActionLibraries(Actions.Delete);
                                    DoActionGameConfig(Actions.Delete);
                                    Log.Print("从游戏卸载管理器模块成功！");
                                }
                            }

                            success = true;
                        }
                        catch (Exception e)
                        {
                            Log.Print(e.ToString());
                            if (write)
                            {
                                Utils.RestoreBackup(assemblyPath);
                                Utils.RestoreBackup(libraryPaths);
                                Utils.RestoreBackup(gameConfigPath);
                                Log.Print("从游戏卸载管理器模块失败！");
                            }
                        }
                    }
                    break;
            }

        EXIT:
            if (!write) return success;
            Utils.DeleteBackup(assemblyPath);
            Utils.DeleteBackup(libraryPaths);
            Utils.DeleteBackup(gameConfigPath);

            return success;
        }

        private static void DoActionLibraries(Actions action)
        {
            Log.Print(action == Actions.Install ? "正在安装管理器模块到游戏……" : "正在从游戏卸载管理器模块……");

            var regex = new Regex(@"(\d+).*");
            var fileVersion = FileVersionInfo.GetVersionInfo(unityPlayerPath).FileVersion;
            var match = regex.Match(fileVersion);

            foreach (var destpath in libraryPaths)
            {
                var filename = Path.GetFileName(destpath);
                if (action == Actions.Install)
                {
                    if (filename.Equals(DLL_UMM) && match.Success)
                    {
                        var key = int.Parse(match.Groups[1].Value);
                        if (UmmDlls.ContainsKey(key)) filename = UmmDlls[key];
                        else
                        {
                            var min = UmmDlls.Keys.Min();
                            var max = UmmDlls.Keys.Max();
                            filename = key < min ? UmmDlls[min] : key > max ? UmmDlls[max] : filename;
                        }
                    }
                    var sourcepath = Path.Combine(Application.StartupPath, filename);
                    if (File.Exists(destpath))
                    {
                        var source = new FileInfo(sourcepath);
                        var dest = new FileInfo(destpath);
                        if (dest.LastWriteTimeUtc == source.LastWriteTimeUtc)
                            continue;
                    }
                    Log.Print($"  {filename}");
                    File.Copy(sourcepath, destpath, true);
                }
                else
                {
                    if (!File.Exists(destpath)) continue;
                    Log.Print($"  {filename}");
                    File.Delete(destpath);
                }
            }

            if (action == Actions.Delete)
            {
                foreach (var file in Directory.GetFiles(managerPath, "*.dll"))
                {
                    var filename = Path.GetFileName(file);
                    Log.Print($"  {filename}");
                    File.Delete(file);
                }
            }
        }

        private static void DoActionGameConfig(Actions action)
        {
            if (action == Actions.Install)
            {
                Log.Print("已创建配置文件“Config.xml”。");
                selectedGame.ExportToGame();
            }
            else if (File.Exists(GameInfo.filepathInGame))
            {
                Log.Print("已删除配置文件“Config.xml”。");
                File.Delete(GameInfo.filepathInGame);
            }
        }

        private static bool RestoreOriginal(string file, string backup)
        {
            try
            {
                File.Copy(backup, file, true);
                Log.Print("已还原游戏原始文件！");
                File.Delete(backup);
                return true;
            }
            catch (Exception e)
            {
                Log.Print(e.Message);
            }

            return false;
        }
    }
}
