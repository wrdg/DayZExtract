﻿using KuruExtract.RV.IO;

namespace KuruExtract.RV.Config;
internal sealed class ParamArray : ParamEntry
{
    public RawArray Array { get; }

    public ParamArray(RVBinaryReader input)
    {
        Name = input.ReadAsciiz();
        Array = new RawArray(input);
    }

    public ParamArray(string name, IEnumerable<RawValue> values)
    {
        Name = name;
        Array = new RawArray(values);
    }

    public ParamArray(string name, params RawValue[] values) : this(name, (IEnumerable<RawValue>)values) { }

    public T[] ToArray<T>()
    {
        return Array.Entries.Select(e => e.Get<T>()).ToArray();
    }

    public override string ToString(int indentionLevel)
    {
        return $"{new string(' ', indentionLevel * 4)}{Name}[]={Array};";
    }
}
