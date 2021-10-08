using System.Collections.Generic;
using System.IO;

namespace UnityModManagerNet.Installer
{
    public class Utils : ConsoleInstaller.Utils
    {
        public static Dictionary<string, string> GetMatchedFiles(string path, string regex, Dictionary<string, string> defaults = null)
        {
            var results = defaults is {Count: > 0} ? new Dictionary<string, string>(defaults) : new Dictionary<string, string>();
            var directoryInfo = new DirectoryInfo(path);
            foreach (var fileInfo in directoryInfo.GetFiles(regex))
                results[Path.GetFileNameWithoutExtension(fileInfo.FullName)] = fileInfo.FullName;
            return results;
        }
    }
}
