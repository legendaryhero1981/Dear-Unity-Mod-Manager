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
            resources.ApplyResources(this.splitContainerMods, "splitContainerMods");
            this.splitContainerMods.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitContainerMods.Name = "splitContainerMods";
            // 
            // splitContainerMods.Panel1
            // 
            this.splitContainerMods.Panel1.Controls.Add(this.listMods);
            // 
            // splitContainerMods.Panel2
            // 
            resources.ApplyResources(this.splitContainerMods.Panel2, "splitContainerMods.Panel2");
            this.splitContainerMods.Panel2.Controls.Add(this.splitContainerModsInstall);
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
            resources.ApplyResources(this.listMods, "listMods");
            this.listMods.FullRowSelect = true;
            this.listMods.HideSelection = false;
            this.listMods.MultiSelect = false;
            this.listMods.Name = "listMods";
            this.listMods.ShowItemToolTips = true;
            this.listMods.Sorting = System.Windows.Forms.SortOrder.Ascending;
            this.listMods.UseCompatibleStateImageBehavior = false;
            this.listMods.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader1
            // 
            resources.ApplyResources(this.columnHeader1, "columnHeader1");
            // 
            // columnHeader2
            // 
            resources.ApplyResources(this.columnHeader2, "columnHeader2");
            // 
            // columnHeader3
            // 
            resources.ApplyResources(this.columnHeader3, "columnHeader3");
            // 
            // columnHeader4
            // 
            resources.ApplyResources(this.columnHeader4, "columnHeader4");
            // 
            // ModcontextMenuStrip1
            // 
            resources.ApplyResources(this.ModcontextMenuStrip1, "ModcontextMenuStrip1");
            this.ModcontextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.installToolStripMenuItem,
            this.updateToolStripMenuItem,
            this.revertToolStripMenuItem,
            this.uninstallToolStripMenuItem,
            this.wwwToolStripMenuItem1});
            this.ModcontextMenuStrip1.Name = "ModcontextMenuStrip1";
            this.ModcontextMenuStrip1.Opening += new System.ComponentModel.CancelEventHandler(this.ModcontextMenuStrip1_Opening);
            // 
            // installToolStripMenuItem
            // 
            this.installToolStripMenuItem.Name = "installToolStripMenuItem";
            resources.ApplyResources(this.installToolStripMenuItem, "installToolStripMenuItem");
            this.installToolStripMenuItem.Click += new System.EventHandler(this.installToolStripMenuItem_Click);
            // 
            // updateToolStripMenuItem
            // 
            this.updateToolStripMenuItem.Name = "updateToolStripMenuItem";
            resources.ApplyResources(this.updateToolStripMenuItem, "updateToolStripMenuItem");
            this.updateToolStripMenuItem.Click += new System.EventHandler(this.updateToolStripMenuItem_Click);
            // 
            // revertToolStripMenuItem
            // 
            this.revertToolStripMenuItem.Name = "revertToolStripMenuItem";
            resources.ApplyResources(this.revertToolStripMenuItem, "revertToolStripMenuItem");
            this.revertToolStripMenuItem.Click += new System.EventHandler(this.revertToolStripMenuItem_Click);
            // 
            // uninstallToolStripMenuItem
            // 
            this.uninstallToolStripMenuItem.Name = "uninstallToolStripMenuItem";
            resources.ApplyResources(this.uninstallToolStripMenuItem, "uninstallToolStripMenuItem");
            this.uninstallToolStripMenuItem.Click += new System.EventHandler(this.uninstallToolStripMenuItem_Click);
            // 
            // wwwToolStripMenuItem1
            // 
            this.wwwToolStripMenuItem1.Name = "wwwToolStripMenuItem1";
            resources.ApplyResources(this.wwwToolStripMenuItem1, "wwwToolStripMenuItem1");
            this.wwwToolStripMenuItem1.Click += new System.EventHandler(this.wwwToolStripMenuItem1_Click);
            // 
            // splitContainerModsInstall
            // 
            resources.ApplyResources(this.splitContainerModsInstall, "splitContainerModsInstall");
            this.splitContainerModsInstall.Name = "splitContainerModsInstall";
            // 
            // splitContainerModsInstall.Panel1
            // 
            this.splitContainerModsInstall.Panel1.Controls.Add(this.btnModInstall);
            // 
            // splitContainerModsInstall.Panel2
            // 
            this.splitContainerModsInstall.Panel2.BackgroundImage = global::UnityModManagerNet.Installer.Properties.Resources.dragdropfiles;
            resources.ApplyResources(this.splitContainerModsInstall.Panel2, "splitContainerModsInstall.Panel2");
            this.splitContainerModsInstall.Panel2.Paint += new System.Windows.Forms.PaintEventHandler(this.splitContainerModsInstall_Panel2_Paint);
            // 
            // btnModInstall
            // 
            resources.ApplyResources(this.btnModInstall, "btnModInstall");
            this.btnModInstall.ForeColor = System.Drawing.Color.Green;
            this.btnModInstall.Name = "btnModInstall";
            this.btnModInstall.UseVisualStyleBackColor = true;
            this.btnModInstall.Click += new System.EventHandler(this.btnModInstall_Click);
            // 
            // splitContainerMain
            // 
            resources.ApplyResources(this.splitContainerMain, "splitContainerMain");
            this.splitContainerMain.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitContainerMain.Name = "splitContainerMain";
            // 
            // splitContainerMain.Panel1
            // 
            this.splitContainerMain.Panel1.Controls.Add(this.tabControl);
            // 
            // splitContainerMain.Panel2
            // 
            this.splitContainerMain.Panel2.Controls.Add(this.statusStrip1);
            this.splitContainerMain.Panel2.Paint += new System.Windows.Forms.PaintEventHandler(this.splitContainerMain_Panel2_Paint);
            // 
            // tabControl
            // 
            this.tabControl.ContextMenuStrip = this.ModcontextMenuStrip1;
            this.tabControl.Controls.Add(this.tabPage1);
            this.tabControl.Controls.Add(this.tabPage2);
            this.tabControl.Controls.Add(this.tabPage3);
            this.tabControl.Controls.Add(this.tabPage4);
            resources.ApplyResources(this.tabControl, "tabControl");
            this.tabControl.HotTrack = true;
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
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
            resources.ApplyResources(this.tabPage1, "tabPage1");
            this.tabPage1.Name = "tabPage1";
            // 
            // installedVersion
            // 
            resources.ApplyResources(this.installedVersion, "installedVersion");
            this.installedVersion.ForeColor = System.Drawing.Color.Green;
            this.installedVersion.Name = "installedVersion";
            this.installedVersion.Tag = "9999";
            // 
            // gameList
            // 
            this.gameList.BackColor = System.Drawing.Color.WhiteSmoke;
            this.gameList.DropDownHeight = 460;
            resources.ApplyResources(this.gameList, "gameList");
            this.gameList.FormattingEnabled = true;
            this.gameList.Name = "gameList";
            this.gameList.Sorted = true;
            this.gameList.Tag = "9999";
            this.gameList.SelectedIndexChanged += new System.EventHandler(this.gameList_Changed);
            // 
            // currentVersion
            // 
            resources.ApplyResources(this.currentVersion, "currentVersion");
            this.currentVersion.ForeColor = System.Drawing.Color.Green;
            this.currentVersion.Name = "currentVersion";
            this.currentVersion.Tag = "9999";
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.ForeColor = System.Drawing.Color.Green;
            this.label2.Name = "label2";
            this.label2.Tag = "9999";
            // 
            // additionallyGroupBox
            // 
            resources.ApplyResources(this.additionallyGroupBox, "additionallyGroupBox");
            this.additionallyGroupBox.Controls.Add(this.notesTextBox);
            this.additionallyGroupBox.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.additionallyGroupBox.Name = "additionallyGroupBox";
            this.additionallyGroupBox.TabStop = false;
            this.additionallyGroupBox.Tag = "9999";
            // 
            // notesTextBox
            // 
            this.notesTextBox.AcceptsTab = true;
            this.notesTextBox.AutoWordSelection = true;
            this.notesTextBox.BackColor = System.Drawing.Color.LightSalmon;
            this.notesTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.notesTextBox.Cursor = System.Windows.Forms.Cursors.Arrow;
            resources.ApplyResources(this.notesTextBox, "notesTextBox");
            this.notesTextBox.Name = "notesTextBox";
            this.notesTextBox.ReadOnly = true;
            this.notesTextBox.ShortcutsEnabled = false;
            this.notesTextBox.TabStop = false;
            this.notesTextBox.Tag = "9999";
            this.notesTextBox.LinkClicked += new System.Windows.Forms.LinkClickedEventHandler(this.notesTextBox_LinkClicked);
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.ForeColor = System.Drawing.Color.Green;
            this.label3.Name = "label3";
            this.label3.Tag = "9999";
            // 
            // installTypeGroup
            // 
            resources.ApplyResources(this.installTypeGroup, "installTypeGroup");
            this.installTypeGroup.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.installTypeGroup.Name = "installTypeGroup";
            this.installTypeGroup.TabStop = false;
            this.installTypeGroup.Tag = "9999";
            // 
            // btnRestore
            // 
            resources.ApplyResources(this.btnRestore, "btnRestore");
            this.btnRestore.Name = "btnRestore";
            this.btnRestore.UseMnemonic = false;
            this.btnRestore.UseVisualStyleBackColor = true;
            this.btnRestore.Click += new System.EventHandler(this.btnRestore_Click);
            // 
            // btnDownloadUpdate
            // 
            resources.ApplyResources(this.btnDownloadUpdate, "btnDownloadUpdate");
            this.btnDownloadUpdate.BackColor = System.Drawing.Color.Transparent;
            this.btnDownloadUpdate.ForeColor = System.Drawing.Color.Green;
            this.btnDownloadUpdate.Name = "btnDownloadUpdate";
            this.btnDownloadUpdate.Tag = "";
            this.btnDownloadUpdate.UseMnemonic = false;
            this.btnDownloadUpdate.UseVisualStyleBackColor = false;
            this.btnDownloadUpdate.Click += new System.EventHandler(this.btnDownloadUpdate_Click);
            // 
            // btnRemove
            // 
            resources.ApplyResources(this.btnRemove, "btnRemove");
            this.btnRemove.Name = "btnRemove";
            this.btnRemove.UseMnemonic = false;
            this.btnRemove.UseVisualStyleBackColor = true;
            this.btnRemove.Click += new System.EventHandler(this.btnRemove_Click);
            // 
            // btnOpenFolder
            // 
            resources.ApplyResources(this.btnOpenFolder, "btnOpenFolder");
            this.btnOpenFolder.ForeColor = System.Drawing.Color.Red;
            this.btnOpenFolder.Name = "btnOpenFolder";
            this.btnOpenFolder.Tag = "";
            this.btnOpenFolder.UseMnemonic = false;
            this.btnOpenFolder.UseVisualStyleBackColor = true;
            this.btnOpenFolder.Click += new System.EventHandler(this.btnOpenFolder_Click);
            // 
            // btnInstall
            // 
            resources.ApplyResources(this.btnInstall, "btnInstall");
            this.btnInstall.Name = "btnInstall";
            this.btnInstall.UseMnemonic = false;
            this.btnInstall.UseVisualStyleBackColor = true;
            this.btnInstall.Click += new System.EventHandler(this.btnInstall_Click);
            // 
            // tabPage2
            // 
            this.tabPage2.BackColor = System.Drawing.Color.WhiteSmoke;
            this.tabPage2.Controls.Add(this.splitContainerMods);
            resources.ApplyResources(this.tabPage2, "tabPage2");
            this.tabPage2.Name = "tabPage2";
            // 
            // tabPage3
            // 
            this.tabPage3.BackColor = System.Drawing.Color.WhiteSmoke;
            this.tabPage3.Controls.Add(this.inputLog);
            resources.ApplyResources(this.tabPage3, "tabPage3");
            this.tabPage3.Name = "tabPage3";
            // 
            // inputLog
            // 
            this.inputLog.BackColor = System.Drawing.Color.WhiteSmoke;
            resources.ApplyResources(this.inputLog, "inputLog");
            this.inputLog.Name = "inputLog";
            this.inputLog.ReadOnly = true;
            // 
            // tabPage4
            // 
            this.tabPage4.BackColor = System.Drawing.Color.WhiteSmoke;
            this.tabPage4.Controls.Add(this.skinSetGroup);
            resources.ApplyResources(this.tabPage4, "tabPage4");
            this.tabPage4.Name = "tabPage4";
            // 
            // skinSetGroup
            // 
            resources.ApplyResources(this.skinSetGroup, "skinSetGroup");
            this.skinSetGroup.Controls.Add(this.skinSetBox);
            this.skinSetGroup.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.skinSetGroup.Name = "skinSetGroup";
            this.skinSetGroup.TabStop = false;
            this.skinSetGroup.Tag = "9999";
            // 
            // skinSetBox
            // 
            this.skinSetBox.BackColor = System.Drawing.Color.LightGreen;
            this.skinSetBox.DropDownHeight = 400;
            resources.ApplyResources(this.skinSetBox, "skinSetBox");
            this.skinSetBox.FormattingEnabled = true;
            this.skinSetBox.Name = "skinSetBox";
            this.skinSetBox.Tag = "9999";
            this.skinSetBox.SelectedIndexChanged += new System.EventHandler(this.UnityModLoaderForm_SkinChanged);
            // 
            // statusStrip1
            // 
            resources.ApplyResources(this.statusStrip1, "statusStrip1");
            this.statusStrip1.GripMargin = new System.Windows.Forms.Padding(0);
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.statusLabel});
            this.statusStrip1.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.Flow;
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.ShowItemToolTips = true;
            this.statusStrip1.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.statusStrip1_ItemClicked);
            // 
            // statusLabel
            // 
            resources.ApplyResources(this.statusLabel, "statusLabel");
            this.statusLabel.Margin = new System.Windows.Forms.Padding(0);
            this.statusLabel.MergeAction = System.Windows.Forms.MergeAction.Replace;
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Overflow = System.Windows.Forms.ToolStripItemOverflow.Never;
            // 
            // panelMain
            // 
            this.panelMain.Controls.Add(this.splitContainerMain);
            resources.ApplyResources(this.panelMain, "panelMain");
            this.panelMain.Name = "panelMain";
            // 
            // folderBrowserDialog
            // 
            this.folderBrowserDialog.RootFolder = System.Environment.SpecialFolder.MyComputer;
            this.folderBrowserDialog.HelpRequest += new System.EventHandler(this.folderBrowserDialog_HelpRequest);
            // 
            // modInstallFileDialog
            // 
            resources.ApplyResources(this.modInstallFileDialog, "modInstallFileDialog");
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
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.panelMain);
            this.DataBindings.Add(new System.Windows.Forms.Binding("Font", global::UnityModManagerNet.Installer.Properties.Settings.Default, "TitleFont", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.DoubleBuffered = true;
            this.Font = global::UnityModManagerNet.Installer.Properties.Settings.Default.TitleFont;
            this.Name = "UnityModManagerForm";
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

