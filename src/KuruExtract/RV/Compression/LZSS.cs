namespace KuruExtract.RV.Compression;

internal static class LZSS
{
    public static void ReadLZSS(Stream input, Span<byte> dst, bool useSignedChecksum)
    {
        const int N = 4096;
        const int F = 18;
        const int THRESHOLD = 2;

        Span<char> text_buf = stackalloc char[N + F - 1];
        text_buf.Fill(' ');

        var bytesLeft = (uint)dst.Length;
        if (bytesLeft == 0) return;

        int iDst = 0;
        int i, j, r, c, csum = 0;
        int flags;
        r = N - F; flags = 0;
        while (bytesLeft > 0)
        {
            if (((flags >>= 1) & 256) == 0)
            {
                c = input.ReadByte();
                flags = c | 0xff00;
            }
            if ((flags & 1) != 0)
            {
                c = input.ReadByte();
                if (useSignedChecksum)
                    csum += (sbyte)c;
                else
                    csum += (byte)c;

                // save byte
                dst[iDst++] = (byte)c;
                bytesLeft--;
                // continue decompression
                text_buf[r] = (char)c;
                r++; r &= N - 1;
            }
            else
            {
                i = input.ReadByte();
                j = input.ReadByte();
                i |= (j & 0xf0) << 4; j &= 0x0f; j += THRESHOLD;

                int ii = r - i;
                int jj = j + ii;

                if (j + 1 > bytesLeft)
                {
                    throw new OverflowException();
                }

                for (; ii <= jj; ii++)
                {
                    c = (byte)text_buf[ii & N - 1];
                    if (useSignedChecksum)
                        csum += (sbyte)c;
                    else
                        csum += (byte)c;

                    // save byte
                    dst[iDst++] = (byte)c;
                    bytesLeft--;
                    // continue decompression
                    text_buf[r] = (char)c;
                    r++; r &= N - 1;
                }
            }
        }

        Span<byte> csData = stackalloc byte[4];
        input.ReadExactly(csData);
        int csr = BitConverter.ToInt32(csData);

        if (csr != csum)
        {
            throw new ArgumentException("Checksum mismatch");
        }
    }
}
