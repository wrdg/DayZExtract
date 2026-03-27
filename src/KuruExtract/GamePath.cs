using KuruExtract.Steam;

namespace KuruExtract;
internal sealed class GamePath
{
    public static string? Stable => SteamLibrary.Games[221100]?.InstallPath;

    public static string? Experimental => SteamLibrary.Games[1024020]?.InstallPath;
}
