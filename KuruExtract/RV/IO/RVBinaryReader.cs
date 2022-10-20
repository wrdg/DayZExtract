using KuruExtract.RV.Compression;
using System.Text;

namespace KuruExtract.RV.IO;

public class RVBinaryReader : BinaryReader
{
    public long Position
    {
        get
        {
            return BaseStream.Position;
        }
        set
        {
            BaseStream.Position = value;
        }
    }

    public RVBinaryReader(Stream stream)
        : base(stream)
    {
    }

    public string ReadAsciiz()
    {
        var str = new StringBuilder();

        char ch;
        while ((ch = (char)ReadByte()) != 0)
            str.Append(ch);

        return str.ToString();
    }

    public byte[] ReadLZSS(uint expectedSize)
    {
        if (expectedSize < 1024)
            return ReadBytes((int)expectedSize);

        LZSS.ReadLZSS(BaseStream, out byte[] buffer, expectedSize, false);
        return buffer;
    }

    public int ReadCompactInteger()
    {
        int result = 0, i = 0;
        bool end;

        do
        {
            int b = ReadByte();
            result |= (b & 0x7f) << (i * 7);
            end = b < 0x80;

            i++;
        } while (!end);

        return result;
    }
}