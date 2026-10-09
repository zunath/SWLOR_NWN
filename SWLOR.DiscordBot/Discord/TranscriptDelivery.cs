using System.Globalization;
using System.IO.Compression;

namespace SWLOR.DiscordBot.Discord;

internal static class TranscriptDelivery
{
    private const int BufferSize = 65536;

    internal static async Task SendAsync(string transcriptPath, ulong attachmentSizeLimit,
        Func<Stream, string, string, CancellationToken, Task> upload, CancellationToken ct,
        Func<CancellationToken, Task>? authorize = null)
    {
        ct.ThrowIfCancellationRequested();
        if (attachmentSizeLimit == 0)
            throw new DiscordValidationException("Discord did not provide a usable attachment size limit for this transcript. Retry the export.");
        var limit = (long)Math.Min(attachmentSizeLimit, (ulong)long.MaxValue);
        await using var source = new FileStream(transcriptPath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, true);
        if (source.Length <= limit)
        {
            await UploadAsync(source, "transcript.html", "Ticket transcript");
            return;
        }

        async Task UploadAsync(Stream stream, string name, string text)
        {
            ct.ThrowIfCancellationRequested();
            // Preparation and earlier parts can take minutes; authorize at each publication boundary.
            if (authorize is not null) await authorize(ct);
            ct.ThrowIfCancellationRequested();
            await upload(stream, name, text, ct);
        }

        // Use the tracked archive volume: the worker's /tmp is a small tmpfs, while tickets can
        // exceed its capacity. An interrupted temporary file remains inside retention ownership.
        var temporaryDirectory = Path.GetDirectoryName(source.Name)!;
        await using var compressed = TemporaryFile(temporaryDirectory);
        await using (var gzip = new GZipStream(compressed, CompressionLevel.Fastest, leaveOpen: true))
            await source.CopyToAsync(gzip, BufferSize, ct);
        compressed.Position = 0;
        if (compressed.Length <= limit)
        {
            await UploadAsync(compressed, "transcript.html.gz",
                "Ticket transcript (gzip). Decompress transcript.html.gz to transcript.html to read the complete transcript.");
            return;
        }

        var total = (compressed.Length - 1) / limit + 1;
        var digits = Math.Max(6, total.ToString(CultureInfo.InvariantCulture).Length);
        var buffer = new byte[BufferSize];
        for (long index = 1; index <= total; index++)
        {
            ct.ThrowIfCancellationRequested();
            await using var part = TemporaryFile(temporaryDirectory);
            var remaining = Math.Min(limit, compressed.Length - compressed.Position);
            while (remaining > 0)
            {
                var read = await compressed.ReadAsync(buffer.AsMemory(0, (int)Math.Min(buffer.Length, remaining)), ct);
                if (read == 0) throw new InvalidDataException("The compressed transcript ended unexpectedly.");
                await part.WriteAsync(buffer.AsMemory(0, read), ct);
                remaining -= read;
            }
            part.Position = 0;
            var name = "transcript.html.gz.part" + index.ToString("D" + digits, CultureInfo.InvariantCulture);
            var instructions = $"Ticket transcript part {index} of {total}. Download all {total} parts into an empty folder with filenames unchanged. Concatenate them as binary bytes in filename order into transcript.html.gz, then decompress that gzip file to transcript.html. Do not join them as text.\n" +
                "Linux/macOS:\n```sh\ncat transcript.html.gz.part* > transcript.html.gz && gzip -d transcript.html.gz\n```\n" +
                "Windows PowerShell:\n```powershell\n$out=[IO.File]::Create('transcript.html.gz')\nGet-ChildItem -File 'transcript.html.gz.part*' | Sort-Object Name | ForEach-Object { $part=[IO.File]::OpenRead($_.FullName); $part.CopyTo($out); $part.Dispose() }\n$out.Dispose()\n```\nThen extract transcript.html.gz with a gzip-compatible archive tool.";
            await UploadAsync(part, name, instructions);
        }
    }

    private static FileStream TemporaryFile(string directory)
    {
        var options = new FileStreamOptions
        {
            Mode = FileMode.CreateNew, Access = FileAccess.ReadWrite, Share = FileShare.None, BufferSize = BufferSize,
            Options = FileOptions.Asynchronous | FileOptions.DeleteOnClose
        };
        if (!OperatingSystem.IsWindows()) options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        return new FileStream(Path.Combine(directory, "swlor-transcript-" + Guid.NewGuid().ToString("N") + ".tmp"), options);
    }
}