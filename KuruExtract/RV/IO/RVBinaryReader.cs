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

    //BIS IS UTF8
    public RVBinaryReader(Stream stream)
        : base(stream, Encoding.UTF8)
    {
    }
    
    public byte[] PeekBytes(int amount = 1) {
        var start = BaseStream.Position;
        var returnVal = ReadBytes(amount);
        BaseStream.Position = start;
        return returnVal;
    }

    public string ReadAsciiZ()
    {
        var str = new StringBuilder();

        char ch;
        while ((ch = (char)ReadByte()) != 0)
            str.Append(ch);

        return str.ToString();
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