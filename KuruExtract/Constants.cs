using Spectre.Console;
using Spectre.Console.Rendering;
using System.Reflection;

namespace KuruExtract;
internal static class Constants
{
    public static IRenderable Header
    {
        get
        {
            return new Panel(new Markup("\nExtracts game content for DayZ\nby Wardog & Ryann\n").Centered())
                .Header($"ExtractDayZ v{Version}", Justify.Center)
                .SafeBorder()
                .Border(BoxBorder.Heavy)
                .Expand();
        }
    }

    public static string Version
    {
        get
        {
            return Assembly.GetExecutingAssembly()?
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion ?? "0.0.0";
        }
    }
}
