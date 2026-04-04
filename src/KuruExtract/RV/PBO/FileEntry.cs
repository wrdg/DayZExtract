using KuruExtract.RV.IO;
using System.Text;

namespace KuruExtract.RV.PBO;

internal readonly record struct FileEntry(
    string FileName,
    int CompressedMagic,
    int UncompressedSize,
    int StartOffset,
    int TimeStamp,
    int DataSize)
{
    public static readonly int VersionMagic    = BitConverter.ToInt32(Encoding.ASCII.GetBytes("sreV"), 0); // Vers
    public static readonly int CompressionMagic = BitConverter.ToInt32(Encoding.ASCII.GetBytes("srpC"), 0); // Cprs
    public static readonly int EncryptionMagic  = BitConverter.ToInt32(Encoding.ASCII.GetBytes("rcnE"), 0); // Encr

    // The PBO header stores an offset field that we discard — we compute StartOffset
    // ourselves from the running data position so that it's always accurate.
    public static FileEntry Read(RVBinaryReader input, int startOffset)
    {
        var fileName        = input.ReadAsciiz();
        var compressedMagic = input.ReadInt32();
        var uncompressedSize = input.ReadInt32();
        _ = input.ReadInt32(); // stored offset — ignored, we use startOffset
        var timeStamp = input.ReadInt32();
        var dataSize  = input.ReadInt32();
        return new(fileName, compressedMagic, uncompressedSize, startOffset, timeStamp, dataSize);
    }

    public bool IsVersion   => CompressedMagic == VersionMagic && TimeStamp == 0 && DataSize == 0;
    public bool IsCompressed => CompressedMagic == CompressionMagic;
}
