
using KuruExtract.RV.Config;
using KuruExtract.RV.IO;

namespace KuruExtract.RV.PBO;

internal class PBO : IDisposable
{
    private FileStream? _pboFileStream;

    public string PBOFilePath { get; private set; }

    public FileStream PBOFileStream
    {
        get
        {
            _pboFileStream ??= File.OpenRead(PBOFilePath);
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
            _pboFileStream?.Close();
            _pboFileStream = null;
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
            PBOFileStream.Position = DataOffset + entry.StartOffset;
            if (entry.CompressedMagic == 0)
            {
                bytes = new byte[entry.DataSize];
                PBOFileStream.ReadExactly(bytes, 0, entry.DataSize);
            }
            else
            {
                if (!entry.IsCompressed)
                    throw new Exception("Unexpected packingMethod");

                var br = new RVBinaryReader(PBOFileStream);
                bytes = br.ReadLZSS((uint)entry.UncompressedSize);
            }
        }

        return bytes;
    }

    public static void ExtractFile(IPBOFileEntry entry, string target)
    {
        bool isConfigBin = false;
        string fileName = entry.FileName;
        if (fileName.EndsWith("config.bin"))
        {
            fileName = fileName.Replace(".bin", ".cpp");
            isConfigBin = true;
        }

        var path = Path.Combine(target, fileName);
        var parentDir = Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(parentDir))
        {
            return;
        }

        Directory.CreateDirectory(parentDir);
        using var targetFile = File.Create(path);
        using var writer = new StreamWriter(targetFile);
        using var source = entry.OpenRead();

        if (isConfigBin)
        {
            var param = new ParamFile(source);
            writer.Write(param.ToString());
            return;
        }

        source.CopyTo(targetFile);
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
            {
                continue;
            }

            yield return new MemoryStream(GetFileData(entry), false);
        }

        if (!keepStreamOpen)
        {
            _pboFileStream?.Close();
            _pboFileStream = null;
        }
    }

    public void Dispose()
    {
        _pboFileStream?.Close();
        _pboFileStream = null;
    }
}
