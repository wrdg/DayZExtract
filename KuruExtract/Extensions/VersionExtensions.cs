namespace KuruExtract.Extensions;

internal static class VersionExtensions
{
    public static Version Normalize(this Version version)
    {
        return new Version(
            Math.Max(version.Major, 0),
            Math.Max(version.Minor, 0),
            Math.Max(version.Build, 0),
            Math.Max(version.Revision, 0)
        );
    }
}
