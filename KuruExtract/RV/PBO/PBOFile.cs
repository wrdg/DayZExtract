using System.Security.Cryptography;
using System.Text;
using KuruExtract.RV.IO;

namespace KuruExtract.RV.PBO;

#pragma warning disable CS0618

public sealed class PBOFile 
{


    private readonly string _pboPath;

    public string PBOName => Path.GetFileNameWithoutExtension(_pboPath);
    public string PBOPrefix => PBOProperties!["prefix"];
    public string PBOProduct => PBOProperties!.ContainsKey("product") ? PBOProperties!["product"] : "dayz";
    public string? PBOVersion => PBOProperties!.ContainsKey("version") ? PBOProperties!["version"] : null;

    public List<PBOEntry> PBOEntries { get; set; } = new();
    public Dictionary<string, string> PBOProperties { get; set; } = new();
    
    public PBOFile(string pboPath) 
    {
        _pboPath = pboPath;
        var stream = File.OpenRead(pboPath);
        using var reader = new RVBinaryReader(stream);
        
        reader.ReadAsciiZ();
        if(reader.ReadInt32() != BitConverter.ToInt32(Encoding.ASCII.GetBytes("sreV"))) 
            throw new Exception("Woah, version entry should be the first in all PBOs. Report this to developer");
        reader.BaseStream.Position += 16;

        
        string propertyName;
        do 
        {
            propertyName = reader.ReadAsciiZ();
            if (propertyName == "") break;

            var value = reader.ReadAsciiZ();
            PBOProperties.Add(propertyName, value);

        }
        while (propertyName != "");
        
        do 
        {
            var entry = PBOEntry.GetEntryMeta(reader);
            if(entry != PBOEntry.EmptyEntry) PBOEntries.Add(entry);
        }
        while (reader.PeekBytes(21).Sum(b =>  b) != 0);

        reader.BaseStream.Position += 21;
        
        foreach (var pboEntry in PBOEntries) 
            pboEntry.ReadEntryData(reader);

    }
}

#pragma warning restore CS0618