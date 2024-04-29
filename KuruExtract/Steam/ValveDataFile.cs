using Gameloop.Vdf;
using Gameloop.Vdf.Linq;

namespace KuruExtract.Steam;
public static class ValveDataFile
{
    public static bool TryDeserialize(string value, out VProperty? result)
    {
        try
        {
            result = VdfConvert.Deserialize(value);
            return true;
        }
        catch
        {
            // ignored
        }

        result = null;
        return false;
    }

    public static VToken? GetChild(this VToken token, string index)
    {
        try
        {
            return token[index];
        }
        catch
        {
            // ignored
        }

        return null;
    }
}
