using KuruExtract.Extensions;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KuruExtract.Update;
internal sealed partial class UpdateChecker
{
    public string Owner { get; }

    public string Repo { get; }

    public string DownloadUrl { get; private set; }

    private const string ApiUrl = "https://api.github.com";

    private string LatestReleaseUrl => $"{ApiUrl}/repos/{Owner}/{Repo}/releases/latest";

    private string ReleasesUrl => $"https://github.com/{Owner}/{Repo}/releases";

    public UpdateChecker(string owner, string repo)
    {
        Owner = owner;
        Repo = repo;
        DownloadUrl = ReleasesUrl;
    }

    public bool CheckUpdate()
    {
        try
        {
            var response = DownloadString(LatestReleaseUrl);

            if (response == null) return false;

            var update = JsonSerializer.Deserialize(response, GitHubReleaseContext.Default.GitHubRelease);

            if (update == null) return false;

            if (CompareVersion(Version.Parse(Constants.Version), Version.Parse(update.TagName[1..])) >= 0)
                return false;

            DownloadUrl = update.HtmlUrl;
        }
        catch { return false; }

        return true;
    }

    public static string? DownloadString(string url)
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue($"DayZExtract", Constants.Version));

        using var response = client.GetAsync(url).GetAwaiter().GetResult();
        using var content = response.Content;

        return content.ReadAsStringAsync().GetAwaiter().GetResult();
    }

    public static int CompareVersion(Version version1, Version version2)
    {
        return version1.Normalize().CompareTo(version2.Normalize());
    }

    private sealed record GitHubRelease(
        [property: JsonPropertyName("tag_name")] string TagName,
        [property: JsonPropertyName("html_url")] string HtmlUrl
    );

    [JsonSerializable(typeof(GitHubRelease))]
    private partial class GitHubReleaseContext : JsonSerializerContext;
}
