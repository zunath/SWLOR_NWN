using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Distribution;

namespace SWLOR.Toolset.Tests;

public class WorkspaceDownloadTests
{
    private string _root = null!;
    private const string Revision = "2222222222222222222222222222222222222222";
    private const string Release = "https://github.com/zunath/SWLOR_NWN/releases/download/test/";

    [SetUp] public void SetUp() { _root = Path.Combine(Path.GetTempPath(), "swlor_download_" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(_root); }
    [TearDown] public void TearDown() => Directory.Delete(_root, recursive: true);

    [Test]
    public async Task DownloadSeparatesReusableAssetsFromEditableWorkspaces()
    {
        var (feed, payloads) = CreateFeed();
        using var client = new HttpClient(new DownloadHandler(payloads));
        var download = new WorkspaceDownload(client);
        var cache = Path.Combine(_root, "cache");
        var first = Path.Combine(_root, "first");
        await download.CreateAsync(feed, first, cache, null, CancellationToken.None);
        File.WriteAllText(Path.Combine(first, "Module/utc/example.utc.json"), "local edit");
        payloads.Remove(Release + "assets"); // The second workspace must reuse the installed cache.
        await download.CreateAsync(feed, Path.Combine(_root, "second"), cache, null, CancellationToken.None);
        File.ReadAllText(Path.Combine(first, "Module/utc/example.utc.json")).Should().Be("local edit");
        File.Exists(Path.Combine(first, "hak/sw_2da.hak")).Should().BeFalse();
        File.Exists(Path.Combine(cache, Revision, "hak/sw_2da.hak")).Should().BeTrue();
    }

    [Test]
    public async Task FailedChecksumLeavesNoInstalledPackOrWorkspace()
    {
        var bytes = new byte[] { 1, 2, 3 };
        using var client = new HttpClient(new DownloadHandler(new() { [Release + "bad"] = bytes }));
        var descriptor = Descriptor(Release + "bad", bytes);
        descriptor.Parts[0] = descriptor.Parts[0] with { Sha256 = new string('A', 64) };
        var download = new WorkspaceDownload(client);
        var feed = new ToolsetFeed { ContentRevision = Revision, AssetRevision = Revision, Assets = descriptor };
        Func<Task> action = () => download.CreateAsync(feed, Path.Combine(_root, "workspace"), Path.Combine(_root, "cache"), null, CancellationToken.None);
        await action.Should().ThrowAsync<InvalidDataException>().WithMessage("*checksum*");
        Directory.Exists(Path.Combine(_root, "cache", Revision)).Should().BeFalse();
        Directory.EnumerateDirectories(Path.Combine(_root, "cache")).Should().BeEmpty();
    }

    [Test]
    public async Task ExistingWorkspaceIsNeverReplaced()
    {
        var existing = Path.Combine(_root, "workspace");
        Directory.CreateDirectory(existing);
        File.WriteAllText(Path.Combine(existing, "keep.txt"), "keep");
        using var client = new HttpClient();
        Func<Task> action = () => new WorkspaceDownload(client).CreateAsync(new ToolsetFeed { ContentRevision = Revision, AssetRevision = Revision }, existing, _root, null, CancellationToken.None);
        await action.Should().ThrowAsync<IOException>();
        File.ReadAllText(Path.Combine(existing, "keep.txt")).Should().Be("keep");
    }

    [TestCase("http://github.com/zunath/SWLOR_NWN/releases/download/tag/file")]
    [TestCase("https://example.com/archive")]
    [TestCase("https://github.com/someone/another/releases/download/tag/file")]
    [TestCase("file:///C:/private")]
    public void OnlyOfficialHttpsReleaseDownloadsAreAccepted(string url)
    {
        Action action = () => WorkspaceDownload.ValidateDescriptor(Descriptor(url, new byte[] { 1 }));
        action.Should().Throw<InvalidDataException>();
    }


    [TestCase("../escape.txt")]
    [TestCase("hak/../escape.txt")]
    [TestCase("tool.exe")]
    public async Task AssetArchivesCannotWriteOutsideTheirDeclaredContent(string entry)
    {
        var payload = Zip(new() { [entry] = new byte[] { 1 } });
        using var client = new HttpClient(new DownloadHandler(new() { [Release + "assets"] = payload }));
        var feed = new ToolsetFeed { ContentRevision = Revision, AssetRevision = Revision, Assets = Descriptor(Release + "assets", payload) };
        Func<Task> action = () => new WorkspaceDownload(client).CreateAsync(feed, Path.Combine(_root, "workspace"), Path.Combine(_root, "cache"), null, CancellationToken.None);
        await action.Should().ThrowAsync<InvalidDataException>();
        Directory.EnumerateFiles(_root, "escape.txt", SearchOption.AllDirectories).Should().BeEmpty();
        Directory.EnumerateDirectories(Path.Combine(_root, "cache")).Should().BeEmpty();
    }

    [Test]
    public async Task WrongRevisionIsRejectedBeforePublishingAnAssetCacheDirectory()
    {
        var files = new Dictionary<string, byte[]> { ["tlk/sw_tlk.tlk"] = new byte[] { 1 }, ["tlk/sw_tlk.tlk.json"] = Encoding.UTF8.GetBytes("{}") };
        var manifest = new AssetManifest { AssetRevision = new string('9', 40), Files = files.ToDictionary(pair => pair.Key, pair => ContentPaths.Hash(pair.Value)) };
        files[AssetManifest.FileName] = JsonSerializer.SerializeToUtf8Bytes(manifest);
        var payload = Zip(files);
        using var client = new HttpClient(new DownloadHandler(new() { [Release + "assets"] = payload }));
        var feed = new ToolsetFeed { ContentRevision = Revision, AssetRevision = Revision, Assets = Descriptor(Release + "assets", payload) };
        Func<Task> action = () => new WorkspaceDownload(client).CreateAsync(feed, Path.Combine(_root, "workspace"), Path.Combine(_root, "cache"), null, CancellationToken.None);
        await action.Should().ThrowAsync<InvalidDataException>().WithMessage("*does not match*");
        Directory.EnumerateDirectories(Path.Combine(_root, "cache")).Should().BeEmpty();
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public async Task DamagedAssetCacheCanBeReinstalledWithoutChangingAnExistingWorkspace(int damage)
    {
        var (feed, payloads) = CreateFeed();
        using var client = new HttpClient(new DownloadHandler(payloads));
        var download = new WorkspaceDownload(client);
        var cache = Path.Combine(_root, "cache");
        var first = Path.Combine(_root, "first");
        await download.CreateAsync(feed, first, cache, null, CancellationToken.None);
        var editable = Path.Combine(first, "Module/utc/example.utc.json");
        File.WriteAllText(editable, "local edit");
        var hak = Path.Combine(cache, Revision, "hak/sw_2da.hak");
        if (damage == 1) File.Delete(hak);
        else if (damage == 2) File.WriteAllText(Path.Combine(cache, Revision, AssetManifest.FileName), "{");
        else if (damage == 3) File.WriteAllText(Path.Combine(cache, Revision, "hak", "unlisted.hak"), "undeclared");
        else File.WriteAllText(hak, "corrupt");
        await download.CreateAsync(feed, Path.Combine(_root, "second"), cache, null, CancellationToken.None);
        File.ReadAllText(hak).Should().Be("archive");
        File.Exists(Path.Combine(cache, Revision, "hak", "unlisted.hak")).Should().BeFalse();
        File.ReadAllText(editable).Should().Be("local edit");
        Directory.EnumerateDirectories(cache).Select(Path.GetFileName).Should().Equal(Revision);
    }

    [Test]
    public async Task ConcurrentWorkspaceDownloadsShareOneVerifiedAssetCache()
    {
        var (feed, payloads) = CreateFeed();
        using var client = new HttpClient(new DownloadHandler(payloads, synchronizeAssets: true));
        var download = new WorkspaceDownload(client);
        var cache = Path.Combine(_root, "cache");
        await Task.WhenAll(
            download.CreateAsync(feed, Path.Combine(_root, "first"), cache, null, CancellationToken.None),
            download.CreateAsync(feed, Path.Combine(_root, "second"), cache, null, CancellationToken.None));
        AssetManifest.Load(Path.Combine(cache, Revision), Revision, verifyFiles: true);
        Directory.EnumerateDirectories(cache).Select(Path.GetFileName).Should().Equal(Revision);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task CatalogChecksumsAreRequiredBeforePublishingDownloadedWorkspaces(bool omitChecksum)
    {
        var (feed, payloads) = CreateFeed();
        using (var stream = new MemoryStream(payloads[Release + "workspace"]))
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
        {
            var files = new Dictionary<string, byte[]>();
            foreach (var entry in archive.Entries)
            {
                using var input = entry.Open();
                using var output = new MemoryStream();
                input.CopyTo(output);
                files[entry.FullName] = output.ToArray();
            }
            if (omitChecksum)
            {
                var manifest = JsonSerializer.Deserialize<WorkspaceManifest>(files[WorkspaceManifest.FileName])!;
                manifest.MetadataHashes.Clear();
                files[WorkspaceManifest.FileName] = JsonSerializer.SerializeToUtf8Bytes(manifest);
            }
            else files[WorkspaceManifest.CatalogPath] = Encoding.UTF8.GetBytes("changed catalog");
            payloads[Release + "workspace"] = Zip(files);
            feed.Workspace = Descriptor(Release + "workspace", payloads[Release + "workspace"]);
        }
        using var client = new HttpClient(new DownloadHandler(payloads));
        var destination = Path.Combine(_root, "workspace");
        Func<Task> download = () => new WorkspaceDownload(client).CreateAsync(feed, destination, Path.Combine(_root, "cache"), null, CancellationToken.None);
        await download.Should().ThrowAsync<InvalidDataException>().WithMessage("*checksum*");
        Directory.Exists(destination).Should().BeFalse();
    }

    private static (ToolsetFeed, Dictionary<string, byte[]>) CreateFeed()
    {
        var assetFiles = new Dictionary<string, byte[]> { ["hak/sw_2da.hak"] = Encoding.UTF8.GetBytes("archive"),
            ["tlk/sw_tlk.tlk.json"] = Encoding.UTF8.GetBytes("{}"), ["tlk/sw_tlk.tlk"] = new byte[] { 1 } };
        var assets = new AssetManifest { AssetRevision = Revision, Files = assetFiles.ToDictionary(pair => pair.Key, pair => ContentPaths.Hash(pair.Value)) };
        assetFiles[AssetManifest.FileName] = JsonSerializer.SerializeToUtf8Bytes(assets);
        var content = new Dictionary<string, byte[]> { ["Module/utc/example.utc.json"] = Encoding.UTF8.GetBytes("{ }\r\n") };
        var workspace = new WorkspaceManifest { ContentRevision = Revision, AssetRevision = Revision,
            Baseline = content.ToDictionary(pair => pair.Key, pair => ContentPaths.Hash(pair.Value)) };
        var catalog = JsonSerializer.SerializeToUtf8Bytes(new SWLOR.Toolset.Domain.GameData.GameCode.GameCodeCatalog
        {
            ContentRevision = Revision, QuestIds = new() { "quest" }, SpawnTableIds = new() { "spawn" }
        });
        workspace.MetadataHashes[WorkspaceManifest.CatalogPath] = ContentPaths.Hash(catalog);
        content[WorkspaceManifest.CatalogPath] = catalog;
        content[WorkspaceManifest.FileName] = JsonSerializer.SerializeToUtf8Bytes(workspace);
        var payloads = new Dictionary<string, byte[]> { [Release + "assets"] = Zip(assetFiles), [Release + "workspace"] = Zip(content) };
        var feed = new ToolsetFeed { ContentRevision = Revision, AssetRevision = Revision,
            Assets = Descriptor(Release + "assets", payloads[Release + "assets"]), Workspace = Descriptor(Release + "workspace", payloads[Release + "workspace"]) };
        return (feed, payloads);
    }

    private static DownloadArchive Descriptor(string url, byte[] bytes) => new() { Sha256 = ContentPaths.Hash(bytes),
        Parts = new() { new DownloadPart(url, ContentPaths.Hash(bytes), bytes.Length) } };
    private static byte[] Zip(Dictionary<string, byte[]> files)
    {
        using var bytes = new MemoryStream();
        using (var zip = new ZipArchive(bytes, ZipArchiveMode.Create, leaveOpen: true))
            foreach (var (name, payload) in files) { using var output = zip.CreateEntry(name).Open(); output.Write(payload); }
        return bytes.ToArray();
    }
    private sealed class DownloadHandler(Dictionary<string, byte[]> payloads, bool synchronizeAssets = false) : HttpMessageHandler
    {
        private readonly TaskCompletionSource _bothAssetRequests = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _assetRequests;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            if (synchronizeAssets && request.RequestUri!.ToString() == Release + "assets")
            {
                if (Interlocked.Increment(ref _assetRequests) == 2) _bothAssetRequests.TrySetResult();
                await _bothAssetRequests.Task.WaitAsync(TimeSpan.FromSeconds(10), token);
            }
            return payloads.TryGetValue(request.RequestUri!.ToString(), out var bytes)
                ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) }
                : new HttpResponseMessage(HttpStatusCode.NotFound);
        }
    }
}
