﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace UnityModManagerNet.ConsoleInstaller
{
    public enum ModStatus { NotInstalled, Installed }
    public enum InstallType { DoorstopProxy, Assembly, Count }

    public class ModInfo : UnityModManager.ModInfo
    {
        [JsonIgnore]
        public ModStatus Status;

        [JsonIgnore]
        public Dictionary<Version, string> AvailableVersions = new();

        [JsonIgnore]
        public Version ParsedVersion;

        [JsonIgnore]
        public string Path;

        public bool IsValid()
        {
            if (string.IsNullOrEmpty(Id))
            {
                return false;
            }
            if (string.IsNullOrEmpty(DisplayName))
            {
                DisplayName = Id;
            }
            if (ParsedVersion == null)
            {
                ParsedVersion = Utils.ParseVersion(Version);
            }

            return true;
        }

        public bool EqualsVersion(ModInfo other)
        {
            return other != null && Id.Equals(other.Id) && Version.Equals(other.Version);
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
        public string OldPatchTarget;
        public string Comment;
        public string FixBlackUI;
        public string MinimalManagerVersion;
        public string ExtraFilesUrl;

        public override string ToString()
        {
            return Name;
        }

        public static string filepathInGame;

        public void ExportToGame()
        {
            try
            {
                using var writer = new StreamWriter(filepathInGame);
                new XmlSerializer(typeof(GameInfo)).Serialize(writer, this);
            }
            catch (Exception e)
            {
                Log.Print($"创建文件“{filepathInGame}”失败！");
                throw e;
            }
        }

        public static GameInfo ImportFromGame()
        {
            try
            {
                using var stream = File.OpenRead(filepathInGame);
                return new XmlSerializer(typeof(GameInfo)).Deserialize(stream) as GameInfo;
            }
            catch (Exception e)
            {
                Log.Print(e.ToString());
                Log.Print($"不能读取文件“{filepathInGame}”！");
                return null;
            }
        }
    }

    public sealed class Config
    {
        public const string filename = "UnityModManagerConfig.xml";

        public string Repository;
        public string HomePage;

        [XmlElement]
        public GameInfo[] GameInfo;

        public static void Create()
        {
            try
            {
                using (var writer = new StreamWriter(Path.Combine(Application.StartupPath, filename)))
                {
                    var config = new Config()
                    {
                        GameInfo = new GameInfo[]
                        {
                            new()
                            {
                                Name = "Game",
                                Folder = "Folder",
                                ModsDirectory = "Mods",
                                ModInfo = "Info.json",
                                EntryPoint = "[Assembly-CSharp.dll]App.Awake",
                                GameExe = "Game.exe"
                            }
                        }
                    };
                    var serializer = new XmlSerializer(typeof(Config));
                    serializer.Serialize(writer, config);
                }

                Log.Print($"文件“{filename}”已自动创建！");
            }
            catch (Exception e)
            {
                Log.Print(e.ToString());
            }
        }

        public static Config Load()
        {
            if (File.Exists(filename))
            {
                try
                {
                    using var stream = File.OpenRead(Path.Combine(Application.StartupPath, filename));
                    var serializer = new XmlSerializer(typeof(Config));
                    var result = serializer.Deserialize(stream) as Config;
                    foreach (var file in new DirectoryInfo(Application.StartupPath).GetFiles("UnityModManagerConfig*.xml", SearchOption.TopDirectoryOnly))
                        if (file.Name != filename)
                        {
                            try
                            {
                                using var localStream = File.OpenRead(file.FullName);
                                var localResult = serializer.Deserialize(localStream) as Config;
                                if (localResult?.GameInfo == null || result?.GameInfo == null) continue;
                                var concatanatedArray = new GameInfo[result.GameInfo.Length + localResult.GameInfo.Length];
                                result.GameInfo.CopyTo(concatanatedArray, 0);
                                localResult.GameInfo.CopyTo(concatanatedArray, result.GameInfo.Length);
                                result.GameInfo = concatanatedArray;
                            }
                            catch (Exception e)
                            {
                                Log.Print(e.ToString());
                            }
                        }
                    OnDeserialize(result);
                    return result;
                }
                catch (Exception e)
                {
                    Log.Print(e + Environment.NewLine + filename);
                }
            }
            else
                Log.Print($"配置文件“{filename}”不存在！");
            return null;
        }

        private static void OnDeserialize(Config value)
        {

        }
    }

    public sealed class Param
    {
        [Serializable]
        public class GameParam
        {
            [XmlAttribute]
            public string Name;
            public string Path;
            public InstallType InstallType = InstallType.DoorstopProxy;
        }

        public int LastSelectedSkin;
        public string LastSelectedGame;
        public List<GameParam> GameParams = new();

        public const string filename = "Params.xml";

        static GameParam CreateGameParam(GameInfo gameInfo)
        {
            return new GameParam { Name = gameInfo.Name };
        }

        public GameParam GetGameParam(GameInfo gameInfo)
        {
            var result = GameParams.FirstOrDefault(x => x.Name == gameInfo.Name);
            if (result == null)
            {
                result = CreateGameParam(gameInfo);
                GameParams.Add(result);
            }
            return result;
        }

        public void Sync(GameInfo[] gameInfos)
        {
            var i = 0;
            while (i < GameParams.Count)
            {
                if (gameInfos.Any(x => x.Name == GameParams[i].Name))
                {
                    i++;
                }
                else
                {
                    GameParams.RemoveAt(i);
                }
            }
        }

        public void Save()
        {
            var path = Path.Combine(Application.LocalUserAppDataPath, filename);
            try
            {
                using var writer = new StreamWriter(path);
                Log.Print($"已保存参数文件“{path}”！");
                var serializer = new XmlSerializer(typeof(Param));
                serializer.Serialize(writer, this);
            }
            catch (Exception e)
            {
                Log.Print(e.ToString() + Environment.NewLine + path);
            }
        }

        public static Param Load()
        {
            var path = Path.Combine(Application.LocalUserAppDataPath, filename);
            if (!File.Exists(path)) return new Param();
            try
            {
                using var stream = File.OpenRead(path);
                Log.Print($"已读取参数文件“{path}”！");
                var serializer = new XmlSerializer(typeof(Param));
                return serializer.Deserialize(stream) as Param;
            }
            catch (Exception e)
            {
                Log.Print(e.ToString() + Environment.NewLine + path);
            }
            return new Param();
        }
    }
}