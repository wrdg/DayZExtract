using System.Diagnostics.CodeAnalysis;

namespace KuruExtract.RV;

internal sealed class ExtensionEqualityComparer : IEqualityComparer<string>
{
    public bool Equals(string? x, string? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (ReferenceEquals(x, null)) return false;
        if (ReferenceEquals(y, null)) return false;

        return x.EndsWith(y, StringComparison.OrdinalIgnoreCase) || y.EndsWith(x, StringComparison.OrdinalIgnoreCase);
    }

    public int GetHashCode([DisallowNull] string obj)
    {
        return obj != null ? obj.GetHashCode() : 0;
    }
}
