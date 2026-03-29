using System.Buffers;
using System.Runtime.CompilerServices;

namespace KuruExtract.RV.Compression;

internal static class LZSS
{
    public static void ReadLZSS(Stream input, Span<byte> dst, bool useSignedChecksum)
    {
        const int N = 4096;
        const int F = 18;
        const int THRESHOLD = 2;

        Span<byte> text_buf = stackalloc byte[N + F - 1];
        text_buf.Fill((byte)' ');

        var bytesLeft = (uint)dst.Length;
        if (bytesLeft == 0) return;

        // rent a read buffer to batch stream reads and avoid per byte virtual dispatch
        // all callers reset stream position before their next use, so reading ahead is safe
        var readBuf = ArrayPool<byte>.Shared.Rent(4096);
        int bufPos = 0, bufLen = 0;

        try
        {
            // AggressiveInlining: this is called 1-3x per decode iteration. The compiler lowers
            // capturing local functions to a hidden static with ref parameters (no heap allocation),
            // so the JIT can inline it. The hint overrides the branch count penalty and folds the
            // hot path (bufPos < bufLen) directly into the loop, eliminating the call overhead
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            int NextByte()
            {
                if (bufPos < bufLen) return readBuf[bufPos++];
                bufLen = input.Read(readBuf, 0, readBuf.Length);
                bufPos = 0;
                return bufLen == 0 ? -1 : readBuf[bufPos++];
            }

            int iDst = 0;
            int i, j, r, c, csum = 0;
            int flags = 0;
            r = N - F;

            while (bytesLeft > 0)
            {
                if (((flags >>= 1) & 256) == 0)
                {
                    c = NextByte();
                    if (c == -1) break;
                    flags = c | 0xff00;
                }
                if ((flags & 1) != 0)
                {
                    c = NextByte();
                    if (c == -1) break;
                    if (useSignedChecksum)
                        csum += (sbyte)(byte)c;
                    else
                        csum += c;

                    dst[iDst++] = (byte)c;
                    bytesLeft--;
                    text_buf[r] = (byte)c;
                    r++; r &= N - 1;
                }
                else
                {
                    if ((i = NextByte()) == -1 || (j = NextByte()) == -1) break;
                    i |= (j & 0xf0) << 4; j &= 0x0f; j += THRESHOLD;

                    int ii = r - i;
                    int jj = j + ii;

                    if (j + 1 > bytesLeft)
                        throw new OverflowException();

                    for (; ii <= jj; ii++)
                    {
                        c = text_buf[ii & (N - 1)];
                        if (useSignedChecksum)
                            csum += (sbyte)(byte)c;
                        else
                            csum += c;

                        dst[iDst++] = (byte)c;
                        bytesLeft--;
                        text_buf[r] = (byte)c;
                        r++; r &= N - 1;
                    }
                }
            }

            // read the 4 byte checksum through our buffer, since we may have already
            // consumed it during look ahead. combine bytes manually to avoid a separate stackalloc
            int b0 = NextByte(), b1 = NextByte(), b2 = NextByte(), b3 = NextByte();
            int csr = b0 | (b1 << 8) | (b2 << 16) | (b3 << 24);

            if (csr != csum)
                throw new ArgumentException("Checksum mismatch");
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(readBuf);
        }
    }
}
