using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace KuruExtract.Interop;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal static partial class ShellShortcut
{
    private static readonly Guid CLSID_ShellLink = new("00021401-0000-0000-C000-000000000046");
    private static readonly Guid IID_IShellLinkW = new("000214F9-0000-0000-C000-000000000046");

    [LibraryImport("ole32.dll")]
    private static partial int CoCreateInstance(
        in Guid rclsid,
        nint pUnkOuter,
        uint dwClsContext,
        in Guid riid,
        [MarshalUsing(typeof(UniqueComInterfaceMarshaller<IShellLinkW>))] out IShellLinkW ppv);

    internal static void Create(string lnkPath, string targetPath, string arguments)
    {
        CoCreateInstance(in CLSID_ShellLink, 0, 1, in IID_IShellLinkW, out var link);
        link.SetPath(targetPath);
        link.SetArguments(arguments);
        ((IPersistFile)link).Save(lnkPath, 0);
    }
}

[GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16)]
[Guid("000214F9-0000-0000-C000-000000000046")]
internal partial interface IShellLinkW
{
    void GetPath(nint pszFile, int cch, nint pfd, uint fFlags);
    void GetIDList(out nint ppidl);
    void SetIDList(nint pidl);
    void GetDescription(nint pszName, int cch);
    void SetDescription(string pszName);
    void GetWorkingDirectory(nint pszDir, int cch);
    void SetWorkingDirectory(string pszDir);
    void GetArguments(nint pszArgs, int cch);
    void SetArguments(string pszArgs);
    void GetHotkey(out ushort pwHotkey);
    void SetHotkey(ushort wHotkey);
    void GetShowCmd(out int piShowCmd);
    void SetShowCmd(int iShowCmd);
    void GetIconLocation(nint pszIconPath, int cch, out int piIcon);
    void SetIconLocation(string pszIconPath, int iIcon);
    void SetRelativePath(string pszPathRel, uint dwReserved);
    void Resolve(nint hwnd, uint fFlags);
    void SetPath(string pszFile);
}

[GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16)]
[Guid("0000010B-0000-0000-C000-000000000046")]
internal partial interface IPersistFile
{
    void GetClassID(out Guid pClassID);
    int IsDirty();
    void Load(string pszFileName, uint dwMode);
    void Save(string? pszFileName, int fRemember);
    void SaveCompleted(string pszFileName);
    void GetCurFile(out nint ppszFileName);
}
