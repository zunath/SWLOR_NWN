using System.Net;
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
        EnsurePrivateDirectory(Root);
        var directory = TicketDirectory(ticket.Id);
        EnsurePrivateDirectory(directory);
        var attachmentDirectory = Path.Combine(directory, "attachments");
        EnsurePrivateDirectory(attachmentDirectory);
        RepairExistingPermissions(directory);
        var files = new Dictionary<ulong, string>();
        if (configuration.Tickets.CopyAttachments)
        {
            foreach (var attachment in snapshot.Messages.SelectMany(m => m.Attachments).DistinctBy(a => a.Id))
            {
                var extension = Path.GetExtension(attachment.FileName);
                if (!Regex.IsMatch(extension, "^\\.[a-zA-Z0-9]{1,8}$")) extension = ".bin";
                var relative = $"attachments/{attachment.Id}{extension}";
                var target = Path.Combine(directory, relative.Replace('/', Path.DirectorySeparatorChar));
                if (attachment.Size < 0 || attachment.Size > configuration.Tickets.MaxAttachmentBytes)
                    throw new InvalidDataException("Ticket attachment exceeds the configured archive limit; cleanup is suspended.");
                if (File.Exists(target))
                {
                    RejectLink(target);
                    if (new FileInfo(target).Length != attachment.Size)
                        throw new InvalidDataException("Cached attachment size does not match its Discord metadata; cleanup is suspended.");
                    files[attachment.Id] = relative;
                    ct.ThrowIfCancellationRequested();
                    progress();
                    continue;
                }
                if (!Uri.TryCreate(attachment.Url, UriKind.Absolute, out var url) || url.Scheme != Uri.UriSchemeHttps ||
                    !AttachmentHosts.Contains(url.Host) || !url.IsDefaultPort || !string.IsNullOrEmpty(url.UserInfo))
                    throw new InvalidDataException("Attachment URL is not an allowed Discord CDN URL.");
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeout.CancelAfter(TimeSpan.FromMinutes(2));
                using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
                response.EnsureSuccessStatusCode();
                progress();
                if (response.RequestMessage?.RequestUri != url)
                    throw new InvalidDataException("Attachment redirects are not supported.");
                if (response.Content.Headers.ContentLength > configuration.Tickets.MaxAttachmentBytes)
                    throw new InvalidDataException("Ticket attachment exceeds the configured archive limit.");
                var temporary = target + ".part";
                RejectLink(temporary);
                try
                {
                    await using (var output = OpenPrivateWriteStream(temporary))
                    await using (var input = await response.Content.ReadAsStreamAsync(timeout.Token))
                    {
                        var buffer = new byte[81920];
                        long bytes = 0;
                        int read;
                        while ((read = await input.ReadAsync(buffer, timeout.Token)) > 0)
                        {
                            bytes += read;
                            if (bytes > configuration.Tickets.MaxAttachmentBytes) throw new InvalidDataException("Attachment is larger than allowed.");
                            await output.WriteAsync(buffer.AsMemory(0, read), timeout.Token);
                            progress();
                        }
                        if (bytes != attachment.Size) throw new InvalidDataException("Attachment size does not match its Discord metadata.");
                    }
                    File.Move(temporary, target, true);
                    progress();
                }
                finally { if (File.Exists(temporary)) File.Delete(temporary); }
                files[attachment.Id] = relative;
            }
        }
        var json = JsonSerializer.Serialize(new { ticket.Id, ticket.Number, ticket.RequesterId, ticket.ChannelId,
            ExportedAt = DateTimeOffset.UtcNow, snapshot.LastMessageId, snapshot.Messages, AttachmentFiles = files }, new JsonSerializerOptions { WriteIndented = true });
        var html = new StringBuilder("<!doctype html><html lang=\"en\"><meta charset=\"utf-8\"><meta http-equiv=\"Content-Security-Policy\" content=\"default-src 'none'; style-src 'unsafe-inline'; base-uri 'none'; form-action 'none'\"><title>Ticket transcript</title><style>body{font:16px system-ui;max-width:900px;margin:2rem auto;padding:1rem}article{border-bottom:1px solid #ccc;padding:1rem 0}pre{white-space:pre-wrap;overflow-wrap:anywhere}</style><h1>Ticket ")
            .Append(ticket.Number).Append("</h1>");
        foreach (var message in snapshot.Messages.OrderBy(m => m.Id))
        {
            html.Append("<article><b>").Append(WebUtility.HtmlEncode(message.AuthorName)).Append("</b> (").Append(message.AuthorId)
                .Append(") <time>").Append(WebUtility.HtmlEncode(message.Timestamp.ToString("O"))).Append("</time><pre>")
                .Append(WebUtility.HtmlEncode(message.Content)).Append("</pre>");
            if (!string.IsNullOrWhiteSpace(message.EmbedsJson)) html.Append("<pre>").Append(WebUtility.HtmlEncode(message.EmbedsJson)).Append("</pre>");
            foreach (var attachment in message.Attachments)
            {
                if (files.TryGetValue(attachment.Id, out var relative)) html.Append("<a href=\"").Append(relative).Append("\">").Append(WebUtility.HtmlEncode(attachment.FileName)).Append("</a><br>");
                else html.Append("<p>Attachment metadata: ").Append(WebUtility.HtmlEncode(attachment.FileName)).Append(" (not copied)</p>");
            }
            html.Append("</article>");
            ct.ThrowIfCancellationRequested();
            progress();
        }
        html.Append("</html>");
        await AtomicWriteAsync(Path.Combine(directory, "transcript.json"), json, ct);
        progress();
        await AtomicWriteAsync(Path.Combine(directory, "transcript.html"), html.ToString(), ct);
        progress();
        return directory;
    }

    public Task DeleteAsync(string path, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var full = Path.GetFullPath(path);
        if (!Guid.TryParseExact(Path.GetFileName(full), "N", out var id) || !PathEquals(full, TicketDirectory(id)))
            throw new InvalidOperationException("Archive path does not identify a managed ticket directory.");
        if (!Directory.Exists(full)) return Task.CompletedTask;
        RejectLink(full);
        foreach (var entry in Directory.EnumerateFileSystemEntries(full, "*", SearchOption.AllDirectories)) RejectLink(entry);
        Directory.Delete(full, true);
        return Task.CompletedTask;
    }

    private string TicketDirectory(Guid id) => Path.Combine(Root, id.ToString("N"));
    private static bool PathEquals(string a, string b) => string.Equals(a, b, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    private static void RejectLink(string path)
    {
        if ((File.Exists(path) || Directory.Exists(path)) && File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint))
            throw new InvalidOperationException("Links are not allowed in ticket archive paths.");
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

    private static void RepairExistingPermissions(string directory)
    {
        // Repair old archives and cached attachments before reading or publishing ticket data.
        foreach (var entry in Directory.EnumerateFileSystemEntries(directory))
        {
            RejectLink(entry);
            if (Directory.Exists(entry))
            {
                EnsurePrivateDirectory(entry);
                RepairExistingPermissions(entry);
            }
            else RestrictFile(entry);
        }
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
    private static async Task AtomicWriteAsync(string path, string text, CancellationToken ct)
    {
        RejectLink(path);
        var temporary = path + ".part";
        RejectLink(temporary);
        try
        {
            await using (var output = OpenPrivateWriteStream(temporary))
            await using (var writer = new StreamWriter(output, new UTF8Encoding(false)))
                await writer.WriteAsync(text.AsMemory(), ct);
            File.Move(temporary, path, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
