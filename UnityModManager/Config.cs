using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;

namespace UnityModManagerNet
{
    public partial class UnityModManager
    {
        public sealed class Param
        {
            [Serializable]
            public class Mod
            {
                [XmlAttribute]
                public string Id;
                [XmlAttribute]
                public bool Enabled = true;
            }

            public KeyBinding Hotkey = new KeyBinding();
            public int CheckUpdates = 1;
            public int ShowOnStart = 1;
            public float WindowWidth;
            public float WindowHeight;
            public float UIScale = 1f;
            public List<Mod> ModParams = new List<Mod>();

            private static readonly string Filepath = Path.Combine(Path.GetDirectoryName(typeof(Param).Assembly.Location) ?? string.Empty, "Params.xml");

            public void Save()
            {
                try
                {
                    ModParams.Clear();
                    foreach (var mod in ModEntries)
                    {
                        ModParams.Add(new Mod { Id = mod.Info.Id, Enabled = mod.Enabled });
                    }

                    using var writer = new StreamWriter(Filepath);
                    var serializer = new XmlSerializer(typeof(Param));
                    serializer.Serialize(writer, this);
                }
                catch (Exception e)
                {
                    Logger.Error($"写入文件“{Filepath}”失败！");
                    Debug.LogException(e);
                }
            }

            public static Param Load()
            {
                if (!File.Exists(Filepath)) return new Param();
                try
                {
                    using var stream = File.OpenRead(Filepath);
                    var serializer = new XmlSerializer(typeof(Param));
                    var result = serializer.Deserialize(stream) as Param;

                    return result;
                }
                catch (Exception e)
                {
                    Logger.Error($"读取文件“{Filepath}”失败！");
                    Debug.LogException(e);
                }
                return new Param();
            }

            internal void ReadModParams()
            {
                foreach (var item in ModParams)
                {
                    var mod = FindMod(item.Id);
                    if (mod != null)
                    {
                        mod.Enabled = item.Enabled;
                    }
                }
            }
        }

        [XmlRoot("Config")]
        public class GameInfo
        {
            [XmlAttribute]
            public string Name;
            public string Folder;
            public string ModsDirectory;
            public string ModInfo;
            public string EntryPoint;
            public string StartingPoint;
            public string UIStartingPoint;
            public string GameExe;
            public string GameName;
            public string GameVersionPoint;
            public string GameScriptName;
            public string FixBlackUI;
            public string MinimalManagerVersion;

            private static readonly string Filepath = Path.Combine(Path.GetDirectoryName(typeof(GameInfo).Assembly.Location) ?? string.Empty, "Config.xml");

            public static GameInfo Load()
            {
                try
                {
                    using var stream = File.OpenRead(Filepath);
                    return new XmlSerializer(typeof(GameInfo)).Deserialize(stream) as GameInfo;
                }
                catch (Exception e)
                {
                    Logger.Error($"读取文件“{Filepath}”失败！");
                    Debug.LogException(e);
                    return null;
                }
            }
        }
    }
}
