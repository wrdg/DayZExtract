using ConsoleAppFramework;
using KuruExtract.RV;
using KuruExtract.RV.PBO;
using KuruExtract.Steam;
using Spectre.Console;
using System.Diagnostics;
using System.Runtime.InteropServices;

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
    public static int Execute(
        [Argument] string? destination = null,
        bool unattended = false,
        bool experimental = false,
        string? gameInstallPath = null,
        string? includeExtensions = null,
        string? excludeExtensions = null,
        int parallel = 0,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(destination))
        {
            destination = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "dayz-extract");
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
            gameInstallPath = experimental ? GamePath.Experimental : GamePath.Stable;
        }

        if (gameInstallPath == null)
            return Error("Unable to locate game installation path.");

        if (!unattended)
        {
            if (Program.UpdateChecker.CheckUpdate())
            {
                AnsiConsole.MarkupLine("An update is available!");
                var update = AnsiConsole.Confirm("Would you like to update?");

                if (update)
                {
                    Process.Start(new ProcessStartInfo(Program.UpdateChecker.DownloadUrl)
                    {
                        UseShellExecute = true
                    });

                    return 1;
                }
            }

            destination = AnsiConsole.Prompt(
                new TextPrompt<string>("Destination path")
                    .DefaultValue(destination)
                    .ValidationErrorMessage("[red]Not a valid path[/]")
                    .Validate(Directory.Exists));

            if (promptExperimental && !experimental && GamePath.Experimental != null)
            {
                experimental = AnsiConsole.Confirm("Extract experimental", false);
                if (experimental) gameInstallPath = GamePath.Experimental;
            }
        }

        var stopWatch = Stopwatch.StartNew();

        var pbos = GetPBOs(gameInstallPath).ToList();

        var progress = AnsiConsole.Progress()
            .HideCompleted(true)
            .Columns([
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn()
            ]);

        string? cleanupError = null;

        progress.Start(ctx =>
        {
            var tasks = new List<ProgressTask>();
            var prefixes = new HashSet<string>();

            // setup tasks in reverse order
            for (var i = pbos.Count - 1; i >= 0; i--)
            {
                var pbo = pbos[i];

                if (!string.IsNullOrEmpty(pbo.Prefix))
                    prefixes.Add(pbo.Prefix);

                tasks.Add(ctx.AddTask(pbo.FileName!, false, pbo.Files.Count).IsIndeterminate());
            }

            var cleanTask = ctx.AddTask("Clean up old files", maxValue: prefixes.Count);

            foreach (var path in prefixes.Select(prefix => Path.Combine(destination!, prefix)))
            {
                if (Directory.Exists(path))
                {
                    try
                    {
                        Directory.Delete(path, true);
                    }
                    catch (IOException ex)
                    {
                        cleanupError = $"Could not delete [grey]{path}[/]: {ex.Message}";
                        return;
                    }
                }

                cleanTask.Increment(1);
            }

            var pboTasks = pbos.Select(pbo => (pbo, task: tasks.First(x => x.Description == pbo.FileName)));
            var parallelism = parallel > 1 ? parallel : pbos.Count;

            pboTasks
                .AsParallel()
                .WithDegreeOfParallelism(parallelism)
                .ForAll(pboTask =>
                {
                    var (pbo, task) = pboTask;
                    ExtractFiles(pbo, task, destination!, excludePatterns ?? includePatterns, excludePatterns != null);
                    pbo.Dispose();
                });
        });

        stopWatch.Stop();

        if (cleanupError != null)
            return Error(cleanupError);

        Console.SetCursorPosition(0, 0);

        AnsiConsole.MarkupLine($"Extracted [yellow]{gameInstallPath}[/] to [yellow]{destination}[/]");
        AnsiConsole.MarkupLine($"Took [yellow]{FormatElapsed(stopWatch.Elapsed)}[/] to complete the operation");

        if (unattended) return 0;
        AnsiConsole.Write("\nPress enter to exit...");
        while (Console.ReadKey(true).Key != ConsoleKey.Enter) { }

        return 0;
    }

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

    private static IEnumerable<PBO> GetPBOs(string root)
    {
        if (!Directory.Exists(root))
            yield break;

        var dirs = new Stack<string>();
        dirs.Push(root);

        while (dirs.Count > 0)
        {
            var dir = dirs.Pop();

            foreach (var pboPath in Directory.EnumerateFiles(dir, "*.pbo", SearchOption.TopDirectoryOnly))
            {
                if (File.Exists(pboPath + ".dayz.bisign"))
                    yield return new PBO(pboPath);
            }

            foreach (var subDir in Directory.EnumerateDirectories(dir))
            {
                if (!new DirectoryInfo(subDir).Attributes.HasFlag(FileAttributes.ReparsePoint))
                    dirs.Push(subDir);
            }
        }
    }

    private static void ExtractFiles(PBO pbo, ProgressTask task, string destination, string[]? exts, bool exclude)
    {
        if (task.IsIndeterminate)
            task.IsIndeterminate(false);

        if (!task.IsStarted)
            task.StartTask();

        foreach (var file in CollectionsMarshal.AsSpan(pbo.Files))
        {
            if (!ShouldExclude(file.FileName, exts, exclude))
            {
                var path = Path.Combine(destination, pbo.Prefix ?? string.Empty);
                PBO.ExtractFile(file, path);
            }

            task.Increment(1);
        }
    }

    private static int Error(string message)
    {
        AnsiConsole.MarkupLine($"[red]Error:[/] {message}");
        return 1;
    }

    private static bool ShouldExclude(string fileName, string[]? exts, bool exclude)
    {
        if (exts == null) return false;

        fileName = fileName.Replace("config.bin", "config.cpp");

        return exclude
            ? exts.Contains(fileName, new ExtensionEqualityComparer())
            : !exts.Contains(fileName, new ExtensionEqualityComparer());
    }
}
