using System;

namespace UnityModManagerNet.Updater
{
    public class Config
    {
        public string Repository;
    }

    public class Repository
    {
        [Serializable]
        public class Release
        {
            public string Id;
            public string Version;
            public string DownloadUrl;
        }

        public Release[] Releases;
    }
}
