using KuruExtract.RV.Compression;
using KuruExtract.RV.Config;
using KuruExtract.RV.IO;

namespace KuruExtract.RV.PBO;

public enum PackingType {
    Uncompressed = 0x00000000,
    Compressed = 0x43707273
}

public sealed class PBOEntry {
    public static readonly PBOEntry EmptyEntry = new PBOEntry() {
        _dataWasRead = false,
        EntryData = Array.Empty<byte>(),
        EntryName = "",
        EntryMimeType = PackingType.Uncompressed,
        PackedDataSize = 0,
        OriginalDataSize = 0
    };

    private PBOEntry() {
    }

    private bool _dataWasRead;


    public PBOEntry(PackingType entryMimeType, string entryName, byte[] entryData, int originalDataSize) {
        _dataWasRead = true;
        EntryMimeType = entryMimeType;
        EntryName = entryName;
        EntryData = entryData;
        PackedDataSize = entryData.Length;
        OriginalDataSize = originalDataSize;
    }

    public PBOEntry(PackingType entryMimeType, string entryName, int storedDataSize, int originalDataSize) {
        _dataWasRead = false;
        EntryMimeType = entryMimeType;
        EntryName = entryName;
        EntryData = new byte[storedDataSize];
        PackedDataSize = storedDataSize;
        OriginalDataSize = originalDataSize;
    }

    public PackingType EntryMimeType { get; private set; }
    public string EntryName { get; private set; }
    public byte[] EntryData { get; private set; }
    public int OriginalDataSize { get; private set; }
    public int PackedDataSize { get; private set; }

    public bool IsCompressed() => EntryMimeType == PackingType.Compressed;

    public static PBOEntry CreateEntry(string entryName, byte[] entryData, bool compressed = false) =>
        new PBOEntry(compressed ? PackingType.Compressed : PackingType.Uncompressed,
            entryName,
            compressed ? BisLZSS.Compress(entryData) : entryData,
            entryData.Length);

    public static PBOEntry GetEntryMeta(RVBinaryReader reader) {
        var entryName = reader.ReadAsciiZ();
        var mimeType = PackingType.Uncompressed;
        if (reader.ReadInt32() == (int)PackingType.Compressed) mimeType = PackingType.Compressed;
        var originalSize = reader.ReadInt32();
        reader.BaseStream.Position += 8;
        var dataLength = reader.ReadInt32();

        return new PBOEntry(mimeType, entryName, dataLength, originalSize);
    }
    
    public void ReadEntryData(RVBinaryReader reader) {
        // EntryData = reader.ReadBytes(PackedDataSize);
        Array.Copy(reader.ReadBytes(PackedDataSize), EntryData, EntryData.Length);
        _dataWasRead = true;
    }

    public void CompressEntry() {
        if (!_dataWasRead) throw new NotSupportedException("Cannot compress an entry without knowing its contents.");
        if (IsCompressed()) return;

        EntryMimeType = PackingType.Compressed;
        OriginalDataSize = EntryData.Length;
        EntryData = BisLZSS.Compress(EntryData);
        PackedDataSize = EntryData.Length;
    }

    public bool TryCompressEntry() {
        if (!_dataWasRead) throw new NotSupportedException("Cannot compress an entry without knowing its contents.");
        if (IsCompressed()) return true;
        try {
            EntryMimeType = PackingType.Compressed;
            var dataLn = EntryData.Length;
            EntryData = BisLZSS.Compress(EntryData);
            OriginalDataSize = dataLn;
            PackedDataSize = EntryData.Length;
            return true;
        }
        catch (Exception e) {
            EntryMimeType = PackingType.Uncompressed;
            return false;
        }
    }

    public void RenameEntry(string name) => EntryName = name;

    public void ExtractEntry(string destination) {
        var fileName = EntryName.Replace("config.bin", "config.cpp");
        var path = Path.Combine(destination, fileName);
        var dir = Path.GetDirectoryName(path);
        if (dir != null)
            Directory.CreateDirectory(dir);

        using var targetFile = File.Create(path);
        using var source = new MemoryStream(IsCompressed() ? BisLZSS.Decompress(EntryData, OriginalDataSize) : EntryData);

        using (var reader = new BinaryReader(source)) {
            if (reader.BaseStream.Length >= 4 && reader.ReadByte() == '\0' && reader.ReadByte() == 'r' && reader.ReadByte() == 'a' &&
                reader.ReadByte() == 'P') {
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

    public void DecompressEntry() {
        if (!_dataWasRead) throw new NotSupportedException("Cannot decompress an entry without knowing its contents.");
        if (!IsCompressed()) return;
        EntryMimeType = PackingType.Uncompressed;
        EntryData = BisLZSS.Decompress(EntryData, OriginalDataSize);
        PackedDataSize = EntryData.Length;
    }
    
    public bool TryDecompressEntry() {
        if (!_dataWasRead) throw new NotSupportedException("Cannot decompress an entry without knowing its contents.");
        if(!IsCompressed()) return true;
        try {
            EntryMimeType = PackingType.Uncompressed;
            EntryData = BisLZSS.Decompress(EntryData, OriginalDataSize);
            PackedDataSize = EntryData.Length;
            return true;
        } catch (Exception) {
            EntryMimeType = PackingType.Compressed;
            return false;
        }
    }
}