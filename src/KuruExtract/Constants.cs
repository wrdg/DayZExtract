using Spectre.Console;
using Spectre.Console.Rendering;
using System.Reflection;

namespace KuruExtract;
internal static class Constants
{
    public static IRenderable Header =>
        new Panel(new Markup("\nExtracts game content for DayZ\nby Wardog\n").Centered())
            .Header($"DayZExtract v{Version}", Justify.Center)
            .SafeBorder()
            .Border(BoxBorder.Heavy)
            .Expand();

    public static string Version
    {
        get
        {
            var version = Assembly.GetExecutingAssembly()?.GetName()?.Version;
            return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "0.0.0";
        }
    }
}
