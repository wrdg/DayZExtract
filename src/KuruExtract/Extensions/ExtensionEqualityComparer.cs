using System.Diagnostics.CodeAnalysis;

namespace KuruExtract.Extensions;

internal sealed class ExtensionEqualityComparer : IEqualityComparer<string>
{
    public bool Equals(string? x, string? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null) return false;
        if (y is null) return false;

        return x.EndsWith(y, StringComparison.OrdinalIgnoreCase) || y.EndsWith(x, StringComparison.OrdinalIgnoreCase);
    }

    public int GetHashCode([DisallowNull] string obj)
    {
        return obj.GetHashCode();
    }
}
