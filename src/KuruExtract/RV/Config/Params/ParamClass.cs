using KuruExtract.RV.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace KuruExtract.RV.Config;
internal sealed class ParamClass : ParamEntry
{
    public string BaseClassName { get; private set; } = string.Empty;

    public List<ParamEntry> Entries { get; private set; } = new List<ParamEntry>(20);

    private ParamClass() { }

    // reads a nested class entry, name and a pointer to the body are both in the stream
    internal static ParamClass Read(RVBinaryReader input)
    {
        var result = new ParamClass { Name = input.ReadAsciiz() };
        var offset = input.ReadUInt32();
        var oldPos = input.Position;
        input.Position = offset;
        result.ReadCore(input);
        input.Position = oldPos;
        return result;
    }

    // reads the file root class, body starts at the current stream position, name is supplied externally
    internal static ParamClass ReadRoot(RVBinaryReader input, string name)
    {
        var result = new ParamClass { Name = name };
        result.ReadCore(input);
        return result;
    }

    public ParamClass? GetClass(string name)
    {
        return Entries.OfType<ParamClass>().FirstOrDefault(c => c.Name == name);
    }
    public T[]? GetArray<T>(string name)
    {
        return Entries.OfType<ParamArray>().FirstOrDefault(c => c.Name == name)?.ToArray<T>();
    }

    private void ReadCore(RVBinaryReader input)
    {
        BaseClassName = input.ReadAsciiz();

        var nEntries = input.ReadCompactInteger();
        var entries = new List<ParamEntry>(nEntries);
        for (var i = 0; i < nEntries; i++)
            entries.Add(ReadParamEntry(input));
        Entries = entries;
    }

    public string ToString(int indentionLevel, bool onlyClassBody)
    {
        var ind = Indent(indentionLevel);
        var classBody = new StringBuilder();

        var indLvl = onlyClassBody ? indentionLevel : indentionLevel + 1;
        var entriesSpan = CollectionsMarshal.AsSpan(Entries);
        for (int i = 0; i < entriesSpan.Length; i++)
            classBody.AppendLine(entriesSpan[i].ToString(indLvl));

        var classHead = BaseClassName.Length == 0 ?
            $"{ind}class {Name}" :
            $"{ind}class {Name} : {BaseClassName}";

        return onlyClassBody
            ? classBody.ToString()
            : $$"""
                {{classHead}}
                {{ind}}{
                {{classBody}}{{ind}}};
                """;
    }

    public override string ToString(int indentionLevel) => ToString(indentionLevel, false);
}
