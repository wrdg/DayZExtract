namespace KuruExtract.RV.PBO;

internal interface IPBOFileEntry
{
    string FileName { get; }
    bool IsConfigBin { get; }
    int Size { get; }
    int TimeStamp { get; }
    bool IsCompressed { get; }
    int DiskSize { get; }
    Stream OpenRead();
    void CopyTo(Stream destination);
}
