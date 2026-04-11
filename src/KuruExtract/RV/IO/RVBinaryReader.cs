using KuruExtract.RV.Compression;
using System.Buffers;
using System.Text;

namespace KuruExtract.RV.IO;
internal sealed class RVBinaryReader : BinaryReader
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

    public string ReadAsciiz() => ReadNullTerminated(Encoding.Latin1);

    public string ReadUTF8z() => ReadNullTerminated(Encoding.UTF8);

    private string ReadNullTerminated(Encoding encoding)
    {
        Span<byte> buffer = stackalloc byte[256];
        int len = 0;

        byte b;
        while ((b = ReadByte()) != 0)
        {
            if (len < buffer.Length)
            {
                buffer[len++] = b;
            }
            else
            {
                var rented = ArrayPool<byte>.Shared.Rent(buffer.Length + 256);
                buffer.CopyTo(rented);
                rented[len++] = b;
                while ((b = ReadByte()) != 0)
                {
                    if (len == rented.Length)
                    {
                        var larger = ArrayPool<byte>.Shared.Rent(rented.Length * 2);
                        rented.AsSpan(0, len).CopyTo(larger);
                        ArrayPool<byte>.Shared.Return(rented);
                        rented = larger;
                    }
                    rented[len++] = b;
                }
                var result = encoding.GetString(rented.AsSpan(0, len));
                ArrayPool<byte>.Shared.Return(rented);
                return result;
            }
        }

        return encoding.GetString(buffer[..len]);
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