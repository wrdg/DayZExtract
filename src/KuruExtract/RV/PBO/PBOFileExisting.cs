namespace KuruExtract.RV.PBO;

internal class PBOFileExisting : IPBOFileEntry
{
    private readonly FileEntry _fileEntry;
    private readonly PBO _pbo;

    public PBOFileExisting(FileEntry fileEntry, PBO pbo)
    {
        _fileEntry = fileEntry;
        _pbo = pbo;
    }

    public string FileName => _fileEntry.FileName;

    public int TimeStamp => _fileEntry.TimeStamp;

    public int Size => _fileEntry.IsCompressed ? _fileEntry.UncompressedSize : _fileEntry.DataSize;

    public bool IsCompressed => _fileEntry.IsCompressed;

    public int DiskSize => _fileEntry.DataSize;

    public string PboFile => _pbo.PBOFilePath;

    public Stream OpenRead()
    {
        return _pbo.GetFileEntryStream(_fileEntry);
    }
}
