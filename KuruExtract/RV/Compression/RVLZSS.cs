namespace System.IO.Compression;

public class RVLZSS {
    private const int PacketFormatUncompressed = 1;
    private const byte Space = 0x20;
    
    //Compression probably breaks when applied to a file with 
    //a byte count over int.MaxValue ~ 2^(32-1)
    public static byte[] Compress(byte[] data) {
        var output = new List<byte>();
        var buffer = new CompressionBuffer();

        var dataLength = data.Length;
        var readData = 0;

        //Generate and add compressed data
        while (readData < dataLength) {
            var packet = new Packet();
            readData = packet.Pack(data, readData, buffer);

            var content = packet.GetContent();

            output.AddRange(content);
        }

        //Calculate and add checksum of compressed data
        var checksum = CalculateChecksum(data);
        output.AddRange(checksum);

        //Console.WriteLine(Encoding.UTF8.GetString(Decompress(output.ToArray(), dataLength)));
        
        return output.ToArray();
    }

    

    private static IEnumerable<byte> CalculateChecksum(IEnumerable<byte> data) {
        return BitConverter.GetBytes(data.Aggregate<byte, uint>(0, (current, t) => current + t));
    }

    private class Packet {
        private const int m_DataBlockCount = 8;
        private const int m_MinPackBytes = 3;
        private const int m_MaxDataBlockSize = m_MinPackBytes + 0b1111;
        private const int m_MaxOffsetForWhitespaces = 0b0000111111111111 - m_MaxDataBlockSize;

        private int m_Flagbits;
        public readonly List<byte> m_Content = new List<byte>();
        private List<byte> m_Next = new List<byte>();
        CompressionBuffer m_CompressionBuffer = new CompressionBuffer();

        public int Pack(byte[] data, int currPos, CompressionBuffer buffer) {
            m_CompressionBuffer = buffer;

            for (var i = 0; i < m_DataBlockCount && currPos < data.Length; i++) {
                var blockSize = Math.Min(m_MaxDataBlockSize, data.Length - currPos);
                if (blockSize < m_MinPackBytes) {
                    currPos += AddUncompressed(i, data, currPos);
                    continue;
                }

                currPos += AddCompressed(i, data, currPos, blockSize);
            }

            return currPos;
        }

        public byte[] GetContent() {
            var output = new byte[1 + m_Content.Count];
            output[0] = BitConverter.GetBytes(m_Flagbits)[0];

            for (var i = 1; i < output.Length; i++) {
                output[i] = m_Content[i - 1];
            }

            return output;
        }

        public int AddUncompressed(int blockIndex, byte[] data, int currPos) {
            m_CompressionBuffer.AddByte(data[currPos]);
            m_Content.Add(data[currPos]);
            m_Flagbits += 1 << blockIndex;
            return 1;
        }

        public int AddCompressed(int blockIndex, byte[] data, int currPos, int blockSize) {
            m_Next = new List<byte>();
            for (var i = 0; i < blockSize; i++) {
                m_Next.Add(data[currPos + i]);
            }

            var next = m_Next.ToArray();
            var intersection = m_CompressionBuffer.Intersect(next, blockSize);
            var whitespace = currPos < m_MaxOffsetForWhitespaces
                ? m_CompressionBuffer.CheckWhiteSpace(next, blockSize)
                : 0;
            var sequence = m_CompressionBuffer.CheckSequence(next, blockSize);

            if (intersection.Length < m_MinPackBytes && whitespace < m_MinPackBytes &&
                sequence.SourceBytes < m_MinPackBytes) {
                return AddUncompressed(blockIndex, data, currPos);
            }

            var processed = 0;
            short pointer = 0;

            if (intersection.Length >= whitespace && intersection.Length >= sequence.SourceBytes) {
                pointer = CreatePointer(m_CompressionBuffer.GetLength() - intersection.Position, intersection.Length);
                processed = intersection.Length;
            }
            else if (whitespace >= intersection.Length && whitespace >= sequence.SourceBytes) {
                pointer = CreatePointer(currPos + whitespace, whitespace);
                processed = whitespace;
            }
            else {
                pointer = CreatePointer(sequence.SequenceBytes, sequence.SourceBytes);
                processed = sequence.SourceBytes;
            }

            m_CompressionBuffer.AddBytes(data, currPos, processed);
            var tmp = BitConverter.GetBytes(pointer);
            foreach (var t in tmp) {
                m_Content.Add(t);
            }

            return processed;
        }

        short CreatePointer(int offset, int length) {
            //4 bits
            //00001111 00000000
            var lengthEntry = (short) ((length - m_MinPackBytes) << 8);
            //12 bits
            //11110000 11111111
            var offsetEntry = (short) (((offset & 0x0F00) << 4) + (offset & 0x00FF));

            return (short) (offsetEntry + lengthEntry);
        }
    }

    private class CompressionBuffer {
        public struct Intersection {
            public int Position;
            public int Length;
        }

        public struct Sequence {
            public int SourceBytes;
            public int SequenceBytes;
        }

        //4095 ---> 2^12
        long m_Size = 0b0000111111111111;
        List<byte> m_Content;

        public CompressionBuffer(long size = 0) {
            if (size != 0) {
                m_Size = size;
            }

            m_Content = new List<byte>();
        }

        public int GetLength() {
            return m_Content.Count;
        }

        public void AddBytes(byte[] data, int currPos, int length) {
            for (var i = 0; i < length; i++) {
                if (m_Size < m_Content.Count + 1) {
                    m_Content.RemoveAt(0);
                }

                m_Content.Add(data[currPos + i]);
            }
        }

        public void AddByte(byte data) {
            if (m_Size < m_Content.Count + 1) {
                m_Content.RemoveAt(0);
            }

            m_Content.Add(data);
        }

        public Intersection Intersect(byte[] buffer, int length) {
            var intersection = new Intersection {
                Position = -1,
                Length = 0
            };

            if (length == 0 || m_Content.Count == 0) {
                return intersection;
            }

            var offset = 0;
            while (true) {
                var next = IntersectAtOffset(buffer, length, offset);

                if (next.Position >= 0 && intersection.Length < next.Length) {
                    intersection = next;
                }

                if (next.Position < 0 || next.Position > m_Content.Count - 1) {
                    break;
                }

                offset = next.Position + 1;
            }

            return intersection;
        }

        Intersection IntersectAtOffset(byte[] buffer, int bLength, int offset) {
            var position = m_Content.IndexOf(buffer[0], offset);
            var length = 0;

            if (position >= 0 && position < m_Content.Count) {
                length++;
                for (int bufIndex = 1, dataIndex = position + 1;
                     bufIndex < bLength && dataIndex < m_Content.Count;
                     bufIndex++, dataIndex++) {
                    if (m_Content[dataIndex] != buffer[bufIndex]) {
                        break;
                    }

                    length++;
                }
            }

            Intersection intersection;
            intersection.Position = position;
            intersection.Length = length;
            return intersection;
        }


        public int CheckWhiteSpace(byte[] buffer, int length) {
            var count = 0;
            for (var i = 0; i < length; i++) {
                if (buffer[i] != 0x20) {
                    break;
                }

                count++;
            }

            return count;
        }

        public Sequence CheckSequence(byte[] buffer, int length) {
            Sequence result;
            result.SequenceBytes = 0;
            result.SourceBytes = 0;

            var maxSourceBytes = Math.Min(m_Content.Count, length);
            for (var i = 1; i < maxSourceBytes; i++) {
                var sequence = CheckSequenceImpl(buffer, length, i);
                if (sequence.SourceBytes > result.SourceBytes) {
                    result = sequence;
                }
            }

            return result;
        }

        Sequence CheckSequenceImpl(byte[] buffer, int length, int sequenceBytes) {
            var sourceBytes = 0;
            Sequence sequence;

            while (sourceBytes < length) {
                for (var i = m_Content.Count - sequenceBytes; i < m_Content.Count && sourceBytes < length; i++) {
                    if (buffer[sourceBytes] != m_Content[i]) {
                        sequence.SourceBytes = sourceBytes;
                        sequence.SequenceBytes = sequenceBytes;
                        return sequence;
                    }

                    sourceBytes++;
                }
            }

            sequence.SourceBytes = sourceBytes;
            sequence.SequenceBytes = sequenceBytes;
            return sequence;
        }
    }


    public static byte[] Decompress(byte[] compressedData, int targetLength, bool useSignedChecksum = false) {
        const int N = 4096;
        const int F = 18;
        const int THRESHOLD = 2;
        var text_buf = new char[N + F - 1];
        var output = new byte[targetLength];

        if (targetLength <= 0) return output;
        using var input = new BinaryReader(new MemoryStream(compressedData));


        int i;
        
        var flags = 0;
        int cSum = 0, iDst = 0, bytesLeft = targetLength;

        for (i = 0; i < N - F; i++) text_buf[i] = ' ';
        var r = N - F;
        
        while (bytesLeft > 0) {
            int c;
            if (((flags >>= 1) & 256) == 0) {
                c = input.ReadByte();
                flags = c | 0xff00;
            }

            if ((flags & 1) != 0) {
                c = input.ReadByte();
                if (useSignedChecksum)
                    cSum += (sbyte)c;
                else
                    cSum += (byte)c;

                // save byte
                output[iDst++] = (byte)c;
                bytesLeft--;
                // continue decompression
                text_buf[r] = (char)c;
                r++;
                r &= (N - 1);
            }
            else {
                i = input.ReadByte();
                int j = input.ReadByte();
                i |= (j & 0xf0) << 4;
                j &= 0x0f;
                j += THRESHOLD;

                int ii = r - i,
                    jj = j + ii;

                if (j + 1 > bytesLeft) {
                    throw new ArgumentException("LZSS overflow");
                }

                for (; ii <= jj; ii++) {
                    c = (byte)text_buf[ii & (N - 1)];
                    if (useSignedChecksum)
                        cSum += (sbyte)c;
                    else
                        cSum += (byte)c;

                    // save byte
                    output[iDst++] = (byte)c;
                    bytesLeft--;
                    // continue decompression
                    text_buf[r] = (char)c;
                    r++;
                    r &= (N - 1);
                }
            }
        }

        var csData = new byte[4];
        input.Read(csData, 0, 4);
        var csr = BitConverter.ToInt32(csData, 0);

        if (csr != cSum) throw new ArgumentException("Checksum mismatch");

        return output;
    }
    
}