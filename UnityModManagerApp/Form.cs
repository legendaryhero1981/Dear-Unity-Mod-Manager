using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using UnityModManagerNet.ConsoleInstaller;
using UnityModManagerNet.Injection;
using UnityModManagerNet.Installer.Properties;
using UnityModManagerNet.Installer.Tools;
using UnityModManagerNet.UI.Utils;

namespace UnityModManagerNet.Installer
{
    [Serializable]
    public partial class UnityModManagerForm : Form
    {
        private const string DLL_IL2CPP = "GameAssembly.dll";
        private const string REG_PATH = @"HKEY_CURRENT_USER\Software\DearUnityModManager";
        private readonly string SKINS_PATH = $@"{Application.StartupPath}\Skins";
        private AutoSizeFormControlUtil _autoSizeFormControlUtil;
        private static readonly Version VER_0_22 = new(0, 22);
        private static readonly Version HARMONY_VER = new(2, 0);

        [Flags]
        enum LibIncParam { Normal = 0, Skip = 1, Minimal_lt_0_22 = 2 }

        private static readonly Dictionary<string, LibIncParam> libraryFiles = new()
        {
            { "background.jpg", LibIncParam.Normal },
            { "0Harmony.dll", LibIncParam.Normal },
            { "0Harmony12.dll", LibIncParam.Minimal_lt_0_22 },
            { "0Harmony-1.2.dll", LibIncParam.Minimal_lt_0_22 },
            { "dnlib.dll", LibIncParam.Normal },
            { "System.Xml.dll", LibIncParam.Normal },
            { nameof(UnityModManager) + ".dll", LibIncParam.Normal }
        };

        public static UnityModManagerForm instance;

        static List<string> libraryPaths;
        static Config config;
        static Param param;
        static Version version;

        static string gamePath;
        static string managedPath;
        static string managerPath;
        static string entryAssemblyPath;
        static string injectedEntryAssemblyPath;
        static string managerAssemblyPath;
        static string entryPoint;
        static string injectedEntryPoint;
        static string gameExePath;
        static string unityPlayerPath;
        static string doorstopFilename = "winhttp.dll";
        static string doorstopConfigFilename = "doorstop_config.ini";
        static string doorstopPath;
        static string doorstopConfigPath;

        static ModuleDefMD assemblyDef;
        static ModuleDefMD injectedAssemblyDef;
        static ModuleDefMD managerDef;

        GameInfo selectedGame => (GameInfo)gameList.SelectedItem;
        Param.GameParam selectedGameParams;
        ModInfo selectedMod => listMods.SelectedItems.Count > 0 ? _mods.Find(x => x.DisplayName == listMods.SelectedItems[0].Text) : null;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetForegroundWindow(IntPtr hwnd);

        public UnityModManagerForm()
        {
            InitializeComponent();
            Load += UnityModManagerForm_Load;
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

        private void UnityModManagerForm_Load(object sender, EventArgs e)
        {
            if (CheckApplicationAlreadyRunning(out var process) && MessageBox.Show("正在运行", "通知", MessageBoxButtons.OK) == DialogResult.OK)
            {
                if (!ConsoleInstaller.Utils.IsUnixPlatform()) SetForegroundWindow(process.MainWindowHandle);
                Close();
                return;
            }
            Init();
            InitPageMods();
        }

        private void Init()
        {
            var skins = new Dictionary<string, string> { ["默认皮肤"] = "" };
            skins = Utils.GetMatchedFiles(SKINS_PATH, "*.ssk", skins);
            var skinSet = new BindingSource { DataSource = skins };
            skinSetBox.DataSource = skinSet;
            skinSetBox.DisplayMember = "Key";
            skinSetBox.ValueMember = "Value";
            _autoSizeFormControlUtil = new AutoSizeFormControlUtil(this);
            _autoSizeFormControlUtil.RefreshControlsInfo(Controls[0]);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            instance = this;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            ConsoleInstaller.Log.Init<Log>();
            if (!ConsoleInstaller.Utils.IsUnixPlatform())
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var registry = asm.GetType("Microsoft.Win32.Registry");
                    if (registry == null) continue;
                    var getValue = registry.GetMethod("GetValue", new[] { typeof(string), typeof(string), typeof(object) });
                    if (getValue != null)
                    {
                        var exePath = getValue.Invoke(null, new object[] { REG_PATH, "ExePath", string.Empty }) as string;
                        if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
                        {
                            var setValue = registry.GetMethod("SetValue", new[] { typeof(string), typeof(string), typeof(object) });
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
                    Text = i == InstallType.DoorstopProxy ? $"{i}（推荐）" : i.ToString(),
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
                selected ??= config.GameInfo.First();
                gameList.SelectedItem = selected;
                selectedGameParams = param.GetGameParam(selected);
            }
            else
            {
                InactiveForm();
                ConsoleInstaller.Log.Print($"解析配置文件“{Config.filename}”失败！");
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
            if (config == null) return;
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

            foreach (var ctrl in installTypeGroup.Controls)
                if (ctrl is RadioButton btn)
                    btn.Enabled = false;
        }

        private bool IsValid(GameInfo gameInfo)
        {
            if (selectedGame == null)
            {
                ConsoleInstaller.Log.Print("请先选定一个游戏！");
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
                ConsoleInstaller.Log.Print($"节点“{prefix}”的子节点“{field.Name}”值为空！");
            }

            return !hasError && (string.IsNullOrEmpty(gameInfo.EntryPoint) || ConsoleInstaller.Utils.TryParseEntryPoint(gameInfo.EntryPoint, out _)) && (string.IsNullOrEmpty(gameInfo.StartingPoint) || ConsoleInstaller.Utils.TryParseEntryPoint(gameInfo.StartingPoint, out _)) && (string.IsNullOrEmpty(gameInfo.UIStartingPoint) || ConsoleInstaller.Utils.TryParseEntryPoint(gameInfo.UIStartingPoint, out _)) && (string.IsNullOrEmpty(gameInfo.OldPatchTarget) || ConsoleInstaller.Utils.TryParseEntryPoint(gameInfo.OldPatchTarget, out _));
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
            gamePath = "";

            if (string.IsNullOrEmpty(selectedGameParams.Path) || !Directory.Exists(selectedGameParams.Path))
            {
                var result = ConsoleInstaller.Utils.FindGameFolder(selectedGame.Folder);
                if (string.IsNullOrEmpty(result))
                {
                    InactiveForm();
                    btnOpenFolder.ForeColor = Color.Red;
                    btnOpenFolder.Text = "选择游戏主目录";
                    folderBrowserDialog.SelectedPath = null;
                    ConsoleInstaller.Log.Print($"游戏主目录“{selectedGame.Folder}”不存在！");
                    return;
                }
                ConsoleInstaller.Log.Print($"已检测到游戏主目录“{result}”。");
                selectedGameParams.Path = result;
            }

            if (!ConsoleInstaller.Utils.IsUnixPlatform() && !Directory.GetFiles(selectedGameParams.Path, "*.exe", SearchOption.TopDirectoryOnly).Any())
            {
                InactiveForm();
                ConsoleInstaller.Log.Print("请选择游戏可执行文件所在的目录。");
                return;
            }

            if (ConsoleInstaller.Utils.IsMacPlatform() && !selectedGameParams.Path.EndsWith(".app"))
            {
                InactiveForm();
                ConsoleInstaller.Log.Print("请选择游戏可执行文件（扩展名为.app）所在的目录。");
                return;
            }

            ConsoleInstaller.Utils.TryParseEntryPoint(selectedGame.EntryPoint, out var assemblyName);
            gamePath = selectedGameParams.Path;
            if (File.Exists(Path.Combine(gamePath, "UnityPlayer.dll")))
                unityPlayerPath = Path.Combine(gamePath, "UnityPlayer.dll");
            btnOpenFolder.ForeColor = Color.Green;
            btnOpenFolder.Text = new DirectoryInfo(gamePath).Name;
            folderBrowserDialog.SelectedPath = gamePath;
            if (File.Exists(Path.Combine(gamePath, DLL_IL2CPP)))
            {
                InactiveForm();
                ConsoleInstaller.Log.Print("尚不支持使用IL2CPP编译的游戏版本！");
                return;
            }
            managedPath = ConsoleInstaller.Utils.FindManagedFolder(gamePath);
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

            if (File.Exists(Path.Combine(managedPath, "System.Xml.dll")))
                libraryFiles["System.Xml.dll"] = LibIncParam.Skip;

            libraryPaths = new List<string>();
            var gameSupportVersion = !string.IsNullOrEmpty(selectedGame.MinimalManagerVersion) ? ConsoleInstaller.Utils.ParseVersion(selectedGame.MinimalManagerVersion) : VER_0_22;
            foreach (var item in libraryFiles.Where(item => (item.Value & LibIncParam.Minimal_lt_0_22) <= 0 || gameSupportVersion < VER_0_22 || (item.Value & LibIncParam.Skip) <= 0))
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
                InactiveForm();
                ConsoleInstaller.Log.Print("DUMM目录不能放在游戏主目录及其子目录下，请先关闭DUMM，再将DUMM目录移动到单独的目录下重试！");
                return;
            }

            try
            {
                assemblyDef = ModuleDefMD.Load(File.ReadAllBytes(entryAssemblyPath));
            }
            catch (Exception e)
            {
                InactiveForm();
                ConsoleInstaller.Log.Print(e + Environment.NewLine + entryAssemblyPath);
                return;
            }

            var useOldPatchTarget = false;
            GameInfo.filepathInGame = Path.Combine(managerPath, "Config.xml");

            if (File.Exists(GameInfo.filepathInGame))
            {
                var gameConfig = GameInfo.ImportFromGame();
                if (gameConfig == null || !ConsoleInstaller.Utils.TryParseEntryPoint(gameConfig.EntryPoint, out assemblyName))
                {
                    InactiveForm();
                    return;
                }
                injectedEntryPoint = gameConfig.EntryPoint;
                injectedEntryAssemblyPath = Path.Combine(managedPath, assemblyName);
            }
            else if (!string.IsNullOrEmpty(selectedGame.OldPatchTarget))
            {
                if (!ConsoleInstaller.Utils.TryParseEntryPoint(selectedGame.OldPatchTarget, out assemblyName))
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
                ConsoleInstaller.Log.Print(e + Environment.NewLine + injectedEntryAssemblyPath + Environment.NewLine + managerAssemblyPath);
                return;
            }

            var disabledMethods = new List<InstallType>();
            var unavailableMethods = new List<InstallType>();
            var managerType = typeof(UnityModManager);
            var starterType = typeof(UnityModManagerStarter);
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

            if (ConsoleInstaller.Utils.IsUnixPlatform() || !File.Exists(gameExePath))
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
                if (ctrl is not RadioButton btn) continue;
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
                btnRestore.Enabled = ConsoleInstaller.Utils.IsDirty(injectedAssemblyDef) && File.Exists($"{injectedEntryAssemblyPath}{ConsoleInstaller.Utils.FileSuffixCache}");

            tabControl.TabPages[1].Enabled = true;
            managerDef ??= injectedAssemblyDef;
            var managerInstalled = managerDef.Types.FirstOrDefault(x => x.Name == managerType.Name);

            if (managerInstalled != null && (hasInjectedAssembly || selectedGameParams.InstallType == InstallType.DoorstopProxy))
            {
                btnInstall.Text = "更新MOD管理器模块";
                btnInstall.Enabled = false;
                btnRemove.Enabled = true;
                Version version2;

                if (v0_12_Installed != null)
                {
                    var versionString = managerInstalled.Fields.First(x => x.Name == nameof(UnityModManager.Version)).Constant.Value.ToString();
                    version2 = ConsoleInstaller.Utils.ParseVersion(versionString);
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
            if (!TestWritePermissions() || !TestCompatibility()) return;
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
                var originalAssemblyPath = $"{injectedEntryAssemblyPath}{ConsoleInstaller.Utils.FileSuffixCache}";
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
                ConsoleInstaller.Log.Print(ex.ToString());
            }
        }

        private bool showChoosePathNotice = true;
        private void btnOpenFolder_Click(object sender, EventArgs e)
        {
            if (showChoosePathNotice)
            {
                MessageBox.Show("请选择游戏安装目录，例如：D:/Steam/steamapps/common/游戏名称", "游戏安装目录", MessageBoxButtons.OK);
                showChoosePathNotice = false;
            }
            var result = folderBrowserDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                selectedGameParams.Path = folderBrowserDialog.SelectedPath;
                RefreshForm();
            }
        }

        private void gameList_Changed(object sender, EventArgs e)
        {
            notesTextBox.Text = "";
            additionallyGroupBox.Visible = false;
            extraFilesTextBox.Text = "";
            extraFilesGroupBox.Visible = false;

            if (((ComboBox)sender).SelectedItem is GameInfo selected)
            {
                ConsoleInstaller.Log.Print($"切换游戏为“{selected.Name}”。");
                param.LastSelectedGame = selected.Name;
                selectedGameParams = param.GetGameParam(selected);
                if (string.IsNullOrEmpty(selectedGameParams.Path))
                {
                    if (ConsoleInstaller.Utils.IsWindowsPlatform())
                    {
                        selectedGameParams.Path = SteamHelper.GetGameDirectory(selected.Folder);
                        if (!string.IsNullOrEmpty(selectedGameParams.Path))
                        {
                            ConsoleInstaller.Log.Print($"游戏目录“{selectedGameParams.Path}”。");
                        }
                    }
                }
                else
                {
                    ConsoleInstaller.Log.Print($"Game path '{selectedGameParams.Path}'.");
                }

                if (!string.IsNullOrEmpty(selected.Comment))
                {
                    notesTextBox.Text = selected.Comment;
                    additionallyGroupBox.Visible = true;
                }

                if (!string.IsNullOrEmpty(selected.ExtraFilesUrl))
                {
                    extraFilesTextBox.Text = "点击“手动”按钮解压缩文件到游戏文件夹，或点击“自动”按钮进行自动安装；必须先安装完附加文件再运行游戏。";
                    extraFilesGroupBox.Visible = true;
                }
            }

            RefreshForm();
        }

        private enum Actions
        {
            Install,
            Remove
        }

        private bool InstallDoorstop(Actions action, bool write = true)
        {
            var gameConfigPath = GameInfo.filepathInGame;

            var success = false;
            switch (action)
            {
                case Actions.Install:
                    try
                    {
                        ConsoleInstaller.Log.Print("=======================================");

                        if (!Directory.Exists(managerPath))
                            Directory.CreateDirectory(managerPath);

                        ConsoleInstaller.Utils.MakeBackup(doorstopPath);
                        ConsoleInstaller.Utils.MakeBackup(doorstopConfigPath);
                        ConsoleInstaller.Utils.MakeBackup(libraryPaths);

                        if (!InstallDoorstop(Actions.Remove, false))
                        {
                            ConsoleInstaller.Log.Print("安装管理器模块到游戏失败，不能卸载上一个版本！");
                            goto EXIT;
                        }

                        ConsoleInstaller.Log.Print("正在复制文件到游戏……");
                        ConsoleInstaller.Log.Print($"“{doorstopFilename}”");
                        File.Copy(doorstopFilename, doorstopPath, true);
                        ConsoleInstaller.Log.Print($"“{doorstopConfigFilename}”");
                        var relativeManagerAssemblyPath = managerAssemblyPath.Substring(gamePath.Length).Trim(Path.DirectorySeparatorChar);
                        File.WriteAllText(doorstopConfigPath, "[General]" + Environment.NewLine + "enabled = true" + Environment.NewLine + "target_assembly = " + relativeManagerAssemblyPath);
                        DoActionLibraries(Actions.Install);
                        DoActionGameConfig(Actions.Install);
                        ConsoleInstaller.Log.Print("安装管理器模块到游戏成功！");

                        success = true;
                    }
                    catch (Exception e)
                    {
                        ConsoleInstaller.Log.Print(e.ToString());
                        ConsoleInstaller.Utils.RestoreBackup(doorstopPath);
                        ConsoleInstaller.Utils.RestoreBackup(doorstopConfigPath);
                        ConsoleInstaller.Utils.RestoreBackup(libraryPaths);
                        ConsoleInstaller.Utils.RestoreBackup(gameConfigPath);
                        ConsoleInstaller.Log.Print("安装管理器模块到游戏失败！");
                    }
                    break;
                case Actions.Remove:
                    try
                    {
                        if (write)
                        {
                            ConsoleInstaller.Log.Print("=======================================");
                        }

                        ConsoleInstaller.Utils.MakeBackup(gameConfigPath);
                        if (write)
                        {
                            ConsoleInstaller.Utils.MakeBackup(doorstopPath);
                            ConsoleInstaller.Utils.MakeBackup(doorstopConfigPath);
                            ConsoleInstaller.Utils.MakeBackup(libraryPaths);
                        }

                        ConsoleInstaller.Log.Print("正在从游戏目录删除文件……");
                        ConsoleInstaller.Log.Print($"  '{doorstopFilename}'");
                        File.Delete(doorstopPath);
                        ConsoleInstaller.Log.Print($"  '{doorstopConfigFilename}'");
                        File.Delete(doorstopConfigPath);

                        if (write)
                        {
                            DoActionLibraries(Actions.Remove);
                            DoActionGameConfig(Actions.Remove);
                            ConsoleInstaller.Log.Print("从游戏目录删除文件成功！");
                        }

                        success = true;
                    }
                    catch (Exception e)
                    {
                        ConsoleInstaller.Log.Print(e.ToString());
                        if (write)
                        {
                            ConsoleInstaller.Utils.RestoreBackup(doorstopPath);
                            ConsoleInstaller.Utils.RestoreBackup(doorstopConfigPath);
                            ConsoleInstaller.Utils.RestoreBackup(libraryPaths);
                            ConsoleInstaller.Utils.RestoreBackup(gameConfigPath);
                            ConsoleInstaller.Log.Print("从游戏目录删除文件失败！");
                        }
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), action, null);
            }
        EXIT:
            if (!write) return success;
            ConsoleInstaller.Utils.DeleteBackup(doorstopPath);
            ConsoleInstaller.Utils.DeleteBackup(doorstopConfigPath);
            ConsoleInstaller.Utils.DeleteBackup(libraryPaths);
            ConsoleInstaller.Utils.DeleteBackup(gameConfigPath);
            return success;
        }

        private bool InjectAssembly(Actions action, ModuleDefMD assemblyDef, bool write = true)
        {
            var managerType = typeof(UnityModManager);
            var starterType = typeof(UnityModManagerStarter);
            var gameConfigPath = GameInfo.filepathInGame;
            var assemblyPath = Path.Combine(managedPath, assemblyDef.Name);
            var originalAssemblyPath = $"{assemblyPath}{ConsoleInstaller.Utils.FileSuffixCache}";
            var success = false;

            switch (action)
            {
                case Actions.Install:
                    {
                        try
                        {
                            ConsoleInstaller.Log.Print("=======================================");

                            if (!Directory.Exists(managerPath))
                                Directory.CreateDirectory(managerPath);

                            ConsoleInstaller.Utils.MakeBackup(assemblyPath);
                            ConsoleInstaller.Utils.MakeBackup(libraryPaths);

                            if (!ConsoleInstaller.Utils.IsDirty(assemblyDef))
                            {
                                File.Copy(assemblyPath, originalAssemblyPath, true);
                                ConsoleInstaller.Utils.MakeDirty(assemblyDef);
                            }

                            if (!InjectAssembly(Actions.Remove, injectedAssemblyDef, assemblyDef != injectedAssemblyDef))
                            {
                                ConsoleInstaller.Log.Print("安装管理器模块到游戏失败，不能卸载上一个版本！");
                                goto EXIT;
                            }

                            ConsoleInstaller.Log.Print($"正在注入文件“{Path.GetFileName(assemblyPath)}”……");

                            if (!ConsoleInstaller.Utils.TryGetEntryPoint(assemblyDef, entryPoint, out var methodDef, out var insertionPlace, true))
                            {
                                goto EXIT;
                            }

                            var starterDef = ModuleDefMD.Load(starterType.Module);
                            var starter = starterDef.Types.First(x => x.Name == starterType.Name);
                            starterDef.Types.Remove(starter);
                            assemblyDef.Types.Add(starter);

                            var instr = OpCodes.Call.ToInstruction(starter.Methods.First(x => x.Name == nameof(UnityModManagerStarter.Start)));
                            if (insertionPlace == "before")
                            {
                                methodDef.Body.Instructions.Insert(0, instr);
                            }
                            else
                            {
                                methodDef.Body.Instructions.Insert(methodDef.Body.Instructions.Count - 1, instr);
                            }

                            assemblyDef.Write(assemblyPath);
                            DoActionLibraries(Actions.Install);
                            DoActionGameConfig(Actions.Install);

                            ConsoleInstaller.Log.Print("安装管理器模块到游戏成功！");

                            success = true;
                        }
                        catch (Exception e)
                        {
                            ConsoleInstaller.Log.Print(e.ToString());
                            ConsoleInstaller.Utils.RestoreBackup(assemblyPath);
                            ConsoleInstaller.Utils.RestoreBackup(libraryPaths);
                            ConsoleInstaller.Utils.RestoreBackup(gameConfigPath);
                            ConsoleInstaller.Log.Print("安装管理器模块到游戏失败！");
                        }
                    }
                    break;
                case Actions.Remove:
                    {
                        try
                        {
                            if (write)
                            {
                                ConsoleInstaller.Log.Print("=======================================");
                            }

                            ConsoleInstaller.Utils.MakeBackup(gameConfigPath);

                            var v0_12_Installed = assemblyDef.Types.FirstOrDefault(x => x.Name == managerType.Name);
                            var newWayInstalled = assemblyDef.Types.FirstOrDefault(x => x.Name == starterType.Name);

                            if (v0_12_Installed != null || newWayInstalled != null)
                            {
                                if (write)
                                {
                                    ConsoleInstaller.Utils.MakeBackup(assemblyPath);
                                    ConsoleInstaller.Utils.MakeBackup(libraryPaths);
                                }

                                ConsoleInstaller.Log.Print("正在从游戏卸载管理器模块……");

                                Instruction instr = null;
                                if (newWayInstalled != null)
                                {
                                    instr = OpCodes.Call.ToInstruction(newWayInstalled.Methods.First(x => x.Name == nameof(UnityModManagerStarter.Start)));
                                }
                                else if (v0_12_Installed != null)
                                {
                                    instr = OpCodes.Call.ToInstruction(v0_12_Installed.Methods.First(x => x.Name == nameof(UnityModManager.Start)));
                                }

                                if (!string.IsNullOrEmpty(injectedEntryPoint))
                                {
                                    if (!ConsoleInstaller.Utils.TryGetEntryPoint(assemblyDef, injectedEntryPoint, out var methodDef, out _, true))
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

                                if (!ConsoleInstaller.Utils.IsDirty(assemblyDef))
                                {
                                    ConsoleInstaller.Utils.MakeDirty(assemblyDef);
                                }

                                if (write)
                                {
                                    assemblyDef.Write(assemblyPath);
                                    DoActionLibraries(Actions.Remove);
                                    DoActionGameConfig(Actions.Remove);
                                    ConsoleInstaller.Log.Print("从游戏卸载管理器模块成功！");
                                }
                            }

                            success = true;
                        }
                        catch (Exception e)
                        {
                            ConsoleInstaller.Log.Print(e.ToString());
                            if (write)
                            {
                                ConsoleInstaller.Utils.RestoreBackup(assemblyPath);
                                ConsoleInstaller.Utils.RestoreBackup(libraryPaths);
                                ConsoleInstaller.Utils.RestoreBackup(gameConfigPath);
                                ConsoleInstaller.Log.Print("从游戏卸载管理器模块失败！");
                            }
                        }
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), action, null);
            }
        EXIT:
            if (!write) return success;
            ConsoleInstaller.Utils.DeleteBackup(assemblyPath);
            ConsoleInstaller.Utils.DeleteBackup(libraryPaths);
            ConsoleInstaller.Utils.DeleteBackup(gameConfigPath);
            return success;
        }

        private bool TestWritePermissions()
        {
            var success = true;

            if (selectedGameParams.InstallType == InstallType.DoorstopProxy)
            {
                success &= ConsoleInstaller.Utils.RemoveReadOnly(doorstopPath);
                success &= ConsoleInstaller.Utils.RemoveReadOnly(doorstopConfigPath);
            }
            else
            {
                success &= ConsoleInstaller.Utils.RemoveReadOnly(entryAssemblyPath);
                if (injectedEntryAssemblyPath != entryAssemblyPath)
                    success &= ConsoleInstaller.Utils.RemoveReadOnly(injectedEntryAssemblyPath);
            }

            if (Directory.Exists(managerPath))
                success = Directory.GetFiles(managerPath).Aggregate(success, (current, f) => current & ConsoleInstaller.Utils.RemoveReadOnly(f));

            if (!success) return false;

            success &= ConsoleInstaller.Utils.IsDirectoryWritable(managedPath);
            success &= ConsoleInstaller.Utils.IsFileWritable(managerAssemblyPath);
            success &= ConsoleInstaller.Utils.IsFileWritable(GameInfo.filepathInGame);
            success = libraryPaths.Aggregate(success, (current, file) => current & ConsoleInstaller.Utils.IsFileWritable(file));

            if (selectedGameParams.InstallType == InstallType.DoorstopProxy)
            {
                success &= ConsoleInstaller.Utils.IsFileWritable(doorstopPath);
                success &= ConsoleInstaller.Utils.IsFileWritable(doorstopConfigPath);
            }
            else
            {
                success &= ConsoleInstaller.Utils.IsFileWritable(entryAssemblyPath);
                if (injectedEntryAssemblyPath != entryAssemblyPath)
                    success &= ConsoleInstaller.Utils.IsFileWritable(injectedEntryAssemblyPath);
            }

            return success;
        }

        private bool TestCompatibility()
        {
            foreach (var f in new DirectoryInfo(gamePath).GetFiles("0Harmony.dll", SearchOption.AllDirectories))
            {
                if (f.FullName.EndsWith(Path.Combine("UnityModManager", "0Harmony.dll"))) continue;
                var domain = AppDomain.CreateDomain("0Harmony", null, null, null, false);
                var asm = domain.Load(File.ReadAllBytes(f.FullName));
                AppDomain.Unload(domain);
                if (asm.GetName().Version < HARMONY_VER)
                {
                    ConsoleInstaller.Log.Print($"游戏有额外的0Harmony.dll类库文件在路径“{f.FullName}”中，这可能与DUMM不兼容，建议删除。");
                    return false;
                }
                ConsoleInstaller.Log.Print($"游戏有额外的0Harmony.dll类库文件在路径“{f.FullName}”中。");
            }

            return true;
        }

        private static bool RestoreOriginal(string file, string backup)
        {
            try
            {
                File.Copy(backup, file, true);
                ConsoleInstaller.Log.Print("已还原游戏原始文件！");
                File.Delete(backup);
                return true;
            }
            catch (Exception e)
            {
                ConsoleInstaller.Log.Print(e.Message);
            }

            return false;
        }

        private static void DoActionLibraries(Actions action)
        {
            ConsoleInstaller.Log.Print(action == Actions.Install ? "正在安装管理器模块到游戏……" : "正在从游戏卸载管理器模块……");

            foreach (var destpath in libraryPaths)
            {
                var filename = Path.GetFileName(destpath);
                if (action == Actions.Install)
                {
                    var sourcepath = Path.Combine(Application.StartupPath, filename);
                    if (File.Exists(destpath))
                    {
                        var source = new FileInfo(sourcepath);
                        var dest = new FileInfo(destpath);
                        if (dest.LastWriteTimeUtc == source.LastWriteTimeUtc)
                            continue;
                    }
                    ConsoleInstaller.Log.Print($"  {filename}");
                    File.Copy(sourcepath, destpath, true);
                }
                else
                {
                    if (!File.Exists(destpath)) continue;
                    ConsoleInstaller.Log.Print($"  {filename}");
                    File.Delete(destpath);
                }
            }

            if (action == Actions.Remove)
            {
                foreach(var file in Directory.GetFiles(managerPath, "*.dll"))
                {
                    var filename = Path.GetFileName(file);
                    ConsoleInstaller.Log.Print($"  {filename}");
                    File.Delete(file);
                }
            }
        }

        private void DoActionGameConfig(Actions action)
        {
            if (action == Actions.Install)
            {
                ConsoleInstaller.Log.Print("已创建配置文件“Config.xml”。");
                selectedGame.ExportToGame();
            }
            else if (File.Exists(GameInfo.filepathInGame))
            {
                ConsoleInstaller.Log.Print("已删除配置文件“Config.xml”。");
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
                    if (!_repositories.ContainsKey(selectedGame))
                        CheckModUpdates();
                    break;
            }
        }

        private void notesTextBox_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            Process.Start(e.LinkText);
        }

        private void extraFilesAutoButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(selectedGame.ExtraFilesUrl)) return;
            var form = new DownloadExtraFiles(selectedGame.ExtraFilesUrl, gamePath);
            form.ShowDialog();
        }

        private void extraFilesManualButton_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(selectedGame.ExtraFilesUrl))
            {
                Process.Start(selectedGame.ExtraFilesUrl);
            }
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

        private void installedVersion_Click(object sender, EventArgs e)
        {
        }

        private void extraFilesGroupBox_Enter(object sender, EventArgs e)
        {
        }

        private void splitContainerModsInstall_Panel1_Paint(object sender, PaintEventArgs e)
        {
        }
    }
}
