using KuruExtract.RV.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace KuruExtract.RV.Config;
internal sealed class RawArray
{
    public List<RawValue> Entries { get; }

    public RawArray(IEnumerable<RawValue> values)
    {
        Entries = [.. values];
    }

    public RawArray(RVBinaryReader input)
    {
        var nEntries = input.ReadCompactInteger();
        Entries = [.. Enumerable.Range(0, nEntries).Select(_ => new RawValue(input))];
    }

    public override string ToString()
    {
        var entries = CollectionsMarshal.AsSpan(Entries);
        if (entries.IsEmpty)
            return "{}";

        var sb = new StringBuilder("{");
        sb.Append(entries[0]);
        for (int i = 1; i < entries.Length; i++)
            sb.Append(", ").Append(entries[i]);
        sb.Append('}');
        return sb.ToString();
    }
}
