using KuruExtract.RV.IO;

namespace KuruExtract.RV.Signatures;

internal record BiSign
{
    public BiPublicKey PublicKey { get; }

    public BiSignVersion Version { get; }

    internal BiSign(BiSignVersion version, BiPublicKey key)
    {
        Version = version;
        PublicKey = key;
    }

    public static BiSign Read(RVBinaryReader reader)
    {
        var key = BiPublicKey.Read(reader);
        ReadCryptoApiBlob(reader);
        var version = (BiSignVersion)reader.ReadUInt32();
        ReadCryptoApiBlob(reader);
        ReadCryptoApiBlob(reader);

        return new(version, key);
    }

    public static BiSign Read(Stream input) => Read(new RVBinaryReader(input));

    private static void ReadCryptoApiBlob(BinaryReader reader)
    {
        var length = reader.ReadUInt32();
        _ = reader.ReadBytes((int)length);
    }
}