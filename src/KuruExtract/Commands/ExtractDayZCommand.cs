using ConsoleAppFramework;
using KuruExtract.Extensions;
using KuruExtract.RV.PBO;
using KuruExtract.RV.Signatures;
using KuruExtract.Steam;
using Microsoft.Win32;
using Spectre.Console;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Velopack;
using Velopack.Sources;

namespace KuruExtract.Commands;
internal static class ExtractDayZCommand
{
    /// <summary>
    /// Extract game content from DayZ PBO archives.
    /// </summary>
    /// <param name="destination">Path to extract files (defaults to home directory if not specified).</param>
    /// <param name="unattended">-u, No prompts or pausing.</param>
    /// <param name="experimental">-x, Extract experimental version of game.</param>
    /// <param name="gameInstallPath">-g, Specify installation path of game.</param>
    /// <param name="includeExtensions">-i, Comma-separated list of extensions to be included in extraction.</param>
    /// <param name="excludeExtensions">-e, Comma-separated list of extensions to be excluded from extraction.</param>
    /// <param name="parallel">-p, Maximum number of PBOs to extract simultaneously.</param>
    /// <param name="flatScripts">-f, Extract scripts flat (no DayZ subfolder per module).</param>
    /// <param name="includeUnofficialPbos">-m, Comma-separated list of mod directories or individual PBO files to include alongside official PBOs. Supports @Name (searches game install subdirectories), relative paths from the game install, and absolute paths.</param>
    public static int Execute(
        [Argument] string? destination = null,
        bool unattended = false,
        bool experimental = false,
        string? gameInstallPath = null,
        string? includeExtensions = null,
        string? excludeExtensions = null,
        int parallel = 0,
        bool flatScripts = false,
        string? includeUnofficialPbos = null,
        CancellationToken cancellationToken = default)
    {
        Program.Unattended = unattended;

        if (!unattended)
        {
            UpdateInfo? info = null;
            var mgr = new UpdateManager(new GithubSource(Constants.UpdateUrl, null, true));

            if (mgr.IsInstalled)
            {
                Exception? updateException = null;

                AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots2)
                    .Start("Checking for updates...", (_) =>
                    {
                        try
                        {
                            info = mgr.CheckForUpdates();
                        }
                        catch (Exception ex)
                        {
                            updateException = ex;
                        }
                    });

                if (updateException != null)
                {
                    Warning($"Failed to fetch updates: {Markup.Escape(updateException.Message)}\n");
                }

                if (info != null)
                {
                    Info($"An update is available v{info.TargetFullRelease.Version}");
                    if (!info.TargetFullRelease.Version.IsPrerelease)
                    {
                        Console.WriteLine();
                    }

                    if (info.TargetFullRelease.Version.IsPrerelease)
                    {
                        Warning("This update is a pre-release.\n");
                    }

                    if (AnsiConsole.ConfirmAsync("Would you like to update?", cancellationToken: cancellationToken)
                        .GetAwaiter().GetResult())
                    {
                        mgr.DownloadUpdates(info);
                        mgr.ApplyUpdatesAndRestart(info);
                        return 1;
                    }
                }
            }

            if (OperatingSystem.IsWindows())
                PromptLegacyUninstall();
        }

        if (string.IsNullOrWhiteSpace(destination))
        {
            destination = OperatingSystem.IsWindows()
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Documents", "DayZ Projects")
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "dayzprojects");

            Directory.CreateDirectory(destination);
        }

        var includePatterns = ParsePatterns(includeExtensions);
        var excludePatterns = ParsePatterns(excludeExtensions);

        if (includePatterns != null && excludePatterns != null)
            return Error("You may only specify include or exclude, not both.");

        var promptExperimental = false;
        if (string.IsNullOrEmpty(gameInstallPath))
        {
            promptExperimental = true;
            SteamLibrary.FetchGames();
            var installPaths = experimental ? GamePath.Experimental : GamePath.Stable;

            gameInstallPath = installPaths.Count switch
            {
                0 => null,
                1 => installPaths[0],
                _ when unattended => installPaths[0],
                _ => AnsiConsole.PromptAsync(
                    new SelectionPrompt<string>()
                        .Title("Multiple installations found:")
                        .AddChoices(installPaths), cancellationToken)
                    .GetAwaiter().GetResult()
            };
        }

        if (gameInstallPath == null)
        {
            if (unattended)
                return Error("Unable to locate game installation path.");

            Warning("Unable to locate game installation path.\n");
            gameInstallPath = AnsiConsole.AskAsync<string>("Game installation path:", cancellationToken)
                .GetAwaiter().GetResult();

            if (!Directory.Exists(gameInstallPath))
                return Error("Game installation path does not exist.");
        }

        var unofficialDirs = ResolveUnofficialDirs(gameInstallPath, includeUnofficialPbos);
        var dayZKey = BiPublicKey.Read(new MemoryStream(Constants.DayZPublicKey.ToArray(), writable: false));
        var pbos = GetPBOs(gameInstallPath, unofficialDirs, dayZKey).ToList();

        if (!unattended)
        {
            if (includeExtensions != null)
                Info($"Including only: [yellow]{includeExtensions}[/]\n");
            else if (excludeExtensions != null)
                Info($"Excluding: [yellow]{excludeExtensions}[/]\n");

            var unofficialPbos = pbos.Where(p => !p.IsOfficial).ToList();
            if (unofficialPbos.Count > 0)
            {
                var pboLabel = unofficialPbos.Count == 1 ? "unofficial PBO" : "unofficial PBOs";
                Info($"Including [yellow]{unofficialPbos.Count}[/] {pboLabel}:");
                foreach (var pbo in unofficialPbos)
                    AnsiConsole.MarkupLine($"  [grey]{Markup.Escape(pbo.PBOFilePath)}[/]");
                Console.WriteLine();
            }

            destination = AnsiConsole.PromptAsync(
                new TextPrompt<string>("Destination path")
                    .DefaultValue(destination), cancellationToken)
                    .GetAwaiter().GetResult();

            if (!Directory.Exists(destination))
            {
                Console.WriteLine();
                return Error("Destination directory does not exist.");
            }

            var experimentalPaths = GamePath.Experimental;
            if (promptExperimental && !experimental && experimentalPaths.Count > 0)
            {
                experimental = AnsiConsole.ConfirmAsync("Extract experimental", false, cancellationToken)
                    .GetAwaiter().GetResult();

                if (experimental)
                {
                    gameInstallPath = experimentalPaths.Count == 1
                        ? experimentalPaths[0]
                        : AnsiConsole.PromptAsync(
                            new SelectionPrompt<string>()
                                .Title("Multiple installations found:")
                                .AddChoices(experimentalPaths), cancellationToken)
                            .GetAwaiter().GetResult();

                    unofficialDirs = ResolveUnofficialDirs(gameInstallPath, includeUnofficialPbos);
                    pbos = [.. GetPBOs(gameInstallPath, unofficialDirs, dayZKey)];
                }
            }
        }

        var stopWatch = Stopwatch.StartNew();
        var exts = excludePatterns ?? includePatterns;
        var isExclude = excludePatterns != null;

        if (exts != null)
        {
            foreach (var pbo in pbos)
                pbo.Files.RemoveAll(f => ShouldExclude(f.FileName, exts, isExclude));
        }

        var progress = AnsiConsole.Progress()
            .AutoClear(true)
            .HideCompleted(true)
            .Columns([
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn()
            ]);

        string? cleanupError = null;
        int filesAdded = 0;
        int filesDeleted = 0;

        try
        {
            progress.Start(ctx =>
            {
                // build the full set of destination paths that will be produced by extraction
                var expectedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var pbo in pbos)
                {
                    foreach (var file in pbo.Files)
                    {
                        var fileName = file.FileName;

                        // mirrors the injectSubDir logic in ExtractFile so expected paths match
                        if (!flatScripts && IsScriptsPBO(pbo))
                        {
                            var sep = fileName.IndexOfAny(['/', '\\']);

                            // handle editor/ paths by skipping the "editor" segment and inserting after that instead
                            if (sep >= 0 && fileName.AsSpan(0, sep).Equals("editor", StringComparison.OrdinalIgnoreCase))
                                sep = fileName.IndexOfAny(['/', '\\'], sep + 1);

                            if (sep >= 0)
                                fileName = Path.Combine(fileName[..sep], "DayZ", fileName[(sep + 1)..]);
                        }

                        expectedFiles.Add(Path.GetFullPath(Path.Combine(destination!, pbo.Prefix ?? string.Empty, fileName)));
                    }
                }

                // scope cleanup to unique root prefix directories (first path segment of each PBO prefix)
                // so that stale files from reorganised or removed PBOs under the same root are also caught
                var prefixDirs = pbos
                    .Select(pbo =>
                    {
                        var prefix = pbo.Prefix ?? string.Empty;
                        var root = prefix.Split(['/', '\\'], 2, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? prefix;
                        return Path.GetFullPath(Path.Combine(destination!, root));
                    })
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Where(Directory.Exists)
                    .ToList();

                // snapshot existing files to compute add/remove diff
                var existingFiles = prefixDirs
                    .SelectMany(dir => Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                // find files within prefix directories that are no longer produced by any PBO
                var filesToDelete = existingFiles
                    .Where(f => !expectedFiles.Contains(f))
                    .ToList();

                filesDeleted = filesToDelete.Count;
                filesAdded = expectedFiles.Count(f => !existingFiles.Contains(f));

                var cleanTask = ctx.AddTask("Clean up old files", maxValue: Math.Max(filesToDelete.Count, 1));

                foreach (var file in filesToDelete)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;

                    try
                    {
                        File.Delete(file);
                    }
                    catch (IOException ex)
                    {
                        cleanupError = $"Could not delete [grey]{file}[/]: {ex.Message}";
                        return;
                    }
                    cleanTask.Increment(1);
                }

                if (filesToDelete.Count == 0)
                    cleanTask.Increment(1);

                // remove subdirectories left empty within prefix directories
                foreach (var prefixDir in prefixDirs)
                    DeleteEmptyDirectories(prefixDir);

                var parallelism = parallel > 1 ? parallel : Environment.ProcessorCount;

                Partitioner.Create(pbos, EnumerablePartitionerOptions.NoBuffering)
                    .AsParallel()
                    .WithDegreeOfParallelism(parallelism)
                    .WithCancellation(cancellationToken)
                    .ForAll(pbo =>
                    {
                        var label = pbo.IsOfficial ? pbo.FileName : $"{pbo.FileName} ({pbo.Prefix})";
                        var task = ctx.AddTask(label, maxValue: pbo.Files.Count);
                        ExtractFiles(pbo, task, destination, flatScripts, cancellationToken);
                        pbo.Dispose();
                    });
            });
        }
        catch (OperationCanceledException)
        {
            stopWatch.Stop();
            Warning("User cancelled the operation.");
            return 1;
        }

        stopWatch.Stop();

        if (cleanupError != null)
            return Error(cleanupError);

        Console.WriteLine();

        var extStats = CollectExtensionStats(pbos);
        RenderBreakdownChart(extStats);

        var totalBytes = extStats.Values.Sum(x => x.Bytes);
        AnsiConsole.MarkupLine($"Extracted [yellow]{gameInstallPath}[/] to [yellow]{destination}[/]");
        AnsiConsole.MarkupLine($"Took [yellow]{FormatElapsed(stopWatch.Elapsed)}[/] to complete the operation");

        AnsiConsole.MarkupLine($"\n[green]+{filesAdded:N0}[/] added [red]-{filesDeleted:N0}[/] removed");
        AnsiConsole.MarkupLine($"Total size extracted [yellow]{FormatBytes(totalBytes)}[/]");

        return 0;
    }

    private static void DeleteEmptyDirectories(string path)
    {
        foreach (var dir in Directory.EnumerateDirectories(path))
            DeleteEmptyDirectories(dir);

        if (!Directory.EnumerateFileSystemEntries(path).Any())
            Directory.Delete(path);
    }

    private static bool IsScriptsPBO(PBO pbo) =>
        pbo.IsOfficial &&
        string.Equals(pbo.Prefix, "scripts", StringComparison.OrdinalIgnoreCase);

    private static string[]? ParsePatterns(string? patterns)
    {
        if (patterns == null) return null;
        return patterns.Replace("*", string.Empty).Split([',', ';', '|'], StringSplitOptions.RemoveEmptyEntries);
    }

    private static string FormatElapsed(TimeSpan elapsed)
    {
        static string Plural(int n, string unit) => n == 1 ? $"1 {unit}" : $"{n} {unit}s";

        if (elapsed.TotalSeconds < 60)
            return Plural((int)elapsed.TotalSeconds, "second");

        if (elapsed.TotalMinutes < 60)
        {
            var mins = Plural(elapsed.Minutes, "minute");
            return elapsed.Seconds > 0 ? $"{mins}, {Plural(elapsed.Seconds, "second")}" : mins;
        }

        var hours = Plural((int)elapsed.TotalHours, "hour");
        return elapsed.Minutes > 0 ? $"{hours}, {Plural(elapsed.Minutes, "minute")}" : hours;
    }

    private static string[] ResolveUnofficialDirs(string gameInstallPath, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return [];
        return [.. value.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(entry =>
            {
                if (Path.IsPathRooted(entry))
                    return entry;
                if (entry.StartsWith('@'))
                    return FindModDirectory(gameInstallPath, entry) ?? Path.Combine(gameInstallPath, entry);
                return Path.Combine(gameInstallPath, entry);
            })];
    }

    private static string? FindModDirectory(string gameInstallPath, string modName)
    {
        var direct = Path.Combine(gameInstallPath, modName);
        if (Directory.Exists(direct))
            return direct;

        return Directory.EnumerateDirectories(gameInstallPath)
            .Select(subDir => Path.Combine(subDir, modName))
            .FirstOrDefault(Directory.Exists);
    }

    private static IEnumerable<PBO> GetPBOs(string root, string[] unofficialDirs, BiPublicKey dayZKey)
    {
        foreach (var pboPath in EnumeratePboPaths(root))
        {
            if (IsSignedByKey(pboPath + ".dayz.bisign", dayZKey))
                yield return new PBO(pboPath) { IsOfficial = true };
        }

        foreach (var entry in unofficialDirs)
        {
            if (entry.EndsWith(".pbo", StringComparison.OrdinalIgnoreCase))
            {
                if (!File.Exists(entry)) continue;
                var pbo = new PBO(entry);
                if (!pbo.IsObfuscated)
                    yield return pbo;
            }
            else
            {
                foreach (var pboPath in EnumeratePboPaths(entry))
                {
                    var pbo = new PBO(pboPath);
                    if (!pbo.IsObfuscated)
                        yield return pbo;
                }
            }
        }
    }

    // walks a directory tree skipping junctions and symlinks in subdirectories
    private static IEnumerable<string> EnumeratePboPaths(string root)
    {
        if (!Directory.Exists(root))
            yield break;

        var dirs = new Stack<string>();
        dirs.Push(root);

        while (dirs.Count > 0)
        {
            var dir = dirs.Pop();

            foreach (var pboPath in Directory.EnumerateFiles(dir, "*.pbo", SearchOption.TopDirectoryOnly))
                yield return pboPath;

            foreach (var subDir in Directory.EnumerateDirectories(dir))
            {
                if (!File.GetAttributes(subDir).HasFlag(FileAttributes.ReparsePoint))
                    dirs.Push(subDir);
            }
        }
    }

    private static bool IsSignedByKey(string bisignPath, BiPublicKey expectedKey)
    {
        try
        {
            using var stream = File.OpenRead(bisignPath);
            var sign = BiSign.Read(stream);
            return sign.PublicKey.Matches(expectedKey);
        }
        catch { return false; }
    }

    private static void ExtractFiles(PBO pbo, ProgressTask task, string destination, bool flatScripts, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(destination, pbo.Prefix ?? string.Empty);
        var injectSubDir = !flatScripts && IsScriptsPBO(pbo) ? "DayZ" : null;

        foreach (var file in CollectionsMarshal.AsSpan(pbo.Files))
        {
            cancellationToken.ThrowIfCancellationRequested();
            PBO.ExtractFile(file, path, injectSubDir);
            task.Increment(1);
        }
    }

    private readonly record struct ExtensionStat(long Count, long Bytes);

    private static Dictionary<string, ExtensionStat> CollectExtensionStats(List<PBO> pbos)
    {
        var stats = new Dictionary<string, ExtensionStat>(StringComparer.OrdinalIgnoreCase);
        foreach (var pbo in pbos)
        {
            foreach (var file in pbo.Files)
            {
                var ext = Path.GetExtension(file.FileName);
                if (string.IsNullOrEmpty(ext)) ext = "(no ext)";

                stats.TryGetValue(ext, out var current);
                stats[ext] = new(current.Count + 1, current.Bytes + file.DiskSize);
            }
        }
        return stats;
    }

    private static readonly FrozenDictionary<string, Color> ExtensionColors = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase)
    {
        // textures
        { "paa",      Color.Blue            },
        { "dds",      Color.SteelBlue1      },
        { "edds",     Color.DodgerBlue1     },
        { "tga",      Color.CornflowerBlue  },
        { "txo",      Color.SkyBlue1        },
        { "nm",       Color.SlateBlue1      },
        { "pac",      Color.LightSkyBlue1   },
        // models and animation
        { "p3d",      Color.Green           },
        { "xob",      Color.SpringGreen1    },
        { "anm",      Color.Chartreuse1     },
        { "agr",      Color.DarkSeaGreen1   },
        { "asi",      Color.DarkSeaGreen2   },
        { "ast",      Color.DarkSeaGreen3   },
        { "asy",      Color.DarkSeaGreen4   },
        { "aw",       Color.DarkOliveGreen1 },
        // world
        { "wrp",      Color.Teal            },
        { "map",      Color.DarkCyan        },
        // audio
        { "ogg",      Color.Orange1         },
        // scripts and configs
        { "cpp",      Color.Red             },
        { "hpp",      Color.IndianRed1      },
        { "c",        Color.LightCoral      },
        { "xml",      Color.OrangeRed1      },
        { "json",     Color.DarkOrange      },
        { "csv",      Color.Gold1           },
        { "cfg",      Color.Yellow          },
        { "txt",      Color.Cornsilk1       },
        // shaders
        { "pso",      Color.Fuchsia         },
        { "vso",      Color.DeepPink1       },
        { "cso",      Color.MediumOrchid1   },
        { "vert",     Color.Orchid1         },
        { "frag",     Color.Plum1           },
        // materials and UI
        { "rvmat",    Color.Aqua            },
        { "bisurf",   Color.Turquoise2      },
        { "emat",     Color.DarkTurquoise   },
        { "layout",   Color.Lime            },
        { "styles",   Color.GreenYellow     },
        { "imageset", Color.PaleGreen1      },
        { "qss",      Color.SeaGreen1       },
        // fonts
        { "ttf",      Color.Magenta1        },
        { "fnt",      Color.MediumPurple1   },
    }.ToFrozenDictionary();

    private static readonly string[] SizeUnits = ["B", "KB", "MB", "GB", "TB", "PB"];

    private static readonly ExtensionEqualityComparer ExtensionComparer = new();

    private static string FormatBytes(double bytes)
    {
        if (bytes <= 0) return $"0 {SizeUnits[0]}";

        var index = (int)Math.Truncate(Math.Log(bytes, 1024));
        index = Math.Clamp(index, 0, SizeUnits.Length - 1);
        return $"{bytes / Math.Pow(1024, index):N2} {SizeUnits[index]}";
    }

    private static void RenderBreakdownChart(Dictionary<string, ExtensionStat> extStats)
    {
        if (extStats.Count == 0) return;

        var chart = new BreakdownChart()
            .Width(Math.Min(Console.WindowWidth - 2, 80))
            .UseValueFormatter((value, _) => FormatBytes(value));

        foreach (var (ext, (_, bytes)) in extStats.OrderByDescending(x => x.Value.Bytes))
        {
            var color = ExtensionColors.GetValueOrDefault(ext.TrimStart('.'), Color.Grey);
            chart.AddItem(ext, bytes, color);
        }

        AnsiConsole.Write(chart);
        AnsiConsole.WriteLine();
    }

    private static int Error(string message)
    {
        AnsiConsole.MarkupLine($"[red]ERROR:[/] {message}");
        return 1;
    }

    private static void Warning(string message)
    {
        AnsiConsole.MarkupLine($"[yellow]WARN:[/] {message}");
    }

    private static void Info(string message)
    {
        AnsiConsole.MarkupLine($"[blue]INFO:[/] {message}");
    }

    private static bool ShouldExclude(string fileName, string[] exts, bool exclude) =>
        exclude ? exts.Contains(fileName, ExtensionComparer) : !exts.Contains(fileName, ExtensionComparer);

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static void PromptLegacyUninstall()
    {
        var key = Registry.LocalMachine.OpenSubKey(
            $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{Constants.LegacyProductCode}");

        if (key == null) return;

        Warning("Legacy installer is detected. It is recommended to uninstall it.\n");

        if (!AnsiConsole.ConfirmAsync("Uninstall now?").GetAwaiter().GetResult()) return;

        Process.Start(new ProcessStartInfo("msiexec.exe", $"/x {Constants.LegacyProductCode} /qb")
        {
            UseShellExecute = true
        })?.WaitForExit();

        RecreateShortcuts();
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static void RecreateShortcuts()
    {
        var exePath = Environment.ProcessPath;
        if (exePath == null) return;

        string[] lnkPaths = [
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "DayZExtract.lnk"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", "DayZExtract.lnk"),
        ];

        foreach (var lnkPath in lnkPaths)
        {
            try
            {
                Interop.ShellShortcut.Create(lnkPath, exePath, string.Empty);
            }
            catch (Exception ex)
            {
                Warning($"Failed to create shortcut [grey]{lnkPath}[/]: {Markup.Escape(ex.Message)}\n");
            }
        }
    }
}
