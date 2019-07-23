extern alias PathfinderKingmaker;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using KM = PathfinderKingmaker::Kingmaker;

namespace UnityModManagerNet
{
    public partial class UnityModManager
    {
        private class GameScripts
        {
            private static readonly List<GameScript> scripts = new List<GameScript>();

            public static void Init()
            {
                foreach (var t in typeof(GameScripts).GetNestedTypes(BindingFlags.NonPublic))
                {
                    if (!t.IsClass || !t.IsSubclassOf(typeof(GameScript)) || !t.Name.Equals(Config.GameScriptName?.Trim())) continue;
                    var script = (GameScript)Activator.CreateInstance(t);
                    scripts.Add(script);
                    Logger.Log($"已初始化游戏脚本“{t.Name}”。");
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

                public override void OnBeforeLoadMods()
                {
                    FreezeUI = () =>
                    {
                        _escMode = KM.GameModes.GameModeType.EscMode == KM.Game.Instance.CurrentMode || KM.GameModes.GameModeType.None == KM.Game.Instance.CurrentMode;
                        if (_escMode) return;
                        Logger.Log($"当前游戏模式为{KM.Game.Instance.CurrentMode.ToString()}，已冻结游戏UI！");
                        KM.Game.Instance.StartMode(KM.GameModes.GameModeType.EscMode);
                    };
                    UnFreezeUI = () =>
                    {
                        if (_escMode) return;
                        KM.Game.Instance.StopMode(KM.GameModes.GameModeType.EscMode);
                        Logger.Log($"已解冻游戏UI，当前游戏模式为{KM.Game.Instance.CurrentMode.ToString()}！");
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

                private static FieldInfo mFieldModded;

                private static FieldInfo FieldModded
                {
                    get
                    {
                        if (mFieldModded != null) return mFieldModded;
                        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                        {
                            if (assembly.ManifestModule.Name != "Assembly-CSharp.dll") continue;
                            mFieldModded = assembly.GetType("RoR2.RoR2Application").GetField("isModded", BindingFlags.Public | BindingFlags.Static);
                            break;
                        }
                        return mFieldModded;
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

            public static void OnBeforeLoadMods()
            {
                foreach (var o in scripts)
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
                foreach (var o in scripts)
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
                foreach (var o in scripts)
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
        }
    }
}
