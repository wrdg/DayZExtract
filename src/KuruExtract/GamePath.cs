using KuruExtract.Steam;

namespace KuruExtract;
internal sealed class GamePath
{
    public static IReadOnlyList<string> Stable => GetPaths(221100);

    public static IReadOnlyList<string> Experimental => GetPaths(1024020);

    private static IReadOnlyList<string> GetPaths(int appId) =>
        SteamLibrary.Games.TryGetValue(appId, out var games)
            ? games.Select(g => g.InstallPath).OfType<string>().ToList()
            : [];
}
