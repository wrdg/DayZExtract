using ConsoleAppFramework;
using KuruExtract.Commands;
using Spectre.Console;
using Velopack;

namespace KuruExtract;
public class Program
{
    internal static bool Unattended;

    public static int Main(string[] args)
    {
        VelopackApp.Build().Run();

        AppDomain.CurrentDomain.ProcessExit += (_, _) => Console.CursorVisible = true;

        var originalArgs = args;
        if (args.Length < 1)
        {
#if DEBUG
            args = [@"P:\"];
#else
            args = OperatingSystem.IsWindows() 
                ? (Directory.Exists(@"P:\") ? [@"P:\"] : []) // check if P:\ exist first
                : ["--help"]; // other OS
#endif
        }

        if (args is not ["--version"])
        {
            if (OperatingSystem.IsWindows())
                Console.Title = "DayZExtract";

            AnsiConsole.Write(Constants.Header);
            Console.WriteLine();
        }

        try
        {
            ConsoleApp.Run(args, ExtractDayZCommand.Execute);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]UNHANDLED EXCEPTION:[/] {Markup.Escape(ex.Message)}");
            AnsiConsole.MarkupLine($"[grey]{Markup.Escape(ex.StackTrace ?? string.Empty)}[/]");
            Environment.ExitCode = 1;
        }
        finally
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();

            PauseIfAttended();
        }

        return Environment.ExitCode;
    }

    private static void PauseIfAttended()
    {
        if (OperatingSystem.IsWindows() && !Unattended)
        {
            AnsiConsole.Write("\nPress enter to exit...");
            while (Console.ReadKey(true).Key != ConsoleKey.Enter) ;
        }
    }
}
