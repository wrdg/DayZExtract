namespace KuruExtract.RV.Signatures.Wincrypt;

/// <summary>
/// <see href="https://github.com/ashelmire/WinCryptoHelp">Reference</see>
/// </summary>
internal sealed class RSAPublicKeyBlob : IEquatable<RSAPublicKeyBlob>
{
    public static uint SignAlgId => 0x31415352; // RSA1

    public uint BitLength { get; private set; }

    public uint PublicExponent { get; private set; }

    public byte[] Modulus { get; private set; }

    public uint BlobLength =>
        sizeof(uint) // SignAlgId
        + sizeof(uint) // BitLength
        + sizeof(uint) // PublicExponent
        + (uint)Modulus.Length;

    private RSAPublicKeyBlob(uint bitLength, uint publicExponent, byte[] modulus)
    {
        BitLength = bitLength;
        PublicExponent = publicExponent;
        Modulus = modulus;
    }

    public static RSAPublicKeyBlob Read(BinaryReader reader)
    {
        var signAlgId = reader.ReadUInt32();
        if (signAlgId != SignAlgId)
        {
            throw new InvalidOperationException();
        }
        var bitLength = reader.ReadUInt32();
        var publicExponent = reader.ReadUInt32();
        var modulus = reader.ReadBytes((int)bitLength / 8);
        return new(bitLength, publicExponent, modulus);
    }

    public override bool Equals(object? obj) => Equals(obj as RSAPublicKeyBlob);

    public bool Equals(RSAPublicKeyBlob? other) =>
        other is not null &&
        BitLength == other.BitLength &&
        PublicExponent == other.PublicExponent &&
        Modulus.AsSpan().SequenceEqual(other.Modulus);

    public override int GetHashCode()
    {
        int hashCode = 1475074757;
        hashCode = hashCode * -1521134295 + BitLength.GetHashCode();
        hashCode = hashCode * -1521134295 + PublicExponent.GetHashCode();
        hashCode = hashCode * -1521134295 + ComputeHash(Modulus);
        return hashCode;
    }

    private static int ComputeHash(params byte[] data)
    {
        unchecked
        {
            const int p = 16777619;
            int hash = (int)2166136261;

            for (int i = 0; i < data.Length; i++)
                hash = (hash ^ data[i]) * p;

            return hash;
        }
    }
}
