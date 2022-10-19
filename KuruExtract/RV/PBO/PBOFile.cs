using System.Security.Cryptography;
using System.Text;
using KuruExtract.RV.IO;

namespace KuruExtract.RV.PBO;

#pragma warning disable CS0618

public sealed class PBOFile {
    public readonly string PBOPrefix;
    public readonly string PBOProduct;
    public readonly string PBOVersion;


    private PBOFile(string pboPrefix, string product, string version) {
        PBOPrefix = pboPrefix;
        PBOProduct = product;
        PBOVersion = version;
    }

    public List<PBOEntry> PBOEntries { get; set; } = new();
    
    public static PBOFile ReadPbo(string destination) {
        var stream = File.OpenRead(destination);
        using var reader = new RVBinaryReader(stream);
        
        string? pboPrefix = null;
        string pboVersion = "0", pboProduct = "dayz";

        reader.ReadAsciiZ();
        if(reader.ReadInt32() != BitConverter.ToInt32(Encoding.ASCII.GetBytes("sreV"))) throw new Exception("Woah, version entry should be the first in all PBOs. Report this to developer");
        reader.BaseStream.Position += 16;

        string propertyName;
        do {
            propertyName = reader.ReadAsciiZ();
            if (propertyName == "") break;

            var value = reader.ReadAsciiZ();

            if (propertyName == "prefix") pboPrefix = value;
            if (propertyName == "product") pboProduct = value;
            if (propertyName == "version") pboVersion = value;

        } while (propertyName != "");

        var pbo = new PBOFile((pboPrefix ?? Path.GetFileNameWithoutExtension(destination)).Trim(), pboProduct, pboVersion);

        do {
            var entry = PBOEntry.GetEntryMeta(reader);
            if(entry != PBOEntry.EmptyEntry) pbo.PBOEntries.Add(entry);
        } while (reader.PeekBytes(21).Sum(b =>  b) != 0);

        reader.BaseStream.Position += 21;
        
        foreach (var pboEntry in pbo.PBOEntries) pboEntry.ReadEntryData(reader);

        return pbo;
    }
}

#pragma warning restore CS0618
