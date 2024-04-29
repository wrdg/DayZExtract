﻿using KuruExtract.RV.IO;

namespace KuruExtract.RV.PBO;
internal sealed class PBO : IDisposable
{
    private FileStream? _pboFileStream;

    public FileStream PBOFileStream
    {
        get
        {
            _pboFileStream ??= File.OpenRead(PBOFilePath);
            return _pboFileStream;
        }
    }

    public int DataOffset { get; private set; }

    public string PBOFilePath { get; private set; }

    public string? Prefix { get; private set; }

    public string? Product { get; private set; }

    public string? Version { get; private set; }

    public string? FileName => Path.GetFileName(PBOFilePath);

    public List<PBOFile> Files { get; } = [];

    public PBO(string fileName)
    {
        PBOFilePath = fileName;

        var input = new RVBinaryReader(PBOFileStream);

        ReadHeader(input);

        _pboFileStream?.Close();
        _pboFileStream = null;
    }

    private void ReadHeader(RVBinaryReader input)
    {
        var curOffset = 0;
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
                do
                {
                    name = input.ReadAsciiz();
                    if (string.IsNullOrEmpty(name))
                        break;

                    var value = input.ReadAsciiz();

                    switch (name)
                    {
                        case "prefix":
                            Prefix = value;
                            break;
                        case "product":
                            Product = value;
                            break;
                        case "version":
                            Version = value;
                            break;
                    }
                }
                while (name != string.Empty);
            }
            else if (pboEntry.FileName != string.Empty)
            {
                Files.Add(new PBOFile(pboEntry, this));
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
                PBOFileStream.Read(bytes, 0, entry.DataSize);
            }
            else
            {
                if (!entry.IsCompressed)
                    throw new Exception("Unexpected packing method");

                var br = new RVBinaryReader(PBOFileStream);
                bytes = br.ReadLZSS((uint)entry.UncompressedSize);
            }
        }

        return bytes;
    }

    public MemoryStream GetFileEntryStream(FileEntry entry)
    {
        return new MemoryStream(GetFileData(entry), false);
    }

    public void Dispose()
    {
        _pboFileStream?.Dispose();
    }
}
