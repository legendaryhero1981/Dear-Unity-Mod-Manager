using System;

namespace UnityModManagerNet
{
    public partial class UnityModManager
    {
        public class ModInfo : IEquatable<ModInfo>
        {
            public string Id;
            public string DisplayName;
            public string Author;
            public string Version;
            public string GameVersion;
            public string ManagerVersion;
            public string[] Requirements;
            public string[] LoadAfter;
            public string AssemblyName;
            public string EntryMethod;
            public string HomePage;
            public string Repository;
            public string ContentType;
            [NonSerialized]
            public bool IsCheat = true;
            /// <summary>
            ///  [0.20.0.15]
            /// </summary>
            public string FreezeUI;

            public bool Equals(ModInfo other)
            {
                return Id.Equals(other?.Id);
            }

            public static implicit operator bool(ModInfo exists)
            {
                return exists != null;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                return obj is ModInfo modInfo && Equals(modInfo);
            }

            public override int GetHashCode()
            {
                return Id.GetHashCode();
            }
        }
    }
}