namespace KuruExtract.Extensions;
public static class PathExtensions
{
    public static string? ResolvePath(this string? path)
    {
        if (path is null || !File.Exists(path) && !Directory.Exists(path))
        {
            return null;
        }

        DirectoryInfo info = new(path);
        if (info.Parent is null)
        {
            return info.Name.ToUpperInvariant();
        }

        var parent = ResolvePath(info.Parent.FullName);
        var name = info.Parent.GetFileSystemInfos(info.Name)[0].Name;
        return parent is null ? name : Path.Combine(parent, name);
    }
}
