using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Harmony12;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UnityModManagerNet
{
    public partial class UnityModManager
    {
        public partial class UI : MonoBehaviour
        {
            public const float UI_SCALE_MIN = 1f;
            public const float UI_SCALE_MAX = 2f;
            public const int WINDOW_WIDTH_MIN = 1080;
            public const int WINDOW_HEIGHT_MIN = 800;
            public const string GLOBAL_FONT_NAME = "微软雅黑";
            public const int GLOBAL_FONT_SIZE = 20;
            public static GUIStyle window;
            public static GUIStyle h1;
            public static GUIStyle h2;
            public static GUIStyle bold;

            /// <summary>
            ///     [0.13.1]
            /// </summary>
            public static GUIStyle button;
            private static GUIStyle settings;
            private static GUIStyle status;
            private static GUIStyle www;
            private static GUIStyle updates;
            private static GUIStyle toggle;

            private static readonly string[] mCheckUpdateStrings = { "从不", "自动" };
            private static readonly string[] mShowOnStartStrings = { "否", "是" };
            private static readonly string[] mHotkeyNames = { "CTRL+F10", "ScrollLock", "Num *", "~" };

            private static int mLastWindowId;
            private readonly List<Column> mColumns = new List<Column>();
            private readonly List<Column> mOriginColumns = new List<Column>
            {
                new Column {name = "名称", width = 400, expand = true},
                new Column {name = "版本", width = 100},
                new Column {name = "依赖MOD", width = 100, expand = true},
                new Column {name = "开/关", width = 100},
                new Column {name = "状态", width = 100}
            };

            /// <summary>
            ///     [0.20.0.10]
            /// </summary>
            private static readonly IEnumerator<object> ActionResult = DoActionsFromMods();

            private GameObject mCanvas;
            private Resolution mCurrentResolution;
            private float mExpectedUIScale = 1f;
            private Vector2 mExpectedWindowSize = Vector2.zero;
            private bool mFirstLaunched;
            private bool mInit;
            private int mPreviousShowModSettings = -1;
            private Vector2[] mScrollPosition = new Vector2[0];
            private int mShowModSettings = -1;
            private float mUIScale = 1f;
            private bool mUIScaleChanged;
            private Rect mWindowRect = new Rect(0, 0, 0, 0);
            private Vector2 mWindowSize = Vector2.zero;

            public int tabId;
            public string[] tabs = { "Mods", "日志", "设置" };

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
                        var task = DoAsyncActions(mods);
                        yield return new WaitUntil(() => task.IsCompleted);
                    }
                    else
                        yield return new WaitForSecondsRealtime(.1f);
                }
            }

            private static async Task<int> DoAsyncActions(List<ModEntry> mods)
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
                    Logger.Log($"异步任务执行器 {typeof(UI).FullName}.DoAsyncActions 本次扫描共执行了{count}个任务！");
                    return count;
                });
            }

            private void Awake()
            {
                Instance = this;
                DontDestroyOnLoad(this);
                mWindowSize = ClampWindowSize(new Vector2(Params.WindowWidth, Params.WindowHeight));
                mExpectedWindowSize = mWindowSize;
                mUIScale = Mathf.Clamp(Params.UIScale, UI_SCALE_MIN, UI_SCALE_MAX);
                mExpectedUIScale = mUIScale;
                Textures.Init();
                StartCoroutine(ActionResult);
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
                StopCoroutine(ActionResult);
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

                if (Params.Hotkey.Up() || Input.GetKeyUp(KeyCode.F10) && KeyBinding.Ctrl()) ToggleWindow();

                if (Opened && Input.GetKey(KeyCode.Escape)) ToggleWindow();
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
                window = new GUIStyle { name = "umm window", normal = { background = Textures.Window } };
                window.normal.background.wrapMode = TextureWrapMode.Repeat;
                h1 = new GUIStyle
                {
                    name = "umm h1",
                    normal = { textColor = Color.white },
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                h2 = new GUIStyle
                {
                    name = "umm h2",
                    normal = { textColor = new Color(0.6f, 0.91f, 1f) },
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                bold = new GUIStyle(GUI.skin.label)
                {
                    name = "umm bold",
                    normal = { textColor = Color.white },
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.UpperLeft
                };
                button = new GUIStyle(GUI.skin.button)
                {
                    name = "umm button",
                    normal = { textColor = Color.white },
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                settings = new GUIStyle { alignment = TextAnchor.MiddleCenter, stretchHeight = true };
                status = new GUIStyle { alignment = TextAnchor.MiddleCenter, stretchHeight = true };
                www = new GUIStyle { alignment = TextAnchor.MiddleCenter, stretchHeight = true };
                updates = new GUIStyle { alignment = TextAnchor.MiddleCenter, stretchHeight = true };
                toggle = new GUIStyle(GUI.skin.toggle) { alignment = TextAnchor.MiddleCenter };
            }

            private void ScaleGUI()
            {
                GUI.skin.font = Font.CreateDynamicFontFromOSFont(GLOBAL_FONT_NAME, Scale(GLOBAL_FONT_SIZE));
                GUI.skin.toggle = toggle;
                toggle.margin.left = Scale(10);
                GUI.skin.button = button;
                button.padding = new RectOffset(Scale(10), Scale(10), Scale(3), Scale(3));
                GUI.skin.horizontalSlider.fixedHeight = Scale(12);
                GUI.skin.horizontalSlider.border = RectOffset(3, 0);
                GUI.skin.horizontalSlider.padding = RectOffset(Scale(-1), 0);
                GUI.skin.horizontalSlider.margin = RectOffset(Scale(4), Scale(8));
                GUI.skin.horizontalSliderThumb.fixedHeight = Scale(12);
                GUI.skin.horizontalSliderThumb.border = RectOffset(4, 0);
                GUI.skin.horizontalSliderThumb.padding = RectOffset(Scale(7), 0);
                GUI.skin.horizontalSliderThumb.margin = RectOffset(0);
                window.padding = RectOffset(Scale(5));
                h1.fontSize = Scale(26);
                h1.margin = RectOffset(Scale(0), Scale(5));
                h2.fontSize = Scale(20);
                h2.margin = RectOffset(Scale(0), Scale(3));
                button.fontSize = Scale(20);
                button.padding = RectOffset(Scale(30), Scale(5));
                const int iconWidth = 24, iconHeight = 28;
                settings.fixedWidth = Scale(iconWidth);
                settings.fixedHeight = Scale(iconHeight);
                status.fixedWidth = Scale(iconWidth);
                status.fixedHeight = Scale(iconHeight);
                www.fixedWidth = Scale(iconWidth);
                www.fixedHeight = Scale(iconHeight);
                updates.fixedWidth = Scale(iconWidth);
                updates.fixedHeight = Scale(iconHeight);
                mColumns.Clear();
                foreach (var column in mOriginColumns)
                    mColumns.Add(new Column
                    { name = column.name, width = Scale(column.width), expand = column.expand, skip = column.skip });
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
                if (mCurrentResolution.width != Screen.currentResolution.width ||
                    mCurrentResolution.height != Screen.currentResolution.height)
                {
                    mCurrentResolution = Screen.currentResolution;
                    CalculateWindowPos();
                }

                if (mUIScaleChanged)
                {
                    mUIScaleChanged = false;
                    ScaleGUI();
                }

                var backgroundColor = GUI.backgroundColor;
                var color = GUI.color;
                GUI.backgroundColor = Color.white;
                GUI.color = Color.white;
                mWindowRect = GUILayout.Window(0, mWindowRect, WindowFunction, "", window,
                    GUILayout.Height(mWindowSize.y));
                GUI.backgroundColor = backgroundColor;
                GUI.color = color;
            }

            public static int Scale(int value)
            {
                if (!Instance)
                    return value;
                return (int)(value * Instance.mUIScale);
            }

            private float Scale(float value)
            {
                if (!Instance)
                    return value;
                return value * mUIScale;
            }

            private void CalculateWindowPos()
            {
                mWindowSize = ClampWindowSize(mWindowSize);
                mWindowRect = new Rect((Screen.width - mWindowSize.x) / 2f, 0, 0, 0);
            }

            private static Vector2 ClampWindowSize(Vector2 orig)
            {
                return new Vector2(Mathf.Clamp(orig.x, Mathf.Min(WINDOW_WIDTH_MIN, Screen.width), Screen.width),
                    Mathf.Clamp(orig.y, Mathf.Min(WINDOW_HEIGHT_MIN, Screen.height), Screen.height));
            }

            private void WindowFunction(int windowId)
            {
                if (Input.GetKey(KeyCode.LeftControl))
                    GUI.DragWindow(mWindowRect);
                UnityAction buttons = () => { };
                GUILayout.Label($"亲爱的Unity游戏Mod管理器v{version}（允哥修正&汉化&美化特别版）", h1);
                GUILayout.Space(5);
                tabId = GUILayout.Toolbar(tabId, tabs, button, GUILayout.ExpandWidth(false));
                GUILayout.Space(5);
                if (mScrollPosition.Length != tabs.Length)
                    mScrollPosition = new Vector2[tabs.Length];
                DrawTab(tabId, ref buttons);
                GUILayout.FlexibleSpace();
                GUILayout.Space(5);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("关闭", button, GUILayout.ExpandWidth(false))) ToggleWindow();
                if (GUILayout.Button("保存", button, GUILayout.ExpandWidth(false))) SaveSettingsAndParams();
                buttons();
                GUILayout.EndHorizontal();
            }

            private void DrawTab(int tabId, ref UnityAction buttons)
            {
                var minWidth = GUILayout.MinWidth(mWindowSize.x - 10);
                switch (tabs[tabId])
                {
                    case "Mods":
                        {
                            mScrollPosition[tabId] = GUILayout.BeginScrollView(mScrollPosition[tabId], minWidth,
                                GUILayout.ExpandHeight(false));
                            var amountWidth = mColumns.Where(x => !x.skip).Sum(x => x.width);
                            var expandWidth = mColumns.Where(x => x.expand && !x.skip).Sum(x => x.width);
                            var mods = ModEntries;
                            var colWidth = mColumns.Select(x => x.expand ? GUILayout.Width(x.width / expandWidth * (mWindowSize.x + expandWidth - amountWidth - 100)) : GUILayout.Width(x.width)).ToArray();
                            GUILayout.BeginVertical("box");
                            GUILayout.BeginHorizontal("box");
                            for (var i = 0; i < mColumns.Count; i++)
                            {
                                if (mColumns[i].skip)
                                    continue;
                                GUILayout.Label(mColumns[i].name, bold, colWidth[i]);
                            }
                            GUILayout.EndHorizontal();
                            for (int i = 0, j = 0, c = mods.Count; i < c; i++, j = 0)
                            {
                                GUILayout.BeginVertical("box");
                                GUILayout.BeginHorizontal();
                                GUILayout.BeginHorizontal(colWidth[j++]);
                                if (mods[i].OnGUI != null || mods[i].CanReload)
                                {
                                    if (GUILayout.Button(mods[i].Info.DisplayName, GUI.skin.label,
                                        GUILayout.ExpandWidth(true))) ShowModSettings = ShowModSettings == i ? -1 : i;
                                    if (GUILayout.Button(
                                        ShowModSettings == i ? Textures.SettingsActive : Textures.SettingsNormal, settings))
                                        ShowModSettings = ShowModSettings == i ? -1 : i;
                                }
                                else
                                {
                                    GUILayout.Label(mods[i].Info.DisplayName);
                                }
                                if (!string.IsNullOrEmpty(mods[i].Info.HomePage))
                                {
                                    GUILayout.Space(10);
                                    if (GUILayout.Button(Textures.WWW, www)) Application.OpenURL(mods[i].Info.HomePage);
                                }
                                if (mods[i].NewestVersion != null)
                                {
                                    GUILayout.Space(10);
                                    GUILayout.Box(Textures.Updates, updates);
                                }
                                GUILayout.Space(20);
                                GUILayout.EndHorizontal();
                                GUILayout.BeginHorizontal(colWidth[j++]);
                                GUILayout.Label(mods[i].Info.Version, GUILayout.ExpandWidth(false));
                                GUILayout.EndHorizontal();
                                if (mods[i].ManagerVersion > GetVersion())
                                {
                                    GUILayout.Label("<color=\"#CD5C5C\">管理器v" + mods[i].Info.ManagerVersion + "</color>",
                                        colWidth[j++]);
                                }
                                else if (gameVersion != VER_0 && mods[i].GameVersion > gameVersion)
                                {
                                    GUILayout.Label("<color=\"#CD5C5C\">游戏v" + mods[i].Info.GameVersion + "</color>",
                                        colWidth[j++]);
                                }
                                else if (mods[i].Requirements.Count > 0)
                                {
                                    foreach (var item in mods[i].Requirements)
                                    {
                                        var id = item.Key;
                                        var mod = FindMod(id);
                                        GUILayout.Label(
                                            (mod == null || item.Value != null && item.Value > mod.Version ||
                                             !mod.Active) && mods[i].Active
                                                ? "<color=\"#CD5C5C\">" + id + "</color>"
                                                : id, colWidth[j]);
                                    }
                                    j++;
                                }
                                else if (!string.IsNullOrEmpty(mods[i].CustomRequirements))
                                {
                                    GUILayout.Label(mods[i].CustomRequirements, colWidth[j++]);
                                }
                                else
                                {
                                    GUILayout.Label("-", colWidth[j++]);
                                }
                                if (!forbidDisableMods)
                                {
                                    var action = mods[i].Enabled;
                                    action = GUILayout.Toggle(action, "", colWidth[j++]);
                                    if (action != mods[i].Enabled)
                                    {
                                        mods[i].Enabled = action;
                                        if (mods[i].Toggleable)
                                            mods[i].Active = action;
                                        else if (action && !mods[i].Loaded)
                                            mods[i].Active = action;
                                    }
                                }
                                else
                                {
                                    GUILayout.Label("", colWidth[j++]);
                                }
                                if (mods[i].Active)
                                    GUILayout.Box(mods[i].Enabled ? Textures.StatusActive : Textures.StatusNeedRestart,
                                        status);
                                else
                                    GUILayout.Box(!mods[i].Enabled ? Textures.StatusInactive : Textures.StatusNeedRestart,
                                        status);
                                if (mods[i].ErrorOnLoading)
                                    GUILayout.Box(Textures.Errors, status);
                                GUILayout.EndHorizontal();
                                if (ShowModSettings == i)
                                {
                                    if (mods[i].CanReload)
                                    {
                                        GUILayout.Label("调试", h2);
                                        if (GUILayout.Button("重载", button, GUILayout.ExpandWidth(false))) mods[i].Reload();
                                        GUILayout.Space(5);
                                    }
                                    if (mods[i].Active && mods[i].OnGUI != null)
                                    {
                                        GUILayout.Label("选项", h2);
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
                                GUILayout.EndVertical();
                            }
                            GUILayout.EndVertical();
                            GUILayout.EndScrollView();
                            GUILayout.Space(10);
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(10);
                            GUILayout.Box(Textures.SettingsNormal, settings);
                            GUILayout.Space(3);
                            GUILayout.Label("选项", bold, GUILayout.ExpandWidth(false));
                            GUILayout.Space(15);
                            GUILayout.Box(Textures.WWW, www);
                            GUILayout.Space(3);
                            GUILayout.Label("网址", bold, GUILayout.ExpandWidth(false));
                            GUILayout.Space(15);
                            GUILayout.Box(Textures.Updates, updates);
                            GUILayout.Space(3);
                            GUILayout.Label("可更新", bold, GUILayout.ExpandWidth(false));
                            GUILayout.Space(15);
                            GUILayout.Box(Textures.Errors, status);
                            GUILayout.Space(3);
                            GUILayout.Label("有错误", bold, GUILayout.ExpandWidth(false));
                            GUILayout.Space(10);
                            GUILayout.Box(Textures.StatusActive, status);
                            GUILayout.Space(3);
                            GUILayout.Label("已启用", bold, GUILayout.ExpandWidth(false));
                            GUILayout.Space(10);
                            GUILayout.Box(Textures.StatusInactive, status);
                            GUILayout.Space(3);
                            GUILayout.Label("已禁用", bold, GUILayout.ExpandWidth(false));
                            GUILayout.Space(10);
                            GUILayout.Box(Textures.StatusNeedRestart, status);
                            GUILayout.Space(3);
                            GUILayout.Label("需重启", bold, GUILayout.ExpandWidth(false));
                            GUILayout.Space(10);
                            GUILayout.Label("按热键[CTRL + LClick]", bold, GUILayout.ExpandWidth(false));
                            GUILayout.Space(3);
                            GUILayout.Label("拖动窗口", bold, GUILayout.ExpandWidth(false));
                            GUILayout.EndHorizontal();
                            break;
                        }
                    case "日志":
                        {
                            mScrollPosition[tabId] = GUILayout.BeginScrollView(mScrollPosition[tabId], minWidth);
                            GUILayout.BeginVertical("box");
                            for (int c = Logger.history.Count, i = Mathf.Max(0, c - Logger.historyCapacity); i < c; i++)
                                GUILayout.Label(Logger.history[i]);
                            GUILayout.EndVertical();
                            GUILayout.EndScrollView();
                            buttons += delegate
                            {
                                if (GUILayout.Button("清空", button, GUILayout.ExpandWidth(false)))
                                    Logger.Clear();
                                if (GUILayout.Button("查看日志文件", button, GUILayout.ExpandWidth(false)))
                                    OpenUnityFileLog();
                            };
                            break;
                        }
                    case "设置":
                        {
                            mScrollPosition[tabId] = GUILayout.BeginScrollView(mScrollPosition[tabId], minWidth);
                            GUILayout.BeginVertical("box");
                            GUILayout.BeginHorizontal();
                            GUILayout.Label("热键（默认Ctrl+F10）", GUILayout.ExpandWidth(false));
                            DrawKeybinding(ref Params.Hotkey, null, GUILayout.ExpandWidth(false));
                            GUILayout.EndHorizontal();
                            GUILayout.Space(5);
                            GUILayout.BeginHorizontal();
                            GUILayout.Label("检查更新", GUILayout.ExpandWidth(false));
                            ToggleGroup(Params.CheckUpdates, mCheckUpdateStrings, i => { Params.CheckUpdates = i; }, null,
                                GUILayout.ExpandWidth(false));
                            GUILayout.EndHorizontal();
                            GUILayout.Space(5);
                            GUILayout.BeginHorizontal();
                            GUILayout.Label("游戏启动时自动显示MOD管理器窗口", GUILayout.ExpandWidth(false));
                            ToggleGroup(Params.ShowOnStart, mShowOnStartStrings, i => { Params.ShowOnStart = i; }, null,
                                GUILayout.ExpandWidth(false));
                            GUILayout.EndHorizontal();
                            GUILayout.Space(5);
                            GUILayout.BeginVertical("box");
                            GUILayout.Label("窗口大小", bold, GUILayout.ExpandWidth(false));
                            GUILayout.BeginHorizontal();
                            GUILayout.Label("宽度", GUILayout.ExpandWidth(false));
                            mExpectedWindowSize.x = GUILayout.HorizontalSlider(mExpectedWindowSize.x,
                                Mathf.Min(Screen.width, WINDOW_WIDTH_MIN), Screen.width, GUILayout.Width(200));
                            GUILayout.Label(" " + mExpectedWindowSize.x.ToString("f0") + " px ",
                                GUILayout.ExpandWidth(false));
                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal();
                            GUILayout.Label("高度", GUILayout.ExpandWidth(false));
                            mExpectedWindowSize.y = GUILayout.HorizontalSlider(mExpectedWindowSize.y,
                                Mathf.Min(Screen.height, WINDOW_HEIGHT_MIN), Screen.height, GUILayout.Width(200));
                            GUILayout.Label(" " + mExpectedWindowSize.y.ToString("f0") + " px ",
                                GUILayout.ExpandWidth(false));
                            if (GUILayout.Button("确定", button, GUILayout.ExpandWidth(false)))
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
                            GUILayout.EndHorizontal();
                            GUILayout.EndVertical();
                            GUILayout.Space(5);
                            GUILayout.BeginVertical("box");
                            GUILayout.Label("UI", bold, GUILayout.ExpandWidth(false));
                            GUILayout.BeginHorizontal();
                            GUILayout.Label("缩放", GUILayout.ExpandWidth(false), GUILayout.ExpandWidth(false));
                            mExpectedUIScale = GUILayout.HorizontalSlider(mExpectedUIScale, UI_SCALE_MIN, UI_SCALE_MAX,
                                GUILayout.Width(200));
                            GUILayout.Label(" " + mExpectedUIScale.ToString("f2"), GUILayout.ExpandWidth(false));
                            if (GUILayout.Button("确定", button, GUILayout.ExpandWidth(false)) && !mUIScale.Equals(mExpectedUIScale))
                            {
                                mUIScaleChanged = true;
                                mUIScale = mExpectedUIScale;
                                Params.UIScale = mUIScale;
                                mExpectedWindowSize.x = Mathf.Min(Screen.width, WINDOW_WIDTH_MIN * Mathf.Pow(mUIScale, 1.5f));
                                mWindowSize.x = Mathf.Floor(mExpectedWindowSize.x) % 2 > 0
                                    ? Mathf.Ceil(mExpectedWindowSize.x)
                                    : Mathf.Floor(mExpectedWindowSize.x);
                                CalculateWindowPos();
                                Params.WindowWidth = mWindowSize.x;
                            }
                            GUILayout.EndHorizontal();
                            GUILayout.EndVertical();
                            GUILayout.EndVertical();
                            GUILayout.EndScrollView();
                            break;
                        }
                }
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