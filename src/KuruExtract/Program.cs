using ConsoleAppFramework;
using KuruExtract.Commands;
using Spectre.Console;
using Velopack;

namespace KuruExtract;
public class Program
{
    public static int Main(string[] args)
    {
        VelopackApp.Build().Run();

        AppDomain.CurrentDomain.ProcessExit += (_, _) => Console.CursorVisible = true;

        var originalArgs = args;
        if (args.Length < 1)
        {
#if DEBUG
            args = [@"P:\", "-u"];
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
            ExtractDayZCommand.PauseIfAttended();
            return 1;
        }

        return Environment.ExitCode;
    }
}
