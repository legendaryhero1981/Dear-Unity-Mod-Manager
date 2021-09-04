extern alias PathfinderKingmaker;
extern alias PathfinderWrathOfTheRighteous;

using System;
using System.Collections.Generic;
using System.Linq;
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
            private static bool _freezeUi;

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
                _freezeUi = null == mod;
                Logger.Log(_freezeUi ? $"DUMM冻结游戏UI模式已开启！" : $"检测到Mod “{mod.Info.DisplayName}” 的配置文件 “{Config.ModInfo}” 设置了 “{nameof(mod.Info.FreezeUI)}” 字段值为 “{mod.Info.FreezeUI}”，DUMM冻结游戏UI模式已关闭！");
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

            private class GameScript
            {
                public virtual void OnModToggle(ModEntry modEntry, bool value) { }
                public virtual void OnBeforeLoadMods() { }
                public virtual void OnAfterLoadMods() { }
            }

            private class PathfinderKingmaker : GameScript
            {
                private bool _escMode;

                public override void OnAfterLoadMods()
                {
                    if (!_freezeUi) return;
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
                    if (!_freezeUi) return;
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

            private class RiskofRain2 : GameScript
            {
                public override void OnModToggle(ModEntry modEntry, bool value)
                {
                    if (!modEntry.Info.IsCheat) return;
                    if (value)
                    {
                        SetModded(true);
                    }
                    else if (ModEntries.All(x => x == modEntry || !x.Info.IsCheat))
                    {
                        SetModded(false);
                    }
                }

                public override void OnBeforeLoadMods()
                {
                    forbidDisableMods = true;
                }

                private static FieldInfo _mFieldModded;

                private static FieldInfo FieldModded
                {
                    get
                    {
                        if (_mFieldModded != null) return _mFieldModded;
                        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                        {
                            if (assembly.ManifestModule.Name != "Assembly-CSharp.dll") continue;
                            _mFieldModded = assembly.GetType("RoR2.RoR2Application").GetField("isModded", BindingFlags.Public | BindingFlags.Static);
                            break;
                        }
                        return _mFieldModded;
                    }
                }

                public static bool GetModded()
                {
                    return (bool)FieldModded.GetValue(null);
                }

                private static void SetModded(bool value)
                {
                    FieldModded.SetValue(null, value);
                }
            }
        }
    }
}
