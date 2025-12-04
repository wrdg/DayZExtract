using KuruExtract.Steam;

namespace KuruExtract;
internal sealed class GamePath
{
    public static string? Stable
    {
        get
        {
            try
            {
                return SteamLibrary.Games[221100]?.InstallPath;
            }
            catch { return null; }
        }
    }

    public static string? Experimental
    {
        get
        {
            try
            {
                return SteamLibrary.Games[1024020]?.InstallPath;
            }
            catch { return null; }
        }
    }
}
