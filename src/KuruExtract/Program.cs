using ConsoleAppFramework;
using KuruExtract.Commands;
using KuruExtract.Update;
using Spectre.Console;

namespace KuruExtract;
public class Program
{
    internal static UpdateChecker UpdateChecker { get; private set; } = null!;

    public static int Main(string[] args)
    {
        AppDomain.CurrentDomain.ProcessExit += (_, _) => Console.CursorVisible = true;

        UpdateChecker = new UpdateChecker("wrdg", "DayZExtract");

        var originalArgs = args;
#if DEBUG
        if (args.Length < 1) args = [@"P:\", "-u"];
#else
        if (args.Length < 1) args = ["--help"];
#endif

        if (args is not ["--version"])
        {
            if (OperatingSystem.IsWindows())
                Console.Title = "DayZExtract";

            AnsiConsole.Write(Constants.Header);
            Console.WriteLine();
        }

        ConsoleApp.Run(args, ExtractDayZCommand.Execute);
        var exitCode = Environment.ExitCode;

        if (originalArgs.Length >= 1) return exitCode;
        AnsiConsole.Write("\nPress enter to exit...");
        while (Console.ReadKey(true).Key != ConsoleKey.Enter) { }

        return exitCode;
    }
}
