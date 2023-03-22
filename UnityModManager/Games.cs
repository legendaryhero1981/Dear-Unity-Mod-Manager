extern alias PathfinderKingmaker;
extern alias PathfinderWrathOfTheRighteous;
extern alias SolastaCrown;

using System;
using System.Collections.Generic;
using System.Reflection;

using KM = PathfinderKingmaker::Kingmaker;
using WOTR = PathfinderWrathOfTheRighteous::Kingmaker;

namespace UnityModManagerNet
{
    public partial class UnityModManager
    {
        private class GameScripts
        {
            private static readonly List<GameScript> Scripts = new();
            /// <summary>
            ///  [0.20.0.15]
            /// </summary>
            private static bool freezeUI;

            public static void Init(List<ModEntry> modEntries)
            {
                foreach (var t in typeof(GameScripts).GetNestedTypes(BindingFlags.NonPublic))
                {
                    if (!t.IsClass || !t.IsSubclassOf(typeof(GameScript)) || !t.Name.Equals(Config.GameScriptName?.Trim())) continue;
                    var script = (GameScript)Activator.CreateInstance(t);
                    Scripts.Add(script);
                    Logger.Log($"已初始化游戏脚本“{t.Name}”。");
                }
            }

            public static void OnBeforeLoadMods()
            {
                foreach (var o in Scripts)
                {
                    try
                    {
                        o.OnBeforeLoadMods();
                    }
                    catch (Exception e)
                    {
                        Logger.LogException("OnBeforeLoadMods", e);
                    }
                }
            }

            public static void OnAfterLoadMods()
            {
                var mod = ModEntries.Find(m => "0".Equals(m.Info.FreezeUI) || "false".Equals(m.Info.FreezeUI?.ToLower()));
                freezeUI = null == mod;
                Logger.Log(freezeUI ? $"DUMM冻结游戏UI模式已开启！" : $"检测到Mod “{mod.Info.DisplayName}” 的配置文件 “{Config.ModInfo}” 设置了 “{nameof(mod.Info.FreezeUI)}” 字段值为 “{mod.Info.FreezeUI}”，DUMM冻结游戏UI模式已关闭！");
                foreach (var o in Scripts)
                {
                    try
                    {
                        o.OnAfterLoadMods();
                    }
                    catch (Exception e)
                    {
                        Logger.LogException("OnAfterLoadMods", e);
                    }
                }
            }

            public static void OnModToggle(ModEntry modEntry, bool value)
            {
                foreach (var o in Scripts)
                {
                    try
                    {
                        o.OnModToggle(modEntry, value);
                    }
                    catch (Exception e)
                    {
                        Logger.LogException("OnModToggle", e);
                    }
                }
            }

            public static void OnToggleWindow(bool value)
            {
                foreach (var o in Scripts)
                {
                    try
                    {
                        o.OnToggleWindow(value);
                    }
                    catch (Exception e)
                    {
                        Logger.LogException("OnToggleWindow", e);
                    }
                }
            }

            private class GameScript
            {
                public virtual void OnModToggle(ModEntry modEntry, bool value) { }
                public virtual void OnBeforeLoadMods() { }
                public virtual void OnAfterLoadMods() { }
                /// <summary>
                /// [0.21.3]
                /// </summary>
                public virtual void OnToggleWindow(bool value) { }
            }

            private class SolastaCrownOfTheMagister : GameScript
            {
                private bool _escMode;

                public override void OnAfterLoadMods()
                {
                    if (!freezeUI) return;
                    FreezeUI = () =>
                    {
                        var screen = SolastaCrown.Gui.CurrentLocationScreen;
                        _escMode = screen is not SolastaCrown.GameLocationScreenExploration;
                        if (_escMode) return;
                        var screenExploration = screen as SolastaCrown.GameLocationScreenExploration;
                        _escMode = screenExploration == null || screenExploration.TimeAndNavigationPanel.GameTime.Paused;
                        if (_escMode) return;
                        screenExploration?.TimeAndNavigationPanel.OnPauseCb();
                        Logger.Log($"已冻结游戏UI，游戏已暂停！");
                    };
                    UnFreezeUI = () =>
                    {
                        if (_escMode) return;
                        var screen = SolastaCrown.Gui.CurrentLocationScreen;
                        _escMode = screen is not SolastaCrown.GameLocationScreenExploration;
                        if (_escMode) return;
                        var screenExploration = screen as SolastaCrown.GameLocationScreenExploration;
                        screenExploration?.TimeAndNavigationPanel.OnPlayCb();
                        Logger.Log($"已解冻游戏UI，游戏已恢复运行！");
                    };
                }
            }

            private class PathfinderKingmaker : GameScript
            {
                private bool _escMode;

                public override void OnAfterLoadMods()
                {
                    if (!freezeUI) return;
                    FreezeUI = () =>
                    {
                        _escMode = KM.GameModes.GameModeType.EscMode == KM.Game.Instance.CurrentMode || KM.GameModes.GameModeType.None == KM.Game.Instance.CurrentMode;
                        if (_escMode) return;
                        KM.Game.Instance.StartMode(KM.GameModes.GameModeType.EscMode);
                        Logger.Log($"已冻结游戏UI，当前游戏模式为{KM.Game.Instance.CurrentMode}！");
                    };
                    UnFreezeUI = () =>
                    {
                        if (_escMode) return;
                        KM.Game.Instance.StopMode(KM.GameModes.GameModeType.EscMode);
                        Logger.Log($"已解冻游戏UI，当前游戏模式为{KM.Game.Instance.CurrentMode}！");
                    };
                }
            }

            private class PathfinderWrathOfTheRighteous : GameScript
            {
                private bool _escMode;

                public override void OnAfterLoadMods()
                {
                    if (!freezeUI) return;
                    FreezeUI = () =>
                    {
                        _escMode = WOTR.GameModes.GameModeType.EscMode == WOTR.Game.Instance.CurrentMode || WOTR.GameModes.GameModeType.None == WOTR.Game.Instance.CurrentMode;
                        if (_escMode) return;
                        WOTR.Game.Instance.StartMode(WOTR.GameModes.GameModeType.EscMode);
                        Logger.Log($"已冻结游戏UI，当前游戏模式为{WOTR.Game.Instance.CurrentMode}！");
                    };
                    UnFreezeUI = () =>
                    {
                        if (_escMode) return;
                        WOTR.Game.Instance.StopMode(WOTR.GameModes.GameModeType.EscMode);
                        Logger.Log($"已解冻游戏UI，当前游戏模式为{WOTR.Game.Instance.CurrentMode}！");
                    };
                }
            }
        }
    }
}
