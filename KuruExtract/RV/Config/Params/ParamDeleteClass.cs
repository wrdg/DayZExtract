using KuruExtract.RV.IO;

namespace KuruExtract.RV.Config;
internal sealed class ParamDeleteClass : ParamEntry
{
    public ParamDeleteClass(RVBinaryReader input)
        : this(input.ReadAsciiz())
    {
    }

    public ParamDeleteClass(string name)
    {
        Name = name;
    }

    public override string ToString(int indentionLevel)
    {
        return $"{new string(' ', indentionLevel * 4)}delete {Name};";
    }
}
