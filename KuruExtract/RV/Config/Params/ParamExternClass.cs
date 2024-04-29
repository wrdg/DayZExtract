using KuruExtract.RV.IO;

namespace KuruExtract.RV.Config;
internal sealed class ParamExternClass : ParamEntry
{
    public ParamExternClass(RVBinaryReader input)
        : this(input.ReadAsciiz())
    {
    }

    public ParamExternClass(string name)
    {
        Name = name;
    }

    public override string ToString(int indentionLevel)
    {
        return $"{new string(' ', indentionLevel * 4)}class {Name};";
    }
}
