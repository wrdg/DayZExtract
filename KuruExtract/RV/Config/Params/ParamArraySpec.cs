using KuruExtract.RV.IO;

namespace KuruExtract.RV.Config;
internal sealed class ParamArraySpec : ParamEntry
{
    public int Flag { get; }

    public RawArray Array { get; }

    public ParamArraySpec(RVBinaryReader input)
    {
        Flag = input.ReadInt32();
        Name = input.ReadAsciiz();
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

    public override string ToString(int indentionLevel)
    {
        return Flag == 1
            ? $"{new string(' ', indentionLevel * 4)}{Name}[]+={Array};"
            : $"{new string(' ', indentionLevel * 4)}{Name}[]={Array}; // Unknown flag {Flag}";
    }
}
