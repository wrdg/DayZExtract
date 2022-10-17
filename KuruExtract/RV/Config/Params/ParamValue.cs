using KuruExtract.RV.IO;

namespace KuruExtract.RV.Config;

internal sealed class ParamValue : ParamEntry
{
    public RawValue Value { get; private set; }

    public ParamValue(string name, bool value)
    {
        Name = name;
        Value = new RawValue(value ? 1 : 0);
    }
    public ParamValue(string name, int value)
    {
        Name = name;
        Value = new RawValue(value);
    }
    public ParamValue(string name, float value)
    {
        Name = name;
        Value = new RawValue(value);
    }
    public ParamValue(string name, string value)
    {
        Name = name;
        Value = new RawValue(value);
    }

    public ParamValue(RVBinaryReader input)
    {
        var subtype = (ValueType)input.ReadByte();
        Name = input.ReadAsciiz();
        Value = new RawValue(input, subtype);
    }

    public override string ToString(int indentionLevel = 0)
    {
        return $"{new string(' ', indentionLevel * 4)}{Name}={Value.ToString()};";
    }
}
