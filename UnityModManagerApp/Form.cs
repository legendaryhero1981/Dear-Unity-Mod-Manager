using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using UnityModManagerNet.UI.Utils;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using UnityModManagerNet.Installer.Properties;

namespace UnityModManagerNet.Installer
{
    [Serializable]
    public partial class UnityModManagerForm : Form
    {
        const string REG_PATH = @"HKEY_CURRENT_USER\Software\DearUnityModManager";
        private readonly string SKINS_PATH = $@"{Application.StartupPath}\Skins";
        private AutoSizeFormControlUtil _autoSizeFormControlUtil;

        public UnityModManagerForm()
        {
            InitializeComponent();
            Init();
            InitPageMods();
        }

        static readonly string[] libraryFiles = new string[]
        {
            //"UnityModManager.xml",
            //"0Harmony.dll",
            "0Harmony12.dll",
            "0Harmony-1.2.dll",
            "dnlib.dll",
            "System.Xml.dll",
            "UnityModManager.dll"
        };

        public static UnityModManagerForm instance = null;

        static string[] libraryPaths;
        static Config config = null;
        static Param param = null;
        static Version version = null;

        static string gamePath = null;
        static string managedPath = null;
        static string managerPath = null;
        static string entryAssemblyPath = null;
        static string injectedEntryAssemblyPath = null;
        static string managerAssemblyPath = null;
        static string entryPoint = null;
        static string injectedEntryPoint = null;

        static string gameExePath = null;

        static string doorstopFilename = "version.dll";
        static string doorstopFilenameX86 = "version_x86.dll";
        static string doorstopFilenameX64 = "version_x64.dll";
        static string doorstopConfigFilename = "doorstop_config.ini";
        static string doorstopPath = null;
        static string doorstopConfigPath = null;

        static ModuleDefMD assemblyDef = null;
        static ModuleDefMD injectedAssemblyDef = null;
        static ModuleDefMD managerDef = null;

        GameInfo selectedGame => (GameInfo)gameList.SelectedItem;
        Param.GameParam selectedGameParams = null;
        ModInfo selectedMod => listMods.SelectedItems.Count > 0 ? mods.Find(x => x.DisplayName == listMods.SelectedItems[0].Text) : null;

        private void Init()
        {
            var skins = new Dictionary<string, string> { ["默认皮肤"] = "" };
            skins = Utils.GetMatchedFiles(SKINS_PATH, "*.ssk", skins);
            var skinSet = new BindingSource { DataSource = skins };
            skinSetBox.DataSource = skinSet;
            skinSetBox.DisplayMember = "Key";
            skinSetBox.ValueMember = "Value";
            _autoSizeFormControlUtil = new AutoSizeFormControlUtil(this);
            _autoSizeFormControlUtil.RefreshControlsInfo(this.Controls[0]);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            instance = this;
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
                                setValue.Invoke(null, new object[] { REG_PATH, "ExePath", Path.Combine(Application.StartupPath, "DearUnityModManager.exe") });
                                setValue.Invoke(null, new object[] { REG_PATH, "Path", Application.StartupPath });
                            }
                        }
                    }
                    break;
                }
            }
            var rbWidth = 0;
            for (var i = (InstallType)0; i < InstallType.Count; i++)
            {
                var rb = new RadioButton
                {
                    Name = i.ToString(),
                    Text = i == InstallType.DoorstopProxy ? $"{i.ToString()}（推荐）" : i.ToString(),
                    AutoSize = true,
                    Location = new Point(rbWidth + 8, 50),
                    Margin = new Padding(0)
                };
                rb.Click += installType_Click;
                installTypeGroup.Controls.Add(rb);
                rbWidth += rb.Width + 200;
            }
            version = typeof(UnityModManager).Assembly.GetName().Version;
            currentVersion.Text = version.ToString();
            config = Config.Load();
            param = Param.Load();
            skinSetBox.SelectedIndex = param.LastSelectedSkin;
            if (config?.GameInfo != null && config.GameInfo.Length > 0)
            {
                config.GameInfo = config.GameInfo.OrderBy(x => x.GameName).ToArray();
                gameList.Items.AddRange(config.GameInfo);

                GameInfo selected = null;
                if (!string.IsNullOrEmpty(param.LastSelectedGame))
                {
                    selected = config.GameInfo.FirstOrDefault(x => x.Name == param.LastSelectedGame);
                }
                selected = selected ?? config.GameInfo.First();
                gameList.SelectedItem = selected;
                selectedGameParams = param.GetGameParam(selected);
            }
            else
            {
                InactiveForm();
                Log.Print($"解析配置文件“{Config.filename}”失败！");
                return;
            }
            CheckLastVersion();
        }

        #region 窗体缩放      
        private void UnityModLoaderForm_SizeChanged(object sender, EventArgs e)
        {
            _autoSizeFormControlUtil?.FormSizeChanged();
        }
        #endregion

        #region 自定义更换皮肤      
        private void UnityModLoaderForm_SkinChanged(object sender, EventArgs e)
        {
            var skin = skinSetBox.SelectedValue.ToString();
            if (!string.IsNullOrEmpty(skin))
            {
                skinEngine.Active = true;
                skinEngine.SkinFile = skin;
            }
            else
                skinEngine.Active = false;
        }
        #endregion

        private void installType_Click(object sender, EventArgs e)
        {
            var btn = (sender as RadioButton);
            if (!btn.Checked) return;
            selectedGameParams.InstallType = (InstallType)Enum.Parse(typeof(InstallType), btn.Name);
            RefreshForm();
        }

        private void UnityModLoaderForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            //Properties.Settings.Default.Save();
            Log.Writer.Close();
            param.LastSelectedSkin = skinSetBox.SelectedIndex;
            param.Sync(config.GameInfo);
            param.Save();
        }

        private void InactiveForm()
        {
            btnInstall.Enabled = false;
            btnRemove.Enabled = false;
            btnRestore.Enabled = false;
            tabControl.TabPages[1].Enabled = false;
            installedVersion.Text = "-";
            additionallyGroupBox.Visible = false;

            foreach (var ctrl in installTypeGroup.Controls)
            {
                if (ctrl is RadioButton btn)
                {
                    btn.Enabled = false;
                }
            }
        }

        private bool IsValid(GameInfo gameInfo)
        {
            if (selectedGame == null)
            {
                Log.Print("请先选定一个游戏！");
                return false;
            }

            var ignoreFields = new List<string>
            {
                nameof(GameInfo.GameExe),
                nameof(GameInfo.GameName),
                nameof(GameInfo.GameVersionPoint),
                nameof(GameInfo.GameScriptName),
                nameof(GameInfo.StartingPoint),
                nameof(GameInfo.UIStartingPoint),
                nameof(GameInfo.OldPatchTarget),
                nameof(GameInfo.Additionally)
            };

            var prefix = (!string.IsNullOrEmpty(gameInfo.Name) ? $"[{gameInfo.Name}]" : "[?]");
            var hasError = false;
            foreach (var field in typeof(GameInfo).GetFields())
            {
                if (field.IsStatic || !field.IsPublic || ignoreFields.Exists(x => x == field.Name)) continue;
                var value = field.GetValue(gameInfo);
                if (value != null && value.ToString() != "") continue;
                hasError = true;
                Log.Print($"节点“{prefix}”的子节点“{field.Name}”值为空！");
            }

            return !hasError && (string.IsNullOrEmpty(gameInfo.EntryPoint) || Utils.TryParseEntryPoint(gameInfo.EntryPoint, out _)) && (string.IsNullOrEmpty(gameInfo.StartingPoint) || Utils.TryParseEntryPoint(gameInfo.StartingPoint, out _)) && (string.IsNullOrEmpty(gameInfo.UIStartingPoint) || Utils.TryParseEntryPoint(gameInfo.UIStartingPoint, out _)) && (string.IsNullOrEmpty(gameInfo.OldPatchTarget) || Utils.TryParseEntryPoint(gameInfo.OldPatchTarget, out _));
        }

        private void RefreshForm()
        {
            if (!IsValid(selectedGame))
            {
                InactiveForm();
                return;
            }

            btnInstall.Text = "安装MOD管理器模块到游戏";
            btnRestore.Enabled = false;
            additionallyGroupBox.Visible = false;
            gamePath = "";

            if (string.IsNullOrEmpty(selectedGameParams.Path) || !Directory.Exists(selectedGameParams.Path))
            {
                var result = FindGameFolder(selectedGame.Folder);
                if (string.IsNullOrEmpty(result))
                {
                    InactiveForm();
                    btnOpenFolder.ForeColor = Color.Red;
                    btnOpenFolder.Text = "选择游戏主目录";
                    folderBrowserDialog.SelectedPath = null;
                    Log.Print($"游戏主目录“{selectedGame.Folder}”不存在！");
                    return;
                }
                Log.Print($"已检测到游戏主目录“{result}”。");
                selectedGameParams.Path = result;
            }

            if (!Utils.IsUnixPlatform() && !Directory.GetFiles(selectedGameParams.Path, "*.exe", SearchOption.TopDirectoryOnly).Any())
            {
                InactiveForm();
                Log.Print("请选择游戏可执行文件所在的目录。");
                return;
            }

            Utils.TryParseEntryPoint(selectedGame.EntryPoint, out var assemblyName);
            gamePath = selectedGameParams.Path;
            btnOpenFolder.ForeColor = Color.Green;
            btnOpenFolder.Text = new DirectoryInfo(gamePath).Name;
            folderBrowserDialog.SelectedPath = gamePath;
            managedPath = FindManagedFolder(gamePath);
            managerPath = Path.Combine(managedPath, nameof(UnityModManager));
            entryAssemblyPath = Path.Combine(managedPath, assemblyName);
            injectedEntryAssemblyPath = entryAssemblyPath;
            managerAssemblyPath = Path.Combine(managerPath, typeof(UnityModManager).Module.Name);
            entryPoint = selectedGame.EntryPoint;
            injectedEntryPoint = selectedGame.EntryPoint;
            assemblyDef = null;
            injectedAssemblyDef = null;
            managerDef = null;
            doorstopPath = Path.Combine(gamePath, doorstopFilename);
            doorstopConfigPath = Path.Combine(gamePath, doorstopConfigFilename);
            libraryPaths = new string[libraryFiles.Length];

            if (!string.IsNullOrEmpty(selectedGame.GameExe))
            {
                if (selectedGame.GameExe.Contains('*'))
                    foreach (var file in new DirectoryInfo(gamePath).GetFiles(selectedGame.GameExe, SearchOption.TopDirectoryOnly))
                        selectedGame.GameExe = file.Name;
                gameExePath = Path.Combine(gamePath, selectedGame.GameExe);
            }
            else
                gameExePath = string.Empty;

            for (var i = 0; i < libraryFiles.Length; i++)
                libraryPaths[i] = Path.Combine(managerPath, libraryFiles[i]);

            var path = new DirectoryInfo(Application.StartupPath).FullName;
            if (path.StartsWith(gamePath))
            {
                InactiveForm();
                Log.Print("DUMM目录不能放在游戏主目录及其子目录下，请先关闭DUMM，再将DUMM目录移动到单独的目录下再试！");
                return;
            }

            try
            {
                assemblyDef = ModuleDefMD.Load(File.ReadAllBytes(entryAssemblyPath));
            }
            catch (Exception e)
            {
                InactiveForm();
                Log.Print(e.ToString());
                return;
            }

            var useOldPatchTarget = false;
            GameInfo.filepathInGame = Path.Combine(managerPath, "Config.xml");

            if (File.Exists(GameInfo.filepathInGame))
            {
                var gameConfig = GameInfo.ImportFromGame();
                if (gameConfig == null || !Utils.TryParseEntryPoint(gameConfig.EntryPoint, out assemblyName))
                {
                    InactiveForm();
                    return;
                }
                injectedEntryPoint = gameConfig.EntryPoint;
                injectedEntryAssemblyPath = Path.Combine(managedPath, assemblyName);
            }
            else if (!string.IsNullOrEmpty(selectedGame.OldPatchTarget))
            {
                if (!Utils.TryParseEntryPoint(selectedGame.OldPatchTarget, out assemblyName))
                {
                    InactiveForm();
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
                InactiveForm();
                Log.Print(e.ToString());
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

            foreach (var ctrl in installTypeGroup.Controls)
            {
                if (!(ctrl is RadioButton btn)) continue;
                if (unavailableMethods.Exists(x => x.ToString() == btn.Name))
                {
                    btn.Visible = false;
                    btn.Enabled = false;
                    continue;
                }
                if (disabledMethods.Exists(x => x.ToString() == btn.Name))
                {
                    btn.Visible = true;
                    btn.Enabled = false;
                    continue;
                }

                btn.Visible = true;
                btn.Enabled = true;
                btn.Checked = btn.Name == selectedGameParams.InstallType.ToString();
            }

            if (selectedGameParams.InstallType == InstallType.Assembly)
                btnRestore.Enabled = IsDirty(injectedAssemblyDef) && File.Exists($"{injectedEntryAssemblyPath}{Utils.FileSuffixCache}");

            tabControl.TabPages[1].Enabled = true;
            managerDef = managerDef ?? injectedAssemblyDef;
            var managerInstalled = managerDef.Types.FirstOrDefault(x => x.Name == managerType.Name);

            if (managerInstalled != null && (hasInjectedAssembly || selectedGameParams.InstallType == InstallType.DoorstopProxy))
            {
                btnInstall.Text = "更新MOD管理器模块";
                btnInstall.Enabled = false;
                btnRemove.Enabled = true;
                Version version2;

                if (v0_12_Installed != null)
                {
                    var versionString = managerInstalled.Fields.First(x => x.Name == nameof(UnityModManager.version)).Constant.Value.ToString();
                    version2 = Utils.ParseVersion(versionString);
                }
                else
                    version2 = managerDef.Assembly.Version;

                installedVersion.Text = version2.ToString();

                if (version > version2 && v0_12_Installed == null)
                    btnInstall.Enabled = true;
            }
            else
            {
                installedVersion.Text = "-";
                btnInstall.Enabled = true;
                btnRemove.Enabled = false;
            }

            if (string.IsNullOrEmpty(selectedGame.Additionally)) return;
            notesTextBox.Text = selectedGame.Additionally;
            additionallyGroupBox.Visible = true;
        }

        private string FindGameFolder(string str)
        {
            var disks = new string[] { @"C:\", @"D:\", @"E:\", @"F:\" };
            var roots = new string[] { "Games", "Program files", "Program files (x86)", "" };
            var folders = new string[] { @"Steam\SteamApps\common", @"GoG Galaxy\Games", "" };
            if (Environment.OSVersion.Platform != PlatformID.Unix)
                return (from disk in disks
                        from root in roots
                        from folder in folders
                        let path = Path.Combine(disk, root)
                        select Path.Combine(path, folder)
                    into path
                        select Path.Combine(path, str)).FirstOrDefault(Directory.Exists);

            disks = new string[] { Environment.GetEnvironmentVariable("HOME") };
            roots = new string[] { "Library/Application Support", ".steam" };
            folders = new string[] { "Steam/SteamApps/common", "steam/steamapps/common" };
            return (from disk in disks
                    from root in roots
                    from folder in folders
                    let path = Path.Combine(disk, root)
                    select Path.Combine(path, folder)
                into path
                    select Path.Combine(path, str)).FirstOrDefault(Directory.Exists);
        }

        private string FindManagedFolder(string str)
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                var appName = $"{Path.GetFileName(str)}.app";
                if (!Directory.Exists(Path.Combine(str, appName)))
                {
                    appName = Directory.GetDirectories(str).FirstOrDefault(dir => dir.EndsWith(".app"));
                }
                var path = Path.Combine(str, $"{appName}/Contents/Resources/Data/Managed");
                if (Directory.Exists(path))
                {
                    return path;
                }
            }
            var regex = new Regex(".*_Data$");
            var directory = new DirectoryInfo(str);
            foreach (var dir in directory.GetDirectories())
            {
                var match = regex.Match(dir.Name);
                if (!match.Success) continue;
                var path = Path.Combine(str, $"{dir.Name}{Path.DirectorySeparatorChar}Managed");
                if (Directory.Exists(path))
                {
                    return path;
                }
            }
            return str;
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            if (!TestWritePermissions())
            {
                return;
            }
            if (selectedGameParams.InstallType == InstallType.DoorstopProxy)
            {
                InstallDoorstop(Actions.Remove);
            }
            else
            {
                InjectAssembly(Actions.Remove, injectedAssemblyDef);
            }

            RefreshForm();
        }

        private void btnInstall_Click(object sender, EventArgs e)
        {
            if (!TestWritePermissions())
            {
                return;
            }
            var modsPath = Path.Combine(gamePath, selectedGame.ModsDirectory);
            if (!Directory.Exists(modsPath))
            {
                Directory.CreateDirectory(modsPath);
            }
            if (selectedGameParams.InstallType == InstallType.DoorstopProxy)
            {
                InstallDoorstop(Actions.Install);
            }
            else
            {
                InjectAssembly(Actions.Install, assemblyDef);
            }

            RefreshForm();
        }

        private void btnRestore_Click(object sender, EventArgs e)
        {
            if (selectedGameParams.InstallType == InstallType.Assembly)
            {
                var injectedEntryAssemblyPath = Path.Combine(managedPath, injectedAssemblyDef.Name);
                var originalAssemblyPath = $"{injectedEntryAssemblyPath}{Utils.FileSuffixCache}";
                RestoreOriginal(injectedEntryAssemblyPath, originalAssemblyPath);
            }

            RefreshForm();
        }

        private void btnDownloadUpdate_Click(object sender, EventArgs e)
        {
            try
            {
                if (Resources.btnDownloadUpdate.Equals(btnDownloadUpdate.Text))
                {
                    if (!string.IsNullOrEmpty(config.HomePage))
                        Process.Start(config.HomePage);
                }
                else
                {
                    Process.Start(Resources.appUpdater);
                }
            }
            catch (Exception ex)
            {
                Log.Print(ex.ToString());
            }
        }

        private void btnOpenFolder_Click(object sender, EventArgs e)
        {
            var result = folderBrowserDialog.ShowDialog();
            if (result != DialogResult.OK) return;
            selectedGameParams.Path = folderBrowserDialog.SelectedPath;
            RefreshForm();
        }

        private void gameList_Changed(object sender, EventArgs e)
        {
            var selected = (GameInfo)((ComboBox)sender).SelectedItem;
            if (selected != null)
            {
                Log.Print($"切换游戏为“{selected.Name}”。");
                param.LastSelectedGame = selected.Name;
                selectedGameParams = param.GetGameParam(selected);
                if (!string.IsNullOrEmpty(selectedGameParams.Path))
                    Log.Print($"游戏目录“{selectedGameParams.Path}”。");
            }

            RefreshForm();
        }

        enum Actions
        {
            Install,
            Remove
        };

        private bool InstallDoorstop(Actions action, bool write = true)
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

                        if (!InstallDoorstop(Actions.Remove, false))
                        {
                            Log.Print("安装管理器模块到游戏失败，不能卸载上一个版本！");
                            goto EXIT;
                        }

                        Log.Print($"正在复制文件到游戏……");
                        var arch = Utils.UnmanagedDllIs64Bit(gameExePath);
                        var filename = arch == true ? doorstopFilenameX64 : doorstopFilenameX86;
                        Log.Print($"  '{filename}'");
                        File.Copy(filename, doorstopPath, true);
                        Log.Print($"  '{doorstopConfigFilename}'");
                        File.WriteAllText(doorstopConfigPath, $@"[UnityDoorstop]{Environment.NewLine}enabled = true{Environment.NewLine}targetAssembly = {managerAssemblyPath}");
                        DoactionLibraries(Actions.Install);
                        DoactionGameConfig(Actions.Install);
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
                        Log.Print("安装管理器模块到游戏失败！");
                    }
                    break;

                case Actions.Remove:
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
                            DoactionLibraries(Actions.Remove);
                            DoactionGameConfig(Actions.Remove);
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
            if (write)
            {
                Utils.DeleteBackup(doorstopPath);
                Utils.DeleteBackup(doorstopConfigPath);
                Utils.DeleteBackup(libraryPaths);
                Utils.DeleteBackup(gameConfigPath);
            }
            return success;
        }

        private bool InjectAssembly(Actions action, ModuleDefMD assemblyDef, bool write = true)
        {
            var managerType = typeof(UnityModManager);
            var starterType = typeof(Injection.UnityModManagerStarter);
            var gameConfigPath = GameInfo.filepathInGame;

            var assemblyPath = Path.Combine(managedPath, assemblyDef.Name);
            var originalAssemblyPath = $"{assemblyPath}{Utils.FileSuffixCache}";

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

                            if (!IsDirty(assemblyDef))
                            {
                                File.Copy(assemblyPath, originalAssemblyPath, true);
                                MakeDirty(assemblyDef);
                            }

                            if (!InjectAssembly(Actions.Remove, injectedAssemblyDef, assemblyDef != injectedAssemblyDef))
                            {
                                Log.Print("安装管理器模块到游戏失败，不能卸载上一个版本！");
                                goto EXIT;
                            }

                            Log.Print($"正在注入文件“{Path.GetFileName(assemblyPath)}”……");

                            if (!Utils.TryGetEntryPoint(assemblyDef, entryPoint, out var methodDef, out var insertionPlace, true))
                            {
                                goto EXIT;
                            }

                            var starterDef = ModuleDefMD.Load(starterType.Module);
                            var starter = starterDef.Types.First(x => x.Name == starterType.Name);
                            starterDef.Types.Remove(starter);
                            assemblyDef.Types.Add(starter);

                            var instr = OpCodes.Call.ToInstruction(starter.Methods.First(x => x.Name == nameof(Injection.UnityModManagerStarter.Start)));
                            if (insertionPlace == "before")
                            {
                                methodDef.Body.Instructions.Insert(0, instr);
                            }
                            else
                            {
                                methodDef.Body.Instructions.Insert(methodDef.Body.Instructions.Count - 1, instr);
                            }

                            assemblyDef.Write(assemblyPath);
                            DoactionLibraries(Actions.Install);
                            DoactionGameConfig(Actions.Install);

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

                case Actions.Remove:
                    {
                        try
                        {
                            if (write)
                            {
                                Log.Print("=======================================");
                            }

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

                                Instruction instr = null;

                                if (newWayInstalled != null)
                                {
                                    instr = OpCodes.Call.ToInstruction(newWayInstalled.Methods.First(x => x.Name == nameof(Injection.UnityModManagerStarter.Start)));
                                }
                                else if (v0_12_Installed != null)
                                {
                                    instr = OpCodes.Call.ToInstruction(v0_12_Installed.Methods.First(x => x.Name == nameof(UnityModManager.Start)));
                                }

                                if (!string.IsNullOrEmpty(injectedEntryPoint))
                                {
                                    if (!Utils.TryGetEntryPoint(assemblyDef, injectedEntryPoint, out var methodDef, out _, true))
                                    {
                                        goto EXIT;
                                    }

                                    for (var i = 0; i < methodDef.Body.Instructions.Count; i++)
                                    {
                                        if (methodDef.Body.Instructions[i].OpCode != instr.OpCode ||
                                            methodDef.Body.Instructions[i].Operand != instr.Operand) continue;
                                        methodDef.Body.Instructions.RemoveAt(i);
                                        break;
                                    }
                                }

                                if (newWayInstalled != null)
                                    assemblyDef.Types.Remove(newWayInstalled);
                                else if (v0_12_Installed != null)
                                    assemblyDef.Types.Remove(v0_12_Installed);

                                if (!IsDirty(assemblyDef))
                                {
                                    MakeDirty(assemblyDef);
                                }

                                if (write)
                                {
                                    assemblyDef.Write(assemblyPath);
                                    DoactionLibraries(Actions.Remove);
                                    DoactionGameConfig(Actions.Remove);
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

        private static bool IsDirty(ModuleDefMD assembly)
        {
            return assembly.Types.FirstOrDefault(x => x.FullName == typeof(Marks.IsDirty).FullName || x.Name == typeof(UnityModManager).Name) != null;
        }

        private static void MakeDirty(ModuleDefMD assembly)
        {
            var moduleDef = ModuleDefMD.Load(typeof(Marks.IsDirty).Module);
            var typeDef = moduleDef.Types.FirstOrDefault(x => x.FullName == typeof(Marks.IsDirty).FullName);
            moduleDef.Types.Remove(typeDef);
            assembly.Types.Add(typeDef);
        }

        private bool TestWritePermissions()
        {
            var success = true;
            success = Utils.IsDirectoryWritable(managedPath) && success;
            success = Utils.IsFileWritable(managerAssemblyPath) && success;
            success = Utils.IsFileWritable(GameInfo.filepathInGame) && success;
            success = libraryPaths.Aggregate(success, (current, file) => Utils.IsFileWritable(file) && current);

            if (selectedGameParams.InstallType == InstallType.DoorstopProxy)
            {
                success = Utils.IsFileWritable(doorstopPath) && success;
            }
            else
            {
                success = Utils.IsFileWritable(entryAssemblyPath) && success;
                if (injectedEntryAssemblyPath != entryAssemblyPath)
                    success = Utils.IsFileWritable(injectedEntryAssemblyPath) && success;
            }

            return success;
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

        private static void DoactionLibraries(Actions action)
        {
            Log.Print(action == Actions.Install ? $"正在安装管理器模块到游戏……" : $"正在从游戏卸载管理器模块……");

            for (var i = 0; i < libraryPaths.Length; i++)
            {
                var filename = libraryFiles[i];
                var path = libraryPaths[i];
                if (action == Actions.Install)
                {
                    if (File.Exists(path))
                    {
                        var source = new FileInfo(filename);
                        var dest = new FileInfo(path);
                        if (dest.LastWriteTimeUtc == source.LastWriteTimeUtc)
                            continue;
                    }

                    Log.Print($"  {filename}");
                    File.Copy(filename, path, true);
                }
                else
                {
                    if (!File.Exists(path)) continue;
                    Log.Print($"  {filename}");
                    File.Delete(path);
                }
            }
        }

        private void DoactionGameConfig(Actions action)
        {
            if (action == Actions.Install)
            {
                Log.Print($"已创建配置文件“Config.xml”。");
                selectedGame.ExportToGame();
            }
            else if (File.Exists(GameInfo.filepathInGame))
            {
                Log.Print($"已删除配置文件“Config.xml”。");
                File.Delete(GameInfo.filepathInGame);
            }
        }

        private void folderBrowserDialog_HelpRequest(object sender, EventArgs e)
        {
        }

        private void tabs_Changed(object sender, EventArgs e)
        {
            switch (tabControl.SelectedIndex)
            {
                case 1: // Mods
                    ReloadMods();
                    RefreshModList();
                    if (!repositories.ContainsKey(selectedGame))
                        CheckModUpdates();
                    break;
            }
        }

        private void notesTextBox_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            Process.Start(e.LinkText);
        }

        private void statusStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
        }

        private void splitContainerMain_Panel2_Paint(object sender, PaintEventArgs e)
        {
        }

        private void splitContainerModsInstall_Panel2_Paint(object sender, PaintEventArgs e)
        {
        }
    }
}
