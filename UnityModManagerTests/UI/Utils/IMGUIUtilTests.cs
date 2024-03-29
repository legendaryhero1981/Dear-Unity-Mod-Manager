﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
using UnityModManagerTests;

namespace UnityModManagerNet.UI.Utils.Tests
{
    [TestClass()]
    public class ImguiUtilTests : BaseTests
    {
        [TestMethod()]
        public void FileToBase64StringTest()
        {
            var imgFilePath = (string) TestContext.Properties["ImguiUtilTests.imgFilePath"];
            Logger.LogMessage(imgFilePath);
            var base64String = ImguiUtil.FileToBase64String(imgFilePath);
            Assert.IsNotNull(base64String);
        }

        [TestMethod()]
        public void Base64StringToTexture2DTest()
        {
            var texture2D = ImguiUtil.Base64StringToTexture2D("");
            Assert.IsNotNull(texture2D);
        }
    }
}