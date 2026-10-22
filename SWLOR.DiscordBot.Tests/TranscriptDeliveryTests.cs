using System.IO.Compression;
using System.Text;
using NUnit.Framework;
using SWLOR.DiscordBot.Discord;

namespace SWLOR.DiscordBot.Tests;

[TestFixture]
public sealed class TranscriptDeliveryTests
{
    private string sourcePath = null!;

    [SetUp]
    public void SetUp()
    {
        var directory = Path.Combine(Path.GetTempPath(), "swlor-transcript-delivery-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        sourcePath = Path.Combine(directory, "transcript.html");
    }

    [TearDown]
    public void TearDown()
    {
        var directory = Path.GetDirectoryName(sourcePath)!;
        if (Directory.Exists(directory)) Directory.Delete(directory, true);
    }

    [TestCase(2048UL)]
    [TestCase(2049UL)]
    public async Task HtmlWithinInteractionLimitIsDeliveredWithoutChangingBytes(ulong limit)
    {
        var bytes = RandomBytes(2048);
        await File.WriteAllBytesAsync(sourcePath, bytes);
        var uploads = await DeliverAsync(limit);
        Assert.That(uploads, Has.Count.EqualTo(1));
        Assert.That(uploads[0].Name, Is.EqualTo("transcript.html"));
        Assert.That(uploads[0].Bytes, Is.EqualTo(bytes));
        Assert.That(await File.ReadAllBytesAsync(sourcePath), Is.EqualTo(bytes));
    }

    [Test]
    public async Task OversizedHtmlCompressesToOneValidAttachmentAndDecompressesExactly()
    {
        var bytes = Encoding.UTF8.GetBytes("<html>" + new string('z', 50000) + "Ω</html>");
        await File.WriteAllBytesAsync(sourcePath, bytes);
        var uploads = await DeliverAsync(1024);
        Assert.That(uploads, Has.Count.EqualTo(1));
        Assert.That(uploads[0].Name, Is.EqualTo("transcript.html.gz"));
        Assert.That(uploads[0].Text, Does.Contain("Decompress transcript.html.gz to transcript.html"));
        Assert.That(await DecompressAsync(uploads[0].Bytes), Is.EqualTo(bytes));
    }

    [Test]
    public async Task IncompressibleTranscriptSplitsUnderActualLimitAndReassemblesExactly()
    {
        var bytes = RandomBytes(4096);
        await File.WriteAllBytesAsync(sourcePath, bytes);
        var uploads = await DeliverAsync(256);
        Assert.That(uploads.Count, Is.GreaterThan(1));
        Assert.That(uploads.Select(x => x.Name), Is.Ordered);
        Assert.That(uploads.Select(x => x.Name).Distinct().Count(), Is.EqualTo(uploads.Count));
        foreach (var upload in uploads)
        {
            Assert.That(upload.Name, Does.StartWith("transcript.html.gz.part"));
            Assert.That(upload.Text, Does.Contain("of " + uploads.Count));
            Assert.That(upload.Text, Does.Contain("binary bytes in filename order"));
            Assert.That(upload.Text, Does.Contain("gzip -d transcript.html.gz"));
            Assert.That(upload.Text, Does.Contain("Windows PowerShell"));
            Assert.That(upload.Text.Length, Is.LessThanOrEqualTo(2000));
        }
        Assert.That(await DecompressAsync(uploads.SelectMany(x => x.Bytes).ToArray()), Is.EqualTo(bytes));
        Assert.That(await File.ReadAllBytesAsync(sourcePath), Is.EqualTo(bytes));
    }

    [Test]
    public async Task LargeTranscriptTemporaryFilesStayInsideTrackedArchiveDirectory()
    {
        await File.WriteAllBytesAsync(sourcePath, RandomBytes(4096));
        var uploads = 0;
        await TranscriptDelivery.SendAsync(sourcePath, 256, (stream, _, _, _) =>
        {
            uploads++;
            Assert.That(stream, Is.TypeOf<FileStream>());
            Assert.That(Path.GetDirectoryName(((FileStream)stream).Name), Is.EqualTo(Path.GetDirectoryName(sourcePath)));
            Assert.That(TemporaryPaths(), Has.Count.EqualTo(2), "gzip and one part use the owned archive volume, not the small /tmp tmpfs");
            return Task.CompletedTask;
        }, default);
        Assert.That(uploads, Is.GreaterThan(1));
        Assert.That(TemporaryPaths(), Is.Empty);
    }

    [Test]
    public void MissingInteractionLimitFailsBeforeOpeningOrLeakingPaths()
    {
        var error = Assert.ThrowsAsync<DiscordValidationException>(() => TranscriptDelivery.SendAsync(
            "private-server-path-not-to-be-disclosed", 0,
            (_, _, _, _) => throw new AssertionException("No upload is possible without a valid limit."), CancellationToken.None));
        Assert.That(error!.Message, Does.Contain("attachment size limit"));
        Assert.That(error.Message, Does.Not.Contain("private-server"));
    }

    [Test]
    public async Task FailedUploadDisposesStreamsAndDeletesTemporaryFiles()
    {
        await File.WriteAllBytesAsync(sourcePath, RandomBytes(4096));
        var before = TemporaryPaths();
        Stream? uploaded = null;
        Assert.ThrowsAsync<IOException>(() => TranscriptDelivery.SendAsync(sourcePath, 256,
            (stream, _, _, _) =>
            {
                uploaded = stream;
                return Task.FromException(new IOException("Upload failed."));
            }, CancellationToken.None));
        Assert.That(uploaded!.CanRead, Is.False);
        Assert.That(TemporaryPaths().Except(before), Is.Empty);
        Assert.That(File.Exists(sourcePath), Is.True);
    }

    [Test]
    public async Task CancellationStopsRemainingPartsAndDeletesTemporaryFiles()
    {
        await File.WriteAllBytesAsync(sourcePath, RandomBytes(4096));
        var before = TemporaryPaths();
        using var cancellation = new CancellationTokenSource();
        var count = 0;
        Assert.ThrowsAsync<OperationCanceledException>(() => TranscriptDelivery.SendAsync(sourcePath, 256,
            (_, _, _, token) =>
            {
                Assert.That(token, Is.EqualTo(cancellation.Token));
                count++;
                cancellation.Cancel();
                return Task.CompletedTask;
            }, cancellation.Token));
        Assert.That(count, Is.EqualTo(1));
        Assert.That(TemporaryPaths().Except(before), Is.Empty);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task RevokedAccessBeforeHtmlOrPreparedGzipPreventsPublication(bool compress)
    {
        var bytes = compress ? Encoding.UTF8.GetBytes(new string('z', 50000)) : RandomBytes(128);
        await File.WriteAllBytesAsync(sourcePath, bytes);
        var authorizations = 0;
        var uploads = 0;
        Assert.ThrowsAsync<DiscordValidationException>(() => TranscriptDelivery.SendAsync(sourcePath, 1024,
            (_, _, _, _) => { uploads++; return Task.CompletedTask; }, default, _ =>
            {
                authorizations++;
                Assert.That(TemporaryPaths().Count, Is.EqualTo(compress ? 1 : 0),
                    "Authorization must follow any gzip preparation and immediately precede publication.");
                throw new DiscordValidationException("Support access changed.");
            }));
        Assert.That(authorizations, Is.EqualTo(1));
        Assert.That(uploads, Is.Zero);
        Assert.That(TemporaryPaths(), Is.Empty);
        Assert.That(await File.ReadAllBytesAsync(sourcePath), Is.EqualTo(bytes));
    }

    [Test]
    public async Task RevokedAccessBetweenPartsStopsFurtherPublicationAndCleansTemporaryFiles()
    {
        var bytes = RandomBytes(4096);
        await File.WriteAllBytesAsync(sourcePath, bytes);
        var support = true;
        var authorizations = 0;
        var uploads = 0;
        Stream? firstPart = null;
        Assert.ThrowsAsync<DiscordValidationException>(() => TranscriptDelivery.SendAsync(sourcePath, 256,
            (stream, _, _, _) =>
            {
                firstPart = stream;
                uploads++;
                support = false;
                return Task.CompletedTask;
            }, default, _ =>
            {
                authorizations++;
                if (!support) throw new DiscordValidationException("Support access changed.");
                return Task.CompletedTask;
            }));
        Assert.That(authorizations, Is.EqualTo(2));
        Assert.That(uploads, Is.EqualTo(1), "Already published parts cannot authorize later parts.");
        Assert.That(firstPart!.CanRead, Is.False);
        Assert.That(TemporaryPaths(), Is.Empty);
        Assert.That(await File.ReadAllBytesAsync(sourcePath), Is.EqualTo(bytes));
    }

    [Test]
    public async Task FailedMembershipRefreshPreventsTranscriptPublication()
    {
        await File.WriteAllBytesAsync(sourcePath, RandomBytes(128));
        Assert.ThrowsAsync<IOException>(() => TranscriptDelivery.SendAsync(sourcePath, 1024,
            (_, _, _, _) => throw new AssertionException("A failed authorization lookup must fail closed."),
            default, _ => Task.FromException(new IOException("Membership unavailable."))));
        Assert.That(TemporaryPaths(), Is.Empty);
    }

    private async Task<List<(string Name, string Text, byte[] Bytes)>> DeliverAsync(ulong limit)
    {
        var before = TemporaryPaths();
        var uploads = new List<(string Name, string Text, byte[] Bytes)>();
        var streams = new List<Stream>();
        await TranscriptDelivery.SendAsync(sourcePath, limit, async (stream, name, text, token) =>
        {
            streams.Add(stream);
            Assert.That(stream.CanSeek, Is.True);
            Assert.That(stream.Position, Is.Zero);
            Assert.That((ulong)stream.Length, Is.LessThanOrEqualTo(limit));
            Assert.That(name, Does.Not.Contain(Path.DirectorySeparatorChar.ToString()));
            Assert.That(text, Does.Not.Contain(sourcePath));
            using var received = new MemoryStream();
            await stream.CopyToAsync(received, token);
            uploads.Add((name, text, received.ToArray()));
        }, CancellationToken.None);
        Assert.That(streams.All(x => !x.CanRead), Is.True);
        Assert.That(TemporaryPaths().Except(before), Is.Empty);
        return uploads;
    }

    private HashSet<string> TemporaryPaths() =>
        Directory.GetFiles(Path.GetDirectoryName(sourcePath)!, "swlor-transcript-*.tmp").ToHashSet(StringComparer.Ordinal);

    private static byte[] RandomBytes(int length)
    {
        var bytes = new byte[length];
        new Random(37).NextBytes(bytes);
        return bytes;
    }

    private static async Task<byte[]> DecompressAsync(byte[] bytes)
    {
        using var source = new MemoryStream(bytes);
        using var gzip = new GZipStream(source, CompressionMode.Decompress);
        using var output = new MemoryStream();
        await gzip.CopyToAsync(output);
        return output.ToArray();
    }
}