using Humanizer;
using KuruExtract.RV;
using KuruExtract.RV.PBO;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;

namespace KuruExtract.Commands;

internal sealed class ExtractDayZCommand : Command<ExtractDayZCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "[Destination]")]
        [Description("Path to extract files (defaults to home directory if not specified)")]
        public string? Destination { get; set; }

        [CommandOption("-u|--unattended")]
        [Description("No prompts or pausing")]
        public bool Unattended { get; init; }

        [CommandOption("-x|--experimental")]
        [Description("Extract experimental version of game")]
        public bool Experimental { get; set; }

        [CommandOption("-g|--game-install-path")]
        [Description("Specify installation path of game")]
        public string? InstallationPath { get; set; }

        [CommandOption("-i|--include-extensions")]
        [Description("List of extensions to be included in extraction")]
        public string[]? IncludePatterns { get; set; }

        [CommandOption("-e|--exclude-extensions")]
        [Description("List of extensions to be exluded from extraction")]
        public string[]? ExcludePatterns { get; set; }
    }

    private static bool _promptExperimental;

    public override ValidationResult Validate([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.Destination))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            settings.Destination = Path.Combine(home, "dayz-extract");

            Directory.CreateDirectory(settings.Destination);
        }

        if (settings.IncludePatterns is { Length: 1 })
        {
            settings.IncludePatterns = settings.IncludePatterns[0]
                .Replace("*", string.Empty)
                .Split(',', ';', '|');
        }

        if (settings.ExcludePatterns is { Length: 1 })
        {
            settings.ExcludePatterns = settings.ExcludePatterns[0]
                .Replace("*", string.Empty)
                .Split(',', ';', '|');
        }

        if (settings.IncludePatterns != null && settings.ExcludePatterns != null)
            return ValidationResult.Error("You may only specify include or exclude, not both.");

        if (string.IsNullOrEmpty(settings.InstallationPath))
        {
            _promptExperimental = true;
            settings.InstallationPath = settings.Experimental ? GamePath.Experimental : GamePath.Stable;
        }

        if (settings.InstallationPath == null)
            return ValidationResult.Error("Unable to locate game installation path.");

        return ValidationResult.Success();
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        if (!settings.Unattended)
        {
            if (Program.UpdateChecker.CheckUpdate())
            {
                AnsiConsole.MarkupLine("An update is available!");
                var update = AnsiConsole.Confirm("Would you like to update?", true);

                if (update)
                {
                    Process.Start(new ProcessStartInfo(Program.UpdateChecker.DownloadUrl)
                    {
                        UseShellExecute = true
                    });

                    return 1;
                }
            }

            PromptAttendant(settings);
        }

        var stopWatch = new Stopwatch();
        stopWatch.Start();

        var pbos = GetPBOs(Path.Combine(settings.InstallationPath!, "dta"), settings).ToList();
        pbos.AddRange(GetPBOs(Path.Combine(settings.InstallationPath!, "addons"), settings));
        pbos.AddRange(GetPBOs(Path.Combine(settings.InstallationPath!, @"bliss\addons"), settings));

        var progress = AnsiConsole.Progress()
            .HideCompleted(true)
            .Columns(new ProgressColumn[]
            {
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn(),
            });

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

                var task = ctx.AddTask(pbo.FileName!, false, pbo.Files.Count)
                    .IsIndeterminate();

                tasks.Add(task);
            }

            var cleanTask = ctx.AddTask("Clean up old files", maxValue: prefixes.Count);

            while (!ctx.IsFinished)
            {
                foreach (var prefix in prefixes)
                {
                    var path = Path.Combine(settings.Destination!, prefix);

                    if (Directory.Exists(path))
                        Directory.Delete(path, true);

                    cleanTask.Increment(1);
                }

                foreach (var pbo in CollectionsMarshal.AsSpan(pbos))
                {
                    var task = tasks.First(x => x.Description == pbo.FileName);

                    if (task.IsIndeterminate)
                        task.IsIndeterminate(false);

                    if (!task.IsStarted)
                        task.StartTask();

                    ExtractFiles(pbo, task, settings);
                    pbo.Dispose();
                }
            }
        });

        stopWatch.Stop();

        Console.SetCursorPosition(0, 0);

        var timePrecision = stopWatch.Elapsed.Minutes > 1 ? 2 : 1;

        AnsiConsole.MarkupLine($"Extracted [yellow]{settings.InstallationPath}[/] to [yellow]{settings.Destination}[/]");
        AnsiConsole.MarkupLine($"Took [yellow]{stopWatch.Elapsed.Humanize(timePrecision, CultureInfo.InvariantCulture)}[/] to complete the operation");

        if (!settings.Unattended)
        {
            AnsiConsole.Write("\nPress enter to exit...");
            while (Console.ReadKey(true).Key != ConsoleKey.Enter) { }
        }

        return 0;
    }

    private static void PromptAttendant(Settings settings)
    {
        settings.Destination = AnsiConsole.Prompt(
            new TextPrompt<string>("Desination path")
                .DefaultValue(settings.Destination ?? @"P:\")
                .ValidationErrorMessage("[red]Not a valid path[/]")
                .Validate(path => Directory.Exists(path)));

        if (_promptExperimental && !settings.Experimental && GamePath.Experimental != null)
        {
            settings.Experimental = AnsiConsole.Confirm("Extract experimental", settings.Experimental);
            if (settings.Experimental) settings.InstallationPath = GamePath.Experimental;
        }
    }

    private static IEnumerable<PBO> GetPBOs(string path, Settings settings)
    {
        if (!Directory.Exists(path))
            yield break;

        var pbos = Directory.GetFiles(path, "*.pbo", SearchOption.TopDirectoryOnly);

        for (int i = 0; i < pbos.Length; i++)
            yield return new PBO(pbos[i]);
    }

    private static void ExtractFiles(PBO pbo, ProgressTask task, Settings settings)
    {
        bool exclude = settings.ExcludePatterns != null;
        string[]? exts = settings.ExcludePatterns ?? settings.IncludePatterns;

        foreach (var file in CollectionsMarshal.AsSpan(pbo.Files))
        {
            if (!ShouldExclude(file.FileName, exts, exclude))
            {
                var path = Path.Combine(settings.Destination!, pbo.Prefix ?? string.Empty);
                file.Extract(path);
            }

            task.Increment(1);
        }
    }

    private static bool ShouldExclude(string fileName, string[]? exts, bool exclude)
    {
        if (exts == null)
            return false;

        fileName = fileName.Replace("config.bin", "config.cpp");

        if (exclude && exts.Contains(fileName, new ExtensionEqualityComparer()))
            return true;

        if (!exclude && !exts.Contains(fileName, new ExtensionEqualityComparer()))
            return true;

        return false;
    }
}
