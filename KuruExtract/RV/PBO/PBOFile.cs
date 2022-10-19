using System.Security.Cryptography;
using System.Text;
using KuruExtract.RV.IO;

namespace KuruExtract.RV.PBO;

#pragma warning disable CS0618

public sealed class PBOFile {
    public readonly string PBOPrefix;

    private PBOFile(string pboPrefix) => PBOPrefix = pboPrefix;

    public List<PBOEntry> PBOEntries { get; set; } = new();
    
    public static PBOFile ReadPbo(string destination) {
        var stream = File.OpenRead(destination);
        using var reader = new RVBinaryReader(stream);
        
        string? pboPrefix = null;
        
        reader.ReadAsciiZ();
        if(reader.ReadInt32() != BitConverter.ToInt32(Encoding.ASCII.GetBytes("sreV"))) throw new Exception("Woah, version entry should be the first in all PBOs. Report this to developer");
        reader.BaseStream.Position += 16;

        string propertyName;
        do {
            propertyName = reader.ReadAsciiZ();
            if (propertyName == "") break;

            var value = reader.ReadAsciiZ();

            if (propertyName == "prefix") pboPrefix = value;
        } while (propertyName != "");

        var pbo = new PBOFile((pboPrefix ?? Path.GetFileNameWithoutExtension(destination)).Trim());

        do {
            var entry = PBOEntry.GetEntryMeta(reader);
            if(entry != PBOEntry.EmptyEntry) pbo.PBOEntries.Add(entry);
        } while (reader.PeekBytes(21).Sum(b =>  b) != 0);

        reader.BaseStream.Position += 21;
        
        foreach (var pboEntry in pbo.PBOEntries) pboEntry.ReadEntryData(reader);

        return pbo;
    }

    public void WritePbo(string destination) => File.WriteAllBytes(destination, WritePbo().ToArray());
    
    public MemoryStream WritePbo() {
        var stream = new MemoryStream();
        using var writer = new RVBinaryWriter(stream);
        writer.Write((byte)0x00);
        writer.WriteAsciiZ("sreV");
        writer.Write(new byte[15]);
            
        writer.WriteAsciiZ("prefix");
        writer.WriteAsciiZ(PBOPrefix);
            
        writer.Write((byte) 0x00);

        foreach (var entry in PBOEntries) entry.WriteEntryMeta(writer);

        PBOEntry.WriteEmptyEntryMeta(writer);

        foreach (var entry in PBOEntries) entry.WriteEntryData(writer);
        
        byte[] CalculatePBOChecksum() {
            var oldPos = stream.Position;

            stream.Position = 0;

            var hash = new SHA1Managed().ComputeHash(stream);

            stream.Position = oldPos;

            return hash;
        }

        
        var checksum = CalculatePBOChecksum();
        writer.Write((byte) 0x0);
        writer.Write(checksum);
        return stream;
    }
}

#pragma warning restore CS0618
