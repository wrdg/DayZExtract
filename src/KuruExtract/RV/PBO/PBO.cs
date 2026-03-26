
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
            pboEntry = new FileEntry(input)
            {
                StartOffset = curOffset
            };

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
                    throw new Exception("Unexpected packingMethod");

                bytes = _reader.ReadLZSS((uint)entry.UncompressedSize);
            }
        }

        return bytes;
    }

    // Streams the file data directly to `destination` using a pooled buffer,
    // avoiding a full-file heap allocation for each entry.
    internal void CopyFileTo(FileEntry entry, Stream destination)
    {
        lock (this)
        {
            _ = PBOFileStream;
            _reader!.Position = DataOffset + entry.StartOffset;

            if (entry.CompressedMagic == 0)
            {
                var buf = ArrayPool<byte>.Shared.Rent(81920);
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
                    throw new Exception("Unexpected packingMethod");

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

    public static void ExtractFile(IPBOFileEntry entry, string target)
    {
        string fileName = entry.FileName;
        bool isConfigBin = fileName.EndsWith("config.bin", StringComparison.OrdinalIgnoreCase);
        if (isConfigBin)
            fileName = Path.ChangeExtension(fileName, ".cpp");

        var path = Path.Combine(target, fileName);
        var parentDir = Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(parentDir))
            return;

        Directory.CreateDirectory(parentDir);

        using var targetFile = File.Create(path);

        if (isConfigBin)
        {
            using var source = entry.OpenRead();
            var param = new ParamFile(source);
            using var writer = new StreamWriter(targetFile);
            writer.Write(param.ToString());
            return;
        }

        entry.CopyTo(targetFile);
    }

    public static void ExtractFiles(IEnumerable<IPBOFileEntry> entries, string target)
    {
        foreach (var entry in entries)
        {
            ExtractFile(entry, target);
        }
    }

    public void ExtractAllFiles(string directory)
    {
        ExtractFiles(Files, Path.Combine(directory, Prefix ?? string.Empty));
    }

    public MemoryStream GetFileEntryStream(FileEntry entry)
    {
        return new MemoryStream(GetFileData(entry), false);
    }

    public IEnumerable<MemoryStream> GetFileEntryStreams(IEnumerable<FileEntry> entries, bool keepStreamOpen = false)
    {
        foreach (var entry in entries)
        {
            if (entry.DataSize <= 0)
                continue;

            yield return new MemoryStream(GetFileData(entry), false);
        }

        if (!keepStreamOpen)
        {
            _pboFileStream?.Dispose();
            _pboFileStream = null;
            _reader = null;
        }
    }

    public void Dispose()
    {
        _pboFileStream?.Dispose();
        _pboFileStream = null;
        _reader = null;
    }
}
