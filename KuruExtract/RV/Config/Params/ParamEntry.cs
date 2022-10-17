using KuruExtract.RV.IO;

namespace KuruExtract.RV.Config;

internal abstract class ParamEntry
{
    public string Name { get; protected set; } = string.Empty;

    public static ParamEntry ReadParamEntry(RVBinaryReader input)
    {
        var entryType = (EntryType)input.ReadByte();

        return entryType switch
        {
            EntryType.Class => new ParamClass(input),
            EntryType.Array => new ParamArray(input),
            EntryType.Value => new ParamValue(input),
            EntryType.ClassDecl => new ParamExternClass(input),
            EntryType.ClassDelete => new ParamDeleteClass(input),
            EntryType.ArraySpec => new ParamArraySpec(input),
            _ => throw new ArgumentException("Unknown ParamEntry Type", nameof(entryType)),
        };
    }

    public virtual string? ToString(int indentionLevel = 0) => base.ToString();

    public override string? ToString() => ToString(0);
}
