using System.Reflection;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace KuruExtract;
internal static class Constants
{
    public const string UpdateUrl = "https://github.com/wrdg/DayZExtract";
    public const string LegacyProductCode = "{B09BF157-5C17-4087-A2B1-07421300B8C8}";

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
        new Panel(new Markup("\nGame content extraction tool for DayZ\nDeveloped by Wardog\n\nhttps://github.com/wrdg/DayZExtract\n").Centered())
            .Header($"DayZExtract v{Version}", Justify.Center)
            .SafeBorder()
            .Border(BoxBorder.Heavy)
            .Expand();
}
