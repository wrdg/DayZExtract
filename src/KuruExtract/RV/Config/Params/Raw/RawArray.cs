using KuruExtract.RV.IO;

namespace KuruExtract.RV.Config;
internal sealed class RawArray
{
    public List<RawValue> Entries { get; }

    public RawArray(IEnumerable<RawValue> values)
    {
        Entries = values.ToList();
    }

    public RawArray(RVBinaryReader input)
    {
        var nEntries = input.ReadCompactInteger();
        Entries = Enumerable.Range(0, nEntries).Select(_ => new RawValue(input)).ToList();
    }

    public override string ToString()
    {
        var valStr = string.Join(", ", Entries.Select(x => x.ToString()));
        return $"{{{valStr}}}";
    }
}
