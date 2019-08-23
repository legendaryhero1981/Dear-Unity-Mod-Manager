using Harmony12;

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
            public const float UI_SCALE_MIN = 1f;
            public const float UI_SCALE_MAX = 2f;
            public const int WINDOW_WIDTH_MIN = 1000;
            public const int WINDOW_HEIGHT_MIN = 800;
            public const string GLOBAL_FONT_NAME = "微软雅黑";
            public const int GLOBAL_FONT_SIZE = 20;
            public const int H1_FONT_SIZE = GLOBAL_FONT_SIZE + 4;
            public const int H2_FONT_SIZE = GLOBAL_FONT_SIZE + 2;

            /// <summary>
            /// [0.20.0.17] 新增多种自定义GUIStyle样式
            /// </summary>
            public static GS WindowStyle;
            public static GS ButtonStyle;
            public static GS ToggleStyle;
            public static GS HSliderStyle;
            public static GS HSliderThumbStyle;
            public static GS IconStyle;
            public static GS H1FontStyle;
            public static GS H2FontStyle;
            public static GS BoldFontStyle;
            public static GS CenterFontStyle;
            public static GS NormalFontStyle;

            private static readonly string[] mCheckUpdateStrings = { "从不", "自动" };
            private static readonly string[] mShowOnStartStrings = { "否", "是" };
            private static readonly string[] mHotkeyNames = { "CTRL+F10", "ScrollLock", "Num *", "~" };

            private static int mLastWindowId;
            private readonly List<Column> mColumns = new List<Column>();
            private readonly List<Column> mOriginColumns = new List<Column>
            {
                new Column {name = "名称", width = 400, expand = true},
                new Column {name = "版本", width = 200},
                new Column {name = "依赖MOD", width = 160, expand = true},
                new Column {name = "开/关", width = 120},
                new Column {name = "状态", width = 100}
            };

            /// <summary>
            /// [0.20.0.17] Mod任务执行
            /// </summary>
            private static readonly IEnumerator<object> ModActions = DoActionsFromMods();

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
                var y = Screen.height + ScrollViewPosition.y - WindowPosition.y - Input.mousePosition.y - Scale(H1_FONT_SIZE + GLOBAL_FONT_SIZE * 2) - textHeight;
                GUI.Label(new Rect(x, y, tooltipSize.x, tooltipSize.y), GUI.tooltip, styleTooltip);
                //GUI.Label(new Rect(x, y, tooltipSize.x, tooltipSize.y), $"x={x},y={y},sx={ScrollViewPosition.x},sy={ScrollViewPosition.y},my={Input.mousePosition.y},th={textHeight},ry={WindowPosition.y}", styleTooltip);
            }
            /// <summary>
            /// [0.20.0.17] 当前选项卡的ScrollView控件位置
            /// </summary>
            public static Vector2 ScrollViewPosition => mScrollPosition[tabId];
            /// <summary>
            /// [0.20.0.17] 窗口位置
            /// </summary>
            public static Vector2 WindowPosition => mWindowRect.position;
            /// <summary>
            ///  [0.20.0.16] 窗口大小
            /// </summary>
            public static Vector2 WindowSize => mWindowSize;
            private static Vector2 mWindowSize = Vector2.zero;

            private GameObject mCanvas;
            private Resolution mCurrentResolution;
            private float mExpectedUIScale = 1f;
            private Vector2 mExpectedWindowSize = Vector2.zero;
            private bool mFirstLaunched;
            private bool mInit;
            private int mPreviousShowModSettings = -1;
            private int mShowModSettings = -1;
            private float mUIScale = 1f;
            private bool mUIScaleChanged;
            private static Rect mWindowRect = new Rect(0, 0, 0, 0);

            public static readonly string[] tabs = { "Mods", "日志", "设置" };
            private static int tabId;
            private static Vector2[] mScrollPosition = new Vector2[tabs.Length];
            private static Texture2D _mBackground;
            private static readonly string FilePathBackground = Path.Combine(Path.GetDirectoryName(typeof(UI).Assembly.Location), "background.jpg");

            public static UI Instance { get; private set; }
            public bool Opened { get; private set; }

            private int ShowModSettings
            {
                get => mShowModSettings;
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

                    mShowModSettings = value;
                    if (mShowModSettings == mPreviousShowModSettings) return;
                    if (mShowModSettings == -1)
                    {
                        Hide(ModEntries[mPreviousShowModSettings]);
                    }
                    else if (mPreviousShowModSettings == -1)
                    {
                        Show(ModEntries[mShowModSettings]);
                    }
                    else
                    {
                        Hide(ModEntries[mPreviousShowModSettings]);
                        Show(ModEntries[mShowModSettings]);
                    }

                    mPreviousShowModSettings = mShowModSettings;
                }
            }

            internal bool GameCursorLocked { get; set; }

            internal static bool Load()
            {
                try
                {
                    new GameObject(typeof(UI).FullName, typeof(UI));
                    return true;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                return false;
            }

            private static IEnumerator<object> DoActionsFromMods()
            {
                Logger.Log($"已启动协程 {typeof(UI).FullName}.DoActionsFromMods！");
                while (true)
                {
                    var mods = ModEntries.FindAll(m => 0 < m.OnModActions.Count);
                    if (0 < mods.Count)
                    {
                        var task = DoActionsAsync(mods);
                        yield return new WaitUntil(() => task.IsCompleted);
                    }
                    else
                        yield return new WaitForSecondsRealtime(.1f);
                }
            }

            private static async Task<int> DoActionsAsync(List<ModEntry> mods)
            {
                return await Task.Run(() =>
                {
                    var count = 0;
                    mods.ForEach(m =>
                    {
                        while (0 < m.OnModActions.Count)
                        {
                            m.OnModActions.TryPop(out var action);
                            action(m);
                            Logger.Log($"异步任务 {action?.Method.FullDescription()} 执行完毕！");
                            count++;
                        }
                    });
                    Logger.Log($"异步任务执行器 {typeof(UI).FullName}.DoActionsAsync 本次扫描共执行了{count}个任务！");
                    return count;
                });
            }

            private void Awake()
            {
                Instance = this;
                DontDestroyOnLoad(this);
                mWindowSize = new Vector2(Params.WindowWidth, Params.WindowHeight);
                CorrectWindowSize();
                mExpectedWindowSize = mWindowSize;
                mUIScale = Mathf.Clamp(Params.UIScale, UI_SCALE_MIN, UI_SCALE_MAX);
                mExpectedUIScale = mUIScale;
                Textures.Init();
                if (null == _mBackground)
                    _mBackground = FileToTexture2D(FilePathBackground, (int)mWindowSize.x, (int)mWindowSize.y);
                StartCoroutine(ModActions);
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
                StopCoroutine(ModActions);
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

            private static void PrepareGUI()
            {
                WindowStyle = new GS()
                {
                    name = "umm window",
                    normal = { textColor = Color.white, background = _mBackground },
                    fontSize = H1_FONT_SIZE,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.UpperCenter,
                    padding = RectOffset(0),
                    margin = RectOffset(0),
                    wordWrap = true
                };
                WindowStyle.normal.background.wrapMode = TextureWrapMode.Repeat;
                HSliderStyle = new GS(GUI.skin.horizontalSlider)
                {
                    fixedHeight = GLOBAL_FONT_SIZE,
                    padding = RectOffset(0),
                    margin = RectOffset(GLOBAL_FONT_SIZE / 4, GLOBAL_FONT_SIZE / 2)
                };
                HSliderThumbStyle = new GS(GUI.skin.horizontalSliderThumb)
                {
                    fixedHeight = GLOBAL_FONT_SIZE,
                    padding = RectOffset(GLOBAL_FONT_SIZE / 2, 0),
                    margin = RectOffset(0)
                };
                H1FontStyle = new GS(GUI.skin.label)
                {
                    name = "umm h1",
                    normal = { textColor = Color.white },
                    fontSize = H1_FONT_SIZE,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    padding = RectOffset(GLOBAL_FONT_SIZE / 4),
                    margin = RectOffset(GLOBAL_FONT_SIZE / 4),
                    wordWrap = true
                };
                H2FontStyle = new GS(H1FontStyle) { name = "umm h2", fontSize = H2_FONT_SIZE };
                CenterFontStyle = new GS(H2FontStyle) { name = "umm center", fontSize = GLOBAL_FONT_SIZE };
                BoldFontStyle = new GS(CenterFontStyle) { name = "umm bold", alignment = TextAnchor.MiddleLeft };
                NormalFontStyle = new GS(BoldFontStyle) { name = "umm normal", fontStyle = FontStyle.Normal };
                IconStyle = new GS(GUI.skin.box)
                {
                    name = "umm icon",
                    alignment = TextAnchor.MiddleCenter,
                    padding = RectOffset(0),
                    margin = RectOffset(GLOBAL_FONT_SIZE / 4, GLOBAL_FONT_SIZE / 2),
                    stretchHeight = true,
                    stretchWidth = true,
                };
                ButtonStyle = new GS(GUI.skin.button)
                {
                    name = "umm button",
                    normal = { textColor = Color.white },
                    fontSize = GLOBAL_FONT_SIZE,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.UpperCenter,
                    padding = new RectOffset(GLOBAL_FONT_SIZE / 4, GLOBAL_FONT_SIZE / 2, GLOBAL_FONT_SIZE / 4, GLOBAL_FONT_SIZE / 2),
                    margin = RectOffset(GLOBAL_FONT_SIZE / 4),
                    wordWrap = false
                };
                ToggleStyle = new GS(GUI.skin.toggle)
                {
                    name = "umm toggle",
                    normal = { textColor = Color.white },
                    fontSize = GLOBAL_FONT_SIZE,
                    alignment = TextAnchor.MiddleCenter,
                    padding = RectOffset(GLOBAL_FONT_SIZE / 2, GLOBAL_FONT_SIZE / 4),
                    margin = RectOffset(GLOBAL_FONT_SIZE / 4),
                    wordWrap = true
                };
            }

            private void ScaleGUI()
            {
                GUI.skin.font = Font.CreateDynamicFontFromOSFont(GLOBAL_FONT_NAME, Scale(GLOBAL_FONT_SIZE));
                GUI.skin.horizontalSlider = HSliderStyle;
                GUI.skin.horizontalSliderThumb = HSliderThumbStyle;
                GUI.skin.toggle = ToggleStyle;
                GUI.skin.button = ButtonStyle;
                GUI.skin.label = NormalFontStyle;
                HSliderStyle.fixedHeight = HSliderThumbStyle.fixedHeight = Scale(GLOBAL_FONT_SIZE);
                ToggleStyle.fontSize = ButtonStyle.fontSize = NormalFontStyle.fontSize = CenterFontStyle.fontSize = BoldFontStyle.fontSize = Scale(GLOBAL_FONT_SIZE);
                WindowStyle.fontSize = H1FontStyle.fontSize = Scale(H1_FONT_SIZE);
                IconStyle.fixedWidth = IconStyle.fixedHeight = H2FontStyle.fontSize = Scale(H2_FONT_SIZE);
                mColumns.Clear();
                foreach (var column in mOriginColumns)
                    mColumns.Add(new Column { name = column.name, width = Scale(column.width), expand = column.expand, skip = column.skip });
            }

            private void OnGUI()
            {
                if (!mInit)
                {
                    mInit = true;
                    PrepareGUI();
                    ScaleGUI();
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
                if (!Opened) return;
                if (mCurrentResolution.width != Screen.currentResolution.width || mCurrentResolution.height != Screen.currentResolution.height)
                {
                    mCurrentResolution = Screen.currentResolution;
                    CalculateWindowPos();
                }

                if (mUIScaleChanged)
                {
                    mUIScaleChanged = false;
                    ScaleGUI();
                }
                //mWindowRect = GUI.ModalWindow(0, mWindowRect, WindowFunction, $"亲爱的Unity游戏Mod管理器v{version}（允哥修正&汉化&美化特别版）", WindowStyle);
                mWindowRect = GL.Window(0, mWindowRect, WindowFunction, $"亲爱的Unity游戏Mod管理器v{version}（允哥修正&汉化&美化特别版）", WindowStyle, GL.Width(mWindowSize.x), GL.Height(mWindowSize.y));
            }

            private void WindowFunction(int windowId)
            {
                UnityAction buttons = () => { };
                if (Input.GetKey(KeyCode.LeftControl)) GUI.DragWindow(mWindowRect);
                else CorrectWindowPos();
                GL.BeginVertical();
                GL.Space(Scale(H1_FONT_SIZE + GLOBAL_FONT_SIZE));
                GL.BeginHorizontal();
                tabId = GL.Toolbar(tabId, tabs, ButtonStyle, GL.ExpandWidth(false));
                GL.FlexibleSpace();
                GL.EndHorizontal();
                DrawTab(tabId, ref buttons);
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
                var minWidth = GL.Width(mWindowSize.x - GLOBAL_FONT_SIZE / 2f);
                switch (tabs[tabId])
                {
                    case "Mods":
                        {
                            mScrollPosition[tabId] = GL.BeginScrollView(mScrollPosition[tabId], minWidth);
                            var amountWidth = mColumns.Where(x => !x.skip).Sum(x => x.width);
                            var expandWidth = mColumns.Where(x => x.expand && !x.skip).Sum(x => x.width);
                            var mods = ModEntries;
                            var colWidth = mColumns.Select(x => x.expand ? GL.Width(x.width / expandWidth * (mWindowSize.x + expandWidth - amountWidth - GLOBAL_FONT_SIZE * 4)) : GL.Width(x.width)).ToArray();
                            GL.BeginVertical("box");
                            GL.BeginHorizontal("box");
                            for (var i = 0; i < mColumns.Count; i++)
                            {
                                if (mColumns[i].skip) continue;
                                GL.Label(mColumns[i].name, BoldFontStyle, colWidth[i]);
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
                                    foreach (var item in mods[i].Requirements)
                                    {
                                        var id = item.Key;
                                        var mod = FindMod(id);
                                        GL.Label((mod == null || item.Value != null && item.Value > mod.Version || !mod.Active) && mods[i].Active ? "<color=\"#CD5C5C\">" + id + "</color>" : id, colWidth[j]);
                                    }
                                    j++;
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
                            mScrollPosition[tabId] = GL.BeginScrollView(mScrollPosition[tabId], minWidth);
                            GL.BeginVertical("box");
                            for (int c = Logger.history.Count, i = Mathf.Max(0, c - Logger.historyCapacity); i < c; i++)
                                GL.Label(Logger.history[i]);
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
                            mScrollPosition[tabId] = GL.BeginScrollView(mScrollPosition[tabId], minWidth);
                            GL.BeginVertical("box");
                            GL.BeginHorizontal();
                            GL.Label("热键（默认Ctrl+F10）", GL.ExpandWidth(false));
                            DrawKeybinding(ref Params.Hotkey, null, GL.ExpandWidth(false));
                            GL.EndHorizontal();
                            GL.BeginHorizontal();
                            GL.Label("检查更新", GL.ExpandWidth(false));
                            ToggleGroup(Params.CheckUpdates, mCheckUpdateStrings, i => { Params.CheckUpdates = i; }, null, GL.ExpandWidth(false));
                            GL.EndHorizontal();
                            GL.BeginHorizontal();
                            GL.Label("游戏启动时自动显示MOD管理器窗口", GL.ExpandWidth(false));
                            ToggleGroup(Params.ShowOnStart, mShowOnStartStrings, i => { Params.ShowOnStart = i; }, null, GL.ExpandWidth(false));
                            GL.EndHorizontal();
                            GL.BeginVertical("box");
                            GL.Label("窗口大小", BoldFontStyle, GL.ExpandWidth(false));
                            GL.BeginHorizontal();
                            GL.Label("宽度", GL.ExpandWidth(false));
                            mExpectedWindowSize.x = GL.HorizontalSlider(mExpectedWindowSize.x,
                                Mathf.Min(Screen.width, WINDOW_WIDTH_MIN), Screen.width, GL.MaxWidth(Scale(200)));
                            GL.Label(" " + mExpectedWindowSize.x.ToString("f0") + " px ",
                                GL.ExpandWidth(false));
                            GL.Label("高度", GL.ExpandWidth(false));
                            mExpectedWindowSize.y = GL.HorizontalSlider(mExpectedWindowSize.y,
                                Mathf.Min(Screen.height, WINDOW_HEIGHT_MIN), Screen.height, GL.MaxWidth(Scale(200)));
                            GL.Label(" " + mExpectedWindowSize.y.ToString("f0") + " px ",
                                GL.ExpandWidth(false));
                            GL.FlexibleSpace();
                            if (GL.Button("确定", ButtonStyle, GL.ExpandWidth(false)))
                            {
                                mWindowSize.x = Mathf.Floor(mExpectedWindowSize.x) % 2 > 0
                                    ? Mathf.Ceil(mExpectedWindowSize.x)
                                    : Mathf.Floor(mExpectedWindowSize.x);
                                mWindowSize.y = Mathf.Floor(mExpectedWindowSize.y) % 2 > 0
                                    ? Mathf.Ceil(mExpectedWindowSize.y)
                                    : Mathf.Floor(mExpectedWindowSize.y);
                                CalculateWindowPos();
                                Params.WindowWidth = mWindowSize.x;
                                Params.WindowHeight = mWindowSize.y;
                            }
                            GL.EndHorizontal();
                            GL.EndVertical();
                            GL.BeginVertical("box");
                            GL.Label("UI", BoldFontStyle, GL.ExpandWidth(false));
                            GL.BeginHorizontal();
                            GL.Label("缩放", GL.ExpandWidth(false));
                            mExpectedUIScale = GL.HorizontalSlider(mExpectedUIScale, UI_SCALE_MIN, UI_SCALE_MAX,
                                GL.MaxWidth(Scale(600)));
                            GL.Label(" " + mExpectedUIScale.ToString("f2"), GL.ExpandWidth(false));
                            GL.FlexibleSpace();
                            if (GL.Button("确定", ButtonStyle, GL.ExpandWidth(false)) && !mUIScale.Equals(mExpectedUIScale))
                            {
                                mUIScaleChanged = true;
                                mUIScale = mExpectedUIScale;
                                Params.UIScale = mUIScale;
                                mExpectedWindowSize.x = Mathf.Min(Screen.width, WINDOW_WIDTH_MIN * Mathf.Pow(mUIScale, 1.5f));
                                mWindowSize.x = Mathf.Floor(mExpectedWindowSize.x) % 2 > 0 ? Mathf.Ceil(mExpectedWindowSize.x) : Mathf.Floor(mExpectedWindowSize.x);
                                CalculateWindowPos();
                                Params.WindowWidth = mWindowSize.x;
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
                return (int)(value * Instance.mUIScale);
            }

            private float Scale(float value)
            {
                if (!Instance) return value;
                return value * mUIScale;
            }

            private void CalculateWindowPos()
            {
                CorrectWindowSize();
                mWindowRect.size = mWindowSize;
                mWindowRect.x = (Screen.width - mWindowSize.x) / 2f;
                mWindowRect.y = 0;
            }

            private void CorrectWindowPos()
            {
                mWindowRect.x = Mathf.Clamp(mWindowRect.x, 0, Screen.width - mWindowRect.width);
                mWindowRect.y = Mathf.Clamp(mWindowRect.y, 0, Screen.height - mWindowRect.height);
            }

            private void CorrectWindowSize()
            {
                mWindowSize.x = Mathf.Clamp(mWindowSize.x, Mathf.Min(Screen.width, WINDOW_WIDTH_MIN), Screen.width);
                mWindowSize.y = Mathf.Clamp(mWindowSize.y, Mathf.Min(Screen.height, WINDOW_HEIGHT_MIN), Screen.height);
            }

            public void FirstLaunch()
            {
                if (mFirstLaunched || Params.ShowOnStart == 0 && ModEntries.All(x => !x.ErrorOnLoading))
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
                    mFirstLaunched = true;
                    ShowModSettings = mShowModSettings;
                }
                else
                {
                    mShowModSettings = ShowModSettings;
                    ShowModSettings = -1;
                }
                try
                {
                    if (open)
                        FreezeUI();
                    else
                        UnFreezeUI();
                    BlockGameUI(Opened = open);
                }
                catch (Exception e)
                {
                    Logger.LogException("ToggleWindow", e);
                }
            }

            private void BlockGameUI(bool value)
            {
                if (value)
                {
                    mCanvas = new GameObject("", typeof(Canvas), typeof(GraphicRaycaster));
                    mCanvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                    mCanvas.GetComponent<Canvas>().sortingOrder = short.MaxValue;
                    DontDestroyOnLoad(mCanvas);
                    var panel = new GameObject("", typeof(Image));
                    panel.transform.SetParent(mCanvas.transform);
                    panel.GetComponent<RectTransform>().anchorMin = new Vector2(1, 0);
                    panel.GetComponent<RectTransform>().anchorMax = new Vector2(0, 1);
                    panel.GetComponent<RectTransform>().offsetMin = Vector2.zero;
                    panel.GetComponent<RectTransform>().offsetMax = Vector2.zero;
                }
                else if (mCanvas)
                    Destroy(mCanvas);
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
                return ++mLastWindowId;
            }

            private class Column
            {
                public bool expand;
                public string name;
                public bool skip;
                public float width;
            }
        }
    }
}