using System.Net;
using System.Text;
using System.Text.Json;
using NUnit.Framework;
using SWLOR.DiscordBot.Configuration;
using SWLOR.DiscordBot.Core;
using SWLOR.DiscordBot.Persistence;

namespace SWLOR.DiscordBot.Tests;

[TestFixture]
public sealed class ArchiveTests
{
    private string _root = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "swlor-discord-archive-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    [Test]
    public async Task PlanningArchiveOwnershipDoesNotPublishFilesAndMatchesExportDirectory()
    {
        using var client = CreateClient(_ => throw new AssertionException("No attachments should be downloaded."));
        var archive = CreateArchive(client);
        var ticket = CreateTicket();
        var planned = archive.GetArchivePath(ticket);
        Assert.That(planned, Is.EqualTo(Path.Combine(_root, ticket.Id.ToString("N"))));
        Assert.That(Directory.Exists(planned), Is.False);
        Assert.That(await archive.ExportAsync(ticket, EmptySnapshot(), default), Is.EqualTo(planned));
    }

    [Test]
    public async Task ExportEscapesUntrustedMessageAuthorAndEmbedContentInHtml()
    {
        using var client = CreateClient(_ => throw new AssertionException("No attachment should be downloaded."));
        var ticket = CreateTicket();
        var snapshot = new TranscriptSnapshot(
        [
            new TranscriptMessage(10, 55, "<img src=x onerror=alert(1)>", "<script>alert('x')</script>",
                DateTimeOffset.UnixEpoch, [], "{\"title\":\"<svg onload=alert(2)>\"}")
        ], 10);
        var archive = CreateArchive(client);

        var directory = await archive.ExportAsync(ticket, snapshot, CancellationToken.None);
        var html = await File.ReadAllTextAsync(Path.Combine(directory, "transcript.html"));
        var json = await File.ReadAllTextAsync(Path.Combine(directory, "transcript.json"));

        Assert.Multiple(() =>
        {
            Assert.That(html, Does.Contain("&lt;script&gt;alert(&#39;x&#39;)&lt;/script&gt;"));
            Assert.That(html, Does.Contain("&lt;img src=x onerror=alert(1)&gt;"));
            Assert.That(html, Does.Contain("&lt;svg onload=alert(2)&gt;"));
            Assert.That(html, Does.Not.Contain("<script>"));
            Assert.That(html, Does.Not.Contain("<img src=x"));
            Assert.That(html, Does.Contain("default-src 'none'"));
            using var parsed = JsonDocument.Parse(json);
            Assert.That(parsed.RootElement.GetProperty("Messages")[0].GetProperty("Content").GetString(), Is.EqualTo("<script>alert('x')</script>"), "JSON preserves the original transcript text as data.");
        });
    }

    [Test]
    public async Task DeleteRejectsAnotherDirectoryNamedForTheTicketAndPreservesItsFiles()
    {
        using var client = CreateClient(_ => throw new AssertionException("No attachment should be downloaded."));
        var ticket = CreateTicket();
        var archive = CreateArchive(client);
        var managed = await archive.ExportAsync(ticket, EmptySnapshot(), CancellationToken.None);
        var outside = Path.Combine(_root, "unmanaged", ticket.Id.ToString("N"));
        Directory.CreateDirectory(outside);
        var sentinel = Path.Combine(outside, "keep.txt");
        await File.WriteAllTextAsync(sentinel, "preserve this directory");

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await archive.DeleteAsync(outside, CancellationToken.None);
        });

        Assert.Multiple(() =>
        {
            Assert.That(Directory.Exists(managed), Is.True);
            Assert.That(File.Exists(sentinel), Is.True);
        });
    }

    [Test]
    public async Task DeleteRemovesOnlyTheManagedTicketArchiveDirectory()
    {
        using var client = CreateClient(_ => throw new AssertionException("No attachment should be downloaded."));
        var archive = CreateArchive(client);
        var ticket = CreateTicket();
        var managed = await archive.ExportAsync(ticket, EmptySnapshot(), CancellationToken.None);

        await archive.DeleteAsync(managed, CancellationToken.None);

        Assert.That(Directory.Exists(managed), Is.False);
        Assert.That(Directory.Exists(_root), Is.True);
    }

    [Test]
    public async Task OversizedCdnResponseFailsBeforePublishingTranscriptFiles()
    {
        var attachmentBytes = Encoding.UTF8.GetBytes("too large");
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new UnknownLengthContent(attachmentBytes)
        });
        using var client = new HttpClient(handler);
        var configuration = CreateConfiguration(copyAttachments: true, maxAttachmentBytes: 5);
        var archive = new FileTranscriptArchive(configuration, client);
        var ticket = CreateTicket();
        var attachment = new TranscriptAttachment(77, "payload.txt", "https://cdn.discordapp.com/attachments/1/77/payload.txt", 4);
        var snapshot = new TranscriptSnapshot(
        [new TranscriptMessage(10, 55, "member", "see attachment", DateTimeOffset.UnixEpoch, [attachment])], 10);

        Assert.ThrowsAsync<InvalidDataException>(async () =>
        {
            await archive.ExportAsync(ticket, snapshot, CancellationToken.None);
        });

        var directory = Path.Combine(_root, ticket.Id.ToString("N"));
        Assert.Multiple(() =>
        {
            Assert.That(handler.RequestCount, Is.EqualTo(1));
            Assert.That(File.Exists(Path.Combine(directory, "transcript.html")), Is.False);
            Assert.That(File.Exists(Path.Combine(directory, "transcript.json")), Is.False);
            Assert.That(Directory.GetFiles(directory, "*.part", SearchOption.AllDirectories), Is.Empty);
        });
    }

    [Test]
    public async Task ExistingCachedAttachmentIsReusedWithoutAnotherCdnRequest()
    {
        using var client = CreateClient(_ => throw new AssertionException("A cached attachment must not be fetched again."));
        var ticket = CreateTicket();
        var archive = new FileTranscriptArchive(CreateConfiguration(copyAttachments: true), client);
        var attachment = new TranscriptAttachment(88, "image.png", "https://cdn.discordapp.com/attachments/1/88/image.png", 3);
        var attachmentPath = Path.Combine(_root, ticket.Id.ToString("N"), "attachments", "88.png");
        Directory.CreateDirectory(Path.GetDirectoryName(attachmentPath)!);
        await File.WriteAllBytesAsync(attachmentPath, new byte[] { 1, 2, 3 });
        var snapshot = new TranscriptSnapshot(
        [
            new TranscriptMessage(10, 55, "member", "first", DateTimeOffset.UnixEpoch, [attachment]),
            new TranscriptMessage(11, 56, "staff", "same attachment", DateTimeOffset.UnixEpoch, [attachment])
        ], 11);

        var directory = await archive.ExportAsync(ticket, snapshot, CancellationToken.None);
        var html = await File.ReadAllTextAsync(Path.Combine(directory, "transcript.html"));

        Assert.Multiple(() =>
        {
            Assert.That(html, Does.Contain("href=\"attachments/88.png\""));
            Assert.That(File.ReadAllBytes(attachmentPath), Is.EqualTo(new byte[] { 1, 2, 3 }));
        });
    }

    [Test]
    public async Task AttachmentUrlOutsideDiscordCdnIsRejectedWithoutNetworkRequest()
    {
        var handler = new FakeHttpMessageHandler(_ => throw new AssertionException("Unsafe URL must be rejected before HTTP."));
        using var client = new HttpClient(handler);
        var archive = new FileTranscriptArchive(CreateConfiguration(copyAttachments: true), client);
        var attachment = new TranscriptAttachment(99, "file.bin", "https://example.com/file.bin", 1);
        var snapshot = new TranscriptSnapshot(
        [new TranscriptMessage(10, 55, "member", "attachment", DateTimeOffset.UnixEpoch, [attachment])], 10);

        Assert.ThrowsAsync<InvalidDataException>(async () =>
        {
            await archive.ExportAsync(CreateTicket(), snapshot, CancellationToken.None);
        });

        Assert.That(handler.RequestCount, Is.Zero);
    }

    [Test]
    public async Task TruncatedCachedAttachmentStopsArchivalWithoutDeletingOrFetchingIt()
    {
        var handler = new FakeHttpMessageHandler(_ => throw new AssertionException("A truncated cache must suspend cleanup."));
        using var client = new HttpClient(handler);
        var ticket = CreateTicket();
        var archive = new FileTranscriptArchive(CreateConfiguration(copyAttachments: true), client);
        var attachment = new TranscriptAttachment(88, "image.png", "https://cdn.discordapp.com/attachments/1/88/image.png", 3);
        var attachmentPath = Path.Combine(_root, ticket.Id.ToString("N"), "attachments", "88.png");
        Directory.CreateDirectory(Path.GetDirectoryName(attachmentPath)!);
        await File.WriteAllBytesAsync(attachmentPath, new byte[] { 1 });
        var snapshot = new TranscriptSnapshot(
        [new TranscriptMessage(10, 55, "member", "attachment", DateTimeOffset.UnixEpoch, [attachment])], 10);

        Assert.ThrowsAsync<InvalidDataException>(async () => await archive.ExportAsync(ticket, snapshot, CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(handler.RequestCount, Is.Zero);
            Assert.That(File.ReadAllBytes(attachmentPath), Is.EqualTo(new byte[] { 1 }));
            Assert.That(File.Exists(Path.Combine(_root, ticket.Id.ToString("N"), "transcript.html")), Is.False);
        });
    }
    [Test]
    public async Task LinuxExportKeepsNewDirectoriesAndDownloadedFilesPrivateDuringAndAfterWriting()
    {
        if (!OperatingSystem.IsLinux()) { Assert.Ignore("Unix mode verification requires Linux."); return; }
        Directory.Delete(_root);
        var ticket = CreateTicket();
        var directory = Path.Combine(_root, ticket.Id.ToString("N"));
        var attachments = Path.Combine(directory, "attachments");
        var attachmentPath = Path.Combine(attachments, "88.png");
        var bytes = new byte[] { 1, 2, 3 };
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new InspectingContent(bytes, () =>
            {
                Assert.That(ReadUnixMode(_root), Is.EqualTo((UnixFileMode)448), "Archive root must already be 0700.");
                Assert.That(ReadUnixMode(directory), Is.EqualTo((UnixFileMode)448));
                Assert.That(ReadUnixMode(attachments), Is.EqualTo((UnixFileMode)448));
                Assert.That(ReadUnixMode(attachmentPath + ".part"), Is.EqualTo((UnixFileMode)384), "Download staging must already be 0600.");
            })
        });
        var archive = new FileTranscriptArchive(CreateConfiguration(copyAttachments: true), client);
        var attachment = new TranscriptAttachment(88, "image.png", "https://cdn.discordapp.com/attachments/1/88/image.png", bytes.Length);
        var snapshot = new TranscriptSnapshot(
        [new TranscriptMessage(10, 55, "member", "attachment", DateTimeOffset.UnixEpoch, [attachment])], 10);

        await archive.ExportAsync(ticket, snapshot, default);

        Assert.Multiple(() =>
        {
            Assert.That(ReadUnixMode(attachmentPath), Is.EqualTo((UnixFileMode)384));
            Assert.That(ReadUnixMode(Path.Combine(directory, "transcript.html")), Is.EqualTo((UnixFileMode)384));
            Assert.That(ReadUnixMode(Path.Combine(directory, "transcript.json")), Is.EqualTo((UnixFileMode)384));
            Assert.That(File.ReadAllBytes(attachmentPath), Is.EqualTo(bytes));
            Assert.That(Directory.GetFiles(directory, "*.part", SearchOption.AllDirectories), Is.Empty);
        });
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task LinuxExportRepairsExistingDirectoriesTranscriptsAndCachedAttachments(bool copyAttachments)
    {
        if (!OperatingSystem.IsLinux()) { Assert.Ignore("Unix mode verification requires Linux."); return; }
        var ticket = CreateTicket();
        var directory = Path.Combine(_root, ticket.Id.ToString("N"));
        var attachments = Path.Combine(directory, "attachments");
        Directory.CreateDirectory(attachments);
        var cached = Path.Combine(attachments, "88.png");
        var orphaned = Path.Combine(attachments, "older.bin");
        var html = Path.Combine(directory, "transcript.html");
        var json = Path.Combine(directory, "transcript.json");
        foreach (var file in new[] { cached, orphaned, html, json })
        {
            await File.WriteAllBytesAsync(file, new byte[] { 1, 2, 3 });
            File.SetUnixFileMode(file, (UnixFileMode)438); // 0666 from a permissive previous deployment.
        }
        foreach (var path in new[] { _root, directory, attachments })
            File.SetUnixFileMode(path, (UnixFileMode)511); // 0777.
        using var client = CreateClient(_ => throw new AssertionException("Existing attachments must not be downloaded."));
        var archive = new FileTranscriptArchive(CreateConfiguration(copyAttachments), client);
        var attachment = new TranscriptAttachment(88, "image.png", "https://cdn.discordapp.com/attachments/1/88/image.png", 3);
        var snapshot = new TranscriptSnapshot(
        [new TranscriptMessage(10, 55, "member", "attachment", DateTimeOffset.UnixEpoch, [attachment])], 10);

        await archive.ExportAsync(ticket, snapshot, default);

        Assert.Multiple(() =>
        {
            foreach (var path in new[] { _root, directory, attachments })
                Assert.That(ReadUnixMode(path), Is.EqualTo((UnixFileMode)448), path);
            foreach (var path in new[] { cached, orphaned, html, json })
                Assert.That(ReadUnixMode(path), Is.EqualTo((UnixFileMode)384), path);
            Assert.That(File.ReadAllBytes(cached), Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(File.ReadAllBytes(orphaned), Is.EqualTo(new byte[] { 1, 2, 3 }));
        });
    }

    [Test]
    public async Task LinuxPermissionRepairRejectsLinksWithoutChangingTheirTarget()
    {
        if (!OperatingSystem.IsLinux()) { Assert.Ignore("Unix mode verification requires Linux."); return; }
        var ticket = CreateTicket();
        var directory = Path.Combine(_root, ticket.Id.ToString("N"));
        var attachments = Path.Combine(directory, "attachments");
        Directory.CreateDirectory(attachments);
        var outside = Path.Combine(_root, "unmanaged.txt");
        await File.WriteAllTextAsync(outside, "preserve");
        File.SetUnixFileMode(outside, (UnixFileMode)420); // 0644.
        var linked = Path.Combine(attachments, "88.png");
        File.CreateSymbolicLink(linked, outside);
        using var client = CreateClient(_ => throw new AssertionException("Links must fail before HTTP."));
        var archive = new FileTranscriptArchive(CreateConfiguration(copyAttachments: true), client);

        Assert.ThrowsAsync<InvalidOperationException>(async () => await archive.ExportAsync(ticket, EmptySnapshot(), default));

        Assert.That(ReadUnixMode(outside), Is.EqualTo((UnixFileMode)420));
        Assert.That(await File.ReadAllTextAsync(outside), Is.EqualTo("preserve"));
        File.Delete(linked);
    }
    [Test]
    public async Task AttachmentCopyReportsForwardProgressBeforePublishingAndPreservesBytes()
    {
        var payload = Enumerable.Range(0, 200000).Select(index => (byte)(index % 251)).ToArray();
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(payload) });
        var archive = new FileTranscriptArchive(CreateConfiguration(copyAttachments: true, maxAttachmentBytes: payload.Length), client);
        var ticket = CreateTicket();
        var directory = archive.GetArchivePath(ticket);
        var target = Path.Combine(directory, "attachments", "88.bin");
        var observedPartialLengths = new List<long>();
        var completedTranscriptWasReported = false;
        var attachment = new TranscriptAttachment(88, "payload.bin", "https://cdn.discordapp.com/attachments/1/88/payload.bin", payload.Length);
        var snapshot = new TranscriptSnapshot(
        [new TranscriptMessage(10, 55, "member", "attachment", DateTimeOffset.UnixEpoch, [attachment])], 10);

        await archive.ExportAsync(ticket, snapshot, default, () =>
        {
            if (File.Exists(target + ".part")) observedPartialLengths.Add(new FileInfo(target + ".part").Length);
            completedTranscriptWasReported |= File.Exists(Path.Combine(directory, "transcript.html"));
        });

        Assert.That(observedPartialLengths.Any(length => length > 0 && length < payload.Length), Is.True,
            "The watchdog needs progress while a multi-chunk attachment is still copying.");
        Assert.That(completedTranscriptWasReported, Is.True);
        Assert.That(await File.ReadAllBytesAsync(target), Is.EqualTo(payload));
    }

    [Test]
    public void CancellationDuringReportedAttachmentProgressRemovesPartialFile()
    {
        var payload = new byte[200000];
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(payload) });
        var archive = new FileTranscriptArchive(CreateConfiguration(copyAttachments: true, maxAttachmentBytes: payload.Length), client);
        var ticket = CreateTicket();
        var directory = archive.GetArchivePath(ticket);
        var target = Path.Combine(directory, "attachments", "88.bin");
        var attachment = new TranscriptAttachment(88, "payload.bin", "https://cdn.discordapp.com/attachments/1/88/payload.bin", payload.Length);
        var snapshot = new TranscriptSnapshot(
        [new TranscriptMessage(10, 55, "member", "attachment", DateTimeOffset.UnixEpoch, [attachment])], 10);
        using var cancellation = new CancellationTokenSource();

        Assert.CatchAsync<OperationCanceledException>(() => archive.ExportAsync(ticket, snapshot, cancellation.Token, () =>
        {
            if (File.Exists(target + ".part") && new FileInfo(target + ".part").Length > 0) cancellation.Cancel();
        }));

        Assert.That(File.Exists(target), Is.False);
        Assert.That(Directory.GetFiles(directory, "*.part", SearchOption.AllDirectories), Is.Empty);
        Assert.That(File.Exists(Path.Combine(directory, "transcript.html")), Is.False);
    }
    [TestCase(5L)]
    [TestCase(6L)]
    public async Task AggregateAttachmentBudgetAllowsExactAndUnderLimitSnapshots(long budget)
    {
        var handler = new FakeHttpMessageHandler(request => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(new byte[request.RequestUri!.AbsolutePath.Contains("/88/") ? 3 : 2])
        });
        using var client = new HttpClient(handler);
        var archive = new FileTranscriptArchive(CreateConfiguration(true, 4, budget), client);
        var directory = await archive.ExportAsync(CreateTicket(), Snapshot(Attachment(88, 3), Attachment(89, 2)), default);
        Assert.That(handler.RequestCount, Is.EqualTo(2));
        Assert.That(Directory.GetFiles(Path.Combine(directory, "attachments")).Sum(file => new FileInfo(file).Length), Is.EqualTo(5));
        Assert.That(File.Exists(Path.Combine(directory, "transcript.html")), Is.True);
    }

    [Test]
    public void SnapshotOverAggregateBudgetFailsBeforeAnyDownload()
    {
        var handler = new FakeHttpMessageHandler(_ => throw new AssertionException("Preflight must reject the complete snapshot before HTTP."));
        using var client = new HttpClient(handler);
        var archive = new FileTranscriptArchive(CreateConfiguration(true, 4, 5), client);
        var ticket = CreateTicket();
        Assert.ThrowsAsync<InvalidDataException>(() => archive.ExportAsync(ticket, Snapshot(Attachment(88, 3), Attachment(89, 3)), default));
        Assert.That(handler.RequestCount, Is.Zero);
        Assert.That(Directory.GetFiles(archive.GetArchivePath(ticket), "*", SearchOption.AllDirectories), Is.Empty);
    }

    [Test]
    public async Task DuplicateAttachmentIdsCountOnceWhenOnlySignedUrlQueriesDiffer()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(new byte[3]) });
        using var client = new HttpClient(handler);
        var archive = new FileTranscriptArchive(CreateConfiguration(true, 3, 3), client);
        var attachment = Attachment(88, 3);
        var snapshot = Snapshot(attachment, attachment with { Url = attachment.Url + "?expires=refreshed" });
        var directory = await archive.ExportAsync(CreateTicket(), snapshot, default);
        Assert.That(handler.RequestCount, Is.EqualTo(1));
        Assert.That(Directory.GetFiles(Path.Combine(directory, "attachments")), Has.Length.EqualTo(1));
    }

    [TestCase("size")]
    [TestCase("filename")]
    [TestCase("resource")]
    public void ConflictingDuplicateAttachmentMetadataFailsBeforeDownloads(string conflict)
    {
        var handler = new FakeHttpMessageHandler(_ => throw new AssertionException("Conflicting IDs must fail before HTTP."));
        using var client = new HttpClient(handler);
        var archive = new FileTranscriptArchive(CreateConfiguration(true, 10, 10), client);
        var original = Attachment(88, 2);
        var duplicate = conflict switch
        {
            "size" => original with { Size = 3 },
            "filename" => original with { FileName = "other.bin" },
            _ => original with { Url = "https://cdn.discordapp.com/attachments/1/88/other.bin" }
        };
        Assert.ThrowsAsync<InvalidDataException>(() => archive.ExportAsync(CreateTicket(), Snapshot(original, duplicate), default));
        Assert.That(handler.RequestCount, Is.Zero);
    }

    [Test]
    public void NegativeAndOverflowingSnapshotSizesCannotBypassPreflight()
    {
        var handler = new FakeHttpMessageHandler(_ => throw new AssertionException("Invalid totals must fail before HTTP."));
        using var client = new HttpClient(handler);
        var archive = new FileTranscriptArchive(CreateConfiguration(true, long.MaxValue, long.MaxValue), client);
        Assert.ThrowsAsync<InvalidDataException>(() => archive.ExportAsync(CreateTicket(), Snapshot(Attachment(88, -1)), default));
        Assert.ThrowsAsync<InvalidDataException>(() => archive.ExportAsync(CreateTicket(), Snapshot(Attachment(88, long.MaxValue), Attachment(89, 1)), default));
        Assert.That(handler.RequestCount, Is.Zero);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task IncorrectMetadataCannotExceedRemainingStreamingBudgetAndPartialFilesAreRemoved(bool knownContentLength)
    {
        var handler = new FakeHttpMessageHandler(request =>
        {
            var bytes = new byte[request.RequestUri!.AbsolutePath.Contains("/88/") ? 2 : 4];
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = knownContentLength ? new ByteArrayContent(bytes) : new UnknownLengthContent(bytes)
            };
        });
        using var client = new HttpClient(handler);
        var archive = new FileTranscriptArchive(CreateConfiguration(true, 10, 5), client);
        var ticket = CreateTicket();
        Assert.ThrowsAsync<InvalidDataException>(() => archive.ExportAsync(ticket, Snapshot(Attachment(88, 2), Attachment(89, 1)), default));
        var directory = archive.GetArchivePath(ticket);
        Assert.That(handler.RequestCount, Is.EqualTo(2));
        Assert.That(new FileInfo(Path.Combine(directory, "attachments", "88.bin")).Length, Is.EqualTo(2));
        Assert.That(File.Exists(Path.Combine(directory, "attachments", "89.bin")), Is.False);
        Assert.That(Directory.GetFiles(directory, "*.part", SearchOption.AllDirectories), Is.Empty);
        Assert.That(File.Exists(Path.Combine(directory, "transcript.html")), Is.False);
        await archive.ExportAsync(ticket, Snapshot(Attachment(88, 2)), default);
        Assert.That(handler.RequestCount, Is.EqualTo(2), "a retry reuses the successfully cached attachment");
    }

    [Test]
    public async Task ChangedSnapshotsCannotGrowRetainedAttachmentsBeyondTicketBudget()
    {
        var handler = new FakeHttpMessageHandler(request => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(new byte[request.RequestUri!.AbsolutePath.Contains("/88/") ? 3 : 2])
        });
        using var client = new HttpClient(handler);
        var archive = new FileTranscriptArchive(CreateConfiguration(true, 4, 5), client);
        var ticket = CreateTicket();
        var directory = await archive.ExportAsync(ticket, Snapshot(Attachment(88, 3)), default);
        await archive.ExportAsync(ticket, Snapshot(Attachment(89, 2)), default);
        var published = await File.ReadAllTextAsync(Path.Combine(directory, "transcript.json"));
        Assert.ThrowsAsync<InvalidDataException>(() => archive.ExportAsync(ticket, Snapshot(Attachment(90, 1)), default));
        Assert.That(handler.RequestCount, Is.EqualTo(2));
        Assert.That(await File.ReadAllTextAsync(Path.Combine(directory, "transcript.json")), Is.EqualTo(published));
        Assert.That(Directory.GetFiles(Path.Combine(directory, "attachments")).Sum(file => new FileInfo(file).Length), Is.EqualTo(5));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ExactBudgetCachedAttachmentIsNotDoubleCountedAndStalePartialIsDiscarded(bool partExtension)
    {
        var handler = new FakeHttpMessageHandler(_ => throw new AssertionException("Exact-budget cache must be reused."));
        using var client = new HttpClient(handler);
        var archive = new FileTranscriptArchive(CreateConfiguration(true, 3, 3), client);
        var ticket = CreateTicket();
        var directory = Path.Combine(archive.GetArchivePath(ticket), "attachments");
        Directory.CreateDirectory(directory);
        var cached = partExtension ? "88.part" : "88.bin";
        var attachment = Attachment(88, 3) with { FileName = partExtension ? "payload.part" : "payload.bin" };
        await File.WriteAllBytesAsync(Path.Combine(directory, cached), new byte[3]);
        await File.WriteAllBytesAsync(Path.Combine(directory, "99.bin.part"), new byte[10]);
        await archive.ExportAsync(ticket, Snapshot(attachment), default);
        await archive.ExportAsync(ticket, Snapshot(attachment), default);
        Assert.That(handler.RequestCount, Is.Zero);
        Assert.That(File.Exists(Path.Combine(directory, "99.bin.part")), Is.False);
        Assert.That(File.Exists(Path.Combine(directory, cached)), Is.True);
        Assert.That(Directory.GetFiles(directory).Sum(file => new FileInfo(file).Length), Is.EqualTo(3));
    }

    [Test]
    public async Task StaleRetainedFilesAreCountedEvenWhenSnapshotMetadataFits()
    {
        var handler = new FakeHttpMessageHandler(_ => throw new AssertionException("Retained files must be included before HTTP."));
        using var client = new HttpClient(handler);
        var archive = new FileTranscriptArchive(CreateConfiguration(true, 4, 4), client);
        var ticket = CreateTicket();
        var directory = Path.Combine(archive.GetArchivePath(ticket), "attachments");
        Directory.CreateDirectory(directory);
        await File.WriteAllBytesAsync(Path.Combine(directory, "88.bin"), new byte[3]);
        await File.WriteAllBytesAsync(Path.Combine(directory, "orphaned.bin"), new byte[2]);
        Assert.ThrowsAsync<InvalidDataException>(() => archive.ExportAsync(ticket, Snapshot(Attachment(88, 3)), default));
        Assert.That(handler.RequestCount, Is.Zero);
        Assert.That(new FileInfo(Path.Combine(directory, "orphaned.bin")).Length, Is.EqualTo(2));
    }

    private static TranscriptAttachment Attachment(ulong id, long size) =>
        new(id, "payload.bin", $"https://cdn.discordapp.com/attachments/1/{id}/payload.bin", size);

    private static TranscriptSnapshot Snapshot(params TranscriptAttachment[] attachments) => new(
        [new TranscriptMessage(10, 55, "member", "attachments", DateTimeOffset.UnixEpoch, attachments)], 10);

    [Test]
    public async Task TranscriptBudgetRejectsCombinedHistoryAndPreservesExistingArchive()
    {
        using var client = CreateClient(_ => throw new AssertionException("No attachment should be downloaded."));
        var config = CreateConfiguration(copyAttachments: false);
        config.Tickets.MaxTranscriptContentBytes = 1024;
        var archive = new FileTranscriptArchive(config, client);
        var ticket = CreateTicket();
        var message = new TranscriptMessage(10, 55, "member", new string('x', 200), DateTimeOffset.UnixEpoch, []);
        var directory = await archive.ExportAsync(ticket, new([message], 10), default);
        var previousJson = await File.ReadAllBytesAsync(Path.Combine(directory, "transcript.json"));
        var previousHtml = await File.ReadAllBytesAsync(Path.Combine(directory, "transcript.html"));

        Assert.ThrowsAsync<InvalidDataException>(() =>
            archive.ExportAsync(ticket, new([message, message with { Id = 11 }], 11), default));

        Assert.That(await File.ReadAllBytesAsync(Path.Combine(directory, "transcript.json")), Is.EqualTo(previousJson));
        Assert.That(await File.ReadAllBytesAsync(Path.Combine(directory, "transcript.html")), Is.EqualTo(previousHtml));
        Assert.That(Directory.GetFiles(directory, "*.part", SearchOption.AllDirectories), Is.Empty);
    }

    [TestCase("author")]
    [TestCase("embeds")]
    [TestCase("attachment-metadata")]
    public void TranscriptBudgetIncludesAllRetainedTextBeforeAnyFilesOrDownloads(string field)
    {
        using var client = CreateClient(_ => throw new AssertionException("Preflight must reject text before HTTP."));
        var config = CreateConfiguration(copyAttachments: true);
        config.Tickets.MaxTranscriptContentBytes = 1024;
        var archive = new FileTranscriptArchive(config, client);
        var ticket = CreateTicket();
        var message = new TranscriptMessage(10, 55, "member", "text", DateTimeOffset.UnixEpoch, []);
        message = field switch
        {
            "author" => message with { AuthorName = new string('a', 800) },
            "embeds" => message with { EmbedsJson = new string('e', 800) },
            _ => message with { Attachments = [new(88, new string('f', 800), "https://cdn.discordapp.com/a", 1)] }
        };

        Assert.ThrowsAsync<InvalidDataException>(() => archive.ExportAsync(ticket, new([message], 10), default));
        Assert.That(Directory.Exists(archive.GetArchivePath(ticket)), Is.False);
    }

    [Test]
    public async Task TranscriptBudgetCountsUtf8BytesAndBoundsEmptyMessageObjects()
    {
        using var client = CreateClient(_ => throw new AssertionException("No attachment should be downloaded."));
        var config = CreateConfiguration(copyAttachments: false);
        config.Tickets.MaxTranscriptContentBytes = 1024;
        var archive = new FileTranscriptArchive(config, client);
        var ticket = CreateTicket();
        var message = new TranscriptMessage(10, 55, "member", new string('a', 300), DateTimeOffset.UnixEpoch, []);
        await archive.ExportAsync(ticket, new([message], 10), default);
        Assert.ThrowsAsync<InvalidDataException>(() =>
            archive.ExportAsync(ticket, new([message with { Content = new string('é', 300) }], 10), default));

        config.Tickets.MaxTranscriptContentBytes = 4096;
        var emptyMessages = Enumerable.Range(1, 100).Select(id =>
            new TranscriptMessage((ulong)id, 55, "", "", DateTimeOffset.UnixEpoch, [])).ToArray();
        Assert.ThrowsAsync<InvalidDataException>(() => archive.ExportAsync(ticket, new(emptyMessages, 100), default));
    }

    [Test]
    public async Task StreamedHtmlPreservesUnicodeAtChunkBoundaryAndEscapesEntireContent()
    {
        using var client = CreateClient(_ => throw new AssertionException("No attachment should be downloaded."));
        var archive = CreateArchive(client);
        var content = new string('a', 8191) + "🚀<script>alert('x')</script>" + new string('&', 17000);
        var message = new TranscriptMessage(10, 55, "member", content, DateTimeOffset.UnixEpoch, []);
        var directory = await archive.ExportAsync(CreateTicket(), new([message], 10), default);
        var html = await File.ReadAllTextAsync(Path.Combine(directory, "transcript.html"));
        Assert.That(html, Does.Contain(WebUtility.HtmlEncode(content)));
        using var json = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(directory, "transcript.json")));
        Assert.That(json.RootElement.GetProperty("Messages")[0].GetProperty("Content").GetString(), Is.EqualTo(content));
    }

    [Test]
    public async Task CancellationDuringHtmlStreamingKeepsPublishedHtmlAndRemovesPartialFiles()
    {
        using var client = CreateClient(_ => throw new AssertionException("No attachment should be downloaded."));
        var archive = CreateArchive(client);
        var ticket = CreateTicket();
        var directory = await archive.ExportAsync(ticket, EmptySnapshot(), default);
        var htmlPath = Path.Combine(directory, "transcript.html");
        var original = await File.ReadAllBytesAsync(htmlPath);
        var message = new TranscriptMessage(10, 55, "member", new string('&', 200000), DateTimeOffset.UnixEpoch, []);
        using var cancellation = new CancellationTokenSource();
        Assert.CatchAsync<OperationCanceledException>(() => archive.ExportAsync(ticket, new([message], 10),
            cancellation.Token, () =>
            {
                if (File.Exists(htmlPath + ".part") && new FileInfo(htmlPath + ".part").Length > 0) cancellation.Cancel();
            }));
        Assert.That(await File.ReadAllBytesAsync(htmlPath), Is.EqualTo(original));
        Assert.That(Directory.GetFiles(directory, "*.part", SearchOption.AllDirectories), Is.Empty);
    }

    private static UnixFileMode ReadUnixMode(string path)
    {
        if (OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Unix mode verification requires Unix.");
        return File.GetUnixFileMode(path);
    }
    private FileTranscriptArchive CreateArchive(HttpClient client) =>
        new(CreateConfiguration(copyAttachments: false), client);

    private BotConfiguration CreateConfiguration(bool copyAttachments, long maxAttachmentBytes = 1024, long maxTicketAttachmentBytes = 1073741824) => new()
    {
        Tickets = new TicketOptions
        {
            ArchiveDirectory = _root,
            CopyAttachments = copyAttachments,
            MaxAttachmentBytes = maxAttachmentBytes,
            MaxTicketAttachmentBytes = maxTicketAttachmentBytes
        }
    };

    private static Ticket CreateTicket() => new(Guid.NewGuid(), "support", 42, 1234,
        TicketState.Closed, 1, DateTimeOffset.UnixEpoch);

    private static TranscriptSnapshot EmptySnapshot() => new([], null);

    private static HttpClient CreateClient(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) =>
        new(new FakeHttpMessageHandler(responseFactory));

    private sealed class InspectingContent(byte[] bytes, Action inspect) : HttpContent
    {
        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            inspect();
            await stream.WriteAsync(bytes);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = bytes.Length;
            return true;
        }
    }
    private sealed class UnknownLengthContent(byte[] bytes) : HttpContent
    {
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            stream.WriteAsync(bytes, 0, bytes.Length);

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }
    private sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            var response = responseFactory(request);
            response.RequestMessage ??= request;
            return Task.FromResult(response);
        }
    }
}
