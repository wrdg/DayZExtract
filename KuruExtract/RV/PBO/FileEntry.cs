using KuruExtract.RV.IO;
using System.Text;

namespace KuruExtract.RV.PBO;

internal sealed class FileEntry
{
    public string FileName { get; set; } = string.Empty;

    public int CompressedMagic { get; set; }

    public int UncompressedSize { get; set; }

    public int StartOffset { get; set; }

    public int TimeStamp { get; set; }

    public int DataSize { get; set; }

    public static readonly int VersionMagic = BitConverter.ToInt32(Encoding.ASCII.GetBytes("sreV"), 0); //Vers
    public static readonly int CompressionMagic = BitConverter.ToInt32(Encoding.ASCII.GetBytes("srpC"), 0); //Cprs
    public static readonly int EncryptionMagic = BitConverter.ToInt32(Encoding.ASCII.GetBytes("rcnE"), 0); //Encr

    public FileEntry()
    {
    }

    public FileEntry(RVBinaryReader input)
        => Read(input);

    public void Read(RVBinaryReader input)
    {
        FileName = input.ReadAsciiz();
        CompressedMagic = input.ReadInt32();
        UncompressedSize = input.ReadInt32();
        StartOffset = input.ReadInt32();
        TimeStamp = input.ReadInt32();
        DataSize = input.ReadInt32();
    }

    public bool IsVersion => CompressedMagic == VersionMagic && TimeStamp == 0 && DataSize == 0;

    public bool IsCompressed => CompressedMagic == CompressionMagic;
}
