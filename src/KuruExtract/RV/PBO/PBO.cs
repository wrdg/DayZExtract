
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

    public List<IPBOFileEntry> Files { get; } = [];
    public List<KeyValuePair<string, string>> PropertiesPairs { get; } = [];
    public int DataOffset { get; private set; }
    public string? Prefix { get; private set; }
    public string FileName => Path.GetFileName(PBOFilePath);
    public bool IsOfficial { get; init; }
    public bool IsObfuscated { get; private set; }

    public PBO(string fileName, bool keepStreamOpen = false)
    {
        PBOFilePath = fileName;
        var input = new RVBinaryReader(PBOFileStream);
        ReadHeader(input);
        if (!keepStreamOpen)
        {
            _pboFileStream?.Dispose();
            _pboFileStream = null;
            _reader = null;
        }
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

    public static void ExtractFile(IPBOFileEntry entry, string target, string? injectSubDir = null)
    {
        string fileName = entry.FileName;
        bool isParamFile = entry.IsParamFile;

        // splice in the injectSubDir after the first path segment
        if (injectSubDir != null)
        {
            var sep = fileName.IndexOfAny(['/', '\\']);

            // handle editor/ paths by skipping the "editor" segment and inserting after that instead
            if (sep >= 0 && fileName.AsSpan(0, sep).Equals("editor", StringComparison.OrdinalIgnoreCase))
                sep = fileName.IndexOfAny(['/', '\\'], sep + 1);

            if (sep >= 0)
                fileName = Path.Combine(fileName[..sep], injectSubDir, fileName[(sep + 1)..]);
        }

        var path = Path.Combine(target, fileName);
        var parentDir = Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(parentDir))
            return;

        Directory.CreateDirectory(parentDir);

        using var targetFile = File.Create(path);

        if (isParamFile)
        {
            using var source = entry.OpenRead();
            var param = new ParamFile(source);
            using var writer = new StreamWriter(targetFile);
            writer.Write(param.ToString());
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
