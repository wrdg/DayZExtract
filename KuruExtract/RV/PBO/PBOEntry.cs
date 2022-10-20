using KuruExtract.RV.Config;
using KuruExtract.RV.IO;
using System.IO.Compression;

namespace KuruExtract.RV.PBO;

public enum PackingType
{
    Uncompressed = 0x00000000,
    Compressed = 0x43707273
}

public sealed class PBOEntry
{
    public PackingType EntryMimeType { get; private set; }

    public string EntryName { get; private set; }

    public byte[] EntryData { get; private set; }

    public int OriginalDataSize { get; private set; }

    public int PackedDataSize { get; private set; }

    public static readonly PBOEntry EmptyEntry = new()
    {
        EntryData = Array.Empty<byte>(),
        EntryName = "",
        EntryMimeType = PackingType.Uncompressed,
        PackedDataSize = 0,
        OriginalDataSize = 0
    };

    private PBOEntry()
    {
    }

    public PBOEntry(PackingType entryMimeType, string entryName, int storedDataSize, int originalDataSize)
    {
        EntryMimeType = entryMimeType;
        EntryName = entryName;
        EntryData = new byte[storedDataSize];
        PackedDataSize = storedDataSize;
        OriginalDataSize = originalDataSize;
    }

    public bool IsCompressed() => EntryMimeType == PackingType.Compressed;

    public static PBOEntry GetEntryMeta(RVBinaryReader reader)
    {
        var entryName = reader.ReadAsciiZ();
        var mimeType = PackingType.Uncompressed;
        if (reader.ReadInt32() == (int)PackingType.Compressed) mimeType = PackingType.Compressed;
        var originalSize = reader.ReadInt32();
        reader.BaseStream.Position += 8;
        var dataLength = reader.ReadInt32();

        return new PBOEntry(mimeType, entryName, dataLength, originalSize);
    }

    public void ReadEntryData(RVBinaryReader reader)
    {
        Array.Copy(reader.ReadBytes(PackedDataSize), EntryData, EntryData.Length);
    }

    public void ExtractEntry(string destination)
    {
        var fileName = EntryName.Replace("config.bin", "config.cpp");
        var path = Path.Combine(destination, fileName);
        var dir = Path.GetDirectoryName(path);
        if (dir != null)
            Directory.CreateDirectory(dir);

        using var targetFile = File.Create(path);
        using var source = new MemoryStream(IsCompressed() ? RVLZSS.Decompress(EntryData, OriginalDataSize) : EntryData);
        using var reader = new BinaryReader(source);

        if (reader.BaseStream.Length >= 4 && reader.ReadByte() == '\0' && reader.ReadByte() == 'r' && reader.ReadByte() == 'a' &&
            reader.ReadByte() == 'P')
        {
            reader.BaseStream.Position = 0;
            using var writer = new StreamWriter(targetFile);
            var param = new ParamFile(source);
            writer.Write(param.ToString());
            return;
        }

        reader.BaseStream.Position = 0;
        source.CopyTo(targetFile);
    }
}