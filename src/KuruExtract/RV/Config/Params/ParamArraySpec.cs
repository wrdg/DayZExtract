using KuruExtract.RV.IO;
using System.Runtime.InteropServices;
using System.Text;

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

        if (Flag == 1)
        {
            if (allNumeric)
                return $"{ind}{Name}[] += {Array};";

            var innerInd = Indent(indentionLevel + 1);
            var sb = new StringBuilder(ind).Append(Name).AppendLine("[] +=");
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

        return $"{ind}{Name}[] = {Array}; // Unknown flag {Flag}";
    }
}
