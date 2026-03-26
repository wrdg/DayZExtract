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
        return [.. Array.Entries.Select(e => e.Get<T>())];
    }

    public override string ToString(int indentionLevel)
    {
        var ind = new string(' ', indentionLevel * 4);
        var entries = Array.Entries;

        bool allNumeric = entries.Count == 0 || entries.All(e => e.Type is ValueType.Int or ValueType.Float or ValueType.Int64);

        if (Flag == 1)
        {
            if (allNumeric)
                return $"{ind}{Name}[] += {Array};";

            var innerInd = new string(' ', (indentionLevel + 1) * 4);
            var items = string.Join(",\n", entries.Select(e => $"{innerInd}{e}"));
            return $"{ind}{Name}[] +=\n{ind}{{\n{items}\n{ind}}};";
        }

        return $"{ind}{Name}[] = {Array}; // Unknown flag {Flag}";
    }
}
