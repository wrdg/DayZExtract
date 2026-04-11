using KuruExtract.RV.IO;
using KuruExtract.RV.Signatures.Wincrypt;

namespace KuruExtract.RV.Signatures;

internal record BiPublicKey
{
    private readonly RSAPublicKeyBlob _key;

    public string Name { get; private set; }

    internal BiPublicKey(string name, RSAPublicKeyBlob key)
    {
        Name = name;
        _key = key;
    }

    public static BiPublicKey Read(RVBinaryReader reader)
    {
        var name = reader.ReadUTF8z();

        var length = reader.ReadUInt32();
        var header = KeyBlobHeader.Read(reader);
        if (header.Type != KeyBlobHeader.BLOB_TYPE.PUBLICKEYBLOB)
        {
            throw new InvalidOperationException();
        }

        var key = RSAPublicKeyBlob.Read(reader);

        if (length != KeyBlobHeader.BlobLength + key.BlobLength)
        {
            throw new InvalidOperationException();
        }

        return new(name, key);
    }

    public static BiPublicKey Read(Stream input) => Read(new RVBinaryReader(input));

    internal bool Matches(BiPublicKey other) => _key.Equals(other._key);
}
