using KuruExtract.RV.IO;
using System.Globalization;
using System.Runtime.InteropServices;

namespace KuruExtract.RV.Config;
internal sealed class RawValue
{
    [StructLayout(LayoutKind.Explicit)]
    private struct NumericUnion
    {
        [FieldOffset(0)] public int Int;
        [FieldOffset(0)] public long Int64;
        [FieldOffset(0)] public float Float;
    }

    public ValueType Type { get; set; }

    private readonly string? _string;
    private readonly RawArray? _array;
    private NumericUnion _numeric;

    public RawValue(string v)
    {
        Type = ValueType.String;
        _string = v;
    }

    public RawValue(int v)
    {
        Type = ValueType.Int;
        _numeric.Int = v;
    }

    public RawValue(long v)
    {
        Type = ValueType.Int64;
        _numeric.Int64 = v;
    }

    public RawValue(float v)
    {
        Type = ValueType.Float;
        _numeric.Float = v;
    }

    public RawValue(RVBinaryReader input) : this(input, (ValueType)input.ReadByte()) { }

    public RawValue(RVBinaryReader input, ValueType type)
    {
        Type = type;
        switch (type)
        {
            case ValueType.Expression or ValueType.String:
                _string = input.ReadAsciiz();
                break;
            case ValueType.Float:
                _numeric.Float = input.ReadSingle();
                break;
            case ValueType.Int:
                _numeric.Int = input.ReadInt32();
                break;
            case ValueType.Int64:
                _numeric.Int64 = input.ReadInt64();
                break;
            case ValueType.Array:
                _array = new RawArray(input);
                break;
            default:
                throw new ArgumentException($"Unexpected value type: {type}");
        }
    }

    public override string? ToString()
    {
        return Type switch
        {
            ValueType.Expression or ValueType.String => FormatString(_string!),
            ValueType.Float => _numeric.Float.ToString(CultureInfo.InvariantCulture),
            ValueType.Int => _numeric.Int.ToString(),
            ValueType.Int64 => _numeric.Int64.ToString(),
            ValueType.Array => _array!.ToString(),
            _ => null
        };
    }

    public bool WasEscaped => Type switch
    {
        ValueType.String or ValueType.Expression => _string!.Contains('"'),
        ValueType.Array => _array!.Entries.Any(e => e.WasEscaped),
        _ => false
    };

    private static string FormatString(string s) =>
        s.Contains('"') ? $"\"{s.Replace("\"", "")}\"" : $"\"{s}\"";

    internal T Get<T>()
    {
        object boxed = Type switch
        {
            ValueType.Expression or ValueType.String => _string!,
            ValueType.Float => _numeric.Float,
            ValueType.Int => _numeric.Int,
            ValueType.Int64 => _numeric.Int64,
            _ => _array!
        };
        return (T)Convert.ChangeType(boxed, typeof(T));
    }
}
