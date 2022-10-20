using KuruExtract.RV.IO;

namespace KuruExtract.RV.Config;

internal sealed class ParamDeleteClass : ParamEntry
{
    public ParamDeleteClass(RVBinaryReader input)
        : this(input.ReadAsciiZ())
    {
    }

    public ParamDeleteClass(string name)
    {
        Name = name;
    }

    public override string ToString(int indentionLevel = 0)
    {
        return $"{new string(' ', indentionLevel * 4)}delete {Name};";
    }
}
