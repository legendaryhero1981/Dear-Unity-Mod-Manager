using System;

namespace UnityModManagerNet.Installer
{
    partial class UnityModManagerForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UnityModManagerForm));
            this.splitContainerMods = new System.Windows.Forms.SplitContainer();
            this.listMods = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.ModcontextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.installToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.updateToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.revertToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.uninstallToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.wwwToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.splitContainerModsInstall = new System.Windows.Forms.SplitContainer();
            this.btnModInstall = new System.Windows.Forms.Button();
            this.splitContainerMain = new System.Windows.Forms.SplitContainer();
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.installedVersion = new System.Windows.Forms.Label();
            this.gameList = new System.Windows.Forms.ComboBox();
            this.currentVersion = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.additionallyGroupBox = new System.Windows.Forms.GroupBox();
            this.notesTextBox = new System.Windows.Forms.RichTextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.installTypeGroup = new System.Windows.Forms.GroupBox();
            this.btnRestore = new System.Windows.Forms.Button();
            this.btnDownloadUpdate = new System.Windows.Forms.Button();
            this.btnRemove = new System.Windows.Forms.Button();
            this.btnOpenFolder = new System.Windows.Forms.Button();
            this.btnInstall = new System.Windows.Forms.Button();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.inputLog = new System.Windows.Forms.TextBox();
            this.tabPage4 = new System.Windows.Forms.TabPage();
            this.skinSetGroup = new System.Windows.Forms.GroupBox();
            this.skinSetBox = new System.Windows.Forms.ComboBox();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.statusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.panelMain = new System.Windows.Forms.Panel();
            this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.modInstallFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.skinEngine = new Sunisoft.IrisSkin.SkinEngine();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerMods)).BeginInit();
            this.splitContainerMods.Panel1.SuspendLayout();
            this.splitContainerMods.Panel2.SuspendLayout();
            this.splitContainerMods.SuspendLayout();
            this.ModcontextMenuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerModsInstall)).BeginInit();
            this.splitContainerModsInstall.Panel1.SuspendLayout();
            this.splitContainerModsInstall.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerMain)).BeginInit();
            this.splitContainerMain.Panel1.SuspendLayout();
            this.splitContainerMain.Panel2.SuspendLayout();
            this.splitContainerMain.SuspendLayout();
            this.tabControl.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.additionallyGroupBox.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.tabPage3.SuspendLayout();
            this.tabPage4.SuspendLayout();
            this.skinSetGroup.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.panelMain.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainerMods
            // 
            this.splitContainerMods.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerMods.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitContainerMods.IsSplitterFixed = true;
            this.splitContainerMods.Location = new System.Drawing.Point(0, 0);
            this.splitContainerMods.Margin = new System.Windows.Forms.Padding(0);
            this.splitContainerMods.Name = "splitContainerMods";
            this.splitContainerMods.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainerMods.Panel1
            // 
            this.splitContainerMods.Panel1.Controls.Add(this.listMods);
            // 
            // splitContainerMods.Panel2
            // 
            this.splitContainerMods.Panel2.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.splitContainerMods.Panel2.Controls.Add(this.splitContainerModsInstall);
            this.splitContainerMods.Size = new System.Drawing.Size(776, 687);
            this.splitContainerMods.SplitterDistance = 542;
            this.splitContainerMods.SplitterWidth = 1;
            this.splitContainerMods.TabIndex = 0;
            // 
            // listMods
            // 
            this.listMods.AllowColumnReorder = true;
            this.listMods.BackColor = System.Drawing.Color.WhiteSmoke;
            this.listMods.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader3,
            this.columnHeader4});
            this.listMods.ContextMenuStrip = this.ModcontextMenuStrip1;
            this.listMods.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listMods.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Bold);
            this.listMods.FullRowSelect = true;
            this.listMods.Location = new System.Drawing.Point(0, 0);
            this.listMods.Margin = new System.Windows.Forms.Padding(0);
            this.listMods.MultiSelect = false;
            this.listMods.Name = "listMods";
            this.listMods.ShowItemToolTips = true;
            this.listMods.Size = new System.Drawing.Size(776, 542);
            this.listMods.Sorting = System.Windows.Forms.SortOrder.Ascending;
            this.listMods.TabIndex = 0;
            this.listMods.UseCompatibleStateImageBehavior = false;
            this.listMods.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "名称";
            this.columnHeader1.Width = 282;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "版本";
            this.columnHeader2.Width = 107;
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "MOD管理器版本";
            this.columnHeader3.Width = 213;
            // 
            // columnHeader4
            // 
            this.columnHeader4.Text = "状态";
            this.columnHeader4.Width = 170;
            // 
            // ModcontextMenuStrip1
            // 
            this.ModcontextMenuStrip1.Font = new System.Drawing.Font("微软雅黑", 14.25F);
            this.ModcontextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.installToolStripMenuItem,
            this.updateToolStripMenuItem,
            this.revertToolStripMenuItem,
            this.uninstallToolStripMenuItem,
            this.wwwToolStripMenuItem1});
            this.ModcontextMenuStrip1.Name = "ModcontextMenuStrip1";
            this.ModcontextMenuStrip1.Size = new System.Drawing.Size(123, 154);
            this.ModcontextMenuStrip1.Text = "操作";
            this.ModcontextMenuStrip1.Opening += new System.ComponentModel.CancelEventHandler(this.ModcontextMenuStrip1_Opening);
            // 
            // installToolStripMenuItem
            // 
            this.installToolStripMenuItem.Name = "installToolStripMenuItem";
            this.installToolStripMenuItem.Size = new System.Drawing.Size(122, 30);
            this.installToolStripMenuItem.Text = "安装";
            this.installToolStripMenuItem.Click += new System.EventHandler(this.installToolStripMenuItem_Click);
            // 
            // updateToolStripMenuItem
            // 
            this.updateToolStripMenuItem.Name = "updateToolStripMenuItem";
            this.updateToolStripMenuItem.Size = new System.Drawing.Size(122, 30);
            this.updateToolStripMenuItem.Text = "更新";
            this.updateToolStripMenuItem.Click += new System.EventHandler(this.updateToolStripMenuItem_Click);
            // 
            // revertToolStripMenuItem
            // 
            this.revertToolStripMenuItem.Name = "revertToolStripMenuItem";
            this.revertToolStripMenuItem.Size = new System.Drawing.Size(122, 30);
            this.revertToolStripMenuItem.Text = "还原";
            this.revertToolStripMenuItem.Click += new System.EventHandler(this.revertToolStripMenuItem_Click);
            // 
            // uninstallToolStripMenuItem
            // 
            this.uninstallToolStripMenuItem.Name = "uninstallToolStripMenuItem";
            this.uninstallToolStripMenuItem.Size = new System.Drawing.Size(122, 30);
            this.uninstallToolStripMenuItem.Text = "卸载";
            this.uninstallToolStripMenuItem.Click += new System.EventHandler(this.uninstallToolStripMenuItem_Click);
            // 
            // wwwToolStripMenuItem1
            // 
            this.wwwToolStripMenuItem1.Name = "wwwToolStripMenuItem1";
            this.wwwToolStripMenuItem1.Size = new System.Drawing.Size(122, 30);
            this.wwwToolStripMenuItem1.Text = "主页";
            this.wwwToolStripMenuItem1.Click += new System.EventHandler(this.wwwToolStripMenuItem1_Click);
            // 
            // splitContainerModsInstall
            // 
            this.splitContainerModsInstall.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerModsInstall.Location = new System.Drawing.Point(0, 0);
            this.splitContainerModsInstall.Margin = new System.Windows.Forms.Padding(0);
            this.splitContainerModsInstall.Name = "splitContainerModsInstall";
            this.splitContainerModsInstall.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainerModsInstall.Panel1
            // 
            this.splitContainerModsInstall.Panel1.Controls.Add(this.btnModInstall);
            // 
            // splitContainerModsInstall.Panel2
            // 
            this.splitContainerModsInstall.Panel2.BackgroundImage = global::UnityModManagerNet.Installer.Properties.Resources.dragdropfiles;
            this.splitContainerModsInstall.Panel2.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.splitContainerModsInstall.Panel2.Paint += new System.Windows.Forms.PaintEventHandler(this.splitContainerModsInstall_Panel2_Paint);
            this.splitContainerModsInstall.Size = new System.Drawing.Size(776, 144);
            this.splitContainerModsInstall.SplitterDistance = 46;
            this.splitContainerModsInstall.SplitterWidth = 1;
            this.splitContainerModsInstall.TabIndex = 0;
            // 
            // btnModInstall
            // 
            this.btnModInstall.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnModInstall.Font = new System.Drawing.Font("微软雅黑", 15.75F, System.Drawing.FontStyle.Bold);
            this.btnModInstall.ForeColor = System.Drawing.Color.Green;
            this.btnModInstall.Location = new System.Drawing.Point(0, 0);
            this.btnModInstall.Margin = new System.Windows.Forms.Padding(0);
            this.btnModInstall.Name = "btnModInstall";
            this.btnModInstall.Size = new System.Drawing.Size(776, 46);
            this.btnModInstall.TabIndex = 0;
            this.btnModInstall.Text = "安装MOD";
            this.btnModInstall.UseVisualStyleBackColor = true;
            this.btnModInstall.Click += new System.EventHandler(this.btnModInstall_Click);
            // 
            // splitContainerMain
            // 
            this.splitContainerMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerMain.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitContainerMain.IsSplitterFixed = true;
            this.splitContainerMain.Location = new System.Drawing.Point(0, 0);
            this.splitContainerMain.Margin = new System.Windows.Forms.Padding(0);
            this.splitContainerMain.Name = "splitContainerMain";
            this.splitContainerMain.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainerMain.Panel1
            // 
            this.splitContainerMain.Panel1.Controls.Add(this.tabControl);
            // 
            // splitContainerMain.Panel2
            // 
            this.splitContainerMain.Panel2.Controls.Add(this.statusStrip1);
            this.splitContainerMain.Panel2.Paint += new System.Windows.Forms.PaintEventHandler(this.splitContainerMain_Panel2_Paint);
            this.splitContainerMain.Panel2MinSize = 30;
            this.splitContainerMain.Size = new System.Drawing.Size(784, 761);
            this.splitContainerMain.SplitterDistance = 730;
            this.splitContainerMain.SplitterWidth = 1;
            this.splitContainerMain.TabIndex = 11;
            // 
            // tabControl
            // 
            this.tabControl.ContextMenuStrip = this.ModcontextMenuStrip1;
            this.tabControl.Controls.Add(this.tabPage1);
            this.tabControl.Controls.Add(this.tabPage2);
            this.tabControl.Controls.Add(this.tabPage3);
            this.tabControl.Controls.Add(this.tabPage4);
            this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl.Font = new System.Drawing.Font("微软雅黑", 15.75F, System.Drawing.FontStyle.Bold);
            this.tabControl.HotTrack = true;
            this.tabControl.ItemSize = new System.Drawing.Size(150, 35);
            this.tabControl.Location = new System.Drawing.Point(0, 0);
            this.tabControl.Margin = new System.Windows.Forms.Padding(0);
            this.tabControl.Name = "tabControl";
            this.tabControl.Padding = new System.Drawing.Point(0, 0);
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(784, 730);
            this.tabControl.TabIndex = 10;
            this.tabControl.SelectedIndexChanged += new System.EventHandler(this.tabs_Changed);
            // 
            // tabPage1
            // 
            this.tabPage1.BackColor = System.Drawing.Color.WhiteSmoke;
            this.tabPage1.Controls.Add(this.installedVersion);
            this.tabPage1.Controls.Add(this.gameList);
            this.tabPage1.Controls.Add(this.currentVersion);
            this.tabPage1.Controls.Add(this.label2);
            this.tabPage1.Controls.Add(this.additionallyGroupBox);
            this.tabPage1.Controls.Add(this.label3);
            this.tabPage1.Controls.Add(this.installTypeGroup);
            this.tabPage1.Controls.Add(this.btnRestore);
            this.tabPage1.Controls.Add(this.btnDownloadUpdate);
            this.tabPage1.Controls.Add(this.btnRemove);
            this.tabPage1.Controls.Add(this.btnOpenFolder);
            this.tabPage1.Controls.Add(this.btnInstall);
            this.tabPage1.Location = new System.Drawing.Point(4, 39);
            this.tabPage1.Margin = new System.Windows.Forms.Padding(0);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Size = new System.Drawing.Size(776, 687);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "安装";
            // 
            // installedVersion
            // 
            this.installedVersion.AutoSize = true;
            this.installedVersion.Font = new System.Drawing.Font("微软雅黑", 20.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.installedVersion.ForeColor = System.Drawing.Color.Green;
            this.installedVersion.Location = new System.Drawing.Point(585, 643);
            this.installedVersion.Margin = new System.Windows.Forms.Padding(0);
            this.installedVersion.Name = "installedVersion";
            this.installedVersion.Size = new System.Drawing.Size(100, 35);
            this.installedVersion.TabIndex = 10;
            this.installedVersion.Tag = "9999";
            this.installedVersion.Text = "1.0.0.0";
            // 
            // gameList
            // 
            this.gameList.BackColor = System.Drawing.Color.WhiteSmoke;
            this.gameList.DropDownHeight = 460;
            this.gameList.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.gameList.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.gameList.Font = new System.Drawing.Font("微软雅黑", 21.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.gameList.FormattingEnabled = true;
            this.gameList.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.gameList.IntegralHeight = false;
            this.gameList.ItemHeight = 38;
            this.gameList.Location = new System.Drawing.Point(0, 159);
            this.gameList.Margin = new System.Windows.Forms.Padding(0);
            this.gameList.MaxDropDownItems = 10;
            this.gameList.Name = "gameList";
            this.gameList.Size = new System.Drawing.Size(776, 46);
            this.gameList.Sorted = true;
            this.gameList.TabIndex = 5;
            this.gameList.Tag = "9999";
            this.gameList.SelectedIndexChanged += new System.EventHandler(this.gameList_Changed);
            // 
            // currentVersion
            // 
            this.currentVersion.AutoSize = true;
            this.currentVersion.Font = new System.Drawing.Font("微软雅黑", 20.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.currentVersion.ForeColor = System.Drawing.Color.Green;
            this.currentVersion.Location = new System.Drawing.Point(154, 643);
            this.currentVersion.Margin = new System.Windows.Forms.Padding(0);
            this.currentVersion.Name = "currentVersion";
            this.currentVersion.Size = new System.Drawing.Size(100, 35);
            this.currentVersion.TabIndex = 8;
            this.currentVersion.Tag = "9999";
            this.currentVersion.Text = "1.0.0.0";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("微软雅黑", 20.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label2.ForeColor = System.Drawing.Color.Green;
            this.label2.Location = new System.Drawing.Point(0, 643);
            this.label2.Margin = new System.Windows.Forms.Padding(0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(150, 35);
            this.label2.TabIndex = 7;
            this.label2.Tag = "9999";
            this.label2.Text = "当前版本：";
            // 
            // additionallyGroupBox
            // 
            this.additionallyGroupBox.AutoSize = true;
            this.additionallyGroupBox.Controls.Add(this.notesTextBox);
            this.additionallyGroupBox.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.additionallyGroupBox.Font = new System.Drawing.Font("微软雅黑", 15.75F, System.Drawing.FontStyle.Bold);
            this.additionallyGroupBox.Location = new System.Drawing.Point(0, 392);
            this.additionallyGroupBox.Margin = new System.Windows.Forms.Padding(0);
            this.additionallyGroupBox.Name = "additionallyGroupBox";
            this.additionallyGroupBox.Padding = new System.Windows.Forms.Padding(0);
            this.additionallyGroupBox.Size = new System.Drawing.Size(776, 186);
            this.additionallyGroupBox.TabIndex = 12;
            this.additionallyGroupBox.TabStop = false;
            this.additionallyGroupBox.Tag = "9999";
            this.additionallyGroupBox.Text = "需要进行额外的安装操作";
            // 
            // notesTextBox
            // 
            this.notesTextBox.AcceptsTab = true;
            this.notesTextBox.AutoWordSelection = true;
            this.notesTextBox.BackColor = System.Drawing.Color.LightSalmon;
            this.notesTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.notesTextBox.Cursor = System.Windows.Forms.Cursors.Arrow;
            this.notesTextBox.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.notesTextBox.Location = new System.Drawing.Point(8, 28);
            this.notesTextBox.Margin = new System.Windows.Forms.Padding(0);
            this.notesTextBox.Name = "notesTextBox";
            this.notesTextBox.ReadOnly = true;
            this.notesTextBox.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.notesTextBox.ShortcutsEnabled = false;
            this.notesTextBox.Size = new System.Drawing.Size(760, 130);
            this.notesTextBox.TabIndex = 13;
            this.notesTextBox.TabStop = false;
            this.notesTextBox.Tag = "9999";
            this.notesTextBox.Text = "";
            this.notesTextBox.LinkClicked += new System.Windows.Forms.LinkClickedEventHandler(this.notesTextBox_LinkClicked);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("微软雅黑", 20.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label3.ForeColor = System.Drawing.Color.Green;
            this.label3.Location = new System.Drawing.Point(400, 643);
            this.label3.Margin = new System.Windows.Forms.Padding(0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(177, 35);
            this.label3.TabIndex = 9;
            this.label3.Tag = "9999";
            this.label3.Text = "已安装版本：";
            // 
            // installTypeGroup
            // 
            this.installTypeGroup.AutoSize = true;
            this.installTypeGroup.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.installTypeGroup.Font = new System.Drawing.Font("微软雅黑", 15.75F, System.Drawing.FontStyle.Bold);
            this.installTypeGroup.Location = new System.Drawing.Point(0, 266);
            this.installTypeGroup.Margin = new System.Windows.Forms.Padding(0);
            this.installTypeGroup.Name = "installTypeGroup";
            this.installTypeGroup.Padding = new System.Windows.Forms.Padding(0);
            this.installTypeGroup.Size = new System.Drawing.Size(776, 120);
            this.installTypeGroup.TabIndex = 11;
            this.installTypeGroup.TabStop = false;
            this.installTypeGroup.Tag = "9999";
            this.installTypeGroup.Text = "请选择安装方式";
            // 
            // btnRestore
            // 
            this.btnRestore.AutoSize = true;
            this.btnRestore.Enabled = false;
            this.btnRestore.Font = new System.Drawing.Font("微软雅黑", 15.75F, System.Drawing.FontStyle.Bold);
            this.btnRestore.Location = new System.Drawing.Point(0, 106);
            this.btnRestore.Margin = new System.Windows.Forms.Padding(0);
            this.btnRestore.Name = "btnRestore";
            this.btnRestore.Size = new System.Drawing.Size(776, 45);
            this.btnRestore.TabIndex = 3;
            this.btnRestore.Text = "还原所有已备份的游戏原始文件";
            this.btnRestore.UseMnemonic = false;
            this.btnRestore.UseVisualStyleBackColor = true;
            this.btnRestore.Click += new System.EventHandler(this.btnRestore_Click);
            // 
            // btnDownloadUpdate
            // 
            this.btnDownloadUpdate.AutoSize = true;
            this.btnDownloadUpdate.BackColor = System.Drawing.Color.Transparent;
            this.btnDownloadUpdate.Font = new System.Drawing.Font("微软雅黑", 15.75F, System.Drawing.FontStyle.Bold);
            this.btnDownloadUpdate.ForeColor = System.Drawing.Color.Green;
            this.btnDownloadUpdate.Location = new System.Drawing.Point(0, 588);
            this.btnDownloadUpdate.Margin = new System.Windows.Forms.Padding(0);
            this.btnDownloadUpdate.Name = "btnDownloadUpdate";
            this.btnDownloadUpdate.Size = new System.Drawing.Size(776, 45);
            this.btnDownloadUpdate.TabIndex = 6;
            this.btnDownloadUpdate.Tag = "";
            this.btnDownloadUpdate.Text = "下载最新的英文版本";
            this.btnDownloadUpdate.UseMnemonic = false;
            this.btnDownloadUpdate.UseVisualStyleBackColor = false;
            this.btnDownloadUpdate.Click += new System.EventHandler(this.btnDownloadUpdate_Click);
            // 
            // btnRemove
            // 
            this.btnRemove.AutoSize = true;
            this.btnRemove.Font = new System.Drawing.Font("微软雅黑", 15.75F, System.Drawing.FontStyle.Bold);
            this.btnRemove.Location = new System.Drawing.Point(0, 53);
            this.btnRemove.Margin = new System.Windows.Forms.Padding(0);
            this.btnRemove.Name = "btnRemove";
            this.btnRemove.Size = new System.Drawing.Size(776, 45);
            this.btnRemove.TabIndex = 2;
            this.btnRemove.Text = "从游戏卸载MOD管理器模块";
            this.btnRemove.UseMnemonic = false;
            this.btnRemove.UseVisualStyleBackColor = true;
            this.btnRemove.Click += new System.EventHandler(this.btnRemove_Click);
            // 
            // btnOpenFolder
            // 
            this.btnOpenFolder.AutoSize = true;
            this.btnOpenFolder.Font = new System.Drawing.Font("微软雅黑", 15.75F, System.Drawing.FontStyle.Bold);
            this.btnOpenFolder.ForeColor = System.Drawing.Color.Red;
            this.btnOpenFolder.Location = new System.Drawing.Point(0, 213);
            this.btnOpenFolder.Margin = new System.Windows.Forms.Padding(0);
            this.btnOpenFolder.Name = "btnOpenFolder";
            this.btnOpenFolder.Size = new System.Drawing.Size(776, 45);
            this.btnOpenFolder.TabIndex = 4;
            this.btnOpenFolder.Tag = "";
            this.btnOpenFolder.Text = "请选择游戏主目录";
            this.btnOpenFolder.UseMnemonic = false;
            this.btnOpenFolder.UseVisualStyleBackColor = true;
            this.btnOpenFolder.Click += new System.EventHandler(this.btnOpenFolder_Click);
            // 
            // btnInstall
            // 
            this.btnInstall.AutoSize = true;
            this.btnInstall.Font = new System.Drawing.Font("微软雅黑", 15.75F, System.Drawing.FontStyle.Bold);
            this.btnInstall.Location = new System.Drawing.Point(0, 0);
            this.btnInstall.Margin = new System.Windows.Forms.Padding(0);
            this.btnInstall.Name = "btnInstall";
            this.btnInstall.Size = new System.Drawing.Size(776, 45);
            this.btnInstall.TabIndex = 1;
            this.btnInstall.Text = "安装MOD管理器模块到游戏";
            this.btnInstall.UseMnemonic = false;
            this.btnInstall.UseVisualStyleBackColor = true;
            this.btnInstall.Click += new System.EventHandler(this.btnInstall_Click);
            // 
            // tabPage2
            // 
            this.tabPage2.BackColor = System.Drawing.Color.WhiteSmoke;
            this.tabPage2.Controls.Add(this.splitContainerMods);
            this.tabPage2.Location = new System.Drawing.Point(4, 39);
            this.tabPage2.Margin = new System.Windows.Forms.Padding(0);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Size = new System.Drawing.Size(776, 687);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Mods";
            // 
            // tabPage3
            // 
            this.tabPage3.BackColor = System.Drawing.Color.WhiteSmoke;
            this.tabPage3.Controls.Add(this.inputLog);
            this.tabPage3.Location = new System.Drawing.Point(4, 39);
            this.tabPage3.Margin = new System.Windows.Forms.Padding(0);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Size = new System.Drawing.Size(776, 687);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "日志";
            // 
            // inputLog
            // 
            this.inputLog.BackColor = System.Drawing.Color.WhiteSmoke;
            this.inputLog.Font = new System.Drawing.Font("微软雅黑", 12F);
            this.inputLog.Location = new System.Drawing.Point(0, 0);
            this.inputLog.Margin = new System.Windows.Forms.Padding(0);
            this.inputLog.Multiline = true;
            this.inputLog.Name = "inputLog";
            this.inputLog.ReadOnly = true;
            this.inputLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.inputLog.Size = new System.Drawing.Size(776, 687);
            this.inputLog.TabIndex = 10;
            // 
            // tabPage4
            // 
            this.tabPage4.BackColor = System.Drawing.Color.WhiteSmoke;
            this.tabPage4.Controls.Add(this.skinSetGroup);
            this.tabPage4.Location = new System.Drawing.Point(4, 39);
            this.tabPage4.Margin = new System.Windows.Forms.Padding(0);
            this.tabPage4.Name = "tabPage4";
            this.tabPage4.Size = new System.Drawing.Size(776, 687);
            this.tabPage4.TabIndex = 3;
            this.tabPage4.Text = "设置";
            // 
            // skinSetGroup
            // 
            this.skinSetGroup.AutoSize = true;
            this.skinSetGroup.Controls.Add(this.skinSetBox);
            this.skinSetGroup.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.skinSetGroup.Font = new System.Drawing.Font("微软雅黑", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.skinSetGroup.Location = new System.Drawing.Point(0, 0);
            this.skinSetGroup.Margin = new System.Windows.Forms.Padding(0);
            this.skinSetGroup.Name = "skinSetGroup";
            this.skinSetGroup.Padding = new System.Windows.Forms.Padding(0);
            this.skinSetGroup.Size = new System.Drawing.Size(776, 100);
            this.skinSetGroup.TabIndex = 0;
            this.skinSetGroup.TabStop = false;
            this.skinSetGroup.Tag = "9999";
            this.skinSetGroup.Text = "个性化皮肤设置";
            // 
            // skinSetBox
            // 
            this.skinSetBox.BackColor = System.Drawing.Color.LightGreen;
            this.skinSetBox.DropDownHeight = 400;
            this.skinSetBox.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.skinSetBox.FormattingEnabled = true;
            this.skinSetBox.IntegralHeight = false;
            this.skinSetBox.ItemHeight = 28;
            this.skinSetBox.Location = new System.Drawing.Point(8, 32);
            this.skinSetBox.Margin = new System.Windows.Forms.Padding(0);
            this.skinSetBox.MaxDropDownItems = 10;
            this.skinSetBox.Name = "skinSetBox";
            this.skinSetBox.Size = new System.Drawing.Size(300, 36);
            this.skinSetBox.TabIndex = 0;
            this.skinSetBox.Tag = "9999";
            this.skinSetBox.SelectedIndexChanged += new System.EventHandler(this.UnityModLoaderForm_SkinChanged);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.statusStrip1.Font = new System.Drawing.Font("微软雅黑", 15.75F);
            this.statusStrip1.GripMargin = new System.Windows.Forms.Padding(0);
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.statusLabel});
            this.statusStrip1.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.Flow;
            this.statusStrip1.Location = new System.Drawing.Point(0, 0);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Padding = new System.Windows.Forms.Padding(1, 0, 22, 0);
            this.statusStrip1.ShowItemToolTips = true;
            this.statusStrip1.Size = new System.Drawing.Size(784, 30);
            this.statusStrip1.TabIndex = 0;
            this.statusStrip1.Text = "statusStrip1";
            this.statusStrip1.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.statusStrip1_ItemClicked);
            // 
            // statusLabel
            // 
            this.statusLabel.Font = new System.Drawing.Font("微软雅黑", 14.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))));
            this.statusLabel.Margin = new System.Windows.Forms.Padding(0);
            this.statusLabel.MergeAction = System.Windows.Forms.MergeAction.Replace;
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Overflow = System.Windows.Forms.ToolStripItemOverflow.Never;
            this.statusLabel.Size = new System.Drawing.Size(50, 26);
            this.statusLabel.Text = "就绪";
            this.statusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // panelMain
            // 
            this.panelMain.Controls.Add(this.splitContainerMain);
            this.panelMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelMain.Location = new System.Drawing.Point(0, 0);
            this.panelMain.Margin = new System.Windows.Forms.Padding(0);
            this.panelMain.Name = "panelMain";
            this.panelMain.Size = new System.Drawing.Size(784, 761);
            this.panelMain.TabIndex = 3;
            // 
            // folderBrowserDialog
            // 
            this.folderBrowserDialog.RootFolder = System.Environment.SpecialFolder.MyComputer;
            this.folderBrowserDialog.HelpRequest += new System.EventHandler(this.folderBrowserDialog_HelpRequest);
            // 
            // modInstallFileDialog
            // 
            this.modInstallFileDialog.Filter = "ZIP|*.zip";
            this.modInstallFileDialog.Multiselect = true;
            // 
            // skinEngine
            // 
            this.skinEngine.@__DrawButtonFocusRectangle = true;
            this.skinEngine.BuiltIn = false;
            this.skinEngine.DefaultButtonStyle = Sunisoft.IrisSkin.DefaultButtonStyle.Shadow;
            this.skinEngine.DisabledButtonTextColor = System.Drawing.Color.Gray;
            this.skinEngine.DisabledMenuFontColor = System.Drawing.SystemColors.GrayText;
            this.skinEngine.InactiveCaptionColor = System.Drawing.SystemColors.InactiveCaptionText;
            this.skinEngine.MenuFont = global::UnityModManagerNet.Installer.Properties.Settings.Default.MenuFont;
            this.skinEngine.SerialNumber = "";
            this.skinEngine.SkinDialogs = false;
            this.skinEngine.SkinFile = null;
            this.skinEngine.TitleFont = global::UnityModManagerNet.Installer.Properties.Settings.Default.TitleFont;
            // 
            // UnityModManagerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 19F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 761);
            this.Controls.Add(this.panelMain);
            this.DataBindings.Add(new System.Windows.Forms.Binding("Font", global::UnityModManagerNet.Installer.Properties.Settings.Default, "TitleFont", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.DoubleBuffered = true;
            this.Font = global::UnityModManagerNet.Installer.Properties.Settings.Default.TitleFont;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "UnityModManagerForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "亲爱的Unity游戏MOD管理器（允哥修正&汉化&美化特别版）";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.UnityModLoaderForm_FormClosing);
            this.SizeChanged += new System.EventHandler(this.UnityModLoaderForm_SizeChanged);
            this.splitContainerMods.Panel1.ResumeLayout(false);
            this.splitContainerMods.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerMods)).EndInit();
            this.splitContainerMods.ResumeLayout(false);
            this.ModcontextMenuStrip1.ResumeLayout(false);
            this.splitContainerModsInstall.Panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerModsInstall)).EndInit();
            this.splitContainerModsInstall.ResumeLayout(false);
            this.splitContainerMain.Panel1.ResumeLayout(false);
            this.splitContainerMain.Panel2.ResumeLayout(false);
            this.splitContainerMain.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerMain)).EndInit();
            this.splitContainerMain.ResumeLayout(false);
            this.tabControl.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.additionallyGroupBox.ResumeLayout(false);
            this.tabPage2.ResumeLayout(false);
            this.tabPage3.ResumeLayout(false);
            this.tabPage3.PerformLayout();
            this.tabPage4.ResumeLayout(false);
            this.tabPage4.PerformLayout();
            this.skinSetGroup.ResumeLayout(false);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.panelMain.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Panel panelMain;
        private System.Windows.Forms.Label currentVersion;
        private System.Windows.Forms.Label installedVersion;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox gameList;
        private System.Windows.Forms.Button btnOpenFolder;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;
        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.Button btnRemove;
        private System.Windows.Forms.Button btnInstall;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.SplitContainer splitContainerMods;
        private System.Windows.Forms.ListView listMods;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.SplitContainer splitContainerMain;
        private System.Windows.Forms.TabPage tabPage3;
        public System.Windows.Forms.TextBox inputLog;
        public System.Windows.Forms.ToolStripStatusLabel statusLabel;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.ColumnHeader columnHeader4;
        private System.Windows.Forms.ContextMenuStrip ModcontextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem updateToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem installToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem uninstallToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem revertToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem wwwToolStripMenuItem1;
        private System.Windows.Forms.SplitContainer splitContainerModsInstall;
        private System.Windows.Forms.Button btnModInstall;
        private System.Windows.Forms.OpenFileDialog modInstallFileDialog;
        private System.Windows.Forms.Button btnDownloadUpdate;
        private System.Windows.Forms.Button btnRestore;
        private System.Windows.Forms.GroupBox installTypeGroup;
        private System.Windows.Forms.RichTextBox notesTextBox;
        private System.Windows.Forms.GroupBox additionallyGroupBox;
        private Sunisoft.IrisSkin.SkinEngine skinEngine;
        private System.Windows.Forms.TabPage tabPage4;
        private System.Windows.Forms.GroupBox skinSetGroup;
        private System.Windows.Forms.ComboBox skinSetBox;
    }
}

