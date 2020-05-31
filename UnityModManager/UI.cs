using HarmonyLib;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

using static UnityModManagerNet.UI.Utils.ImguiUtil;

using GC = UnityEngine.GUIContent;
using GL = UnityEngine.GUILayout;
using GS = UnityEngine.GUIStyle;

namespace UnityModManagerNet
{
    public partial class UnityModManager
    {
        public partial class UI : MonoBehaviour
        {
            public const float UiScaleMin = 1f;
            public const float UiScaleMax = 2f;
            public const int WindowWidthMin = 1000;
            public const int WindowHeightMin = 800;
            public const string GlobalFontName = "微软雅黑";
            public const int GlobalFontSize = 20;
            public const int H1FontSize = GlobalFontSize + 4;
            public const int H2FontSize = GlobalFontSize + 2;

            /// <summary>
            /// [0.20.0.17] 新增多种自定义GUIStyle样式
            /// </summary>
            public static GS WindowStyle;
            public static GS H1FontStyle;
            public static GS H2FontStyle;
            public static GS BoldFontStyle;
            public static GS ButtonStyle;
            public static GS IconStyle;
            public static GS ToggleStyle;
            public static GS HSliderStyle;
            public static GS HSliderThumbStyle;
            public static GS CenterFontStyle;
            public static GS NormalFontStyle;
            /// <summary>
            /// [0.22.4.30] 新增UMM的GUIStyle样式，以便支持UMM相关Mod调用。
            /// </summary>
            public static GS window;
            public static GS h1;
            public static GS h2;
            public static GS bold;
            public static GS button;

            private static readonly string[] MCheckUpdateStrings = { "从不", "自动" };
            private static readonly string[] MShowOnStartStrings = { "否", "是" };

            private static int _mLastWindowId;
            private readonly List<Column> _mColumns = new List<Column>();
            private readonly List<Column> _mOriginColumns = new List<Column>
            {
                new Column {Name = "名称", Width = 400, Expand = true},
                new Column {Name = "版本", Width = 200},
                new Column {Name = "依赖MOD", Width = 160, Expand = true},
                new Column {Name = "开/关", Width = 120},
                new Column {Name = "状态", Width = 100}
            };

            /// <summary>
            /// [0.21.1.20] 新增Mod依赖列表字段
            /// </summary>
            private static List<string> _mJoinList = new List<string>();

            /// <summary>
            /// [0.20.0.17] 当前选项卡的ScrollView控件位置
            /// </summary>
            public static Vector2 ScrollViewPosition => MScrollPosition[_tabId];
            /// <summary>
            /// [0.20.0.17] 窗口位置
            /// </summary>
            public static Vector2 WindowPosition => _mWindowRect.position;
            /// <summary>
            ///  [0.20.0.16] 窗口大小
            /// </summary>
            public static Vector2 WindowSize => _mWindowSize;
            private static Vector2 _mWindowSize = Vector2.zero;

            private GameObject _mCanvas;
            private Resolution _mCurrentResolution;
            private float _mExpectedUiScale = 1f;
            private Vector2 _mExpectedWindowSize = Vector2.zero;
            private bool _mFirstLaunched;
            private bool _mInit;
            private int _mPreviousShowModSettings = -1;
            private int _mShowModSettings = -1;
            private float _mUiScale = 1f;
            private bool _mUiScaleChanged;
            private static Rect _mWindowRect = new Rect(0, 0, 0, 0);

            public static readonly string[] Tabs = { "Mods", "日志", "设置" };
            private static int _tabId;
            private static readonly Vector2[] MScrollPosition = new Vector2[Tabs.Length];
            private static Texture2D _mBackground;
            private static readonly string FilePathBackground = Path.Combine(Path.GetDirectoryName(typeof(UI).Assembly.Location), "background.jpg");

            public static UI Instance { get; private set; }
            public bool Opened { get; private set; }

            /// <summary>
            /// [0.20.0.17] 显示控件的文本提示
            /// </summary>
            public static void ShowTooltip()
            {
                if (string.IsNullOrEmpty(GUI.tooltip)) return;
                var tooltip = new GC(GUI.tooltip);
                var styleRect = GUI.skin.box;
                var tooltipSize = styleRect.CalcSize(tooltip);
                var textHeight = styleRect.CalcHeight(tooltip, tooltipSize.x);
                var styleTooltip = new GS() { normal = { background = new Texture2D(1, 1) } };
                styleTooltip.normal.background.SetPixels32(new[] { new Color32(0, 0, 0, 220) });
                var x = (WindowSize.x - tooltipSize.x) / 2;
                var y = Screen.height + ScrollViewPosition.y - WindowPosition.y - Input.mousePosition.y - Scale(H1FontSize + GlobalFontSize * 2) - textHeight;
                GUI.Label(new Rect(x, y, tooltipSize.x, tooltipSize.y), GUI.tooltip, styleTooltip);
                //GUI.Label(new Rect(x, y, tooltipSize.x, tooltipSize.y), $"x={x},y={y},sx={ScrollViewPosition.x},sy={ScrollViewPosition.y},my={Input.mousePosition.y},th={textHeight},ry={WindowPosition.y}", styleTooltip);
            }

            private int ShowModSettings
            {
                get => _mShowModSettings;
                set
                {
                    void Hide(ModEntry mod)
                    {
                        if (!mod.Active || mod.OnHideGUI == null || mod.OnGUI == null) return;
                        try
                        {
                            mod.OnHideGUI(mod);
                        }
                        catch (Exception e)
                        {
                            mod.Logger.LogException("OnHideGUI", e);
                        }
                    }

                    void Show(ModEntry mod)
                    {
                        if (!mod.Active || mod.OnShowGUI == null || mod.OnGUI == null) return;
                        try
                        {
                            mod.OnShowGUI(mod);
                        }
                        catch (Exception e)
                        {
                            mod.Logger.LogException("OnShowGUI", e);
                        }
                    }

                    _mShowModSettings = value;
                    if (_mShowModSettings == _mPreviousShowModSettings) return;
                    if (_mShowModSettings == -1)
                    {
                        Hide(ModEntries[_mPreviousShowModSettings]);
                    }
                    else if (_mPreviousShowModSettings == -1)
                    {
                        Show(ModEntries[_mShowModSettings]);
                    }
                    else
                    {
                        Hide(ModEntries[_mPreviousShowModSettings]);
                        Show(ModEntries[_mShowModSettings]);
                    }

                    _mPreviousShowModSettings = _mShowModSettings;
                }
            }

            internal static bool Load()
            {
                try
                {
                    var gameObject = new GameObject(typeof(UI).FullName, typeof(UI));
                    return true;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                return false;
            }

            /// <summary>
            /// [0.21.4.24] 使用独立的协程异步高效地执行来自各Mod的Actions
            /// </summary>
            private static IEnumerator<object> DoActionsFromMods
            {
                get
                {
                    Logger.Log($"已启动协程 {typeof(UI).FullName}.DoActionsFromMods！");
                    var waitUntil = new WaitUntil(() => DoActionsAsync().IsCompleted);
                    var waitSeconds = new WaitForSecondsRealtime(1f);
                    while (true)
                    {
                        yield return waitUntil;
                        yield return waitSeconds;
                    }
                }
            }

            private static async Task<int> DoActionsAsync()
            {
                return await Task.Run(() =>
                {
                    var count = 0;
                    ModEntries.FindAll(m => 0 < m.OnModActions.Count).ForEach(m =>
                    {
                        while (0 < m.OnModActions.Count)
                        {
                            m.OnModActions.TryPop(out var action);
                            action(m);
                            Logger.Log($"异步任务 {action?.Method.FullDescription()} 执行完毕！");
                            count++;
                        }
                    });
                    if (0 < count)
                        Logger.Log($"异步任务执行器 {typeof(UI).FullName}.DoActionsAsync 本次扫描共执行了{count}个任务！");
                    return count;
                });
            }

            private void Awake()
            {
                Instance = this;
                DontDestroyOnLoad(this);
                _mWindowSize = new Vector2(Params.WindowWidth, Params.WindowHeight);
                CorrectWindowSize();
                _mExpectedWindowSize = _mWindowSize;
                _mUiScale = Mathf.Clamp(Params.UIScale, UiScaleMin, UiScaleMax);
                _mExpectedUiScale = _mUiScale;
                Textures.Init();
                if (null == _mBackground)
                {
                    _mBackground = FileToTexture2D(FilePathBackground, (int)_mWindowSize.x, (int)_mWindowSize.y);
                    if (null == _mBackground)
                        _mBackground = "1".Equals(Config.FixBlackUI) || "true".Equals(Config.FixBlackUI?.ToLower()) ? Textures.WindowLighter : Textures.Window;
                }
                StartCoroutine(DoActionsFromMods);
            }

            private void Start()
            {
                CalculateWindowPos();
                if (string.IsNullOrEmpty(Config.UIStartingPoint)) FirstLaunch();
                if (Params.CheckUpdates == 1) CheckModUpdates();
            }

            private void OnDestroy()
            {
                Logger.Log($"已关闭协程 {typeof(UI).FullName}.DoActionsFromMods！");
                StopCoroutine(DoActionsFromMods);
                SaveSettingsAndParams();
                Logger.WriteBuffers();
            }

            private void Update()
            {
                if (Opened)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }

                var deltaTime = Time.deltaTime;
                foreach (var mod in ModEntries)
                {
                    if (!mod.Active || mod.OnUpdate == null) continue;
                    try
                    {
                        mod.OnUpdate.Invoke(mod, deltaTime);
                    }
                    catch (Exception e)
                    {
                        mod.Logger.LogException("OnUpdate", e);
                    }
                }

                if (Params.Hotkey.Up() || Input.GetKeyUp(KeyCode.F10) && KeyBinding.Ctrl() || Opened && Input.GetKey(KeyCode.Escape))
                    ToggleWindow();
            }

            private void FixedUpdate()
            {
                var deltaTime = Time.fixedDeltaTime;
                foreach (var mod in ModEntries)
                    if (mod.Active && mod.OnFixedUpdate != null)
                        try
                        {
                            mod.OnFixedUpdate.Invoke(mod, deltaTime);
                        }
                        catch (Exception e)
                        {
                            mod.Logger.LogException("OnFixedUpdate", e);
                        }
            }

            private void LateUpdate()
            {
                var deltaTime = Time.deltaTime;
                foreach (var mod in ModEntries)
                    if (mod.Active && mod.OnLateUpdate != null)
                        try
                        {
                            mod.OnLateUpdate.Invoke(mod, deltaTime);
                        }
                        catch (Exception e)
                        {
                            mod.Logger.LogException("OnLateUpdate", e);
                        }

                Logger.Watcher(deltaTime);
            }

            private static void PrepareGui()
            {
                WindowStyle = window = new GS()
                {
                    name = "umm window",
                    normal = { textColor = Color.white, background = _mBackground },
                    fontSize = H1FontSize,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.UpperCenter,
                    padding = RectOffset(0),
                    margin = RectOffset(0),
                    wordWrap = true
                };
                WindowStyle.normal.background.wrapMode = TextureWrapMode.Repeat;
                H1FontStyle = h1 = new GS(GUI.skin.label)
                {
                    name = "umm h1",
                    normal = { textColor = Color.white },
                    fontSize = H1FontSize,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    padding = RectOffset(GlobalFontSize / 4),
                    margin = RectOffset(GlobalFontSize / 4),
                    wordWrap = true
                };
                H2FontStyle = h2 = new GS(H1FontStyle) { name = "umm h2", fontSize = H2FontSize };
                CenterFontStyle = new GS(H2FontStyle) { name = "umm center", fontSize = GlobalFontSize };
                BoldFontStyle = bold = new GS(CenterFontStyle) { name = "umm bold", alignment = TextAnchor.MiddleLeft };
                NormalFontStyle = new GS(BoldFontStyle) { name = "umm normal", fontStyle = FontStyle.Normal };
                ButtonStyle = button = new GS(GUI.skin.button)
                {
                    name = "umm button",
                    normal = { textColor = Color.white },
                    fontSize = GlobalFontSize,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.UpperCenter,
                    padding = new RectOffset(GlobalFontSize / 4, GlobalFontSize / 2, GlobalFontSize / 4, GlobalFontSize / 2),
                    margin = RectOffset(GlobalFontSize / 4),
                    wordWrap = false
                };
                IconStyle = new GS(GUI.skin.box)
                {
                    name = "umm icon",
                    alignment = TextAnchor.MiddleCenter,
                    padding = RectOffset(0),
                    margin = RectOffset(GlobalFontSize / 4, GlobalFontSize / 2),
                    stretchHeight = true,
                    stretchWidth = true,
                };
                HSliderStyle = new GS(GUI.skin.horizontalSlider)
                {
                    fixedHeight = GlobalFontSize,
                    padding = RectOffset(0),
                    margin = RectOffset(GlobalFontSize / 4, GlobalFontSize / 2)
                };
                HSliderThumbStyle = new GS(GUI.skin.horizontalSliderThumb)
                {
                    fixedHeight = GlobalFontSize,
                    padding = RectOffset(GlobalFontSize / 2, 0),
                    margin = RectOffset(0)
                };
                ToggleStyle = new GS(GUI.skin.toggle)
                {
                    name = "umm toggle",
                    normal = { textColor = Color.white },
                    fontSize = GlobalFontSize,
                    alignment = TextAnchor.MiddleCenter,
                    padding = RectOffset(GlobalFontSize / 2, GlobalFontSize / 4),
                    margin = RectOffset(GlobalFontSize / 4),
                    wordWrap = true
                };
            }

            private void ScaleGui()
            {
                GUI.skin.font = Font.CreateDynamicFontFromOSFont(GlobalFontName, Scale(GlobalFontSize));
                GUI.skin.horizontalSlider = HSliderStyle;
                GUI.skin.horizontalSliderThumb = HSliderThumbStyle;
                GUI.skin.toggle = ToggleStyle;
                GUI.skin.button = ButtonStyle;
                GUI.skin.label = NormalFontStyle;
                HSliderStyle.fixedHeight = HSliderThumbStyle.fixedHeight = Scale(GlobalFontSize);
                ToggleStyle.fontSize = ButtonStyle.fontSize = NormalFontStyle.fontSize = CenterFontStyle.fontSize = BoldFontStyle.fontSize = Scale(GlobalFontSize);
                WindowStyle.fontSize = H1FontStyle.fontSize = Scale(H1FontSize);
                IconStyle.fixedWidth = IconStyle.fixedHeight = H2FontStyle.fontSize = Scale(H2FontSize);
                _mColumns.Clear();
                foreach (var column in _mOriginColumns)
                    _mColumns.Add(new Column { Name = column.Name, Width = Scale(column.Width), Expand = column.Expand, Skip = column.Skip });
            }

            private void OnGUI()
            {
                if (!_mInit)
                {
                    _mInit = true;
                    PrepareGui();
                    ScaleGui();
                }

                var toRemove = new List<PopupToggleGroup_GUI>(0);
                var anyRendered = false;
                foreach (var item in PopupToggleGroup_GUI.mList)
                {
                    item.mDestroyCounter.Add(Time.frameCount);
                    if (item.mDestroyCounter.Count > 1)
                    {
                        toRemove.Add(item);
                        continue;
                    }

                    if (!item.Opened || anyRendered) continue;
                    item.Render();
                    anyRendered = true;
                }

                foreach (var item in toRemove) PopupToggleGroup_GUI.mList.Remove(item);
                if (Opened)
                {
                    if (_mCurrentResolution.width != Screen.currentResolution.width || _mCurrentResolution.height != Screen.currentResolution.height)
                    {
                        _mCurrentResolution = Screen.currentResolution;
                        CalculateWindowPos();
                    }

                    if (_mUiScaleChanged)
                    {
                        _mUiScaleChanged = false;
                        ScaleGui();
                    }
                    _mWindowRect = GL.Window(0, _mWindowRect, WindowFunction, $"亲爱的Unity游戏Mod管理器v{version}（允哥修正&汉化&美化特别版）", WindowStyle, GL.Width(_mWindowSize.x), GL.Height(_mWindowSize.y));
                }

                foreach (var mod in ModEntries)
                {
                    if (!mod.Active || mod.OnFixedGUI == null) continue;
                    try
                    {
                        mod.OnFixedGUI.Invoke(mod);
                    }
                    catch (Exception e)
                    {
                        mod.Logger.LogException("OnFixedGUI", e);
                    }
                }
            }

            private void WindowFunction(int windowId)
            {
                UnityAction buttons = () => { };
                if (Input.GetKey(KeyCode.LeftControl)) GUI.DragWindow(_mWindowRect);
                else CorrectWindowPos();
                GL.BeginVertical();
                GL.Space(Scale(H1FontSize + GlobalFontSize));
                GL.BeginHorizontal();
                _tabId = GL.Toolbar(_tabId, Tabs, ButtonStyle, GL.ExpandWidth(false));
                GL.FlexibleSpace();
                GL.EndHorizontal();
                DrawTab(_tabId, ref buttons);
                GL.FlexibleSpace();
                GL.BeginHorizontal();
                if (GL.Button("关闭", ButtonStyle, GL.ExpandWidth(false))) ToggleWindow();
                if (GL.Button("保存", ButtonStyle, GL.ExpandWidth(false))) SaveSettingsAndParams();
                buttons();
                GL.EndHorizontal();
                GL.EndVertical();
            }

            private void DrawTab(int tabId, ref UnityAction buttons)
            {
                var minWidth = GL.Width(_mWindowSize.x - GlobalFontSize / 2f);
                switch (Tabs[tabId])
                {
                    case "Mods":
                        {
                            MScrollPosition[tabId] = GL.BeginScrollView(MScrollPosition[tabId], minWidth);
                            var amountWidth = _mColumns.Where(x => !x.Skip).Sum(x => x.Width);
                            var expandWidth = _mColumns.Where(x => x.Expand && !x.Skip).Sum(x => x.Width);
                            var mods = ModEntries;
                            var colWidth = _mColumns.Select(x => x.Expand ? GL.Width(x.Width / expandWidth * (_mWindowSize.x + expandWidth - amountWidth - GlobalFontSize * 4)) : GL.Width(x.Width)).ToArray();
                            GL.BeginVertical("box");
                            GL.BeginHorizontal("box");
                            for (var i = 0; i < _mColumns.Count; i++)
                            {
                                if (_mColumns[i].Skip) continue;
                                GL.Label(_mColumns[i].Name, BoldFontStyle, colWidth[i]);
                            }
                            GL.EndHorizontal();
                            for (int i = 0, j = 0, c = mods.Count; i < c; i++, j = 0)
                            {
                                GL.BeginVertical("box");
                                GL.BeginHorizontal();
                                GL.BeginHorizontal(colWidth[j++]);
                                if (mods[i].OnGUI != null || mods[i].CanReload)
                                {
                                    if (GL.Button(mods[i].Info.DisplayName, NormalFontStyle, GL.ExpandWidth(true)))
                                        ShowModSettings = ShowModSettings == i ? -1 : i;
                                    if (GL.Button(ShowModSettings == i ? Textures.SettingsActive : Textures.SettingsNormal, IconStyle))
                                        ShowModSettings = ShowModSettings == i ? -1 : i;
                                }
                                else GL.Label(mods[i].Info.DisplayName);
                                if (!string.IsNullOrEmpty(mods[i].Info.HomePage) && GL.Button(Textures.WWW, IconStyle))
                                    Application.OpenURL(mods[i].Info.HomePage);
                                if (mods[i].NewestVersion != null) GL.Box(Textures.Updates, IconStyle);
                                GL.EndHorizontal();
                                GL.BeginHorizontal(colWidth[j++]);
                                GL.Label(mods[i].Info.Version);
                                GL.EndHorizontal();
                                if (mods[i].ManagerVersion > GetVersion())
                                    GL.Label("<color=\"#CD5C5C\">管理器v" + mods[i].Info.ManagerVersion + "</color>", colWidth[j++]);
                                else if (gameVersion != VER_0 && mods[i].GameVersion > gameVersion)
                                    GL.Label("<color=\"#CD5C5C\">游戏v" + mods[i].Info.GameVersion + "</color>", colWidth[j++]);
                                else if (mods[i].Requirements.Count > 0)
                                {
                                    GL.BeginHorizontal(colWidth[j++]);
                                    _mJoinList.Clear();
                                    foreach (var item in mods[i].Requirements)
                                    {
                                        var id = item.Key;
                                        var mod = FindMod(id);
                                        _mJoinList.Add(((mod == null || item.Value != null && item.Value > mod.Version || !mod.Active) && mods[i].Active) ? "<color=\"#CD5C5C\">" + id + "</color> " : id);
                                    }
                                    GL.Label(string.Join(", ", _mJoinList.ToArray()));
                                    GL.EndHorizontal();
                                }
                                else if (!string.IsNullOrEmpty(mods[i].CustomRequirements))
                                    GL.Label(mods[i].CustomRequirements, colWidth[j++]);
                                else GL.Label("-", colWidth[j++]);
                                if (!forbidDisableMods)
                                {
                                    var action = mods[i].Enabled;
                                    action = GL.Toggle(action, "", colWidth[j++]);
                                    if (action != mods[i].Enabled)
                                    {
                                        mods[i].Enabled = action;
                                        if (mods[i].Toggleable) mods[i].Active = action;
                                        else if (action && !mods[i].Loaded) mods[i].Active = action;
                                    }
                                }
                                else GL.Label("", colWidth[j++]);
                                if (mods[i].Active)
                                    GL.Box(mods[i].Enabled ? Textures.StatusActive : Textures.StatusNeedRestart, IconStyle);
                                else
                                    GL.Box(!mods[i].Enabled ? Textures.StatusInactive : Textures.StatusNeedRestart, IconStyle);
                                if (mods[i].ErrorOnLoading) GL.Box(Textures.Errors, IconStyle);
                                GL.EndHorizontal();
                                if (ShowModSettings == i)
                                {
                                    if (mods[i].CanReload)
                                    {
                                        GL.Label("调试", H2FontStyle);
                                        if (GL.Button("重载", ButtonStyle)) mods[i].Reload();
                                        GL.Space(5);
                                    }
                                    if (mods[i].Active && mods[i].OnGUI != null)
                                    {
                                        GL.Label("选项", H2FontStyle);
                                        try
                                        {
                                            mods[i].OnGUI(mods[i]);
                                        }
                                        catch (Exception e)
                                        {
                                            mods[i].Logger.LogException("OnGUI", e);
                                            ShowModSettings = -1;
                                            GUIUtility.ExitGUI();
                                        }
                                    }
                                }
                                GL.EndVertical();
                            }
                            GL.EndVertical();
                            GL.EndScrollView();
                            GL.BeginVertical("box");
                            GL.BeginHorizontal();
                            GL.Box(Textures.SettingsNormal, IconStyle);
                            GL.Label("选项", BoldFontStyle);
                            GL.Box(Textures.WWW, IconStyle);
                            GL.Label("网址", BoldFontStyle);
                            GL.Box(Textures.Updates, IconStyle);
                            GL.Label("可更新", BoldFontStyle);
                            GL.Box(Textures.Errors, IconStyle);
                            GL.Label("有错误", BoldFontStyle);
                            GL.Box(Textures.StatusActive, IconStyle);
                            GL.Label("已启用", BoldFontStyle);
                            GL.Box(Textures.StatusInactive, IconStyle);
                            GL.Label("已禁用", BoldFontStyle);
                            GL.Box(Textures.StatusNeedRestart, IconStyle);
                            GL.Label("需重启", BoldFontStyle);
                            GL.EndHorizontal();
                            GL.BeginHorizontal();
                            GL.Label("按热键 [CTRL + LClick] 拖动窗口", CenterFontStyle);
                            GL.EndHorizontal();
                            GL.EndVertical();
                            break;
                        }
                    case "日志":
                        {
                            MScrollPosition[tabId] = GL.BeginScrollView(MScrollPosition[tabId], minWidth);
                            GL.BeginVertical("box");
                            for (int c = Logger.History.Count, i = Mathf.Max(0, c - Logger.HistoryCapacity); i < c; i++)
                                GL.Label(Logger.History[i]);
                            GL.EndVertical();
                            GL.EndScrollView();
                            buttons += delegate
                            {
                                if (GL.Button("清空", ButtonStyle, GL.ExpandWidth(false))) Logger.Clear();
                                if (GL.Button("查看日志文件", ButtonStyle, GL.ExpandWidth(false))) OpenUnityFileLog();
                            };
                            break;
                        }
                    case "设置":
                        {
                            MScrollPosition[tabId] = GL.BeginScrollView(MScrollPosition[tabId], minWidth);
                            GL.BeginVertical("box");
                            GL.BeginHorizontal();
                            GL.Label("热键（默认Ctrl+F10）", GL.ExpandWidth(false));
                            DrawKeybinding(ref Params.Hotkey, null, GL.ExpandWidth(false));
                            GL.EndHorizontal();
                            GL.BeginHorizontal();
                            GL.Label("检查更新", GL.ExpandWidth(false));
                            ToggleGroup(Params.CheckUpdates, MCheckUpdateStrings, i => { Params.CheckUpdates = i; }, null, GL.ExpandWidth(false));
                            GL.EndHorizontal();
                            GL.BeginHorizontal();
                            GL.Label("游戏启动时自动显示MOD管理器窗口", GL.ExpandWidth(false));
                            ToggleGroup(Params.ShowOnStart, MShowOnStartStrings, i => { Params.ShowOnStart = i; }, null, GL.ExpandWidth(false));
                            GL.EndHorizontal();
                            GL.BeginVertical("box");
                            GL.Label("窗口大小", BoldFontStyle, GL.ExpandWidth(false));
                            GL.BeginHorizontal();
                            GL.Label("宽度", GL.ExpandWidth(false));
                            _mExpectedWindowSize.x = GL.HorizontalSlider(_mExpectedWindowSize.x,
                                Mathf.Min(Screen.width, WindowWidthMin), Screen.width, GL.MaxWidth(Scale(200)));
                            GL.Label(" " + _mExpectedWindowSize.x.ToString("f0") + " px ",
                                GL.ExpandWidth(false));
                            GL.Label("高度", GL.ExpandWidth(false));
                            _mExpectedWindowSize.y = GL.HorizontalSlider(_mExpectedWindowSize.y,
                                Mathf.Min(Screen.height, WindowHeightMin), Screen.height, GL.MaxWidth(Scale(200)));
                            GL.Label(" " + _mExpectedWindowSize.y.ToString("f0") + " px ",
                                GL.ExpandWidth(false));
                            GL.FlexibleSpace();
                            if (GL.Button("确定", ButtonStyle, GL.ExpandWidth(false)))
                            {
                                _mWindowSize.x = Mathf.Floor(_mExpectedWindowSize.x) % 2 > 0
                                    ? Mathf.Ceil(_mExpectedWindowSize.x)
                                    : Mathf.Floor(_mExpectedWindowSize.x);
                                _mWindowSize.y = Mathf.Floor(_mExpectedWindowSize.y) % 2 > 0
                                    ? Mathf.Ceil(_mExpectedWindowSize.y)
                                    : Mathf.Floor(_mExpectedWindowSize.y);
                                CalculateWindowPos();
                                Params.WindowWidth = _mWindowSize.x;
                                Params.WindowHeight = _mWindowSize.y;
                            }
                            GL.EndHorizontal();
                            GL.EndVertical();
                            GL.BeginVertical("box");
                            GL.Label("UI", BoldFontStyle, GL.ExpandWidth(false));
                            GL.BeginHorizontal();
                            GL.Label("缩放", GL.ExpandWidth(false));
                            _mExpectedUiScale = GL.HorizontalSlider(_mExpectedUiScale, UiScaleMin, UiScaleMax,
                                GL.MaxWidth(Scale(600)));
                            GL.Label(" " + _mExpectedUiScale.ToString("f2"), GL.ExpandWidth(false));
                            GL.FlexibleSpace();
                            if (GL.Button("确定", ButtonStyle, GL.ExpandWidth(false)) && !_mUiScale.Equals(_mExpectedUiScale))
                            {
                                _mUiScaleChanged = true;
                                _mUiScale = _mExpectedUiScale;
                                Params.UIScale = _mUiScale;
                                _mExpectedWindowSize.x = Mathf.Min(Screen.width, WindowWidthMin * Mathf.Pow(_mUiScale, 1.5f));
                                _mWindowSize.x = Mathf.Floor(_mExpectedWindowSize.x) % 2 > 0 ? Mathf.Ceil(_mExpectedWindowSize.x) : Mathf.Floor(_mExpectedWindowSize.x);
                                CalculateWindowPos();
                                Params.WindowWidth = _mWindowSize.x;
                            }
                            GL.EndHorizontal();
                            GL.EndVertical();
                            GL.EndVertical();
                            GL.EndScrollView();
                            break;
                        }
                }
            }

            public static int Scale(int value)
            {
                if (!Instance) return value;
                return (int)(value * Instance._mUiScale);
            }

            private float Scale(float value)
            {
                if (!Instance) return value;
                return value * _mUiScale;
            }

            private void CalculateWindowPos()
            {
                CorrectWindowSize();
                _mWindowRect.size = _mWindowSize;
                _mWindowRect.x = (Screen.width - _mWindowSize.x) / 2f;
                _mWindowRect.y = 0;
            }

            private static void CorrectWindowPos()
            {
                _mWindowRect.x = Mathf.Clamp(_mWindowRect.x, 0, Screen.width - _mWindowRect.width);
                _mWindowRect.y = Mathf.Clamp(_mWindowRect.y, 0, Screen.height - _mWindowRect.height);
            }

            private static void CorrectWindowSize()
            {
                _mWindowSize.x = Mathf.Clamp(_mWindowSize.x, Mathf.Min(Screen.width, WindowWidthMin), Screen.width);
                _mWindowSize.y = Mathf.Clamp(_mWindowSize.y, Mathf.Min(Screen.height, WindowHeightMin), Screen.height);
            }

            public void FirstLaunch()
            {
                if (_mFirstLaunched || Params.ShowOnStart == 0 && ModEntries.All(x => !x.ErrorOnLoading))
                    return;
                ToggleWindow(true);
            }

            public void ToggleWindow()
            {
                ToggleWindow(!Opened);
            }

            public void ToggleWindow(bool open)
            {
                if (open == Opened)
                    return;
                if (open)
                {
                    _mFirstLaunched = true;
                    ShowModSettings = _mShowModSettings;
                }
                else
                {
                    _mShowModSettings = ShowModSettings;
                    ShowModSettings = -1;
                }
                try
                {
                    if (open)
                        FreezeUI();
                    else
                        UnFreezeUI();
                    BlockGameUi(Opened = open);
                }
                catch (Exception e)
                {
                    Logger.LogException("ToggleWindow", e);
                }
            }

            private void BlockGameUi(bool value)
            {
                if (value)
                {
                    _mCanvas = new GameObject("UMM blocking UI", typeof(Canvas), typeof(GraphicRaycaster));
                    var canvas = _mCanvas.GetComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvas.sortingOrder = short.MaxValue;
                    DontDestroyOnLoad(_mCanvas);
                    var panel = new GameObject("Image", typeof(Image));
                    panel.transform.SetParent(_mCanvas.transform);
                    var rect = panel.GetComponent<RectTransform>();
                    rect.anchorMin = new Vector2(0, 0);
                    rect.anchorMax = new Vector2(1, 1);
                    rect.offsetMin = Vector2.zero;
                    rect.offsetMax = Vector2.zero;
                    panel.GetComponent<Image>().color = new Color(0, 0, 0, 0.3f);
                }
                else if (_mCanvas)
                    Destroy(_mCanvas);
            }

            private static RectOffset RectOffset(int value)
            {
                return new RectOffset(value, value, value, value);
            }

            private static RectOffset RectOffset(int x, int y)
            {
                return new RectOffset(x, x, y, y);
            }

            public static int GetNextWindowId()
            {
                return ++_mLastWindowId;
            }

            private class Column
            {
                public bool Expand;
                public string Name;
                public bool Skip;
                public float Width;
            }
        }
    }
}