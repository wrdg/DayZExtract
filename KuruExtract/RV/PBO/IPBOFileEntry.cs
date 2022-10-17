namespace KuruExtract.RV.PBO;

internal interface IPBOFileEntry
{
    string FileName { get; }

    int Size { get; }

    int TimeStamp { get; }

    bool IsCompressed { get; }

    int DiskSize { get; }

    Stream OpenRead();

    void Extract(string target);
}
