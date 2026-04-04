using KuruExtract.RV.IO;

namespace KuruExtract.RV.Config;
internal abstract class ParamEntry
{
    public string Name { get; init; } = string.Empty;

    private static readonly string[] IndentStrings = Enumerable.Range(0, 16)
        .Select(i => new string(' ', i * 4))
        .ToArray();

    protected static string Indent(int level) =>
        (uint)level < (uint)IndentStrings.Length ? IndentStrings[level] : new string(' ', level * 4);

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

    public virtual string? ToString(int indentionLevel) => base.ToString();

    public override string? ToString() => ToString(0);
}
