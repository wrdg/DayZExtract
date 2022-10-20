using KuruExtract.RV.Config;
using System.Text;

namespace KuruExtract.RV.PBO;

internal sealed class PBOFileExisting : IPBOFileEntry
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

    public Stream OpenRead()
    {
        return _pbo.GetFileEntryStream(_fileEntry);
    }

    public void Extract(string target)
    {
        var fileName = FileName.EndsWith("config.bin")
            ? FileName.Replace("config.bin", "config.cpp")
            : FileName;

        var path = Path.Combine(target, fileName);
        var dir = Path.GetDirectoryName(path);

        if (dir != null)
            Directory.CreateDirectory(dir);

        using var source = OpenRead();

        if (FileName.EndsWith("config.bin") || FileName.EndsWith(".rvmat"))
        {
            var param = new ParamFile(source);
            File.WriteAllText(path, param.ToString(), Encoding.UTF8);

            return;
        }

        using var targetFile = File.Create(path);
        source.CopyTo(targetFile);
    }
}
