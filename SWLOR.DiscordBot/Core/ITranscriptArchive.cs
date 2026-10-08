namespace SWLOR.DiscordBot.Core;

public interface ITranscriptArchive
{
    // Plan a managed directory without writing files, so ownership can commit before publication.
    string GetArchivePath(Ticket ticket);
    Task<string> ExportAsync(Ticket ticket, TranscriptSnapshot snapshot, CancellationToken ct);
    Task<string> ExportAsync(Ticket ticket, TranscriptSnapshot snapshot, CancellationToken ct, Action progress) => ExportAsync(ticket, snapshot, ct);
    Task DeleteAsync(string path, CancellationToken ct);
}
