using KuruExtract.Commands;
using KuruExtract.Update;
using Spectre.Console;
using Spectre.Console.Cli;

namespace KuruExtract;

public class Program
{
    internal static UpdateChecker UpdateChecker { get; private set; } = null!;

    public static int Main(string[] args)
    {
        Console.Title = "DayZExtract";

        Thread.Sleep(50); // header will get jank on windows terminal
        AnsiConsole.Write(Constants.Header);

        UpdateChecker = new UpdateChecker("wrdg", "DayZExtract");

        var app = new CommandApp<ExtractDayZCommand>();
        app.Configure(config =>
        {
#if DEBUG
            config.ValidateExamples();
#endif
            config.AddExample(new[] { @"P:\" });
            config.AddExample(new[] { @"P:\", "--experimental", "-u", "-i", "*.c" });
            config.AddExample(new[] { @"P:\", "-u", "-e", "*.p3d,*.ogg,*.aw" });

            config.SetApplicationName("DayZExtract.exe");

            config.SetExceptionHandler(ex =>
            {
                if (ex is CommandRuntimeException)
                    AnsiConsole.MarkupLine($"[red]{ex.Message}[/]");
                else
                    AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
            });

        });

        var result = app.Run(args.Length < 1 ? new[] { "-h" } : args);

        if (args.Length < 1)
        {
            AnsiConsole.Write("\nPress enter to exit...");
            while (Console.ReadKey(true).Key != ConsoleKey.Enter) { }
        }

        return result;
    }
}