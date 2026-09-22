
using System.Buffers;
using KuruExtract.RV.Compression;
using KuruExtract.RV.Config;
using KuruExtract.RV.IO;

namespace KuruExtract.RV.PBO;

internal sealed class PBO : IDisposable
{
    private FileStream? _pboFileStream;
    private RVBinaryReader? _reader;

    public string PBOFilePath { get; private set; }

    public FileStream PBOFileStream
    {
        get
        {
            if (_pboFileStream == null)
            {
                _pboFileStream = File.OpenRead(PBOFilePath);
                _reader = new RVBinaryReader(_pboFileStream);
            }
            return _pboFileStream;
        }
    }

    public List<PBOFileExisting> Files { get; } = [];
    public List<KeyValuePair<string, string>> PropertiesPairs { get; } = [];
    public int DataOffset { get; private set; }
    public string? Prefix { get; private set; }
    public string FileName => Path.GetFileName(PBOFilePath);
    public bool IsOfficial { get; init; }
    public bool IsObfuscated { get; private set; }

    public PBO(string fileName)
    {
        PBOFilePath = fileName;
        var input = new RVBinaryReader(PBOFileStream);
        ReadHeader(input);
        _pboFileStream?.Dispose();
        _pboFileStream = null;
        _reader = null;
    }

    private void ReadHeader(RVBinaryReader input)
    {
        int curOffset = 0;
        FileEntry pboEntry;
        do
        {
            pboEntry = FileEntry.Read(input, curOffset);

            curOffset += pboEntry.DataSize;

            if (pboEntry.IsVersion)
            {
                string name;
                string value;
                do
                {
                    name = input.ReadAsciiz();
                    if (name == string.Empty) break;
                    value = input.ReadAsciiz();

                    PropertiesPairs.Add(new KeyValuePair<string, string>(name, value));

                    if (name == "prefix")
                        Prefix = value;
                    else if (name == "obfuscated")
                        IsObfuscated = true;
                }
                while (name != string.Empty);
            }
            else if (pboEntry.FileName != string.Empty)
            {
                Files.Add(new PBOFileExisting(pboEntry, this));
            }
        }
        while (pboEntry.FileName != string.Empty || Files.Count == 0);

        DataOffset = (int)input.Position;
        Prefix ??= Path.GetFileNameWithoutExtension(PBOFilePath);
    }

    private byte[] GetFileData(FileEntry entry)
    {
        byte[] bytes;
        lock (this)
        {
            _ = PBOFileStream;
            _reader!.Position = DataOffset + entry.StartOffset;

            if (entry.CompressedMagic == 0)
            {
                bytes = new byte[entry.DataSize];
                _pboFileStream!.ReadExactly(bytes, 0, entry.DataSize);
            }
            else
            {
                if (!entry.IsCompressed)
                    throw new InvalidDataException("Unexpected packingMethod");

                bytes = _reader.ReadLZSS((uint)entry.UncompressedSize);
            }
        }

        return bytes;
    }

    // streams file data using a pooled buffer to avoid allocating the full entry on the heap
    internal void CopyFileTo(FileEntry entry, Stream destination)
    {
        lock (this)
        {
            _ = PBOFileStream;
            _reader!.Position = DataOffset + entry.StartOffset;

            if (entry.CompressedMagic == 0)
            {
                var buf = ArrayPool<byte>.Shared.Rent(20_480);
                try
                {
                    int remaining = entry.DataSize;
                    while (remaining > 0)
                    {
                        int toRead = Math.Min(remaining, buf.Length);
                        _pboFileStream!.ReadExactly(buf, 0, toRead);
                        destination.Write(buf, 0, toRead);
                        remaining -= toRead;
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buf);
                }
            }
            else
            {
                if (!entry.IsCompressed)
                    throw new InvalidDataException("Unexpected packingMethod");

                int size = entry.UncompressedSize;
                var buf = ArrayPool<byte>.Shared.Rent(size);
                try
                {
                    LZSS.ReadLZSS(_pboFileStream!, buf.AsSpan(0, size), false);
                    destination.Write(buf, 0, size);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buf);
                }
            }
        }
    }

    public static void ExtractFile(PBOFileExisting entry, string target)
    {
        bool isParamFile = entry.IsParamFile;

        var path = Path.Combine(target, entry.FileName);
        var parentDir = Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(parentDir))
            return;

        Directory.CreateDirectory(parentDir);

        using var targetFile = File.Create(path);

        if (isParamFile)
        {
            using var source = entry.OpenRead();

            // some mods ship plaintext config.bin / .rvmat that aren't rapified;
            // detect the "\0raP" magic and copy those through untouched
            ReadOnlySpan<byte> sig = [0x00, 0x72, 0x61, 0x50];
            Span<byte> magic = stackalloc byte[4];
            int read = source.ReadAtLeast(magic, magic.Length, throwOnEndOfStream: false);
            source.Position = 0;

            if (read == 4 && magic.SequenceEqual(sig))
            {
                var param = new ParamFile(source);
                using var writer = new StreamWriter(targetFile);
                writer.Write(param.ToString());
            }
            else
            {
                source.CopyTo(targetFile);
            }
            return;
        }

        entry.CopyTo(targetFile);
    }

    internal MemoryStream GetFileEntryStream(FileEntry entry)
    {
        return new MemoryStream(GetFileData(entry), false);
    }

    public void Dispose()
    {
        _pboFileStream?.Dispose();
        _pboFileStream = null;
        _reader = null;
    }
}
