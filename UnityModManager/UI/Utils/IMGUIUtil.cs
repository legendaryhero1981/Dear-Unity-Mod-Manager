using System;
using System.IO;

using UnityEngine;

using Logger = UnityModManagerNet.UnityModManager.Logger;

namespace UnityModManagerNet.UI.Utils
{
    public sealed class ImguiUtil
    {
        /// <summary>
        /// 将图片文件转换成Texture2D对象
        /// </summary>
        public static Texture2D FileToTexture2D(string path, int width, int height)
        {
            try
            {
                var texture2D = new Texture2D(width, height);
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    var buffer = new byte[fs.Length];
                    fs.Read(buffer, 0, (int)fs.Length);
                    texture2D.LoadImage(buffer);
                    Logger.Log($"将文件{path}转换为Texture2D对象成功！");
                    _ = true;
                    return texture2D;
                }
            }
            catch (Exception e)
            {
                Logger.Error($"将文件{path}转换为Texture2D对象失败！错误信息：{e.Message}");
                return null;
            }
        }
        /// <summary>
        /// 将图片文件转换成base64编码文本
        /// </summary>
        public static string FileToBase64String(string path)
        {
            var base64String = "";
            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    var buffer = new byte[fs.Length];
                    fs.Read(buffer, 0, (int)fs.Length);
                    base64String = Convert.ToBase64String(buffer);
                    Logger.Log($"获取当前图片base64为：{base64String}");
                }
            }
            catch (Exception e)
            {
                Logger.Error($"ImgToBase64String 转换失败：{e.Message}");
            }
            return base64String;
        }

        /// <summary>
        /// 将base64编码文本转换成Texture2D对象
        /// </summary>
        public static Texture2D Base64StringToTexture2D(string base64)
        {
            var texture2D = new Texture2D(2, 2);
            texture2D.LoadImage(Convert.FromBase64String(base64));
            return texture2D;
        }
    }
}
