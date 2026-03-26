using KuruExtract.RV.IO;
using System.Runtime.InteropServices;
using System.Text;

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
        var ind = Indent(indentionLevel);
        var entries = CollectionsMarshal.AsSpan(Array.Entries);

        bool allNumeric = true;
        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i].Type is not (ValueType.Int or ValueType.Float or ValueType.Int64))
            {
                allNumeric = false;
                break;
            }
        }

        if (allNumeric)
            return $"{ind}{Name}[] = {Array};";

        var innerInd = Indent(indentionLevel + 1);
        var sb = new StringBuilder(ind).Append(Name).AppendLine("[] =");
        sb.Append(ind).AppendLine("{");
        for (int i = 0; i < entries.Length; i++)
        {
            sb.Append(innerInd).Append(entries[i]);
            if (i < entries.Length - 1) sb.Append(',');
            sb.AppendLine();
        }
        sb.Append(ind).Append("};");
        return sb.ToString();
    }
}
