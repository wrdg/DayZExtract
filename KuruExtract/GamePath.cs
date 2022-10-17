using Microsoft.Win32;

namespace KuruExtract;

internal sealed class GamePath
{
    public static string? Stable
    {
        get
        {
            try
            {
                using RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\bohemia interactive\dayz");
                return key?.GetValue("main") as string;
            }
            catch { return null; }
        }
    }

    public static string? Experimental
    {
        get
        {
            try
            {
                using RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\bohemia interactive\dayz exp");
                return key?.GetValue("main") as string;
            }
            catch { return null; }
        }
    }
}
