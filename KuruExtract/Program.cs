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
        AppDomain.CurrentDomain.ProcessExit += ProcessExitHandler;

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
            config.AddExample(@"P:\");
            config.AddExample(@"P:\", "--experimental", "-u", "-i", "*.c");
            config.AddExample(@"P:\", "-u", "-e", "*.p3d,*.ogg,*.aw");

            config.SetApplicationName("DayZExtract.exe");

            config.PropagateExceptions();
        });

        try
        {
#if DEBUG
            var result = app.Run(args.Length < 1 ? ["P:\\", "-u"] : args);
#else
            var result = app.Run(args.Length < 1 ? ["-h"] : args);
#endif

            if (args.Length >= 1) return result;
            AnsiConsole.Write("\nPress enter to exit...");
            while (Console.ReadKey(true).Key != ConsoleKey.Enter)
            {
            }

            return result;
        }
        catch (Exception ex)
        {
            if (ex is CommandRuntimeException)
                AnsiConsole.MarkupLine($"[red]{ex.Message}[/]");
            else
                AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);

            Thread.Sleep(5000);
        }

        return 0;
    }

    private static void ProcessExitHandler(object? sender, EventArgs e)
    {
        Console.CursorVisible = true;
    }
}