using dnlib.DotNet;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace UnityModManagerNet.Installer
{
    internal static class Utils
    {
        internal const string FileSuffixBak = ".bak";
        internal const string FileSuffixCache = ".cache";

        static Utils() { }

        internal static Dictionary<string, string> GetMatchedFiles(string path, string regex, Dictionary<string, string> defaults = null)
        {
            Dictionary<string, string> results;
            if (null != defaults && 0 < defaults.Count)
                results = new Dictionary<string, string>(defaults);
            else
                results = new Dictionary<string, string>();
            var directoryInfo = new DirectoryInfo(path);
            foreach (var fileInfo in directoryInfo.GetFiles(regex))
                results[Path.GetFileNameWithoutExtension(fileInfo.FullName)] = fileInfo.FullName;
            return results;
        }

        internal static Version ParseVersion(string str)
        {
            var array = str.Split('.');
            var regex = new Regex(@"\D");

            switch (array.Length)
            {
                case 1:
                    return new Version(int.Parse(regex.Replace(array[0], "")), 0);
                case 2:
                    return new Version(int.Parse(regex.Replace(array[0], "")), int.Parse(regex.Replace(array[1], "")));
                case 3:
                    return new Version(int.Parse(regex.Replace(array[0], "")), int.Parse(regex.Replace(array[1], "")), int.Parse(regex.Replace(array[2], "")));
                case 4:
                    return new Version(int.Parse(regex.Replace(array[0], "")), int.Parse(regex.Replace(array[1], "")), int.Parse(regex.Replace(array[2], "")), int.Parse(regex.Replace(array[3], "")));
                default:
                    Log.Print($"版本字符串“{str}”解析失败！");
                    return new Version();
            }
        }

        internal static bool IsDirectoryWritable(string dirpath)
        {
            try
            {
                if (!Directory.Exists(dirpath)) return true;
                using (var fs = File.Create(Path.Combine(dirpath, Path.GetRandomFileName()), 1, FileOptions.DeleteOnClose)) { }
                return true;
            }
            catch
            {
                Log.Print($"目录“{dirpath}”没有写入权限！");
                return false;
            }
        }

        internal static bool IsFileWritable(string filepath)
        {
            try
            {
                if (!File.Exists(filepath)) return true;
                using (var fs = File.OpenWrite(filepath)) { }
                return true;
            }
            catch
            {
                Log.Print($"文件“{filepath}”没有写入权限！");
                return false;
            }
        }

        internal static bool TryParseEntryPoint(string str, out string assembly)
        {
            return TryParseEntryPoint(str, out assembly, out _, out _, out _);
        }

        internal static bool TryParseEntryPoint(string str, out string assembly, out string @class, out string method, out string insertionPlace)
        {
            assembly = string.Empty;
            @class = string.Empty;
            method = string.Empty;
            insertionPlace = string.Empty;

            var regex = new Regex(@"(?:(?<=\[)(?'assembly'.+(?>\.dll))(?=\]))|(?:(?'class'[\w|\.]+)(?=\.))|(?:(?<=\.)(?'func'\w+))|(?:(?<=\:)(?'mod'\w+))", RegexOptions.IgnoreCase);
            var matches = regex.Matches(str);
            var groupNames = regex.GetGroupNames();

            if (matches.Count > 0)
            {
                foreach (Match match in matches)
                {
                    foreach (var group in groupNames)
                    {
                        if (!match.Groups[@group].Success) continue;
                        switch (@group)
                        {
                            case "assembly":
                                assembly = match.Groups[@group].Value;
                                break;
                            case "class":
                                @class = match.Groups[@group].Value;
                                break;
                            case "func":
                                method = match.Groups[@group].Value;
                                if (method == "ctor") method = ".ctor";
                                else if (method == "cctor") method = ".cctor";
                                break;
                            case "mod":
                                insertionPlace = match.Groups[@group].Value.ToLower();
                                break;
                        }
                    }
                }
                //Log.Print($"{assembly},{@class},{method},{insertionPlace}");
            }

            var hasError = false;

            if (string.IsNullOrEmpty(assembly))
            {
                hasError = true;
                Log.Print("找不到Assembly名称！");
            }

            if (string.IsNullOrEmpty(@class))
            {
                hasError = true;
                Log.Print("找不到类名称！");
            }

            if (string.IsNullOrEmpty(method))
            {
                hasError = true;
                Log.Print("找不到方法名称！");
            }

            if (!hasError) return true;
            Log.Print($"解析入口点字符串“{str}”失败！");
            return false;

        }

        internal static bool TryGetEntryPoint(ModuleDefMD assemblyDef, string str, out MethodDef foundMethod, out string insertionPlace, bool createConstructor = false)
        {
            foundMethod = null;

            if (!TryParseEntryPoint(str, out var assembly, out var className, out var methodName, out insertionPlace))
            {
                return false;
            }

            var targetClass = assemblyDef.Types.FirstOrDefault(x => x.FullName == className);
            if (targetClass == null)
            {
                Log.Print($"找不到类名称“{className}”！");
                return false;
            }

            foundMethod = targetClass.Methods.FirstOrDefault(x => x.Name == methodName);
            if (foundMethod != null) return true;
            if (createConstructor && methodName == ".cctor")
            {
                //var m = new MethodDefUser(".cctor", assemblyDef.CorLibTypes.Void, MethodAttributes.Private | MethodAttributes.RTSpecialName | MethodAttributes.SpecialName | MethodAttributes.Static);
                var typeDef = ModuleDefMD.Load(typeof(Utils).Module).Types.FirstOrDefault(x => x.FullName == typeof(Utils).FullName);
                var method = typeDef.Methods.FirstOrDefault(x => x.Name == ".cctor");
                if (method != null)
                {
                    typeDef.Methods.Remove(method);
                    targetClass.Methods.Add(method);
                    foundMethod = method;

                    return true;
                }
            }
            Log.Print($"找不到方法名称“{methodName}”！");
            return false;
        }

        internal static string ResolveOSXFileUrl(string url)
        {
            var p = new Process
            {
                StartInfo =
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    FileName = "osascript",
                    Arguments = $"-e \"获取posix文件的路径 \\\"{url}\\\"\""
                }
            };
            p.Start();
            var output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            return output.TrimEnd();
        }

        internal static bool IsUnixPlatform()
        {
            var p = (int)Environment.OSVersion.Platform;
            return (p == 4) || (p == 6) || (p == 128);
        }

        public static bool IsMacPlatform()
        {
            var p = (int)Environment.OSVersion.Platform;
            return (p == 6);
        }

        public static bool IsLinuxPlatform()
        {
            var p = (int)Environment.OSVersion.Platform;
            return (p == 4) || (p == 128);
        }

        internal static bool MakeBackup(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Copy(path, $"{path}{FileSuffixBak}", true);
                }
            }
            catch (Exception e)
            {
                Log.Print(e.Message);
                return false;
            }

            return true;
        }

        internal static bool MakeBackup(List<string> arr)
        {
            try
            {
                foreach (var path in arr.Where(File.Exists))
                {
                    File.Copy(path, $"{path}{FileSuffixBak}", true);
                }
            }
            catch (Exception e)
            {
                Log.Print(e.Message);
                return false;
            }

            return true;
        }

        internal static bool RestoreBackup(string path)
        {
            try
            {
                var backup = $"{path}{FileSuffixBak}";
                if (File.Exists(backup))
                {
                    File.Copy(backup, path, true);
                }
            }
            catch (Exception e)
            {
                Log.Print(e.Message);
                return false;
            }

            return true;
        }

        internal static bool RestoreBackup(List<string> arr)
        {
            try
            {
                foreach (var path in arr)
                {
                    var backup = $"{path}{FileSuffixBak}";
                    if (File.Exists(backup))
                    {
                        File.Copy(backup, path, true);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Print(e.Message);
                return false;
            }

            return true;
        }

        internal static bool DeleteBackup(string path)
        {
            try
            {
                var backup = $"{path}{FileSuffixBak}";
                if (File.Exists(backup))
                {
                    File.Delete(backup);
                }
            }
            catch (Exception e)
            {
                Log.Print(e.Message);
                return false;
            }

            return true;
        }

        internal static bool DeleteBackup(List<string> arr)
        {
            try
            {
                foreach (var backup in arr.Select(path => $"{path}{FileSuffixBak}").Where(File.Exists))
                    File.Delete(backup);
            }
            catch (Exception e)
            {
                Log.Print(e.Message);
                return false;
            }

            return true;
        }

        internal enum MachineType : ushort
        {
            IMAGE_FILE_MACHINE_UNKNOWN = 0x0,
            IMAGE_FILE_MACHINE_AM33 = 0x1d3,
            IMAGE_FILE_MACHINE_AMD64 = 0x8664,
            IMAGE_FILE_MACHINE_ARM = 0x1c0,
            IMAGE_FILE_MACHINE_EBC = 0xebc,
            IMAGE_FILE_MACHINE_I386 = 0x14c,
            IMAGE_FILE_MACHINE_IA64 = 0x200,
            IMAGE_FILE_MACHINE_M32R = 0x9041,
            IMAGE_FILE_MACHINE_MIPS16 = 0x266,
            IMAGE_FILE_MACHINE_MIPSFPU = 0x366,
            IMAGE_FILE_MACHINE_MIPSFPU16 = 0x466,
            IMAGE_FILE_MACHINE_POWERPC = 0x1f0,
            IMAGE_FILE_MACHINE_POWERPCFP = 0x1f1,
            IMAGE_FILE_MACHINE_R4000 = 0x166,
            IMAGE_FILE_MACHINE_SH3 = 0x1a2,
            IMAGE_FILE_MACHINE_SH3DSP = 0x1a3,
            IMAGE_FILE_MACHINE_SH4 = 0x1a6,
            IMAGE_FILE_MACHINE_SH5 = 0x1a8,
            IMAGE_FILE_MACHINE_THUMB = 0x1c2,
            IMAGE_FILE_MACHINE_WCEMIPSV2 = 0x169,
        }

        internal static MachineType GetDllMachineType(string dllPath)
        {
            // See http://www.microsoft.com/whdc/system/platform/firmware/PECOFF.mspx
            // Offset to PE header is always at 0x3C.
            // The PE header starts with "PE\0\0" =  0x50 0x45 0x00 0x00,
            // followed by a 2-byte machine type field (see the document above for the enum).
            var fs = new FileStream(dllPath, FileMode.Open, FileAccess.Read);
            var br = new BinaryReader(fs);
            fs.Seek(0x3c, SeekOrigin.Begin);
            var peOffset = br.ReadInt32();
            fs.Seek(peOffset, SeekOrigin.Begin);
            var peHead = br.ReadUInt32();

            if (peHead != 0x00004550) // "PE\0\0", little-endian
                throw new Exception($"找不到文件{dllPath}的PE头数据！");

            var machineType = (MachineType)br.ReadUInt16();
            br.Close();
            fs.Close();
            return machineType;
        }

        internal static bool? UnmanagedDllIs64Bit(string dllPath)
        {
            switch (GetDllMachineType(dllPath))
            {
                case MachineType.IMAGE_FILE_MACHINE_AMD64:
                case MachineType.IMAGE_FILE_MACHINE_IA64:
                    return true;
                case MachineType.IMAGE_FILE_MACHINE_I386:
                    return false;
                default:
                    return null;
            }
        }
    }
}
