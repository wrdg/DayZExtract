using KuruExtract.RV.IO;

namespace KuruExtract.RV.Config;

internal sealed class ParamArraySpec : ParamEntry
{
    public int Flag { get; }

    public RawArray Array { get; private set; }

    public ParamArraySpec(RVBinaryReader input)
    {
        Flag = input.ReadInt32();
        Name = input.ReadAsciiZ();
        Array = new RawArray(input);
    }

    public ParamArraySpec(string name, int flag, IEnumerable<RawValue> values)
    {
        Name = name;
        Flag = flag;
        Array = new RawArray(values);
    }

    public ParamArraySpec(string name, int flag, params RawValue[] values) : this(name, flag, (IEnumerable<RawValue>)values) { }

    public T[] ToArray<T>()
    {
        return Array.Entries.Select(e => e.Get<T>()).ToArray();
    }

    public override string ToString(int indentionLevel = 0)
    {
        if (Flag == 1)
        {
            return $"{new string(' ', indentionLevel * 4)}{Name}[]+={Array.ToString()};";
        }
        return $"{new string(' ', indentionLevel * 4)}{Name}[]={Array.ToString()}; // Unknown flag {Flag}";
    }
}
