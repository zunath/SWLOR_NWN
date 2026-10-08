using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using SWLOR.DiscordBot.Configuration;
using SWLOR.DiscordBot.Core;

namespace SWLOR.DiscordBot.Persistence;

public sealed class FileTranscriptArchive(BotConfiguration configuration, HttpClient http) : ITranscriptArchive
{
    private const UnixFileMode PrivateDirectoryMode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;
    private const UnixFileMode PrivateFileMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
    private string Root => Path.GetFullPath(configuration.Tickets.ArchiveDirectory);
    private static readonly HashSet<string> AttachmentHosts = new(StringComparer.OrdinalIgnoreCase)
        { "cdn.discordapp.com", "media.discordapp.net", "cdn.discordapp.net" };

    public string GetArchivePath(Ticket ticket) => TicketDirectory(ticket.Id);

    public Task<string> ExportAsync(Ticket ticket, TranscriptSnapshot snapshot, CancellationToken ct) =>
        ExportAsync(ticket, snapshot, ct, static () => { });

    public async Task<string> ExportAsync(Ticket ticket, TranscriptSnapshot snapshot, CancellationToken ct, Action progress)
    {
        ValidateOwnership(ticket);
        var contentBudget = new TranscriptContentBudget(configuration.Tickets.MaxTranscriptContentBytes);
        foreach (var message in snapshot.Messages)
        {
            ct.ThrowIfCancellationRequested();
            contentBudget.Add(message);
            progress();
        }
        EnsurePrivateDirectory(Root);
        var directory = TicketDirectory(ticket.Id);
        EnsurePrivateDirectory(directory);
        var attachmentDirectory = Path.Combine(directory, "attachments");
        EnsurePrivateDirectory(attachmentDirectory);
        RepairExistingPermissions(directory, ct, progress);
        var files = new Dictionary<ulong, string>();
        var attachmentSizes = new Dictionary<ulong, long>();
        if (configuration.Tickets.CopyAttachments)
        {
            var attachments = PlanAttachments(snapshot, ct, progress);
            var retainedBytes = CountRetainedAttachments(attachmentDirectory, ct, progress);
            var plannedBytes = retainedBytes;
            // Include cached and older retained attachments so changed snapshots cannot grow the
            // same archive beyond its cap across retries. Validate the whole plan before any HTTP.
            foreach (var plan in attachments)
            {
                var target = Path.Combine(directory, plan.Relative.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(target))
                {
                    RejectLink(target);
                    if (new FileInfo(target).Length != plan.Attachment.Size)
                        throw new InvalidDataException("Cached attachment size does not match its Discord metadata; cleanup is suspended.");
                }
                else plannedBytes = AddWithinTicketBudget(plannedBytes, plan.Attachment.Size);
                ct.ThrowIfCancellationRequested();
                progress();
            }
            foreach (var plan in attachments)
            {
                var attachment = plan.Attachment;
                attachmentSizes.Add(attachment.Id, attachment.Size);
                var relative = plan.Relative;
                var target = Path.Combine(directory, relative.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(target))
                {
                    files[attachment.Id] = relative;
                    ct.ThrowIfCancellationRequested();
                    progress();
                    continue;
                }
                var url = plan.Url;
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeout.CancelAfter(TimeSpan.FromMinutes(2));
                using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
                response.EnsureSuccessStatusCode();
                progress();
                if (response.RequestMessage?.RequestUri != url)
                    throw new InvalidDataException("Attachment redirects are not supported.");
                if (response.Content.Headers.ContentLength is { } contentLength &&
                    (contentLength > configuration.Tickets.MaxAttachmentBytes ||
                     contentLength > configuration.Tickets.MaxTicketAttachmentBytes - retainedBytes ||
                     contentLength != attachment.Size))
                    throw new InvalidDataException("Attachment content length exceeds the archive budget or differs from its metadata.");
                var temporary = target + ".part";
                RejectLink(temporary);
                long bytes = 0;
                try
                {
                    await using (var output = OpenPrivateWriteStream(temporary))
                    await using (var input = await response.Content.ReadAsStreamAsync(timeout.Token))
                    {
                        var buffer = new byte[81920];
                        int read;
                        while ((read = await input.ReadAsync(buffer, timeout.Token)) > 0)
                        {
                            if (read > configuration.Tickets.MaxAttachmentBytes - bytes ||
                                read > configuration.Tickets.MaxTicketAttachmentBytes - retainedBytes - bytes)
                                throw new InvalidDataException("Attachment streaming exceeds the per-file or per-ticket archive budget.");
                            bytes += read;
                            await output.WriteAsync(buffer.AsMemory(0, read), timeout.Token);
                            progress();
                        }
                        if (bytes != attachment.Size) throw new InvalidDataException("Attachment size does not match its Discord metadata.");
                    }
                    File.Move(temporary, target, true);
                    retainedBytes = AddWithinTicketBudget(retainedBytes, bytes);
                    progress();
                }
                finally { if (File.Exists(temporary)) File.Delete(temporary); }
                files[attachment.Id] = relative;
            }
        }
        // Publish a complete pair by selecting an immutable snapshot in the ticket record.
        // Its owning root was committed before the scan; interrupted publication is still retained.
        var snapshots = Path.Combine(directory, "snapshots");
        EnsurePrivateDirectory(snapshots);
        var snapshotDirectory = Path.Combine(snapshots, Guid.NewGuid().ToString("N"));
        EnsurePrivateDirectory(snapshotDirectory);
        var snapshotFiles = files.ToDictionary(pair => pair.Key, pair => "../../" + pair.Value);
        var complete = false;
        try
        {
            await AtomicWriteAsync(Path.Combine(snapshotDirectory, "transcript.json"),
                output => WriteJsonAsync(output, ticket, snapshot, snapshotFiles, ct, progress), ct);
            progress();
            await AtomicWriteAsync(Path.Combine(snapshotDirectory, "transcript.html"),
                output => WriteHtmlAsync(output, ticket, snapshot, snapshotFiles, ct, progress), ct);
            progress();
            await AtomicWriteAsync(Path.Combine(snapshotDirectory, "attachment-manifest.json"),
                output => WriteAttachmentManifestAsync(output, ticket, snapshotDirectory, files, attachmentSizes, ct, progress), ct);
            progress();
            var manifestHash = await HashFileAsync(Path.Combine(snapshotDirectory, "attachment-manifest.json"), ct, progress);
            await AtomicWriteAsync(Path.Combine(snapshotDirectory, "attachment-manifest.sha256"),
                async output => await output.WriteAsync(Encoding.ASCII.GetBytes(manifestHash), ct), ct);
            progress();
            ct.ThrowIfCancellationRequested();
            complete = true;
            return snapshotDirectory;
        }
        finally
        {
            if (!complete) DeleteSnapshotDirectory(ticket, snapshotDirectory);
        }
    }

    private void ValidateOwnership(Ticket ticket)
    {
        if (ticket.ArchivePath is null || !Path.IsPathRooted(ticket.ArchivePath) ||
            !PathEquals(Path.GetFullPath(ticket.ArchivePath), TicketDirectory(ticket.Id)))
            throw new InvalidOperationException("Commit the managed ticket archive directory before writing transcript files.");
        if (ticket.ArchiveSnapshotPath is not null) ValidateSnapshotPath(ticket, ticket.ArchiveSnapshotPath);
    }

    private void ValidateSnapshotPath(Ticket ticket, string path)
    {
        if (!Path.IsPathRooted(path)) throw new InvalidOperationException("Snapshot paths must be absolute.");
        var full = Path.GetFullPath(path);
        if (!Guid.TryParseExact(Path.GetFileName(full), "N", out var snapshotId) ||
            !PathEquals(full, Path.Combine(TicketDirectory(ticket.Id), "snapshots", snapshotId.ToString("N"))))
            throw new InvalidOperationException("Snapshot path does not identify a managed ticket snapshot.");
    }

    public Task PruneSnapshotsAsync(Ticket ticket, CancellationToken ct) =>
        PruneSnapshotsAsync(ticket, ct, static () => { });

    public async Task PruneSnapshotsAsync(Ticket ticket, CancellationToken ct, Action progress)
    {
        ct.ThrowIfCancellationRequested();
        ValidateOwnership(ticket);
        var directory = TicketDirectory(ticket.Id);
        if (!Directory.Exists(directory)) return;
        RejectLink(Root);
        // Validate every candidate before deleting any generation or shared cached payload.
        ValidateTree(directory, ct, progress);
        var snapshots = Path.Combine(directory, "snapshots");
        if (ticket.ArchiveSnapshotPath is not null)
        {
            foreach (var name in new[] { "transcript.json", "transcript.html" })
                if (!File.Exists(Path.Combine(ticket.ArchiveSnapshotPath, name)))
                    throw new InvalidOperationException("The selected snapshot is incomplete; pruning is suspended.");
        }
        // A complete selection needs a trustworthy manifest before reclaiming shared cache.
        var selectedFiles = ticket.ArchiveComplete && ticket.ArchiveSnapshotPath is not null
            ? await ReadSelectedAttachmentFilesAsync(ticket, ct, progress) : null;
        // First-export downloads and generations have no durable readers until selection commits.
        // Preserve any legacy root transcript artifact, including records missing the complete flag.
        var legacyRoot = new[] { "transcript.json", "transcript.html" }.Any(name =>
        {
            var path = Path.Combine(directory, name);
            return File.Exists(path) || Directory.Exists(path);
        });
        var discardUnpublishedCache = !ticket.ArchiveComplete && ticket.ArchiveSnapshotPath is null && !legacyRoot;
        var obsoleteSnapshots = new List<string>();
        if (Directory.Exists(snapshots))
        {
            foreach (var path in Directory.EnumerateFileSystemEntries(snapshots))
            {
                ct.ThrowIfCancellationRequested();
                ValidateSnapshotPath(ticket, path);
                if (!Directory.Exists(path)) throw new InvalidOperationException("Unexpected file in managed snapshots.");
                if (ticket.ArchiveSnapshotPath is null || !PathEquals(path, ticket.ArchiveSnapshotPath))
                    obsoleteSnapshots.Add(path);
                progress();
            }
        }
        var obsoleteAttachments = new List<string>();
        var attachments = Path.Combine(directory, "attachments");
        if ((selectedFiles is not null || discardUnpublishedCache) && Directory.Exists(attachments))
        {
            foreach (var path in Directory.EnumerateFileSystemEntries(attachments))
            {
                ct.ThrowIfCancellationRequested();
                if (Directory.Exists(path)) throw new InvalidDataException("Unexpected directory in the attachment cache; pruning is suspended.");
                if (selectedFiles is null || !selectedFiles.Contains(path)) obsoleteAttachments.Add(path);
                progress();
            }
        }
        foreach (var path in obsoleteSnapshots)
        {
            DeleteSnapshotDirectory(ticket, path, ct);
            progress();
        }
        if (selectedFiles is not null)
        {
            // Legacy exports lived at the root. Retire them only after the new selection commits.
            foreach (var name in new[] { "transcript.json", "transcript.html" })
            {
                ct.ThrowIfCancellationRequested();
                var path = Path.Combine(directory, name);
                RejectLink(path);
                if (File.Exists(path)) File.Delete(path);
                progress();
            }
        }
        foreach (var path in obsoleteAttachments)
        {
            ct.ThrowIfCancellationRequested();
            RejectLink(path);
            File.Delete(path);
            progress();
        }
    }

    private async Task<HashSet<string>?> ReadSelectedAttachmentFilesAsync(Ticket ticket, CancellationToken ct, Action progress)
    {
        var snapshot = ticket.ArchiveSnapshotPath!;
        var manifest = Path.Combine(snapshot, "attachment-manifest.json");
        var seal = Path.Combine(snapshot, "attachment-manifest.sha256");
        if (!File.Exists(manifest) && !File.Exists(seal)) return null;
        if (!File.Exists(manifest) || !File.Exists(seal) || new FileInfo(seal).Length != 64)
            throw new InvalidDataException("The selected attachment manifest is incomplete; pruning is suspended.");
        // Each original attachment reserves 256 content-budget bytes. A compact manifest row
        // needs at most 96 bytes; bound both parse memory and object counts before trusting it.
        var maxFiles = Math.Clamp(configuration.Tickets.MaxTranscriptContentBytes / 256, 0, 67108864 / 256);
        await using var input = File.OpenRead(manifest);
        if (input.Length > 512 + maxFiles * 96)
            throw new InvalidDataException("The selected attachment manifest exceeds its memory budget; pruning is suspended.");
        if (await File.ReadAllTextAsync(seal, ct) != await HashFileAsync(manifest, ct, progress))
            throw new InvalidDataException("The selected attachment manifest checksum is invalid; pruning is suspended.");
        JsonDocument document;
        try { document = await JsonDocument.ParseAsync(input, new JsonDocumentOptions { MaxDepth = 4 }, ct); }
        catch (JsonException ex) { throw new InvalidDataException("The selected attachment manifest is corrupt; pruning is suspended.", ex); }
        using (document)
        {
            var root = document.RootElement;
            if (!ManifestProperty(root, "Id", JsonValueKind.String).TryGetGuid(out var id) || id != ticket.Id)
                throw new InvalidDataException("The selected snapshot belongs to another ticket; pruning is suspended.");
            if (ManifestProperty(root, "TranscriptSha256", JsonValueKind.String).GetString() !=
                    await HashFileAsync(Path.Combine(snapshot, "transcript.json"), ct, progress) ||
                ManifestProperty(root, "HtmlSha256", JsonValueKind.String).GetString() !=
                    await HashFileAsync(Path.Combine(snapshot, "transcript.html"), ct, progress))
                throw new InvalidDataException("The selected snapshot does not match its attachment manifest; pruning is suspended.");
            var files = ManifestProperty(root, "AttachmentFiles", JsonValueKind.Object);
            var selectedFiles = new HashSet<string>(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
            var attachmentIds = new HashSet<ulong>();
            foreach (var file in files.EnumerateObject())
            {
                ct.ThrowIfCancellationRequested();
                if (attachmentIds.Count >= maxFiles ||
                    !Regex.IsMatch(file.Name, @"^attachments/[0-9]+\.[a-zA-Z0-9]{1,8}$", RegexOptions.CultureInvariant) ||
                    file.Value.ValueKind != JsonValueKind.Number || !file.Value.TryGetInt64(out var size) || size < 0)
                    throw new InvalidDataException("The selected attachment manifest reference is invalid; pruning is suspended.");
                var name = Path.GetFileName(file.Name);
                var number = name[..name.LastIndexOf('.')];
                if (!ulong.TryParse(number, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var attachmentId) ||
                    number != attachmentId.ToString(System.Globalization.CultureInfo.InvariantCulture) || !attachmentIds.Add(attachmentId))
                    throw new InvalidDataException("The selected attachment manifest has conflicting IDs; pruning is suspended.");
                var path = Path.Combine(TicketDirectory(ticket.Id), file.Name.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(path) || new FileInfo(path).Length != size)
                    throw new InvalidDataException("The selected snapshot attachment is missing or incomplete; pruning is suspended.");
                selectedFiles.Add(path);
                progress();
            }
            return selectedFiles;
        }
    }

    private static JsonElement ManifestProperty(JsonElement value, string name, JsonValueKind kind)
    {
        if (value.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("The selected snapshot manifest is invalid; pruning is suspended.");
        var match = default(JsonElement);
        var count = 0;
        foreach (var property in value.EnumerateObject())
        {
            if (property.Name != name) continue;
            match = property.Value;
            count++;
        }
        if (count != 1 || match.ValueKind != kind)
            throw new InvalidDataException("The selected snapshot manifest is invalid; pruning is suspended.");
        return match;
    }

    private void DeleteSnapshotDirectory(Ticket ticket, string path, CancellationToken ct = default)
    {
        ValidateSnapshotPath(ticket, path);
        RejectLink(TicketDirectory(ticket.Id));
        RejectLink(Path.GetDirectoryName(path)!);
        if (!Directory.Exists(path)) return;
        ValidateTree(path, ct);
        Directory.Delete(path, true);
    }

    private static void ValidateTree(string directory, CancellationToken ct, Action? progress = null)
    {
        ct.ThrowIfCancellationRequested();
        RejectLink(directory);
        foreach (var entry in Directory.EnumerateFileSystemEntries(directory))
        {
            ct.ThrowIfCancellationRequested();
            RejectLink(entry);
            if (Directory.Exists(entry)) ValidateTree(entry, ct, progress);
            progress?.Invoke();
        }
    }

    private sealed record AttachmentPlan(TranscriptAttachment Attachment, string Relative, Uri Url);

    private IReadOnlyList<AttachmentPlan> PlanAttachments(TranscriptSnapshot snapshot, CancellationToken ct, Action progress)
    {
        var unique = new Dictionary<ulong, AttachmentPlan>();
        long declaredBytes = 0;
        foreach (var attachment in snapshot.Messages.SelectMany(message => message.Attachments))
        {
            ct.ThrowIfCancellationRequested();
            if (attachment.Size < 0 || attachment.Size > configuration.Tickets.MaxAttachmentBytes)
                throw new InvalidDataException("Ticket attachment exceeds the configured archive limit; cleanup is suspended.");
            if (!Uri.TryCreate(attachment.Url, UriKind.Absolute, out var url) || url.Scheme != Uri.UriSchemeHttps ||
                !AttachmentHosts.Contains(url.Host) || !url.IsDefaultPort || !string.IsNullOrEmpty(url.UserInfo))
                throw new InvalidDataException("Attachment URL is not an allowed Discord CDN URL.");
            if (unique.TryGetValue(attachment.Id, out var original))
            {
                // Discord may refresh a signed URL's query, but an ID must still identify one immutable file.
                if (original.Attachment.Size != attachment.Size || original.Attachment.FileName != attachment.FileName ||
                    original.Url.GetLeftPart(UriPartial.Path) != url.GetLeftPart(UriPartial.Path))
                    throw new InvalidDataException("Conflicting metadata for a repeated attachment ID.");
            }
            else
            {
                declaredBytes = AddWithinTicketBudget(declaredBytes, attachment.Size);
                unique.Add(attachment.Id, new(attachment, AttachmentRelativePath(attachment.Id, attachment.FileName), url));
            }
            progress();
        }
        return unique.Values.ToArray();
    }

    private static string AttachmentRelativePath(ulong id, string fileName)
    {
        var extension = Path.GetExtension(fileName);
        if (!Regex.IsMatch(extension, "^\\.[a-zA-Z0-9]{1,8}$")) extension = ".bin";
        return $"attachments/{id.ToString(System.Globalization.CultureInfo.InvariantCulture)}{extension}";
    }

    private long CountRetainedAttachments(string directory, CancellationToken ct, Action progress)
    {
        long bytes = 0;
        foreach (var path in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
        {
            ct.ThrowIfCancellationRequested();
            RejectLink(path);
            // Partial files are never referenced by a published transcript; discard interrupted copies.
            if (Regex.IsMatch(Path.GetFileName(path), @"^\d+\.[a-zA-Z0-9]{1,8}\.part$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)) File.Delete(path);
            else bytes = AddWithinTicketBudget(bytes, new FileInfo(path).Length);
            progress();
        }
        return bytes;
    }

    private long AddWithinTicketBudget(long current, long bytes)
    {
        if (bytes < 0 || bytes > configuration.Tickets.MaxTicketAttachmentBytes - current)
            throw new InvalidDataException("Ticket attachments exceed the cumulative archive budget; cleanup is suspended.");
        return current + bytes;
    }

    public Task DeleteAsync(string path, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var full = Path.GetFullPath(path);
        if (!Guid.TryParseExact(Path.GetFileName(full), "N", out var id) || !PathEquals(full, TicketDirectory(id)))
            throw new InvalidOperationException("Archive path does not identify a managed ticket directory.");
        if (!Directory.Exists(full)) return Task.CompletedTask;
        ValidateTree(full, ct);
        Directory.Delete(full, true);
        return Task.CompletedTask;
    }

    private string TicketDirectory(Guid id) => Path.Combine(Root, id.ToString("N"));
    private static bool PathEquals(string a, string b) => string.Equals(a, b, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    private static void RejectLink(string path)
    {
        try
        {
            if (File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint))
                throw new InvalidOperationException("Links are not allowed in ticket archive paths.");
        }
        catch (FileNotFoundException) { }
        catch (DirectoryNotFoundException) { }
    }
    private static void EnsurePrivateDirectory(string path)
    {
        RejectLink(path);
        if (OperatingSystem.IsWindows()) Directory.CreateDirectory(path);
        else
        {
            Directory.CreateDirectory(path, PrivateDirectoryMode);
            RejectLink(path);
            File.SetUnixFileMode(path, PrivateDirectoryMode);
        }
        RejectLink(path);
    }

    private static void RepairExistingPermissions(string directory, CancellationToken ct, Action progress)
    {
        // Repair old archives and cached attachments before reading or publishing ticket data.
        ct.ThrowIfCancellationRequested();
        RejectLink(directory);
        progress();
        foreach (var entry in Directory.EnumerateFileSystemEntries(directory))
        {
            ct.ThrowIfCancellationRequested();
            RejectLink(entry);
            if (Directory.Exists(entry))
            {
                EnsurePrivateDirectory(entry);
                RepairExistingPermissions(entry, ct, progress);
            }
            else
            {
                RestrictFile(entry);
                progress();
            }
        }
        ct.ThrowIfCancellationRequested();
    }

    private static void RestrictFile(string path)
    {
        RejectLink(path);
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(path, PrivateFileMode);
    }

    private static FileStream OpenPrivateWriteStream(string path)
    {
        RejectLink(path);
        if (File.Exists(path)) RestrictFile(path);
        var options = new FileStreamOptions
        {
            Mode = FileMode.Create,
            Access = FileAccess.Write,
            Share = FileShare.None,
            BufferSize = 81920,
            Options = FileOptions.Asynchronous
        };
        if (!OperatingSystem.IsWindows()) options.UnixCreateMode = PrivateFileMode;
        var stream = new FileStream(path, options);
        try
        {
            RestrictFile(path);
            return stream;
        }
        catch
        {
            stream.Dispose();
            throw;
        }
    }
    private static async Task WriteAttachmentManifestAsync(Stream output, Ticket ticket, string snapshotDirectory,
        IReadOnlyDictionary<ulong, string> files, IReadOnlyDictionary<ulong, long> sizes, CancellationToken ct, Action progress)
    {
        using var writer = new Utf8JsonWriter(output);
        writer.WriteStartObject();
        writer.WriteString("Id", ticket.Id);
        writer.WriteString("TranscriptSha256", await HashFileAsync(Path.Combine(snapshotDirectory, "transcript.json"), ct, progress));
        progress();
        writer.WriteString("HtmlSha256", await HashFileAsync(Path.Combine(snapshotDirectory, "transcript.html"), ct, progress));
        progress();
        writer.WriteStartObject("AttachmentFiles");
        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();
            writer.WriteNumber(file.Value, sizes[file.Key]);
            await writer.FlushAsync(ct);
            progress();
        }
        writer.WriteEndObject();
        writer.WriteEndObject();
        await writer.FlushAsync(ct);
    }

    private static async Task<string> HashFileAsync(string path, CancellationToken ct, Action progress)
    {
        await using var input = File.OpenRead(path);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[81920];
        int read;
        while ((read = await input.ReadAsync(buffer, ct)) > 0)
        {
            hash.AppendData(buffer, 0, read);
            progress();
        }
        ct.ThrowIfCancellationRequested();
        return Convert.ToHexString(hash.GetHashAndReset());
    }

    private static async Task WriteJsonAsync(Stream output, Ticket ticket, TranscriptSnapshot snapshot,
        IReadOnlyDictionary<ulong, string> files, CancellationToken ct, Action progress)
    {
        using var writer = new Utf8JsonWriter(output, new JsonWriterOptions { Indented = true });
        writer.WriteStartObject();
        writer.WriteString("Id", ticket.Id);
        writer.WriteNumber("Number", ticket.Number);
        writer.WriteNumber("RequesterId", ticket.RequesterId);
        if (ticket.ChannelId is { } channel) writer.WriteNumber("ChannelId", channel);
        else writer.WriteNull("ChannelId");
        writer.WriteString("ExportedAt", DateTimeOffset.UtcNow);
        if (snapshot.LastMessageId is { } last) writer.WriteNumber("LastMessageId", last);
        else writer.WriteNull("LastMessageId");
        writer.WriteStartArray("Messages");
        foreach (var message in snapshot.Messages)
        {
            ct.ThrowIfCancellationRequested();
            JsonSerializer.Serialize(writer, message);
            await writer.FlushAsync(ct);
            progress();
        }
        writer.WriteEndArray();
        writer.WriteStartObject("AttachmentFiles");
        foreach (var file in files)
        {
            writer.WriteString(file.Key.ToString(System.Globalization.CultureInfo.InvariantCulture), file.Value);
            await writer.FlushAsync(ct);
            progress();
        }
        writer.WriteEndObject();
        writer.WriteEndObject();
        await writer.FlushAsync(ct);
        progress();
    }

    private static async Task WriteHtmlAsync(Stream output, Ticket ticket, TranscriptSnapshot snapshot,
        IReadOnlyDictionary<ulong, string> files, CancellationToken ct, Action progress)
    {
        await using var writer = new StreamWriter(output, new UTF8Encoding(false), 81920, leaveOpen: true);
        await writer.WriteAsync(("<!doctype html><html lang=\"en\"><meta charset=\"utf-8\"><meta http-equiv=\"Content-Security-Policy\" content=\"default-src 'none'; style-src 'unsafe-inline'; base-uri 'none'; form-action 'none'\"><title>Ticket transcript</title><style>body{font:16px system-ui;max-width:900px;margin:2rem auto;padding:1rem}article{border-bottom:1px solid #ccc;padding:1rem 0}pre{white-space:pre-wrap;overflow-wrap:anywhere}</style><h1>Ticket " + ticket.Number + "</h1>").AsMemory(), ct);
        foreach (var message in snapshot.Messages.OrderBy(m => m.Id))
        {
            ct.ThrowIfCancellationRequested();
            await writer.WriteAsync("<article><b>".AsMemory(), ct);
            await WriteEncodedAsync(writer, message.AuthorName, ct, progress);
            await writer.WriteAsync(("</b> (" + message.AuthorId + ") <time>").AsMemory(), ct);
            await WriteEncodedAsync(writer, message.Timestamp.ToString("O"), ct, progress);
            await writer.WriteAsync("</time><pre>".AsMemory(), ct);
            await WriteEncodedAsync(writer, message.Content, ct, progress);
            await writer.WriteAsync("</pre>".AsMemory(), ct);
            if (!string.IsNullOrWhiteSpace(message.EmbedsJson))
            {
                await writer.WriteAsync("<pre>".AsMemory(), ct);
                await WriteEncodedAsync(writer, message.EmbedsJson, ct, progress);
                await writer.WriteAsync("</pre>".AsMemory(), ct);
            }
            if (!string.IsNullOrWhiteSpace(message.MetadataJson))
            {
                await writer.WriteAsync("<p>Message details</p><pre>".AsMemory(), ct);
                await WriteEncodedAsync(writer, message.MetadataJson, ct, progress);
                await writer.WriteAsync("</pre>".AsMemory(), ct);
            }
            foreach (var attachment in message.Attachments)
            {
                if (files.TryGetValue(attachment.Id, out var relative))
                {
                    await writer.WriteAsync(("<a href=\"" + relative + "\">").AsMemory(), ct);
                    await WriteEncodedAsync(writer, attachment.FileName, ct, progress);
                    await writer.WriteAsync("</a><br>".AsMemory(), ct);
                }
                else
                {
                    await writer.WriteAsync("<p>Attachment metadata: ".AsMemory(), ct);
                    await WriteEncodedAsync(writer, attachment.FileName, ct, progress);
                    await writer.WriteAsync(" (not copied)</p>".AsMemory(), ct);
                }
            }
            await writer.WriteAsync("</article>".AsMemory(), ct);
            await writer.FlushAsync(ct);
            progress();
        }
        await writer.WriteAsync("</html>".AsMemory(), ct);
        await writer.FlushAsync(ct);
        progress();
    }

    private static async Task WriteEncodedAsync(StreamWriter writer, string? text, CancellationToken ct, Action progress)
    {
        if (text is null) return;
        for (var offset = 0; offset < text.Length;)
        {
            var count = Math.Min(8192, text.Length - offset);
            // Keep surrogate pairs together across chunks so astral Unicode escapes are preserved.
            if (offset + count < text.Length && char.IsHighSurrogate(text[offset + count - 1])) count--;
            var encoded = WebUtility.HtmlEncode(text.Substring(offset, count));
            await writer.WriteAsync(encoded.AsMemory(), ct);
            offset += count;
            progress();
        }
    }

    private static async Task AtomicWriteAsync(string path, Func<Stream, Task> write, CancellationToken ct)
    {
        RejectLink(path);
        var temporary = path + ".part";
        RejectLink(temporary);
        try
        {
            await using (var output = OpenPrivateWriteStream(temporary))
            {
                ct.ThrowIfCancellationRequested();
                await write(output);
            }
            ct.ThrowIfCancellationRequested();
            File.Move(temporary, path, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
