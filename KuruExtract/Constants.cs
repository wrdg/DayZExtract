using Spectre.Console;
using Spectre.Console.Rendering;
using System.Diagnostics;
using System.Reflection;

namespace KuruExtract;
internal static class Constants
{
    public static IRenderable Header =>
        new Panel(new Markup("\nExtracts game content for DayZ\nby Wardog\n").Centered())
            .Header($"ExtractDayZ v{Version}", Justify.Center)
            .SafeBorder()
            .Border(BoxBorder.Heavy)
            .Expand();

    public static string Version =>
        FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion ?? "0.0.0";
}
