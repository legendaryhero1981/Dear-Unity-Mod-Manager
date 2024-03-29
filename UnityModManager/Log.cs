﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace UnityModManagerNet;

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

        public static readonly string Filepath = Path.Combine(Path.GetDirectoryName(typeof(GameInfo).Assembly.Location) ?? string.Empty, "Log.txt");

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

        private static bool hasErrors;
        private const int bufferCapacity = 100;
        private static readonly List<string> buffer = new List<string>(bufferCapacity);
        internal static int historyCapacity = 200;
        internal static List<string> history = new List<string>(historyCapacity * 2);

        private static void Write(string str, bool onlyNative = false)
        {
            if (str == null) return;

            Console.WriteLine(str);

            if (onlyNative) return;

            buffer.Add(str);
            history.Add(str);

            if (history.Count < historyCapacity * 2) return;

            var result = history.Skip(historyCapacity);
            history.Clear();
            history.AddRange(result);
        }

        private static float _timer;

        internal static void Watcher(float dt)
        {
            if (buffer.Count >= bufferCapacity || _timer > 0.5f)
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
                if (buffer.Count > 0 && !hasErrors)
                {
                    using var writer = File.AppendText(Filepath);
                    foreach (var str in buffer)
                    {
                        writer.WriteLine(str);
                    }
                }
            }
            catch (UnauthorizedAccessException e)
            {
                hasErrors = true;
                Console.WriteLine(PrefixException + e);
                Console.WriteLine(Prefix + "已取消选中UnityModManager目录的只读复选框。");
                history.Add(PrefixException + e);
                history.Add(Prefix + "已取消选中UnityModManager目录的只读复选框。");
            }
            catch (Exception e)
            {
                hasErrors = true;
                Console.WriteLine(PrefixException + e);
                history.Add(PrefixException + e);
            }

            buffer.Clear();
            _timer = 0;
        }

        public static void Clear()
        {
            buffer.Clear();
            history.Clear();
        }
    }
}