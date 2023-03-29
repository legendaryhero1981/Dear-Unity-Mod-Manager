using JetBrains.Annotations;

namespace Doorstop;

[UsedImplicitly]
public class Entrypoint
{
    [UsedImplicitly]
    public static void Start()
    {
        UnityModManagerNet.UnityModManager.Main();
    }
}