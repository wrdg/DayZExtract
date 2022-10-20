using KuruExtract.RV.IO;
using System.Text;

namespace KuruExtract.RV.PBO;

public sealed class PBOFile
{
    public string? FileName { get; private set; }

    public string? Prefix { get; private set; }

    public List<PBOEntry> PBOEntries { get; set; } = new();

    public PBOFile(string pboPath)
    {
        FileName = Path.GetFileName(pboPath);

        var stream = File.OpenRead(pboPath);
        using var reader = new RVBinaryReader(stream);

        reader.ReadAsciiZ();
        if (reader.ReadInt32() != BitConverter.ToInt32(Encoding.ASCII.GetBytes("sreV")))
            throw new Exception("Woah, version entry should be the first in all PBOs. Report this to developer");

        reader.BaseStream.Position += 16;


        string propertyName;
        do
        {
            propertyName = reader.ReadAsciiZ();
            if (propertyName == "") break;

            var value = reader.ReadAsciiZ();

            if (propertyName == "prefix")
                Prefix = value;
        }
        while (propertyName != "");

        do
        {
            var entry = PBOEntry.GetEntryMeta(reader);
            if (entry != PBOEntry.EmptyEntry) PBOEntries.Add(entry);
        }
        while (reader.PeekBytes(21).Sum(b => b) != 0);

        reader.BaseStream.Position += 21;

        foreach (var pboEntry in PBOEntries)
            pboEntry.ReadEntryData(reader);

    }
}
