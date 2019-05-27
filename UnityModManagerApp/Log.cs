using System;
using System.IO;

namespace UnityModManagerNet.Installer
{
    static class Log
    {
        public const string fileLog = "Log.txt";
        public static readonly StreamWriter Writer = File.CreateText(fileLog);
        private static bool firstLine = true;

        public static void Print(string str, bool append = false)
        {
            if (append)
            {
                UnityModManagerForm.instance.statusLabel.Text += str;
                UnityModManagerForm.instance.statusLabel.ToolTipText += str;
            }
            else
            {
                UnityModManagerForm.instance.statusLabel.Text = str;
                UnityModManagerForm.instance.statusLabel.ToolTipText = str;
                if (firstLine)
                {
                    firstLine = false;
                    str = $"[{DateTime.Now.ToShortTimeString()}] {str}";
                }
                else
                {
                    str = $"\r\n[{DateTime.Now.ToShortTimeString()}] {str}";
                }
            }

            UnityModManagerForm.instance.inputLog.AppendText(str);

            try
            {
                Writer.Write(str);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public static void Append(string str)
        {
            Print(str, true);
        }
    }
}
