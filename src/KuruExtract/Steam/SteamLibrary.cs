using KuruExtract.Extensions;
using Microsoft.Win32;
using System.Text;

namespace KuruExtract.Steam;
internal static class SteamLibrary
{
    public static string? InstallPath { get; } = FindInstallPath();

    private static string? FindInstallPath()
    {
        if (OperatingSystem.IsWindows())
        {
            var path = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null) as string;
            path ??= Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam", "InstallPath", null) as string;
            return path.ResolvePath();
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string[] candidates = [
            Path.Combine(home, ".steam", "steam"),
            Path.Combine(home, ".local", "share", "Steam"),
            Path.Combine(home, ".var", "app", "com.valvesoftware.Steam", ".local", "share", "Steam"),
        ];
        return candidates.FirstOrDefault(Directory.Exists);
    }

    private static readonly Dictionary<int, List<SteamGame>> _games = [];
    public static IReadOnlyDictionary<int, List<SteamGame>> Games => _games;

    public static void FetchGames()
    {
        foreach (var game in GetLibraryDirectories().SelectMany(GetGamesFromLibrary))
        {
            if (!_games.TryGetValue(game.AppId, out var list))
                _games[game.AppId] = list = [];

            list.Add(game);
        }
    }

    private static List<SteamGame> GetGamesFromLibrary(string libraryDirectory)
    {
        List<SteamGame> games = [];
        if (!Directory.Exists(libraryDirectory))
            return games;

        var libraryAppPath = Path.Combine(libraryDirectory, "common");
        foreach (var file in Directory.EnumerateFiles(libraryDirectory, "*.acf"))
        {
            if (!VdfParser.TryParse(File.ReadAllText(file, Encoding.UTF8), out var doc, out _))
                continue;

            // ACF files have a single top-level key ("AppState") wrapping all properties
            var appState = doc!.Children.FirstOrDefault().Value;
            if (appState is null)
                continue;

            if (!int.TryParse(appState["appid"]?.ToString(), out var appId))
                continue;

            var installDir = appState["installdir"]?.ToString();
            var name = appState["name"]?.ToString();
            if (string.IsNullOrWhiteSpace(installDir) || string.IsNullOrWhiteSpace(name))
                continue;

            var gameDirectory = Path.Combine(libraryAppPath, installDir).ResolvePath();
            if (gameDirectory is null)
                continue;

            games.Add(new SteamGame { AppId = appId, Name = name, InstallPath = gameDirectory });
        }

        return games;
    }

    private static HashSet<string> GetLibraryDirectories()
    {
        HashSet<string> libraryDirectories = [];

        var steamInstallPath = InstallPath;
        if (steamInstallPath == null || !Directory.Exists(steamInstallPath))
            return libraryDirectories;

        var steamapps = Path.Combine(steamInstallPath, "steamapps");
        if (!Directory.Exists(steamapps))
            return libraryDirectories;

        libraryDirectories.Add(steamapps);

        // newer Steam stores libraryfolders.vdf under config/, older installs use steamapps/
        var vdfPath = Path.Combine(steamInstallPath, "config", "libraryfolders.vdf");
        if (!File.Exists(vdfPath))
            vdfPath = Path.Combine(steamapps, "libraryfolders.vdf");

        if (!File.Exists(vdfPath))
            return libraryDirectories;

        if (!VdfParser.TryParse(File.ReadAllText(vdfPath, Encoding.UTF8), out var doc, out var vdfError))
        {
            Console.Error.WriteLine($"WARN: Failed to parse {vdfPath}: {vdfError}");
            return libraryDirectories;
        }

        var lf = doc!["libraryfolders"];
        if (lf is null)
            return libraryDirectories;

        foreach (var (key, node) in lf.Children)
        {
            if (!int.TryParse(key, out _)) continue;
            var path = node["path"]?.ToString();
            if (string.IsNullOrWhiteSpace(path)) continue;
            var libraryPath = Path.Combine(path, "steamapps");
            if (Directory.Exists(libraryPath))
                libraryDirectories.Add(libraryPath);
        }

        return libraryDirectories;
    }
}
