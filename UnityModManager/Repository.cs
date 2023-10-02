using System;

namespace UnityModManagerNet
{
    public partial class UnityModManager
    {
        public class Repository
        {
            public Release[] Releases;

            [Serializable]
            public class Release : IEquatable<Release>
            {
                public string DownloadUrl;
                public string Id;
                public string Version;

                public bool Equals(Release other)
                {
                    return Id.Equals(other.Id);
                }

                public override bool Equals(object obj)
                {
                    if (ReferenceEquals(null, obj)) return false;
                    return obj is Release obj2 && Equals(obj2);
                }

                public override int GetHashCode()
                {
                    return Id.GetHashCode();
                }
            }
        }
    }
}