namespace KuruExtract.RV.PBO;

public interface IPBOFileEntry
{
    string FileName { get; }
    int Size { get; }
    int TimeStamp { get; }
    bool IsCompressed { get; }
    int DiskSize { get; }
    Stream OpenRead();
    void CopyTo(Stream destination);
}
