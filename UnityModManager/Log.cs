using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace UnityModManagerNet
{
    public partial class UnityModManager
    {
        public partial class ModEntry
        {
            public class ModLogger
            {
                protected readonly string Prefix;
                protected readonly string PrefixError;
                protected readonly string PrefixCritical;
                protected readonly string PrefixWarning;
                protected readonly string PrefixException;

                public ModLogger(string id)
                {
                    Prefix = $"[{id}] ";
                    PrefixError = $"[{id}] [错误] ";
                    PrefixCritical = $"[{id}] [严重错误] ";
                    PrefixWarning = $"[{id}] [警告] ";
                    PrefixException = $"[{id}] [异常] ";
                }

                public void Log(string str)
                {
                    UnityModManager.Logger.Log(str, Prefix);
                }

                public void Error(string str)
                {
                    UnityModManager.Logger.Log(str, PrefixError);
                }

                public void Critical(string str)
                {
                    UnityModManager.Logger.Log(str, PrefixCritical);
                }

                public void Warning(string str)
                {
                    UnityModManager.Logger.Log(str, PrefixWarning);
                }

                public void NativeLog(string str)
                {
                    UnityModManager.Logger.NativeLog(str, Prefix);
                }

                /// <summary>
                /// [0.17.0]
                /// </summary>
                public void LogException(string key, Exception e)
                {
                    UnityModManager.Logger.LogException(key, e, PrefixException);
                }

                /// <summary>
                /// [0.17.0]
                /// </summary>
                public void LogException(Exception e)
                {
                    UnityModManager.Logger.LogException(null, e, PrefixException);
                }
            }
        }

        public static class Logger
        {
            private const string Prefix = "[MOD管理器] ";
            private const string PrefixError = "[MOD管理器] [错误] ";
            private const string PrefixException = "[MOD管理器] [异常] ";

            public static readonly string Filepath = Path.Combine(Path.Combine(Application.dataPath, Path.Combine("Managed", nameof(UnityModManager))), "Log.txt");

            private static bool _clearOnce;

            public static void NativeLog(string str)
            {
                NativeLog(str, Prefix);
            }

            public static void NativeLog(string str, string prefix)
            {
                Write(prefix + str, true);
            }

            public static void Log(string str)
            {
                Log(str, Prefix);
            }

            public static void Log(string str, string prefix)
            {
                Write(prefix + str);
            }

            public static void Error(string str)
            {
                Error(str, PrefixError);
            }

            public static void Error(string str, string prefix)
            {
                Write(prefix + str);
            }

            /// <summary>
            /// [0.17.0]
            /// </summary>
            public static void LogException(Exception e)
            {
                LogException(null, e, PrefixException);
            }

            /// <summary>
            /// [0.17.0]
            /// </summary>
            public static void LogException(string key, Exception e)
            {
                LogException(key, e, PrefixException);
            }

            /// <summary>
            /// [0.17.0]
            /// </summary>
            public static void LogException(string key, Exception e, string prefix)
            {
                Write(string.IsNullOrEmpty(key)
                    ? $"{prefix}{e.GetType().Name} - {e.Message}"
                    : $"{prefix}{key}: {e.GetType().Name} - {e.Message}");
                Console.WriteLine(e.ToString());
            }

            private const int BufferCapacity = 100;
            private static readonly List<string> Buffer = new List<string>(BufferCapacity);
            internal static int HistoryCapacity = 200;
            internal static List<string> History = new List<string>(HistoryCapacity * 2);

            private static void Write(string str, bool onlyNative = false)
            {
                if (str == null) return;

                Console.WriteLine(str);

                if (onlyNative) return;

                Buffer.Add(str);
                History.Add(str);

                if (History.Count < HistoryCapacity * 2) return;

                var result = History.Skip(HistoryCapacity);
                History.Clear();
                History.AddRange(result);
            }

            private static float _timer;

            internal static void Watcher(float dt)
            {
                if (Buffer.Count >= BufferCapacity || _timer > 1f)
                    WriteBuffers();
                else
                    _timer += dt;
            }

            internal static void WriteBuffers()
            {
                try
                {
                    if (!_clearOnce)
                    {
                        File.Create(Filepath).Close();
                        _clearOnce = true;
                    }
                    if (Buffer.Count > 0)
                    {
                        using (var writer = File.AppendText(Filepath))
                        {
                            foreach (var str in Buffer)
                            {
                                writer.WriteLine(str);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }

                Buffer.Clear();
                _timer = 0;
            }

            public static void Clear()
            {
                Buffer.Clear();
                History.Clear();
            }
        }
    }
}
