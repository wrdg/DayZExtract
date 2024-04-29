namespace KuruExtract.Steam;
public record SteamGame
{
    public int AppId { get; init; }
    public string? Name { get; init; }
    public string? InstallPath { get; init; }
}
