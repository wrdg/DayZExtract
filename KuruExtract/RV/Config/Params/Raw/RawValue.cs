using KuruExtract.RV.IO;
using System.Globalization;

namespace KuruExtract.RV.Config;

internal sealed class RawValue
{
    public ValueType Type { get; set; }

    public object Value { get; private set; }

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
        switch (Type)
        {
            case ValueType.Expression:
            case ValueType.String:
                Value = input.ReadAsciiz();
                break;
            case ValueType.Float:
                Value = input.ReadSingle();
                break;
            case ValueType.Int:
                Value = input.ReadInt32();
                break;
            case ValueType.Int64:
                Value = input.ReadInt64();
                break;
            case ValueType.Array:
                Value = new RawArray(input);
                break;

            default: throw new ArgumentException();
        }
    }

    public override string? ToString()
    {
        if (Type == ValueType.Expression || Type == ValueType.String)
            return $"\"{Value}\"";

        if (Type == ValueType.Float)
            return ((float)Value).ToString(CultureInfo.InvariantCulture);

        return Value.ToString();
    }

    internal T Get<T>()
    {
        return (T)Convert.ChangeType(Value, typeof(T));
    }
}
