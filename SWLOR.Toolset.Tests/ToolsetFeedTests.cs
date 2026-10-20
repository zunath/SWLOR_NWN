using System.Net;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Distribution;
using SWLOR.Toolset.Distribution;

namespace SWLOR.Toolset.Tests;

public class ToolsetFeedTests
{
    [TestCase("1.1.0-beta", "https://github.com/zunath/SWLOR_NWN/releases/download/toolset-v1.1.0-beta/toolset-feed.json")]
    [TestCase("1.0.0", "https://github.com/zunath/SWLOR_NWN/releases/download/toolset-v1.0.0/toolset-feed.json")]
    [TestCase(null, ToolsetFeed.DefaultUrl)]
    public async Task WorkspaceFeedMatchesTheInstalledRelease(string? version, string expected)
    {
        var directory = Directory.CreateTempSubdirectory("swlor-release-").FullName;
        try
        {
            if (version != null)
                File.WriteAllText(Path.Combine(directory, ToolsetFeed.ReleaseFileName), JsonSerializer.Serialize(new { Version = version }));
            using var client = new HttpClient(new FeedHandler(expected));
            var download = new WorkspaceDownload(client, ToolsetFeed.ReadInstalledVersion(directory));
            var feed = await download.ReadFeedAsync(CancellationToken.None);
            feed.ContentRevision.Should().Be(new string('1', 40));
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    [Test]
    public void DefaultFeedIsTheFixedToolsetReleaseAndNeverGithubLatest()
    {
        ToolsetFeed.DefaultUrl.Should().Be("https://github.com/zunath/SWLOR_NWN/releases/download/toolset-latest/toolset-feed.json");
        ToolsetFeed.DefaultUrl.Should().NotContain("/releases/latest");
    }

    [TestCase("../other")]
    [TestCase("")]
    [TestCase("https://example.com")]
    public void InvalidInstalledVersionsCannotSelectAnotherDownloadSource(string version)
    {
        Action action = () => ToolsetFeed.UrlForVersion(version);
        action.Should().Throw<InvalidDataException>();
    }

    [TestCase(null, false)]
    [TestCase("1.0.0", false)]
    [TestCase("1.1.0-beta", true)]
    [TestCase("1.1.0-beta.2", true)]
    public void GithubUpdateSourceFollowsTheInstalledVersionChannel(string? version, bool prerelease)
    {
        var source = ToolsetUpdateSource.Create(version);
        source.Prerelease.Should().Be(prerelease);
    }

    private sealed class FeedHandler(string expected) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            request.RequestUri!.AbsoluteUri.Should().Be(expected);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new ToolsetFeed
                {
                    ContentRevision = new string('1', 40), AssetRevision = new string('2', 40)
                }))
            });
        }
    }
}
