using KuruExtract.Extensions;
using Newtonsoft.Json;
using System.Net.Http.Headers;

namespace KuruExtract.Update;
internal sealed class UpdateChecker
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

            var update = JsonConvert.DeserializeObject<GitHubResponse>(response);

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

    public sealed record GitHubResponse(
        [property: JsonProperty("url")] string Url,
        [property: JsonProperty("assets_url")] string AssetsUrl,
        [property: JsonProperty("upload_url")] string UploadUrl,
        [property: JsonProperty("html_url")] string HtmlUrl,
        [property: JsonProperty("id")] int Id,
        [property: JsonProperty("author")] object Author,
        [property: JsonProperty("node_id")] string NodeId,
        [property: JsonProperty("tag_name")] string TagName,
        [property: JsonProperty("target_commitish")] string TargetCommitish,
        [property: JsonProperty("name")] string Name,
        [property: JsonProperty("draft")] bool Draft,
        [property: JsonProperty("prerelease")] bool Prerelease,
        [property: JsonProperty("created_at")] DateTime CreatedAt,
        [property: JsonProperty("published_at")] DateTime PublishedAt,
        [property: JsonProperty("assets")] IReadOnlyList<object> Assets,
        [property: JsonProperty("tarball_url")] string TarballUrl,
        [property: JsonProperty("zipball_url")] string ZipballUrl,
        [property: JsonProperty("body")] string Body,
        [property: JsonProperty("reactions")] object Reactions
    );
}
