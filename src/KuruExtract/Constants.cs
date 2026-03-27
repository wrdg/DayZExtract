using System.Reflection;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace KuruExtract;
internal static class Constants
{
    public static readonly string Version = GetVersion();

    private static string GetVersion()
    {
        var asm = typeof(Constants).Assembly;
        var v = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
             ?? asm.GetCustomAttribute<AssemblyVersionAttribute>()?.Version
             ?? "1.0.0";
        var plus = v.IndexOf('+');
        return plus != -1 ? v[..plus] : v;
    }

    public static IRenderable Header =>
        new Panel(new Markup("\nExtracts game content for DayZ\nby Wardog\n").Centered())
            .Header($"DayZExtract v{Version}", Justify.Center)
            .SafeBorder()
            .Border(BoxBorder.Heavy)
            .Expand();
}
