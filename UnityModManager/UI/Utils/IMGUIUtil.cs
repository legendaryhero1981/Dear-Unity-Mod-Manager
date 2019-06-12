using System;
using System.IO;
using UnityEngine;

namespace UnityModManagerNet.UI.Utils
{
    public sealed class ImguiUtil
    {
        /// <summary>
        /// 将图片文件转换成base64编码文本
        /// </summary>
        public static string FileToBase64String(string path)
        {
            var base64String = "";
            try
            {
                var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                var buffer = new byte[fs.Length];
                fs.Read(buffer, 0, (int)fs.Length);
                base64String = Convert.ToBase64String(buffer);
                Debug.Log("获取当前图片base64为：" + base64String);
            }
            catch (Exception e)
            {
                Debug.Log("ImgToBase64String 转换失败：" + e.Message);
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
