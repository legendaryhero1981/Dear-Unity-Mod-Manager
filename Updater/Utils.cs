using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;

namespace UnityModManagerNet.Updater
{
    internal static class Utils
    {
        private const string RepositoryUrl = "raw.githubusercontent.com";
        /// <summary>
        /// 执行内部命令（cmd.exe 中的命令）
        /// </summary>
        /// <param name="cmd">命令行</param>
        /// <returns>执行结果</returns>
        public static string ExecuteInCmd(string cmd)
        {
            using var process = new Process
            {
                StartInfo =
                {
                    FileName = "cmd.exe",
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.StandardInput.AutoFlush = true;
            process.StandardInput.WriteLine(cmd + "&exit");
            //获取cmd窗口的输出信息  
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            process.Close();
            return output;
        }
        /// <summary>
        /// 执行外部命令
        /// </summary>
        /// <param name="param">命令参数</param>
        /// <param name="path">命令程序路径</param>
        /// <returns>执行结果</returns>
        public static string ExecuteOutCmd(string param, string path)
        {
            using var process = new Process
            {
                StartInfo =
                {
                    Arguments = param,
                    FileName = path,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.StandardInput.AutoFlush = true;
            process.StandardInput.WriteLine("exit");
            //获取cmd窗口的输出信息  
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            process.Close();
            return output;
        }

        public static Version ParseVersion(string str)
        {
            var array = str.Split('.', ',');
            if (array.Length < 3) return new Version();
            var regex = new Regex(@"\D");
            return new Version(int.Parse(regex.Replace(array[0], "")), int.Parse(regex.Replace(array[1], "")), int.Parse(regex.Replace(array[2], "")));
        }

        public static bool HasNetworkConnection()
        {
            try
            {
                using var ping = new Ping();
                return ping.Send(RepositoryUrl, 3000)?.Status == IPStatus.Success;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsUnixPlatform()
        {
            var p = (int)Environment.OSVersion.Platform;
            return (p == 4) || (p == 6) || (p == 128);
        }
    }
}
