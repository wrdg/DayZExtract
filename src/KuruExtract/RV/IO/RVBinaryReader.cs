using KuruExtract.RV.Compression;
using System.Text;

namespace KuruExtract.RV.IO;
public sealed class RVBinaryReader : BinaryReader
{
    public long Position
    {
        get => BaseStream.Position;
        set => BaseStream.Position = value;
    }

    public RVBinaryReader(Stream stream)
        : base(stream)
    {
    }

    public string ReadAsciiz()
    {
        Span<char> buffer = stackalloc char[256];
        int len = 0;

        char ch;
        while ((ch = (char)ReadByte()) != 0)
        {
            if (len < buffer.Length)
            {
                buffer[len++] = ch;
            }
            else
            {
                // filename exceeded 256 chars — fall back to StringBuilder
                var sb = new StringBuilder(buffer.Length + 256);
                sb.Append(buffer);
                sb.Append(ch);
                while ((ch = (char)ReadByte()) != 0)
                    sb.Append(ch);
                return sb.ToString();
            }
        }

        return new string(buffer[..len]);
    }

    public byte[] ReadLZSS(uint expectedSize)
    {
        // RV only compresses blocks >= 1024 bytes. smaller blocks are
        // stored verbatim even when the packing method is marked as LZSS, so we
        // read them as raw bytes rather than attempting decompression
        if (expectedSize < 1024)
            return ReadBytes((int)expectedSize);

        var buffer = new byte[expectedSize];
        LZSS.ReadLZSS(BaseStream, buffer, false);
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