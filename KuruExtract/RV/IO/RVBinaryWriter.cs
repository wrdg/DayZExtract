using System.Text;

namespace KuruExtract.RV.IO; 

public class RVBinaryWriter : BinaryWriter
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
    
    public RVBinaryWriter(Stream dstStream) : base(dstStream, Encoding.UTF8) { }

    public RVBinaryWriter(Stream dstStream, bool leaveOpen): base(dstStream, Encoding.UTF8, leaveOpen) {}
    
    public void WriteAsciiZ(string text)
    {
        Write(text.ToCharArray());
        Write(char.MinValue);
    }
}