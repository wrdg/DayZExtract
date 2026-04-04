using KuruExtract.RV.IO;

namespace KuruExtract.RV.Config;
internal sealed class ParamFile
{
    public ParamClass? Root { get; private set; }

    public List<KeyValuePair<string, int>>? EnumValues { get; private set; }

    public ParamFile()
    {
        EnumValues = new List<KeyValuePair<string, int>>(10);
    }

    public ParamFile(Stream stream)
    {
        Read(new RVBinaryReader(stream));
    }

    public void Read(RVBinaryReader input)
    {
        ReadOnlySpan<byte> sig = [0x00, 0x72, 0x61, 0x50];
        if (!input.ReadBytes(4).AsSpan().SequenceEqual(sig))
        {
            input.BaseStream.Seek(-4, SeekOrigin.Current);
            throw new FormatException();
        }

        var ofpVersion = input.ReadInt32();
        var version = input.ReadInt32();
        var offsetToEnums = input.ReadInt32();

        Root = ParamClass.ReadRoot(input, "rootClass");

        input.Position = offsetToEnums;
        var nEnumValues = input.ReadInt32();
        var enumValues = new List<KeyValuePair<string, int>>(nEnumValues);
        for (var i = 0; i < nEnumValues; i++)
            enumValues.Add(new(input.ReadAsciiz(), input.ReadInt32()));
        EnumValues = enumValues;
    }

    public override string? ToString()
    {
        return Root?.ToString(0, true);
    }
}
