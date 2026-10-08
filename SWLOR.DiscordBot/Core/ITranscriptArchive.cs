namespace SWLOR.DiscordBot.Core;

public interface ITranscriptArchive
{
    // Plan a managed directory without writing files, so ownership can commit before publication.
    string GetArchivePath(Ticket ticket);
    // Write an immutable complete pair inside the durably owned root; return its snapshot directory.
    Task<string> ExportAsync(Ticket ticket, TranscriptSnapshot snapshot, CancellationToken ct);
    Task<string> ExportAsync(Ticket ticket, TranscriptSnapshot snapshot, CancellationToken ct, Action progress) => ExportAsync(ticket, snapshot, ct);
    // Prune only with the current durable ticket, after its snapshot selection has committed.
    Task PruneSnapshotsAsync(Ticket ticket, CancellationToken ct) => Task.CompletedTask;
    Task DeleteAsync(string path, CancellationToken ct);
}
