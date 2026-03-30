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
            args = OperatingSystem.IsWindows() ? [@"P:\"] : ["--help"];
#endif
        }

        if (args is not ["--version"])
        {
            if (OperatingSystem.IsWindows())
                Console.Title = "DayZExtract";

            AnsiConsole.Write(Constants.Header);
            Console.WriteLine();
        }

        ConsoleApp.Run(args, ExtractDayZCommand.Execute);
        return Environment.ExitCode;
    }
}
