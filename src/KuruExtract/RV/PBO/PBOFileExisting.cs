namespace KuruExtract.RV.PBO;

internal sealed class PBOFileExisting : IPBOFileEntry
{
    private readonly FileEntry _fileEntry;
    private readonly PBO _pbo;

    public PBOFileExisting(FileEntry fileEntry, PBO pbo)
    {
        _fileEntry = fileEntry;
        _pbo = pbo;
        IsConfigBin = fileEntry.FileName.EndsWith("config.bin", StringComparison.OrdinalIgnoreCase);
        FileName = IsConfigBin ? Path.ChangeExtension(fileEntry.FileName, ".cpp") : fileEntry.FileName;
    }

    public string FileName { get; }
    public bool IsConfigBin { get; }

    public int TimeStamp => _fileEntry.TimeStamp;

    public int Size => _fileEntry.IsCompressed ? _fileEntry.UncompressedSize : _fileEntry.DataSize;

    public bool IsCompressed => _fileEntry.IsCompressed;

    public int DiskSize => _fileEntry.DataSize;

    public string PboFile => _pbo.PBOFilePath;

    public Stream OpenRead() => _pbo.GetFileEntryStream(_fileEntry);

    public void CopyTo(Stream destination) => _pbo.CopyFileTo(_fileEntry, destination);
}
