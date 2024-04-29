using KuruExtract.RV.IO;
using System.Globalization;

namespace KuruExtract.RV.Config;
internal sealed class RawValue
{
    public ValueType Type { get; set; }

    public object Value { get; }

    public RawValue(string v)
    {
        Type = ValueType.String;
        Value = v;
    }

    public RawValue(int v)
    {
        Type = ValueType.Int;
        Value = v;
    }

    public RawValue(long v)
    {
        Type = ValueType.Int64;
        Value = v;
    }

    public RawValue(float v)
    {
        Type = ValueType.Float;
        Value = v;
    }

    public RawValue(RVBinaryReader input) : this(input, (ValueType)input.ReadByte()) { }

    public RawValue(RVBinaryReader input, ValueType type)
    {
        Type = type;
        Value = Type switch
        {
            ValueType.Expression or ValueType.String => input.ReadAsciiz(),
            ValueType.Float => input.ReadSingle(),
            ValueType.Int => input.ReadInt32(),
            ValueType.Int64 => input.ReadInt64(),
            ValueType.Array => new RawArray(input),
            _ => throw new ArgumentException(),
        };
    }

    public override string? ToString()
    {
        return Type switch
        {
            ValueType.Expression or ValueType.String => $"\"{((string)Value).Replace("\"", "\"\"")}\"",
            ValueType.Float => ((float)Value).ToString(CultureInfo.InvariantCulture),
            _ => Value.ToString()
        };
    }

    internal T Get<T>()
    {
        return (T)Convert.ChangeType(Value, typeof(T));
    }
}
