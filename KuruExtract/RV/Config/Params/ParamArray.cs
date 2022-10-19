using KuruExtract.RV.IO;

namespace KuruExtract.RV.Config;

internal sealed class ParamArray : ParamEntry
{
    public RawArray Array { get; private set; }

    public ParamArray(RVBinaryReader input)
    {
        Name = input.ReadAsciiZ();
        Array = new RawArray(input);
    }

    public ParamArray(string name, IEnumerable<RawValue> values)
    {
        Name = name;
        Array = new RawArray(values);
    }

    public ParamArray(string name, params RawValue[] values) : this(name, (IEnumerable<RawValue>)values) { }

    public T[] ToArray<T>()
    {
        return Array.Entries.Select(e => e.Get<T>()).ToArray();
    }

    public override string ToString(int indentionLevel = 0)
    {
        return $"{new string(' ', indentionLevel * 4)}{Name}[]={Array.ToString()};";
    }
}
