using KuruExtract.RV.IO;
using System.Text;

namespace KuruExtract.RV.Config;

internal sealed class ParamClass : ParamEntry
{
    public string BaseClassName { get; private set; } = string.Empty;

    public List<ParamEntry> Entries { get; private set; } = new List<ParamEntry>(20);

    public ParamClass(string name, string baseclass, IEnumerable<ParamEntry> entries)
    {
        BaseClassName = baseclass;
        Name = name;
        Entries = entries.ToList();
    }

    public ParamClass(string name, IEnumerable<ParamEntry> entries) : this(name, "", entries) { }

    public ParamClass(string name, params ParamEntry[] entries) : this(name, (IEnumerable<ParamEntry>)entries) { }

    public ParamClass(RVBinaryReader input)
    {
        Name = input.ReadAsciiz();
        var offset = input.ReadUInt32();
        var oldPos = input.Position;
        input.Position = offset;
        ReadCore(input);
        input.Position = oldPos;
    }

    public ParamClass(RVBinaryReader input, string fileName)
    {
        Name = fileName;
        ReadCore(input);
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
        Entries = Enumerable.Range(0, nEntries).Select(_ => ReadParamEntry(input)).ToList();
    }

    public string ToString(int indentionLevel, bool onlyClassBody)
    {
        var ind = new string(' ', indentionLevel * 4);
        var classBody = new StringBuilder();

        var indLvl = (onlyClassBody) ? indentionLevel : indentionLevel + 1;
        foreach (var entry in Entries)
            classBody.AppendLine(entry.ToString(indLvl));

        var classHead = string.IsNullOrEmpty(BaseClassName) ?
            $"{ind}class {Name}" :
            $"{ind}class {Name} : {BaseClassName}";

        if (onlyClassBody)
            return classBody.ToString();

        return
$@"{classHead}
{ind}{{
{classBody}{ind}}};";
    }

    public override string ToString(int indentionLevel = 0) => ToString(indentionLevel, false);
}
