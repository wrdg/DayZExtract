using ConsoleAppFramework;
using KuruExtract.Commands;
using Spectre.Console;
using System.Text;
using Velopack;
using Velopack.Locators;

namespace KuruExtract;
public class Program
{
    internal static bool Unattended;

    public static int Main(string[] args)
    {
        var app = VelopackApp.Build();

        if (OperatingSystem.IsWindows())
        {
            app.OnAfterInstallFastCallback(_ => UpdatePathEnvironment(true));
            app.OnBeforeUninstallFastCallback(_ => UpdatePathEnvironment(false));
        }
            
        app.Run();

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

    private static void UpdatePathEnvironment(bool add)
    {
        var rootAppDir = VelopackLocator.CreateDefaultForPlatform().AppContentDir;

        if (rootAppDir is null)
            return;

        var target = EnvironmentVariableTarget.User;

        var oldPath = Environment.GetEnvironmentVariable("PATH", target) ?? string.Empty;

        var builder = new StringBuilder(oldPath.Length);

        foreach (var segment in oldPath.AsSpan().Split(';'))
        {
            ReadOnlySpan<char> entry = oldPath.AsSpan()[segment];

            if (entry.IsEmpty || entry.Equals(rootAppDir.AsSpan(), StringComparison.OrdinalIgnoreCase))
                continue;

            if (builder.Length > 0) builder.Append(';');
            builder.Append(entry);
        }

        if (add)
        {
            if (builder.Length > 0) builder.Append(';');
            builder.Append(rootAppDir);
        }

        Environment.SetEnvironmentVariable("PATH", builder.ToString(), target);
    }
}
