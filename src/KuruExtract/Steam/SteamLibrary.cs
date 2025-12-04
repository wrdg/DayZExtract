using Gameloop.Vdf.Linq;
using KuruExtract.Extensions;
using Microsoft.Win32;
using System.Text;

namespace KuruExtract.Steam;
public class SteamLibrary
{
    private static string? _installPath;

    public static string? InstallPath
    {
        get
        {
            _installPath ??= Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null) as string;
            _installPath ??= Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam", "InstallPath", null) as string;
            return _installPath.ResolvePath();
        }
    }

    public static Dictionary<int, SteamGame> Games { get; } = [];

    public static void FetchGames()
    {
        var gameLibraryDirectories = GetLibraryDirectories();
        foreach (var game in gameLibraryDirectories.Select(GetGamesFromLibrary).SelectMany(games => games))
        {
            Games[game.AppId] = game;
        }
    }

    private static List<SteamGame> GetGamesFromLibrary(string libraryDirectory)
    {
        List<SteamGame> games = [];
        if (!Directory.Exists(libraryDirectory))
        {
            return games;
        }

        var libraryAppPath = Path.Combine(libraryDirectory, "common");
        foreach (var file in Directory.EnumerateFiles(libraryDirectory, "*.acf"))
        {
            if (!ValveDataFile.TryDeserialize(File.ReadAllText(file, Encoding.UTF8), out var result))
            {
                continue;
            }

            if (result is null || !int.TryParse(result.Value.GetChild("appid")?.ToString(), out var appId))
            {
                continue;
            }

            var installDir = result.Value.GetChild("installdir")?.ToString();
            var name = result.Value.GetChild("name")?.ToString();
            if (string.IsNullOrWhiteSpace(installDir) || string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            ;
            var gameDirectory = Path.Combine(libraryAppPath, installDir).ResolvePath();
            if (gameDirectory is null || games.Any(g => g.AppId == appId))
            {
                continue;
            }

            games.Add(new SteamGame
            {
                AppId = appId,
                Name = name,
                InstallPath = gameDirectory
            });
        }

        return games;
    }

    private static HashSet<string> GetLibraryDirectories()
    {
        HashSet<string> libraryDirectories = [];

        var steamInstallPath = InstallPath;
        if (steamInstallPath == null || !Directory.Exists(steamInstallPath))
        {
            return libraryDirectories;
        }

        var libraryFolder = Path.Combine(steamInstallPath, "steamapps");
        if (!Directory.Exists(libraryFolder))
        {
            return libraryDirectories;
        }

        _ = libraryDirectories.Add(libraryFolder);
        var libraryFolders = Path.Combine(libraryFolder, "libraryfolders.vdf");
        if (!File.Exists(libraryFolders) ||
            !ValveDataFile.TryDeserialize(File.ReadAllText(libraryFolders, Encoding.UTF8), out var result))
        {
            return libraryDirectories;
        }

        if (result is null)
        {
            return libraryDirectories;
        }

        foreach (var vToken in result.Value.Where(p =>
                     p is VProperty property && int.TryParse(property.Key, out _)))
        {
            var property = (VProperty)vToken;
            var path = property.Value.GetChild("path")?.ToString();
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            var libraryPath = Path.Combine(path, "steamapps");
            if (Directory.Exists(path))
            {
                _ = libraryDirectories.Add(libraryPath);
            }
        }

        return libraryDirectories;
    }
}