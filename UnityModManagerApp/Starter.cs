using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnityModManagerNet.Injection
{
    internal class UnityModManagerStarter
    {
        public static void Start()
        {
            Injector.Run();
        }
    }
}
