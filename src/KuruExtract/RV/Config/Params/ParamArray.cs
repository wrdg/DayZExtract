using KuruExtract.RV.IO;

namespace KuruExtract.RV.Config;
internal sealed class ParamArray : ParamEntry
{
    public RawArray Array { get; }

    public ParamArray(RVBinaryReader input)
    {
        Name = input.ReadAsciiz();
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
        return [.. Array.Entries.Select(e => e.Get<T>())];
    }

    public override string ToString(int indentionLevel)
    {
        var ind = new string(' ', indentionLevel * 4);
        var entries = Array.Entries;

        bool allNumeric = entries.Count == 0 || entries.All(e => e.Type is ValueType.Int or ValueType.Float or ValueType.Int64);

        if (allNumeric)
            return $"{ind}{Name}[] = {Array};";

        var innerInd = new string(' ', (indentionLevel + 1) * 4);
        var items = string.Join(",\n", entries.Select(e => $"{innerInd}{e}"));
        return $"{ind}{Name}[] =\n{ind}{{\n{items}\n{ind}}};";
    }
}
