using Spectre.Console;
using Spectre.Console.Rendering;

namespace KuruExtract;
internal static class Constants
{
    public const string Version = "1.1.0";

    public static IRenderable Header =>
        new Panel(new Markup("\nExtracts game content for DayZ\nby Wardog\n").Centered())
            .Header($"DayZExtract v{Version}", Justify.Center)
            .SafeBorder()
            .Border(BoxBorder.Heavy)
            .Expand();
}
