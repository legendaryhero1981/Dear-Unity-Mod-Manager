using Microsoft.VisualStudio.TestTools.UnitTesting;
using UnityEngine;
using UnityModManagerTests;

namespace UnityModManagerNet.UI.Utils.Tests
{
    [TestClass()]
    public class IMGUIUtilTests : BaseTests
    {
        [TestMethod()]
        public void FileToBase64StringTest()
        {
            string base64String = IMGUIUtil.FileToBase64String((string)TestContext.Properties["IMGUIUtilTests.imgFilePath"]);
            Assert.IsNotNull(base64String);
        }

        [TestMethod()]
        public void Base64StringToTexture2DTest()
        {
            Texture2D texture2D = IMGUIUtil.Base64StringToTexture2D("");
            Assert.IsNotNull(texture2D);
        }
    }
}