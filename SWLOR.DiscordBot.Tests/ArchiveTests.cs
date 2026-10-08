using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
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
    public async Task PlanningArchiveOwnershipDoesNotPublishFilesAndContainsExportSnapshot()
    {
        using var client = CreateClient(_ => throw new AssertionException("No attachments should be downloaded."));
        var archive = CreateArchive(client);
        var ticket = CreateTicket();
        var planned = archive.GetArchivePath(ticket);
        Assert.That(planned, Is.EqualTo(Path.Combine(_root, ticket.Id.ToString("N"))));
        Assert.That(Directory.Exists(planned), Is.False);
        var snapshot = await archive.ExportAsync(ticket, EmptySnapshot(), default);
        Assert.That(Path.GetDirectoryName(snapshot), Is.EqualTo(Path.Combine(planned, "snapshots")));
        Assert.That(File.Exists(Path.Combine(snapshot, "transcript.html")), Is.True);
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

        await archive.DeleteAsync(archive.GetArchivePath(ticket), CancellationToken.None);

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
            Assert.That(html, Does.Contain("href=\"../../attachments/88.png\""));
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

        var exported = await archive.ExportAsync(ticket, snapshot, default);

        Assert.Multiple(() =>
        {
            Assert.That(ReadUnixMode(attachmentPath), Is.EqualTo((UnixFileMode)384));
            Assert.That(ReadUnixMode(Path.Combine(exported, "transcript.html")), Is.EqualTo((UnixFileMode)384));
            Assert.That(ReadUnixMode(Path.Combine(exported, "transcript.json")), Is.EqualTo((UnixFileMode)384));
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
    public async Task PermissionRepairReportsEveryExistingDirectoryAndFileBeforePublishingReplacement()
    {
        using var client = CreateClient(_ => throw new AssertionException("No attachment download expected."));
        var archive = CreateArchive(client);
        var ticket = CreateTicket();
        var previous = await archive.ExportAsync(ticket, EmptySnapshot(), default);
        ticket = ticket with { ArchiveSnapshotPath = previous, ArchiveComplete = true };
        var directory = ticket.ArchivePath!;
        var empty = Path.Combine(directory, "attachments", "empty");
        Directory.CreateDirectory(empty);
        for (var id = 88; id < 108; id++)
            await File.WriteAllBytesAsync(Path.Combine(directory, "attachments", $"{id}.bin"), new byte[3]);
        var existing = Directory.GetFileSystemEntries(directory, "*", SearchOption.AllDirectories);
        if (OperatingSystem.IsLinux())
        {
            foreach (var path in existing)
                File.SetUnixFileMode(path, Directory.Exists(path) ? (UnixFileMode)511 : (UnixFileMode)438);
        }
        var repairProgress = 0;
        var replacement = await archive.ExportAsync(ticket, EmptySnapshot(), default, () =>
        {
            if (Directory.GetDirectories(Path.GetDirectoryName(previous)!).Length == 1) repairProgress++;
        });
        Assert.That(repairProgress, Is.EqualTo(existing.Length + 1), "Repair reports the owning directory, every child directory and every file.");
        Assert.That(File.Exists(Path.Combine(replacement, "transcript.html")), Is.True);
        if (OperatingSystem.IsLinux())
        {
            foreach (var path in existing)
                Assert.That(ReadUnixMode(path), Is.EqualTo(Directory.Exists(path) ? (UnixFileMode)448 : (UnixFileMode)384), path);
        }
    }

    [Test]
    public async Task CancellationDuringPermissionRepairStopsBeforePublicationAndPreservesSelectedPair()
    {
        using var client = CreateClient(_ => throw new AssertionException("No attachment download expected."));
        var archive = CreateArchive(client);
        var ticket = CreateTicket();
        var previous = await archive.ExportAsync(ticket, EmptySnapshot(), default);
        ticket = ticket with { ArchiveSnapshotPath = previous, ArchiveComplete = true };
        var json = await File.ReadAllBytesAsync(Path.Combine(previous, "transcript.json"));
        var html = await File.ReadAllBytesAsync(Path.Combine(previous, "transcript.html"));
        var repairProgress = 0;
        using var cancellation = new CancellationTokenSource();
        Assert.CatchAsync<OperationCanceledException>(() => archive.ExportAsync(ticket, EmptySnapshot(), cancellation.Token, () =>
        {
            Assert.That(Directory.GetDirectories(Path.GetDirectoryName(previous)!), Is.EqualTo(new[] { previous }),
                "Permission traversal must report progress before any replacement is staged.");
            if (++repairProgress == 3) cancellation.Cancel();
        }));
        Assert.That(repairProgress, Is.EqualTo(3), "Canceled traversal stops at its next entry or directory boundary.");
        Assert.That(await File.ReadAllBytesAsync(Path.Combine(previous, "transcript.json")), Is.EqualTo(json));
        Assert.That(await File.ReadAllBytesAsync(Path.Combine(previous, "transcript.html")), Is.EqualTo(html));
        Assert.That(Directory.GetDirectories(Path.GetDirectoryName(previous)!), Is.EqualTo(new[] { previous }));
        Assert.That(Directory.GetFiles(ticket.ArchivePath!, "*.part", SearchOption.AllDirectories), Is.Empty);
    }

    [Test]
    public async Task LinuxCancellationDuringPermissionRepairKeepsRepairedFilesPrivateAndStopsBeforeRemainingFiles()
    {
        if (!OperatingSystem.IsLinux()) { Assert.Ignore("Unix mode verification requires Linux."); return; }
        using var client = CreateClient(_ => throw new AssertionException("No attachment download expected."));
        var archive = CreateArchive(client);
        var ticket = CreateTicket();
        var previous = await archive.ExportAsync(ticket, EmptySnapshot(), default);
        ticket = ticket with { ArchiveSnapshotPath = previous, ArchiveComplete = true };
        var cache = Path.Combine(ticket.ArchivePath!, "attachments");
        var files = Enumerable.Range(88, 20).Select(id => Path.Combine(cache, $"{id}.bin")).ToArray();
        foreach (var path in files)
        {
            await File.WriteAllBytesAsync(path, new byte[3]);
            File.SetUnixFileMode(path, (UnixFileMode)438);
        }
        File.SetUnixFileMode(cache, (UnixFileMode)511);
        var repairProgress = 0;
        using var cancellation = new CancellationTokenSource();
        Assert.CatchAsync<OperationCanceledException>(() => archive.ExportAsync(ticket, EmptySnapshot(), cancellation.Token, () =>
        {
            repairProgress++;
            if (files.Count(path => ReadUnixMode(path) == (UnixFileMode)384) == 2) cancellation.Cancel();
        }));
        Assert.That(repairProgress, Is.GreaterThan(2));
        Assert.That(files.Count(path => ReadUnixMode(path) == (UnixFileMode)384), Is.EqualTo(2));
        Assert.That(files.Count(path => ReadUnixMode(path) == (UnixFileMode)438), Is.EqualTo(18));
        Assert.That(ReadUnixMode(cache), Is.EqualTo((UnixFileMode)448));
        Assert.That(Directory.GetDirectories(Path.GetDirectoryName(previous)!), Is.EqualTo(new[] { previous }));
        foreach (var path in files) Assert.That(await File.ReadAllBytesAsync(path), Is.EqualTo(new byte[3]));
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
            completedTranscriptWasReported |= Directory.Exists(directory) && Directory.GetFiles(directory, "transcript.html", SearchOption.AllDirectories).Length > 0;
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
        var ticket = CreateTicket();
        var directory = await archive.ExportAsync(ticket, Snapshot(Attachment(88, 3), Attachment(89, 2)), default);
        Assert.That(handler.RequestCount, Is.EqualTo(2));
        Assert.That(Directory.GetFiles(Path.Combine(archive.GetArchivePath(ticket), "attachments")).Sum(file => new FileInfo(file).Length), Is.EqualTo(5));
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
        var ticket = CreateTicket();
        await archive.ExportAsync(ticket, snapshot, default);
        Assert.That(handler.RequestCount, Is.EqualTo(1));
        Assert.That(Directory.GetFiles(Path.Combine(archive.GetArchivePath(ticket), "attachments")), Has.Length.EqualTo(1));
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
        directory = await archive.ExportAsync(ticket, Snapshot(Attachment(89, 2)), default);
        var published = await File.ReadAllTextAsync(Path.Combine(directory, "transcript.json"));
        Assert.ThrowsAsync<InvalidDataException>(() => archive.ExportAsync(ticket, Snapshot(Attachment(90, 1)), default));
        Assert.That(handler.RequestCount, Is.EqualTo(2));
        Assert.That(await File.ReadAllTextAsync(Path.Combine(directory, "transcript.json")), Is.EqualTo(published));
        Assert.That(Directory.GetFiles(Path.Combine(archive.GetArchivePath(ticket), "attachments")).Sum(file => new FileInfo(file).Length), Is.EqualTo(5));
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
    [TestCase("message-metadata")]
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
            "message-metadata" => message with { MetadataJson = new string('m', 800) },
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
    public async Task ArchivePreservesAndEscapesAdditionalMessagePayloadsInJsonAndHtml()
    {
        using var client = CreateClient(_ => throw new AssertionException("No attachment should be downloaded."));
        var archive = CreateArchive(client);
        var metadata = "{\"Poll\":{\"Question\":\"<script>alert('poll')</script>\"},\"Stickers\":[\"sticker\"],\"Components\":[\"text display\"],\"ForwardedMessages\":[\"evidence\"]}";
        var message = new TranscriptMessage(10, 55, "member", "", DateTimeOffset.UnixEpoch, [], MetadataJson: metadata);
        var directory = await archive.ExportAsync(CreateTicket(), new([message], 10), default);
        var html = await File.ReadAllTextAsync(Path.Combine(directory, "transcript.html"));
        Assert.That(html, Does.Contain(WebUtility.HtmlEncode(metadata)));
        Assert.That(html, Does.Not.Contain("<script>"));
        using var json = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(directory, "transcript.json")));
        Assert.That(json.RootElement.GetProperty("Messages")[0].GetProperty("MetadataJson").GetString(), Is.EqualTo(metadata));
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
        var originalJson = await File.ReadAllBytesAsync(Path.Combine(directory, "transcript.json"));
        var message = new TranscriptMessage(10, 55, "member", new string('&', 200000), DateTimeOffset.UnixEpoch, []);
        using var cancellation = new CancellationTokenSource();
        Assert.CatchAsync<OperationCanceledException>(() => archive.ExportAsync(ticket, new([message], 10),
            cancellation.Token, () =>
            {
                if (Directory.GetFiles(archive.GetArchivePath(ticket), "transcript.html.part", SearchOption.AllDirectories)
                    .Any(path => new FileInfo(path).Length > 0)) cancellation.Cancel();
            }));
        Assert.That(await File.ReadAllBytesAsync(htmlPath), Is.EqualTo(original));
        Assert.That(await File.ReadAllBytesAsync(Path.Combine(directory, "transcript.json")), Is.EqualTo(originalJson));
        Assert.That(Directory.GetFiles(archive.GetArchivePath(ticket), "*.part", SearchOption.AllDirectories), Is.Empty);
        Assert.That(Directory.GetDirectories(Path.GetDirectoryName(directory)!), Is.EqualTo(new[] { directory }));
    }

    [Test]
    public void ExportRejectsUncommittedOwnershipBeforeCreatingFiles()
    {
        using var client = CreateClient(_ => throw new AssertionException("No HTTP before durable ownership."));
        var archive = CreateArchive(client);
        var ticket = CreateTicket() with { ArchivePath = null };
        Assert.ThrowsAsync<InvalidOperationException>(() => archive.ExportAsync(ticket, EmptySnapshot(), default));
        Assert.That(Directory.GetFileSystemEntries(_root), Is.Empty);
    }

    [Test]
    public async Task FailedHtmlPublicationKeepsBothPreviousTranscriptFilesAndDiscardsOnlyStaging()
    {
        using var client = CreateClient(_ => throw new AssertionException("No attachments should be downloaded."));
        var archive = CreateArchive(client);
        var ticket = CreateTicket();
        var previous = await archive.ExportAsync(ticket, EmptySnapshot(), default);
        ticket = ticket with { ArchiveSnapshotPath = previous, ArchiveComplete = true };
        var json = await File.ReadAllBytesAsync(Path.Combine(previous, "transcript.json"));
        var html = await File.ReadAllBytesAsync(Path.Combine(previous, "transcript.html"));
        var replacement = new TranscriptSnapshot([new(10, 55, "member", "new content", DateTimeOffset.UnixEpoch, [])], 10);
        var error = Assert.CatchAsync<Exception>(() => archive.ExportAsync(ticket, replacement, default, () =>
        {
            var stagedJson = Directory.GetFiles(ticket.ArchivePath!, "transcript.json", SearchOption.AllDirectories)
                .SingleOrDefault(path => Path.GetDirectoryName(path) != previous);
            if (stagedJson is not null)
                Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(stagedJson)!, "transcript.html"));
        }));
        Assert.That(error, Is.InstanceOf<IOException>().Or.InstanceOf<UnauthorizedAccessException>());
        Assert.That(await File.ReadAllBytesAsync(Path.Combine(previous, "transcript.json")), Is.EqualTo(json));
        Assert.That(await File.ReadAllBytesAsync(Path.Combine(previous, "transcript.html")), Is.EqualTo(html));
        Assert.That(Directory.GetDirectories(Path.GetDirectoryName(previous)!), Is.EqualTo(new[] { previous }));
        Assert.That(Directory.GetFiles(ticket.ArchivePath!, "*.part", SearchOption.AllDirectories), Is.Empty);
    }

    [Test]
    public async Task PruningPreservesSelectedPairAndSharedAttachmentsAndExpirationRemovesEveryGeneration()
    {
        var payload = new byte[] { 1, 2, 3 };
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(payload) });
        var archive = new FileTranscriptArchive(CreateConfiguration(true), client);
        var ticket = CreateTicket();
        var previous = await archive.ExportAsync(ticket, Snapshot(Attachment(88, 3)), default);
        ticket = ticket with { ArchiveSnapshotPath = previous, ArchiveComplete = true };
        var uncommitted = await archive.ExportAsync(ticket, EmptySnapshot(), default);
        await archive.PruneSnapshotsAsync(ticket, default);
        Assert.That(Directory.Exists(previous), Is.True);
        Assert.That(Directory.Exists(uncommitted), Is.False);
        var replacement = await archive.ExportAsync(ticket, Snapshot(Attachment(88, 3)), default);
        await archive.PruneSnapshotsAsync(ticket with { ArchiveSnapshotPath = replacement }, default);
        Assert.That(Directory.Exists(previous), Is.False);
        Assert.That(await File.ReadAllBytesAsync(Path.Combine(ticket.ArchivePath!, "attachments", "88.bin")), Is.EqualTo(payload));
        var html = await File.ReadAllTextAsync(Path.Combine(replacement, "transcript.html"));
        Assert.That(html, Does.Contain("../../attachments/88.bin"));
        await archive.ExportAsync(ticket, EmptySnapshot(), default);
        await archive.DeleteAsync(ticket.ArchivePath!, default);
        Assert.That(Directory.Exists(ticket.ArchivePath!), Is.False);
    }

    [Test]
    public async Task PruningReclaimsRemovedAttachmentsOnlyAfterCompleteReplacementSelection()
    {
        var handler = new FakeHttpMessageHandler(request => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(new byte[request.RequestUri!.AbsolutePath.Contains("/89/") ? 2 : 3])
        });
        using var client = new HttpClient(handler);
        var archive = new FileTranscriptArchive(CreateConfiguration(true, 3, 5), client);
        var ticket = CreateTicket();
        var previous = await archive.ExportAsync(ticket, Snapshot(Attachment(88, 3), Attachment(89, 2)), default);
        ticket = ticket with { ArchiveSnapshotPath = previous, ArchiveComplete = true };
        var cache = Path.Combine(ticket.ArchivePath!, "attachments");
        var uncommitted = await archive.ExportAsync(ticket, Snapshot(Attachment(89, 2)), default);
        await archive.PruneSnapshotsAsync(ticket, default);
        Assert.That(Directory.Exists(uncommitted), Is.False);
        Assert.That(File.Exists(Path.Combine(cache, "88.bin")), Is.True, "The old durable selection still references this file.");
        var replacement = await archive.ExportAsync(ticket, Snapshot(Attachment(89, 2)), default);
        await archive.PruneSnapshotsAsync(ticket with { ArchiveSnapshotPath = replacement, ArchiveComplete = false }, default);
        Assert.That(File.Exists(Path.Combine(cache, "88.bin")), Is.True, "An incomplete selection cannot reclaim cache.");
        ticket = ticket with { ArchiveSnapshotPath = replacement };
        await archive.PruneSnapshotsAsync(ticket, default);
        Assert.That(Directory.Exists(previous), Is.False);
        Assert.That(Directory.GetFiles(cache).Select(Path.GetFileName), Is.EqualTo(new[] { "89.bin" }));
        Assert.That(await File.ReadAllTextAsync(Path.Combine(replacement, "transcript.html")), Does.Contain("../../attachments/89.bin"));
        var next = await archive.ExportAsync(ticket, Snapshot(Attachment(90, 3)), default);
        await archive.PruneSnapshotsAsync(ticket with { ArchiveSnapshotPath = next }, default);
        Assert.That(Directory.GetFiles(cache).Select(Path.GetFileName), Is.EqualTo(new[] { "90.bin" }));
        Assert.That(handler.RequestCount, Is.EqualTo(3), "The selected cache is reused and obsolete bytes free room for new files.");
    }

    [Test]
    public async Task PruningEmptySelectedHistoryReclaimsAllAttachmentPayloads()
    {
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(new byte[3]) });
        var archive = new FileTranscriptArchive(CreateConfiguration(true, 3, 3), client);
        var ticket = CreateTicket();
        var previous = await archive.ExportAsync(ticket, Snapshot(Attachment(88, 3)), default);
        ticket = ticket with { ArchiveSnapshotPath = previous, ArchiveComplete = true };
        var replacement = await archive.ExportAsync(ticket, EmptySnapshot(), default);
        await archive.PruneSnapshotsAsync(ticket with { ArchiveSnapshotPath = replacement }, default);
        Assert.That(Directory.GetFiles(Path.Combine(ticket.ArchivePath!, "attachments")), Is.Empty);
        Assert.That(Directory.Exists(replacement), Is.True);
        Assert.That(Directory.Exists(previous), Is.False);
    }

    [Test]
    public async Task FailedRefreshRetriesReclaimStagingAndPreserveSelectedAttachmentWithinPeakBudget()
    {
        var handler = new FakeHttpMessageHandler(request => request.RequestUri!.AbsolutePath.Contains("/90/")
            ? new HttpResponseMessage(HttpStatusCode.InternalServerError)
            : new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(new byte[request.RequestUri!.AbsolutePath.Contains("/88/") ? 3 : 1])
            });
        using var client = new HttpClient(handler);
        var archive = new FileTranscriptArchive(CreateConfiguration(true, 3, 5), client);
        var ticket = CreateTicket();
        var previous = await archive.ExportAsync(ticket, Snapshot(Attachment(88, 3)), default);
        ticket = ticket with { ArchiveSnapshotPath = previous, ArchiveComplete = true };
        var json = await File.ReadAllBytesAsync(Path.Combine(previous, "transcript.json"));
        var html = await File.ReadAllBytesAsync(Path.Combine(previous, "transcript.html"));
        var cache = Path.Combine(ticket.ArchivePath!, "attachments");
        for (var attempt = 0; attempt < 3; attempt++)
        {
            Assert.ThrowsAsync<HttpRequestException>(() => archive.ExportAsync(ticket, Snapshot(Attachment(89, 1), Attachment(90, 1)), default));
            Assert.That(File.Exists(Path.Combine(cache, "89.bin")), Is.True);
            Assert.That(Directory.GetFiles(cache).Sum(file => new FileInfo(file).Length), Is.LessThanOrEqualTo(5));
            await archive.PruneSnapshotsAsync(ticket, default);
            Assert.That(Directory.GetFiles(cache).Select(Path.GetFileName), Is.EqualTo(new[] { "88.bin" }));
            Assert.That(await File.ReadAllBytesAsync(Path.Combine(previous, "transcript.json")), Is.EqualTo(json));
            Assert.That(await File.ReadAllBytesAsync(Path.Combine(previous, "transcript.html")), Is.EqualTo(html));
            Assert.That(Directory.GetFiles(ticket.ArchivePath!, "*.part", SearchOption.AllDirectories), Is.Empty);
        }
        Assert.That(handler.RequestCount, Is.EqualTo(7));
    }

    [Test]
    public async Task ReplacementOverPhysicalPeakBudgetFailsBeforeHttpAndKeepsSelectedArchiveUsable()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(new byte[3]) });
        using var client = new HttpClient(handler);
        var archive = new FileTranscriptArchive(CreateConfiguration(true, 3, 4), client);
        var ticket = CreateTicket();
        var previous = await archive.ExportAsync(ticket, Snapshot(Attachment(88, 3)), default);
        ticket = ticket with { ArchiveSnapshotPath = previous, ArchiveComplete = true };
        await archive.PruneSnapshotsAsync(ticket, default);
        // Each generation fits, but preserving the selected file through commit needs five bytes.
        Assert.ThrowsAsync<InvalidDataException>(() => archive.ExportAsync(ticket, Snapshot(Attachment(89, 2)), default));
        Assert.That(handler.RequestCount, Is.EqualTo(1));
        Assert.That(await File.ReadAllBytesAsync(Path.Combine(ticket.ArchivePath!, "attachments", "88.bin")), Is.EqualTo(new byte[3]));
        Assert.That(Directory.GetDirectories(Path.GetDirectoryName(previous)!), Is.EqualTo(new[] { previous }));
        await archive.PruneSnapshotsAsync(ticket, default);
        Assert.That(File.Exists(Path.Combine(previous, "transcript.html")), Is.True);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task LegacySelectionWithoutManifestKeepsCacheWhilePruningAbandonedGenerations(bool rootSelection)
    {
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(new byte[3]) });
        var archive = new FileTranscriptArchive(CreateConfiguration(true, 3, 3), client);
        var ticket = CreateTicket();
        var selected = await archive.ExportAsync(ticket, Snapshot(Attachment(88, 3)), default);
        ticket = ticket with { ArchiveSnapshotPath = rootSelection ? null : selected, ArchiveComplete = true };
        foreach (var name in new[] { "transcript.json", "transcript.html" })
            File.Copy(Path.Combine(selected, name), Path.Combine(ticket.ArchivePath!, name));
        File.Delete(Path.Combine(selected, "attachment-manifest.json"));
        File.Delete(Path.Combine(selected, "attachment-manifest.sha256"));
        var abandoned = await archive.ExportAsync(ticket, EmptySnapshot(), default);
        await archive.PruneSnapshotsAsync(ticket, default);
        Assert.That(Directory.Exists(abandoned), Is.False);
        Assert.That(Directory.Exists(selected), Is.EqualTo(!rootSelection));
        Assert.That(File.Exists(Path.Combine(ticket.ArchivePath!, "transcript.json")), Is.True);
        Assert.That(File.Exists(Path.Combine(ticket.ArchivePath!, "transcript.html")), Is.True);
        Assert.That(new FileInfo(Path.Combine(ticket.ArchivePath!, "attachments", "88.bin")).Length, Is.EqualTo(3));
    }

    [TestCase("json-content")]
    [TestCase("html-content")]
    [TestCase("manifest-content")]
    [TestCase("missing-manifest")]
    [TestCase("missing-seal")]
    [TestCase("manifest-size")]
    [TestCase("wrong-ticket")]
    [TestCase("missing-list")]
    [TestCase("unsafe-path")]
    [TestCase("duplicate-reference")]
    [TestCase("omitted-reference")]
    [TestCase("reference-count")]
    [TestCase("missing-cache")]
    [TestCase("cache-size")]
    public async Task InvalidSelectedManifestSuspendsAllSnapshotAndAttachmentPruning(string corruption)
    {
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(new byte[3]) });
        var config = CreateConfiguration(true, 3, 5);
        config.Tickets.MaxTranscriptContentBytes = 1024;
        var archive = new FileTranscriptArchive(config, client);
        var ticket = CreateTicket();
        var selected = await archive.ExportAsync(ticket, Snapshot(Attachment(88, 3)), default);
        ticket = ticket with { ArchiveSnapshotPath = selected, ArchiveComplete = true };
        var abandoned = await archive.ExportAsync(ticket, EmptySnapshot(), default);
        var cache = Path.Combine(ticket.ArchivePath!, "attachments");
        var orphan = Path.Combine(cache, "89.bin");
        await File.WriteAllBytesAsync(orphan, new byte[2]);
        var manifest = Path.Combine(selected, "attachment-manifest.json");
        var text = await File.ReadAllTextAsync(manifest);
        var node = JsonNode.Parse(text)!.AsObject();
        string? edited = null;
        switch (corruption)
        {
            case "json-content": await File.AppendAllTextAsync(Path.Combine(selected, "transcript.json"), " "); break;
            case "html-content": await File.AppendAllTextAsync(Path.Combine(selected, "transcript.html"), "changed"); break;
            case "manifest-content": edited = "{"; break;
            case "missing-manifest": File.Delete(manifest); break;
            case "missing-seal": File.Delete(Path.Combine(selected, "attachment-manifest.sha256")); break;
            case "manifest-size": edited = new string(' ', 1024); break;
            case "wrong-ticket": node["Id"] = Guid.NewGuid().ToString(); edited = node.ToJsonString(); break;
            case "missing-list": node.Remove("AttachmentFiles"); edited = node.ToJsonString(); break;
            case "unsafe-path":
                node["AttachmentFiles"] = new JsonObject { ["../../outside.bin"] = 3 };
                edited = node.ToJsonString();
                break;
            case "duplicate-reference": edited = text.Replace("\"attachments/88.bin\":3", "\"attachments/88.bin\":3,\"attachments/88.bin\":3"); break;
            case "omitted-reference": node["AttachmentFiles"] = new JsonObject(); edited = node.ToJsonString(); break;
            case "reference-count":
                var files = node["AttachmentFiles"]!.AsObject();
                files["attachments/89.bin"] = 2;
                for (var id = 90; id <= 92; id++)
                {
                    files[$"attachments/{id}.bin"] = 0;
                    await File.WriteAllBytesAsync(Path.Combine(cache, $"{id}.bin"), []);
                }
                edited = node.ToJsonString();
                break;
            case "missing-cache": File.Delete(Path.Combine(cache, "88.bin")); break;
            case "cache-size": await File.WriteAllBytesAsync(Path.Combine(cache, "88.bin"), new byte[2]); break;
        }
        if (edited is not null)
        {
            await File.WriteAllTextAsync(manifest, edited);
            if (corruption != "omitted-reference")
                await File.WriteAllTextAsync(Path.Combine(selected, "attachment-manifest.sha256"), Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(edited))));
        }
        Assert.ThrowsAsync<InvalidDataException>(() => archive.PruneSnapshotsAsync(ticket, default));
        Assert.That(Directory.Exists(selected), Is.True);
        Assert.That(Directory.Exists(abandoned), Is.True);
        Assert.That(await File.ReadAllBytesAsync(orphan), Is.EqualTo(new byte[2]));
    }

    [Test]
    public async Task CancellationDuringManifestHashingReportsChunkProgressAndKeepsPreviousSelection()
    {
        using var client = CreateClient(_ => throw new AssertionException("No attachment download expected."));
        var archive = CreateArchive(client);
        var ticket = CreateTicket();
        var previous = await archive.ExportAsync(ticket, EmptySnapshot(), default);
        ticket = ticket with { ArchiveSnapshotPath = previous, ArchiveComplete = true };
        var message = new TranscriptMessage(10, 55, "member", new string('&', 200000), DateTimeOffset.UnixEpoch, []);
        using var cancellation = new CancellationTokenSource();
        var hashProgress = 0;
        Assert.CatchAsync<OperationCanceledException>(() => archive.ExportAsync(ticket, new([message], 10), cancellation.Token, () =>
        {
            if (Directory.GetFiles(ticket.ArchivePath!, "attachment-manifest.json.part", SearchOption.AllDirectories).Length == 0) return;
            if (++hashProgress == 2) cancellation.Cancel();
        }));
        Assert.That(hashProgress, Is.EqualTo(2), "Checksum reads must report progress for each chunk.");
        Assert.That(File.Exists(Path.Combine(previous, "transcript.json")), Is.True);
        Assert.That(File.Exists(Path.Combine(previous, "transcript.html")), Is.True);
        Assert.That(Directory.GetDirectories(Path.GetDirectoryName(previous)!), Is.EqualTo(new[] { previous }));
        Assert.That(Directory.GetFiles(ticket.ArchivePath!, "*.part", SearchOption.AllDirectories), Is.Empty);
    }

    [Test]
    public async Task CancellationDuringPruningHashReadsReportsProgressBeforeDeletingAnyGeneration()
    {
        using var client = CreateClient(_ => throw new AssertionException("No attachment download expected."));
        var archive = CreateArchive(client);
        var ticket = CreateTicket();
        var message = new TranscriptMessage(10, 55, "member", new string('&', 200000), DateTimeOffset.UnixEpoch, []);
        var selected = await archive.ExportAsync(ticket, new([message], 10), default);
        ticket = ticket with { ArchiveSnapshotPath = selected, ArchiveComplete = true };
        var abandoned = await archive.ExportAsync(ticket, EmptySnapshot(), default);
        var treeEntries = Directory.GetFileSystemEntries(ticket.ArchivePath!, "*", SearchOption.AllDirectories).Length;
        var progress = 0;
        using var cancellation = new CancellationTokenSource();
        Assert.CatchAsync<OperationCanceledException>(() => archive.PruneSnapshotsAsync(ticket, cancellation.Token, () =>
        {
            // Tree validation reports once per entry, then the small manifest hash once.
            // Cancel after two chunks of the selected transcript hash.
            if (++progress == treeEntries + 3) cancellation.Cancel();
        }));
        Assert.That(progress, Is.EqualTo(treeEntries + 3));
        Assert.That(Directory.Exists(selected), Is.True);
        Assert.That(Directory.Exists(abandoned), Is.True);
    }

    [Test]
    public async Task LinuxPruningRejectsLinkedCacheWithoutDeletingEitherGenerationOrItsTarget()
    {
        if (!OperatingSystem.IsLinux()) { Assert.Ignore("Symbolic link verification requires Linux."); return; }
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(new byte[3]) });
        var archive = new FileTranscriptArchive(CreateConfiguration(true, 3, 3), client);
        var ticket = CreateTicket();
        var selected = await archive.ExportAsync(ticket, Snapshot(Attachment(88, 3)), default);
        ticket = ticket with { ArchiveSnapshotPath = selected, ArchiveComplete = true };
        var abandoned = await archive.ExportAsync(ticket, EmptySnapshot(), default);
        var outside = Path.Combine(_root, "unmanaged.bin");
        var cached = Path.Combine(ticket.ArchivePath!, "attachments", "88.bin");
        await File.WriteAllBytesAsync(outside, new byte[3]);
        File.Delete(cached);
        File.CreateSymbolicLink(cached, outside);
        try
        {
            Assert.ThrowsAsync<InvalidOperationException>(() => archive.PruneSnapshotsAsync(ticket, default));
            Assert.That(Directory.Exists(selected), Is.True);
            Assert.That(Directory.Exists(abandoned), Is.True);
            Assert.That(await File.ReadAllBytesAsync(outside), Is.EqualTo(new byte[3]));
        }
        finally { File.Delete(cached); }
    }

    [Test]
    public async Task FirstExportDownloadFailuresReclaimCacheBeforeChangedHistoryRetriesWithinPeakBudget()
    {
        var handler = new FakeHttpMessageHandler(request => request.RequestUri!.AbsolutePath.Contains("/99/")
            ? new HttpResponseMessage(HttpStatusCode.InternalServerError)
            : new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(new byte[3]) });
        using var client = new HttpClient(handler);
        var archive = new FileTranscriptArchive(CreateConfiguration(true, 3, 4), client);
        var ticket = CreateTicket();
        var cache = Path.Combine(ticket.ArchivePath!, "attachments");
        Assert.That(ticket.ArchiveComplete, Is.False);
        Assert.That(ticket.ArchiveSnapshotPath, Is.Null);
        for (ulong id = 88; id <= 90; id++)
        {
            Assert.ThrowsAsync<HttpRequestException>(() => archive.ExportAsync(ticket, Snapshot(Attachment(id, 3), Attachment(99, 1)), default));
            Assert.That(Directory.GetFiles(cache).Sum(file => new FileInfo(file).Length), Is.EqualTo(3));
            var requests = handler.RequestCount;
            Assert.ThrowsAsync<InvalidDataException>(() => archive.ExportAsync(ticket, Snapshot(Attachment(100, 3)), default));
            Assert.That(handler.RequestCount, Is.EqualTo(requests), "The hard physical peak cap still rejects retained plus new files.");
            await archive.PruneSnapshotsAsync(ticket, default);
            Assert.That(Directory.GetFiles(cache), Is.Empty);
        }
        var replacement = await archive.ExportAsync(ticket, Snapshot(Attachment(100, 3)), default);
        await archive.PruneSnapshotsAsync(ticket with { ArchiveSnapshotPath = replacement, ArchiveComplete = true }, default);
        Assert.That(Directory.GetFiles(cache).Select(Path.GetFileName), Is.EqualTo(new[] { "100.bin" }));
        Assert.That(File.Exists(Path.Combine(replacement, "transcript.html")), Is.True);
        Assert.That(handler.RequestCount, Is.EqualTo(7));
    }

    [TestCase("complete")]
    [TestCase("partial")]
    [TestCase("corrupt-manifest")]
    public async Task UncommittedFirstGenerationAndCacheAreReclaimedBeforeNewExactBudgetExport(string interruptedGeneration)
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(new byte[3]) });
        using var client = new HttpClient(handler);
        var archive = new FileTranscriptArchive(CreateConfiguration(true, 3, 3), client);
        var ticket = CreateTicket();
        var uncommitted = await archive.ExportAsync(ticket, Snapshot(Attachment(88, 3)), default);
        if (interruptedGeneration == "partial") File.Delete(Path.Combine(uncommitted, "transcript.html"));
        if (interruptedGeneration == "corrupt-manifest")
            await File.WriteAllTextAsync(Path.Combine(uncommitted, "attachment-manifest.json"), "{", Encoding.UTF8);
        var cache = Path.Combine(ticket.ArchivePath!, "attachments");
        await archive.PruneSnapshotsAsync(ticket, default);
        Assert.That(Directory.Exists(uncommitted), Is.False, "No durable ticket selection references this generation.");
        Assert.That(Directory.GetFiles(cache), Is.Empty);
        var replacement = await archive.ExportAsync(ticket, Snapshot(Attachment(89, 3)), default);
        await archive.PruneSnapshotsAsync(ticket with { ArchiveSnapshotPath = replacement, ArchiveComplete = true }, default);
        Assert.That(Directory.GetDirectories(Path.GetDirectoryName(replacement)!), Is.EqualTo(new[] { replacement }));
        Assert.That(Directory.GetFiles(cache).Select(Path.GetFileName), Is.EqualTo(new[] { "89.bin" }));
        Assert.That(handler.RequestCount, Is.EqualTo(2));
    }

    [TestCase("transcript.json")]
    [TestCase("transcript.html")]
    [TestCase("both")]
    public async Task LegacyRootArtifactsProtectCacheEvenWhenDurableCompleteFlagIsMissing(string artifact)
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(new byte[3]) });
        using var client = new HttpClient(handler);
        var archive = new FileTranscriptArchive(CreateConfiguration(true, 3, 3), client);
        var ticket = CreateTicket();
        var uncommitted = await archive.ExportAsync(ticket, Snapshot(Attachment(88, 3)), default);
        var legacyFiles = artifact == "both" ? new[] { "transcript.json", "transcript.html" } : new[] { artifact };
        var original = new Dictionary<string, byte[]>();
        foreach (var name in legacyFiles)
        {
            original[name] = await File.ReadAllBytesAsync(Path.Combine(uncommitted, name));
            await File.WriteAllBytesAsync(Path.Combine(ticket.ArchivePath!, name), original[name]);
        }
        await archive.PruneSnapshotsAsync(ticket, default);
        Assert.That(Directory.Exists(uncommitted), Is.False);
        foreach (var file in original)
            Assert.That(await File.ReadAllBytesAsync(Path.Combine(ticket.ArchivePath!, file.Key)), Is.EqualTo(file.Value));
        Assert.That(new FileInfo(Path.Combine(ticket.ArchivePath!, "attachments", "88.bin")).Length, Is.EqualTo(3));
        Assert.ThrowsAsync<InvalidDataException>(() => archive.ExportAsync(ticket, Snapshot(Attachment(89, 3)), default));
        Assert.That(handler.RequestCount, Is.EqualTo(1), "Preserving legacy cache still enforces the physical peak budget.");
    }

    [Test]
    public async Task FirstExportPruningRejectsUnexpectedCacheDirectoryBeforeDeletingGeneration()
    {
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(new byte[3]) });
        var archive = new FileTranscriptArchive(CreateConfiguration(true, 3, 3), client);
        var ticket = CreateTicket();
        var uncommitted = await archive.ExportAsync(ticket, Snapshot(Attachment(88, 3)), default);
        var cache = Path.Combine(ticket.ArchivePath!, "attachments");
        var nested = Path.Combine(cache, "unexpected");
        Directory.CreateDirectory(nested);
        var sentinel = Path.Combine(nested, "preserve.txt");
        await File.WriteAllTextAsync(sentinel, "preserve", Encoding.UTF8);
        Assert.ThrowsAsync<InvalidDataException>(() => archive.PruneSnapshotsAsync(ticket, default));
        Assert.That(Directory.Exists(uncommitted), Is.True);
        Assert.That(File.Exists(Path.Combine(cache, "88.bin")), Is.True);
        Assert.That(await File.ReadAllTextAsync(sentinel, Encoding.UTF8), Is.EqualTo("preserve"));
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

    private Ticket CreateTicket()
    {
        var id = Guid.NewGuid();
        return new(id, "support", 42, 1234, TicketState.Closed, 1, DateTimeOffset.UnixEpoch,
            ArchivePath: Path.Combine(_root, id.ToString("N")));
    }

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
