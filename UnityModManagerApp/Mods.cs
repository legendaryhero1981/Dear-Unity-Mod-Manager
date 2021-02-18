using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;
using Ionic.Zip;

namespace UnityModManagerNet.Installer
{
    public partial class UnityModManagerForm
    {
        private readonly List<ModInfo> _mods = new List<ModInfo>();
        private const string ZipFilePostfix = ".zip";

        private void InitPageMods()
        {
            splitContainerModsInstall.Panel2.AllowDrop = true;
            splitContainerModsInstall.Panel2.DragEnter += Mods_DragEnter;
            splitContainerModsInstall.Panel2.DragDrop += Mods_DragDrop;
        }

        private void btnModInstall_Click(object sender, EventArgs e)
        {
            var result = modInstallFileDialog.ShowDialog();
            if (result != DialogResult.OK || modInstallFileDialog.FileNames.Length == 0) return;
            SaveAndInstallZipFiles(modInstallFileDialog.FileNames);
            ReloadMods();
            RefreshModList();
        }

        private void Mods_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        private void Mods_DragDrop(object sender, DragEventArgs e)
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length == 0) return;
            //Drag and drop files on OS X are in the format /.file/id=6571367.2773272
            if (Environment.OSVersion.Platform == PlatformID.Unix && files[0].StartsWith("/.file"))
            {
                files = files.Select(Utils.ResolveOSXFileUrl).ToArray();
            }

            SaveAndInstallZipFiles(files);
            ReloadMods();
            RefreshModList();
        }

        private void SaveAndInstallZipFiles(IEnumerable<string> files)
        {
            var programModsPath = Path.Combine(Application.StartupPath, selectedGame.Folder);
            var newMods = new List<ModInfo>();

            foreach (var filepath in files)
                try
                {
                    if (ZipFilePostfix.Equals(Path.GetExtension(filepath)?.ToLower()))
                    {
                        using var zip = ZipFile.Read(filepath);
                        InstallMod(zip, false);
                        var modInfo = ReadModInfoFromZip(zip);
                        if (!modInfo) continue;
                        newMods.Add(modInfo);
                        var dir = Path.Combine(programModsPath, modInfo.Id);
                        if (!Directory.Exists(dir))
                        {
                            Directory.CreateDirectory(dir);
                        }
                        var target = Path.Combine(dir, $"{modInfo.Id}-{modInfo.Version.Replace('.', '-')}{ZipFilePostfix}");
                        if (filepath != target)
                            File.Copy(filepath, target, true);
                    }
                    else
                    {
                        Log.Print($"只能处理后缀名为.zip的压缩文件！");
                    }
                }
                catch (Exception ex)
                {
                    Log.Print(ex.Message);
                    Log.Print($"安装文件“{Path.GetFileName(filepath)}”失败！");
                }

            // delete old zip files if count > 2
            if (newMods.Count <= 0) return;
            foreach (var tempList in newMods.Select(modInfo => Directory.GetFiles(Path.Combine(programModsPath, modInfo.Id), $"*{ZipFilePostfix}", SearchOption.AllDirectories).Select(ReadModInfoFromZip).Where(mod => mod && !mod.EqualsVersion(modInfo)).ToList()).Select(tempList => tempList.OrderBy(x => x.ParsedVersion).ToList()))
            {
                while (tempList.Count > 2)
                {
                    var item = tempList.First();
                    try
                    {
                        tempList.Remove(item);
                        File.Delete(item.Path);
                    }
                    catch (Exception ex)
                    {
                        Log.Print(ex.Message);
                        Log.Print($"删除旧压缩文件“{item.Path}”失败！");
                        break;
                    }
                }
            }
        }

        private void UninstallMod(string name)
        {
            if (selectedGame == null)
            {
                Log.Print("请先选定一个游戏！");
                return;
            }

            var modsPath = Path.Combine(gamePath, selectedGame.ModsDirectory);
            if (!Directory.Exists(modsPath))
            {
                Log.Print("请先安装MOD管理器模块到游戏！");
                return;
            }

            var modPath = Path.Combine(modsPath, name);

            if (Directory.Exists(modPath))
            {
                try
                {
                    Directory.Delete(modPath, true);
                    Log.Print($"卸载MOD“{name}”成功！");
                }
                catch (Exception ex)
                {
                    Log.Print(ex.Message);
                    Log.Print($"卸载MOD“{name}”失败！");
                }
            }
            else
            {
                Log.Print($"目录“{modPath}”不存在！");
            }

            ReloadMods();
            RefreshModList();
        }

        private void InstallMod(string filepath)
        {
            if (!File.Exists(filepath))
            {
                Log.Print($"文件“{Path.GetFileName(filepath)}”不存在！");
            }
            try
            {
                using var zip = ZipFile.Read(filepath);
                InstallMod(zip);
            }
            catch (Exception e)
            {
                Log.Print(e.Message);
                Log.Print($"安装MOD“{Path.GetFileName(filepath)}”失败！");
            }
        }

        private void InstallMod(ZipFile zip, bool reloadMods = true)
        {
            if (selectedGame == null)
            {
                Log.Print("请先选定一个游戏！");
                return;
            }

            var modsPath = Path.Combine(gamePath, selectedGame.ModsDirectory);
            if (!Directory.Exists(modsPath))
            {
                Log.Print("请先安装MOD管理器模块到游戏！");
                return;
            }

            try
            {
                foreach (var e in zip.EntriesSorted)
                {
                    if (e.IsDirectory)
                        continue;

                    var filepath = Path.Combine(modsPath, e.FileName);
                    if (File.Exists(filepath))
                    {
                        File.Delete(filepath);
                    }
                }
                foreach (var entry in zip.EntriesSorted)
                {
                    if (entry.IsDirectory)
                    {
                        Directory.CreateDirectory(Path.Combine(modsPath, entry.FileName));
                    }
                    else
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(modsPath, entry.FileName)) ?? string.Empty);
                        using var fs = new FileStream(Path.Combine(modsPath, entry.FileName), FileMode.Create, FileAccess.Write);
                        entry.Extract(fs);
                    }
                }
                Log.Print($"解压缩文件“{Path.GetFileName(zip.Name)}”成功！");
            }
            catch (Exception ex)
            {
                Log.Print(ex.Message);
                Log.Print(ex.StackTrace);
                Log.Print($"解压缩文件“{Path.GetFileName(zip.Name)}”失败！");
            }

            if (!reloadMods) return;
            ReloadMods();
            RefreshModList();
        }

        private void ReloadMods()
        {
            _mods.Clear();
            if (selectedGame == null) return;

            var modsPath = Path.Combine(gamePath, selectedGame.ModsDirectory);

            if (Directory.Exists(modsPath))
            {
                foreach (var dir in Directory.GetDirectories(modsPath))
                {
                    var jsonPath = Path.Combine(dir, selectedGame.ModInfo);

                    if (!File.Exists(jsonPath)) continue;

                    try
                    {
                        var modInfo = JsonConvert.DeserializeObject<ModInfo>(File.ReadAllText(jsonPath));
                        if (modInfo && modInfo.IsValid())
                        {
                            modInfo.Path = dir;
                            modInfo.Status = ModStatus.Installed;
                            _mods.Add(modInfo);
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Print(e.Message);
                        Log.Print($"解析文件“{jsonPath}”失败！");
                    }
                }
            }

            LoadZipMods();
        }

        private void LoadZipMods()
        {
            if (selectedGame == null) return;

            var dir = Path.Combine(Application.StartupPath, selectedGame.Folder);

            if (!Directory.Exists(dir)) return;

            foreach (var filepath in Directory.GetFiles(dir, $"*{ZipFilePostfix}", SearchOption.AllDirectories))
            {
                var modInfo = ReadModInfoFromZip(filepath);

                if (!modInfo) continue;

                var index = _mods.FindIndex(m => m.Id == modInfo.Id);

                if (index == -1)
                {
                    modInfo.Status = ModStatus.NotInstalled;
                    modInfo.AvailableVersions.Add(modInfo.ParsedVersion, filepath);
                    _mods.Add(modInfo);
                }
                else if (!_mods[index].AvailableVersions.ContainsKey(modInfo.ParsedVersion))
                {
                    _mods[index].AvailableVersions.Add(modInfo.ParsedVersion, filepath);
                }
            }
        }

        private void RefreshModList()
        {
            listMods.Items.Clear();

            if (selectedGame == null || _mods.Count == 0 || tabControl.SelectedIndex != 1) return;

            _mods.Sort((x, y) => string.Compare(x.DisplayName, y.DisplayName, StringComparison.OrdinalIgnoreCase));

            foreach (var modInfo in _mods)
            {
                var status = "";

                if (ModStatus.Installed.Equals(modInfo.Status))
                {
                    var release = _repositories.ContainsKey(selectedGame) ? _repositories[selectedGame].FirstOrDefault(x => x.Id == modInfo.Id) : null;
                    var web = !string.IsNullOrEmpty(release?.Version) ? Utils.ParseVersion(release.Version) : new Version();
                    var local = modInfo.AvailableVersions.Keys.Max(x => x) ?? new Version();

                    if (local > modInfo.ParsedVersion && local >= web)
                    {
                        status = $"更新 {local}";
                    }
                    else if (web > modInfo.ParsedVersion && web > local)
                    {
                        status = string.IsNullOrEmpty(release?.DownloadUrl) ? $"可更新 {web}" : $"下载 {web}";
                    }
                    else
                    {
                        status = "就绪";
                    }
                }

                var listItem = new ListViewItem(modInfo.DisplayName);

                if (modInfo.Status == ModStatus.NotInstalled)
                {
                    listItem.SubItems.Add(modInfo.AvailableVersions.Count > 0 ? modInfo.AvailableVersions.Keys.Max(x => x).ToString() : modInfo.Version);
                }
                else
                {
                    listItem.SubItems.Add(modInfo.Version);
                }

                if (!string.IsNullOrEmpty(modInfo.ManagerVersion))
                {
                    listItem.SubItems.Add(modInfo.ManagerVersion);
                    if (version < Utils.ParseVersion(modInfo.ManagerVersion))
                    {
                        listItem.ForeColor = System.Drawing.Color.FromArgb(192, 0, 0);
                        status = "需要更新MOD管理器";
                    }
                }
                else
                {
                    listItem.SubItems.Add("");
                }

                listItem.SubItems.Add(status);
                listMods.Items.Add(listItem);
            }
        }

        private ModInfo ReadModInfoFromZip(string filepath)
        {
            try
            {
                using var zip = ZipFile.Read(filepath);
                return ReadModInfoFromZip(zip);
            }
            catch (Exception e)
            {
                Log.Print(e.Message);
                Log.Print($"解析文件“{Path.GetFileName(filepath)}”失败！");
            }

            return null;
        }

        private ModInfo ReadModInfoFromZip(ZipFile zip)
        {
            try
            {
                foreach (var e in zip)
                {
                    if (!e.FileName.EndsWith(selectedGame.ModInfo, StringComparison.InvariantCultureIgnoreCase)) continue;
                    using var s = new StreamReader(e.OpenReader());
                    var modInfo = JsonConvert.DeserializeObject<ModInfo>(s.ReadToEnd());
                    if (modInfo.IsValid())
                    {
                        modInfo.Path = zip.Name;
                        return modInfo;
                    }

                    break;
                }
            }
            catch (Exception e)
            {
                Log.Print(e.Message);
                Log.Print($"解析文件“{Path.GetFileName(zip.Name)}”失败！");
            }

            return null;
        }

        private void ModcontextMenuStrip1_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            installToolStripMenuItem.Visible = false;
            uninstallToolStripMenuItem.Visible = false;
            updateToolStripMenuItem.Visible = false;
            revertToolStripMenuItem.Visible = false;
            wwwToolStripMenuItem1.Visible = false;

            var modInfo = selectedMod;
            if (!modInfo)
            {
                e.Cancel = true;
                return;
            }

            switch (modInfo.Status)
            {
                case ModStatus.Installed:
                    {
                        uninstallToolStripMenuItem.Visible = true;

                        var inRepository = new Version();
                        if (_repositories.ContainsKey(selectedGame))
                        {
                            var release = _repositories[selectedGame].FirstOrDefault(x => x.Id == modInfo.Id);
                            if (release != null && !string.IsNullOrEmpty(release.DownloadUrl) && !string.IsNullOrEmpty(release.Version))
                            {
                                var ver = Utils.ParseVersion(release.Version);
                                if (modInfo.ParsedVersion < ver)
                                {
                                    inRepository = ver;
                                    updateToolStripMenuItem.Text = $@"更新到v{release.Version}";
                                    updateToolStripMenuItem.Visible = true;
                                }
                            }
                        }

                        var newest = modInfo.AvailableVersions.Keys.Max(x => x);
                        if (newest != null && newest > modInfo.ParsedVersion && inRepository <= newest)
                        {
                            updateToolStripMenuItem.Text = $@"更新到v{newest}";
                            updateToolStripMenuItem.Visible = true;
                        }
                        var previous = modInfo.AvailableVersions.Keys.Where(x => x < modInfo.ParsedVersion).Max(x => x);
                        if (previous != null)
                        {
                            revertToolStripMenuItem.Text = $@"还原到v{previous}";
                            revertToolStripMenuItem.Visible = true;
                        }

                        break;
                    }

                case ModStatus.NotInstalled:
                    installToolStripMenuItem.Visible = true;
                    break;
            }

            if (!string.IsNullOrEmpty(modInfo.HomePage))
            {
                wwwToolStripMenuItem1.Visible = true;
            }
        }

        private void installToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var modInfo = selectedMod;
            if (!modInfo) return;
            var newest = modInfo.AvailableVersions.OrderByDescending(x => x.Key).FirstOrDefault();
            if (!string.IsNullOrEmpty(newest.Value))
            {
                InstallMod(newest.Value);
            }
        }

        private void updateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var modInfo = selectedMod;
            if (!modInfo) return;
            if (_repositories.ContainsKey(selectedGame))
            {
                var release = _repositories[selectedGame].FirstOrDefault(x => x.Id == modInfo.Id);
                if (release != null && !string.IsNullOrEmpty(release.DownloadUrl) && !string.IsNullOrEmpty(release.Version) && modInfo.AvailableVersions.All(x => x.Key < Utils.ParseVersion(release.Version)))
                {
                    var downloadForm = new DownloadMod(release);
                    var result = downloadForm.ShowDialog();
                    if (result != DialogResult.OK) return;
                    SaveAndInstallZipFiles(new string[] { downloadForm.TempFilepath });
                    ReloadMods();
                    RefreshModList();
                    return;
                }
            }
            installToolStripMenuItem_Click(sender, e);
        }

        private void uninstallToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var modInfo = selectedMod;
            if (modInfo)
            {
                UninstallMod(modInfo.Id);
            }
        }

        private void revertToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var modInfo = selectedMod;
            if (!modInfo) return;
            var previous = modInfo.AvailableVersions.Where(x => x.Key < modInfo.ParsedVersion).OrderByDescending(x => x.Key).FirstOrDefault();
            if (!string.IsNullOrEmpty(previous.Value))
            {
                InstallMod(previous.Value);
            }
        }

        private void wwwToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            var modInfo = selectedMod;
            if (modInfo)
            {
                System.Diagnostics.Process.Start(modInfo.HomePage);
            }
        }
    }
}
