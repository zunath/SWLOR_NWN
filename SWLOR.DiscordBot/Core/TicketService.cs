using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using SWLOR.DiscordBot.Configuration;

namespace SWLOR.DiscordBot.Core;

public sealed class TicketService(BotConfiguration configuration, ITicketStore store,
    IDiscordTickets discord, ITranscriptArchive archive, TimeProvider clock,
    ILogger<TicketService>? logger = null)
{
    // One worker owns the guild; in-flight exports protect only their ticket's archive from cleanup.
    private readonly ConcurrentDictionary<Guid, byte> _exports = new();
    private readonly ConcurrentDictionary<Guid, byte> _creations = new();
    private readonly ConcurrentDictionary<Guid, byte> _archiveDeletions = new();
    private Guid? _lastMaintenanceTicketId;
    private static readonly TimeSpan TicketMaintenanceInactivityTimeout = TimeSpan.FromMinutes(4);
    private TicketOptions Options => configuration.Tickets;
    public bool CanSupport(Actor actor) => actor.IsGuildOwner || actor.RoleIds.Intersect(Options.SupportRoleIds).Any();
    private bool Bypasses(Actor actor, bool? scope) => scope == true && actor.RoleIds.Intersect(Options.BypassRoleIds).Any();
    private static bool Active(Ticket t) => t.State is TicketState.Creating or TicketState.Open or TicketState.Closing or TicketState.Reopening;

    public async Task<TicketResult> OpenAsync(string panelId, Actor actor, string interactionId, CancellationToken ct = default)
    {
        if (!Options.Enabled) return new(false, "Ticketing is disabled.");
        var panel = Options.Panels.SingleOrDefault(p => p.Id == panelId);
        if (panel is null) return new(false, "This ticket panel is unavailable.");
        Ticket ticket;
        await using (var session = await store.LockAsync(ct))
        {
            actor = await discord.ActorAsync(actor.UserId, ct);
            var previous = await session.FindInteractionAsync(interactionId, ct);
            if (previous is not null)
            {
                if (previous.RequesterId != actor.UserId) return new(false, "This interaction belongs to another requester.");
                if (previous.State != TicketState.Creating)
                    return new(true, "This interaction has already been handled.", previous);
                ticket = previous;
            }
            else
            {
                var tickets = await session.GetTicketsAsync(ct);
                var pending = tickets.FirstOrDefault(t => t.RequesterId == actor.UserId && t.PanelId == panelId && t.State == TicketState.Creating);
                if (pending is not null) ticket = pending;
                else
                {
                    var active = tickets.Where(Active).ToArray();
                    if (!Bypasses(actor, Options.BypassMemberLimit) && active.Count(t => t.RequesterId == actor.UserId) >= Options.MemberLimit)
                        return new(false, "You already have the maximum number of open tickets.", active.FirstOrDefault(t => t.RequesterId == actor.UserId));
                    if (!Bypasses(actor, Options.BypassPanelLimit) && active.Count(t => t.PanelId == panelId) >= panel.OpenLimit)
                        return new(false, "This ticket panel has reached its open-ticket limit.");
                    if (!Bypasses(actor, Options.BypassGuildLimit) && active.Length >= Options.GuildLimit)
                        return new(false, "The server has reached its open-ticket limit.");
                    ticket = await session.ReserveAsync(panelId, actor.UserId, interactionId, clock.GetUtcNow(), ct);
                }
            }
        }
        return await ResumeCreationAsync(ticket.Id, ct);
    }

    private async Task<TicketResult> ResumeCreationAsync(Guid id, CancellationToken ct, Action? progress = null)
    {
        var claimed = false;
        Guid? creationAttemptId = null;
        var createStarted = false;
        try
        {
            Ticket ticket;
            await using (var session = await store.LockAsync(ct))
            {
                ticket = await session.GetTicketAsync(id, ct) ?? throw new InvalidOperationException("Ticket record disappeared.");
                if (ticket.State != TicketState.Creating)
                    return new(true, "This interaction has already been handled.", ticket);
                if (!_creations.TryAdd(id, 0))
                    return new(false, "This ticket is already being opened; try again shortly.", ticket);
                claimed = true;
            }
            // Discovery, creation, and history scans can wait on Discord rate limits without blocking other tickets.
            progress ??= static () => { };
            var channel = ticket.ChannelId ?? await WithProgressAsync(discord.FindManagedChannelAsync(ticket.Id, ct), progress);
            if (channel is null)
            {
                await using (var creation = await store.LockAsync(ct))
                {
                    var current = await creation.GetTicketAsync(id, ct) ?? throw new InvalidOperationException("Ticket record disappeared.");
                    if (current.State != TicketState.Creating)
                        return new(true, "This interaction has already been handled.", current);
                    channel = current.ChannelId;
                    if (channel is null)
                    {
                        // The claim survives lease loss, process restarts, and remote outcomes that outlive cancellation.
                        // Absence in a discovery response cannot prove a previous POST will never create a channel.
                        if (current.ChannelCreationAttemptId is not null)
                            throw new InvalidOperationException("An earlier channel creation remains unconfirmed. Discovery will recover a visible channel; staff must investigate if none appears.");
                        creationAttemptId = Guid.NewGuid();
                        ticket = current with { ChannelCreationAttemptId = creationAttemptId };
                        await creation.SaveAsync(ticket, "channel-creation-claimed", null, ct);
                    }
                }
                if (channel is null)
                {
                    createStarted = true;
                    try { channel = await WithProgressAsync(discord.CreateAsync(ticket, ct), progress); }
                    catch (ChannelCreationNotSentException)
                    {
                        await ReleaseUnsentCreationAsync(id, ticket.ChannelCreationAttemptId!.Value);
                        ct.ThrowIfCancellationRequested();
                        throw;
                    }
                }
            }
            ct.ThrowIfCancellationRequested();
            await using (var binding = await store.LockAsync(ct))
            {
                var current = await binding.GetTicketAsync(id, ct) ?? throw new InvalidOperationException("Ticket record disappeared.");
                if (current.State != TicketState.Creating || current.ChannelId is { } existing && existing != channel)
                    throw new InvalidOperationException("Ticket creation binding changed during channel discovery.");
                ticket = current with { ChannelId = channel, ChannelCreationAttemptId = null };
                if (current.ChannelId != channel)
                    await binding.SaveAsync(ticket, "channel-bound", ticket.RequesterId, ct);
            }
            await discord.OpenAsync(ticket, true, ct, progress);
            ct.ThrowIfCancellationRequested();
            await using (var completed = await store.LockAsync(ct))
            {
                var current = await completed.GetTicketAsync(id, ct) ?? throw new InvalidOperationException("Ticket record disappeared.");
                if (current.State != TicketState.Creating || current.ChannelId != ticket.ChannelId)
                    throw new InvalidOperationException("Ticket creation binding changed during opening.");
                ticket = current with { State = TicketState.Open, LastError = null };
                await completed.SaveAsync(ticket, "opened", ticket.RequesterId, ct);
            }
            await NotifyAsync($"Ticket {ticket.Number} opened.", ct);
            return new(true, $"Your ticket is ready: <#{ticket.ChannelId}>.", ticket);
        }
        catch (Exception ex)
        {
            if (creationAttemptId is { } attempt && !createStarted)
                await ReleaseUnsentCreationAsync(id, attempt);
            if (!claimed || ct.IsCancellationRequested) throw;
            logger?.LogWarning(ex, "Ticket {TicketId} creation will be reconciled", id);
            await using var failed = await store.LockAsync(ct);
            var current = await failed.GetTicketAsync(id, ct) ?? throw new InvalidOperationException("Ticket record disappeared.");
            var unconfirmed = current.ChannelCreationAttemptId is not null;
            var error = unconfirmed
                ? "Channel creation is unconfirmed; discovery pending. Staff must investigate before another creation attempt."
                : "Channel creation or opening failed; reconciliation pending.";
            if (current.State == TicketState.Creating && current.LastError != error)
            {
                current = current with { LastError = error };
                await failed.SaveAsync(current, "creation-failed", null, ct);
            }
            return new(false, unconfirmed
                ? "Channel creation is unconfirmed. Recovery will keep checking for its channel; staff must investigate before another creation attempt."
                : "The ticket could not be opened yet. Staff can check the bot logs; retrying will recover this request.", current);
        }
        finally { if (claimed) _creations.TryRemove(id, out _); }
    }

    private async Task ReleaseUnsentCreationAsync(Guid id, Guid attemptId)
    {
        // Cancellation before POST is safe to release even after the caller/worker token has ended.
        // A failed cleanup keeps the durable fence; never infer success from a timeout here either.
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        try
        {
            await using var session = await store.LockAsync(timeout.Token);
            var current = await session.GetTicketAsync(id, timeout.Token);
            if (current is { State: TicketState.Creating, ChannelId: null } && current.ChannelCreationAttemptId == attemptId)
                await session.SaveAsync(current with { ChannelCreationAttemptId = null }, "channel-creation-not-sent", null, timeout.Token);
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Ticket {TicketId} creation claim retained after an unsent request; staff must investigate", id);
        }
    }
    public Task<TicketResult> CloseAsync(ulong channelId, Actor actor, CancellationToken ct = default) => MutateAsync(channelId, actor, async (session, ticket, currentActor) =>
    {
        if (ticket.RequesterId != currentActor.UserId && !CanSupport(currentActor)) return new(false, "Only the requester or support staff can close this ticket.");
        if (ticket.State == TicketState.Closed) return new(true, "This ticket is already closed.", ticket);
        if (ticket.State is not (TicketState.Open or TicketState.Closing)) return new(false, "This ticket is busy; try again shortly.");
        var now = ticket.ClosedAt ?? clock.GetUtcNow();
        ticket = ticket with { State = TicketState.Closing, ClosedAt = now, DeleteAfter = now + Options.CleanupDelay, LastError = null,
            ArchiveExpiresAt = now.AddDays(Options.ArchiveRetentionDays) };
        await session.SaveAsync(ticket, "closing", currentActor.UserId, ct);
        await discord.CloseAsync(ticket, ct);
        ticket = ticket with { State = TicketState.Closed };
        await session.SaveAsync(ticket, "closed", currentActor.UserId, ct);
        await NotifyAsync($"Ticket {ticket.Number} closed.", ct);
        return new(true, "Ticket closed. Staff can reopen it before cleanup.", ticket);
    }, ct);

    public Task<TicketResult> RenameAsync(ulong channelId, Actor actor, string name, CancellationToken ct = default) => MutateAsync(channelId, actor, async (session, ticket, currentActor) =>
    {
        if (!CanSupport(currentActor)) return new(false, "Only support staff can rename tickets.");
        if (ticket.State is not (TicketState.Open or TicketState.Closed)) return new(false, "This ticket is busy; try again shortly.");
        var normalized = Regex.Replace(name.Trim().ToLowerInvariant(), "[^a-z0-9_-]+", "-").Trim('-');
        if (normalized.Length is < 1 or > 100) return new(false, "Use a channel name between 1 and 100 characters.");
        await discord.RenameAsync(ticket, normalized, ct);
        await session.SaveAsync(ticket, "renamed", currentActor.UserId, ct);
        return new(true, "Ticket renamed.", ticket);
    }, ct);

    public Task<TicketResult> ReopenAsync(ulong channelId, Actor actor, CancellationToken ct = default) => MutateAsync(channelId, actor, async (session, ticket, currentActor) =>
    {
        if (!CanSupport(currentActor)) return new(false, "Only support staff can reopen tickets.");
        if (ticket.State == TicketState.Open) return new(true, "This ticket is already open.", ticket);
        if (ticket.State is not (TicketState.Closed or TicketState.Reopening)) return new(false, "This ticket cannot be reopened while an operation is in progress.");
        var tickets = (await session.GetTicketsAsync(ct)).Where(t => t.Id != ticket.Id && Active(t)).ToArray();
        var panel = Options.Panels.Single(p => p.Id == ticket.PanelId);
        // Refresh the requester, so staff privileges and stale membership never grant a bypass.
        var requester = await discord.ActorAsync(ticket.RequesterId, ct);
        if ((!Bypasses(requester, Options.BypassMemberLimit) && tickets.Count(t => t.RequesterId == ticket.RequesterId) >= Options.MemberLimit) ||
            (!Bypasses(requester, Options.BypassPanelLimit) && tickets.Count(t => t.PanelId == ticket.PanelId) >= panel.OpenLimit) ||
            (!Bypasses(requester, Options.BypassGuildLimit) && tickets.Length >= Options.GuildLimit))
            return new(false, "Reopening would exceed an open-ticket limit.");
        ticket = ticket with { State = TicketState.Reopening, DeleteAfter = null, ClosedAt = null, ArchiveExpiresAt = null, LastError = null };
        await session.SaveAsync(ticket, "reopening", currentActor.UserId, ct);
        await discord.OpenAsync(ticket, false, ct);
        ticket = ticket with { State = TicketState.Open };
        await session.SaveAsync(ticket, "reopened", currentActor.UserId, ct);
        return new(true, "Ticket reopened; scheduled cleanup cancelled.", ticket);
    }, ct);

    public Task<TicketResult> SetHoldAsync(ulong channelId, Actor actor, bool hold, CancellationToken ct = default) => MutateAsync(channelId, actor, async (session, ticket, currentActor) =>
    {
        if (!CanSupport(currentActor)) return new(false, "Only support staff can change archive holds.");
        if (ticket.State == TicketState.Deleted) return new(false, "The ticket channel has already been deleted.");
        ticket = ticket with { Hold = hold };
        await session.SaveAsync(ticket, hold ? "hold-set" : "hold-released", currentActor.UserId, ct);
        return new(true, hold ? "Cleanup and archive expiration are on hold." : "Hold released.", ticket);
    }, ct);

    public async Task<TicketResult> ExportAsync(ulong channelId, Actor actor, CancellationToken ct = default,
        Func<Ticket, CancellationToken, Task>? deliver = null, bool useSavedArchive = false)
    {
        if (!CanSupport(actor)) return new(false, "Only support staff can export transcripts.");
        Guid? exportId = null;
        try
        {
            Ticket ticket;
            await using (var session = await store.LockAsync(ct))
            {
                var found = await session.FindByChannelAsync(channelId, ct);
                if (found is null) return new(false, "This channel is not a ticket managed by this bot.");
                ticket = found;
                if (_archiveDeletions.ContainsKey(ticket.Id)) return new(false, "Archive cleanup is in progress for this ticket; try again shortly.");
                if (ticket.State is not (TicketState.Open or TicketState.Closed)) return new(false, "This ticket is busy; try again shortly.");
                if (useSavedArchive && (!ticket.ArchiveComplete || ticket.ArchivePath is null))
                    return new(false, "This ticket has no complete saved transcript. Export a fresh transcript first.");
                if (!_exports.TryAdd(ticket.Id, 0)) return new(false, "A transcript export is already in progress for this ticket.");
                exportId = ticket.Id;
                if (!useSavedArchive)
                {
                    ticket = ticket with { ArchivePath = ticket.ArchivePath ?? archive.GetArchivePath(ticket) };
                    await session.SaveAsync(ticket, "archive-pending", actor.UserId, ct);
                }
            }
            Ticket current;
            if (useSavedArchive)
            {
                // A fresh interaction can deliver a long export that outlived Discord's interaction token.
                // Recheck both live staff membership and exact managed-channel identity before preparation.
                var currentActor = await discord.ActorAsync(actor.UserId, ct);
                if (!CanSupport(currentActor)) return new(false, "Support access changed; transcript delivery was denied.", ticket);
                if (!await discord.ExistsAsync(ticket, ct)) return new(false, "The managed ticket channel is unavailable.", ticket);
                current = ticket;
            }
            else
            {
                // Discord pagination and attachment downloads must not hold the shared database lock.
                string path;
                using (var progressTimeout = new MaintenanceProgressTimeout(clock, ct))
                {
                    await PrunePublishedSnapshotsAsync(ticket, progressTimeout.Token, progressTimeout.ReportProgress);
                    var snapshot = await discord.ReadTranscriptAsync(ticket, progressTimeout.Token, progressTimeout.ReportProgress);
                    progressTimeout.ReportProgress();
                    path = await archive.ExportAsync(ticket, snapshot, progressTimeout.Token, progressTimeout.ReportProgress);
                    progressTimeout.Token.ThrowIfCancellationRequested();
                }
                await using (var saveSession = await store.LockAsync(ct))
                {
                    current = await saveSession.GetTicketAsync(ticket.Id, ct) ?? throw new InvalidOperationException("Ticket record disappeared.");
                    // Preserve closes, reopens, holds, and deadlines changed while the snapshot was written.
                    current = current with { ArchiveSnapshotPath = path, ArchiveComplete = true };
                    await saveSession.SaveAsync(current, "exported", actor.UserId, ct);
                }
                await PrunePublishedSnapshotsAsync(current, ct);
            }
            // Keep this ticket's files protected through delivery without blocking unrelated mutations.
            if (deliver is not null)
            {
                if (useSavedArchive)
                {
                    await using (var session = await store.LockAsync(ct))
                        current = await session.GetTicketAsync(ticket.Id, ct) ?? throw new InvalidOperationException("Ticket record disappeared.");
                    if (current.ChannelId != channelId || current.State is not (TicketState.Open or TicketState.Closed) ||
                        !current.ArchiveComplete || current.ArchivePath != ticket.ArchivePath ||
                        current.ArchiveSnapshotPath != ticket.ArchiveSnapshotPath)
                        return new(false, "The saved transcript is no longer available for this ticket.", current);
                    if (!await discord.ExistsAsync(current, ct)) return new(false, "The managed ticket channel is unavailable.", current);
                }
                var currentActor = await discord.ActorAsync(actor.UserId, ct);
                if (!CanSupport(currentActor)) return new(false, "Support access changed; transcript delivery was denied.", current);
                ct.ThrowIfCancellationRequested();
                await deliver(current, ct);
            }
            return new(true, useSavedArchive ? "Latest saved transcript ready." : "Transcript archived.", current);
        }
        finally { if (exportId is { } id) _exports.TryRemove(id, out _); }
    }

    private async Task<TicketResult> MutateAsync(ulong channelId, Actor actor,
        Func<ITicketSession, Ticket, Actor, Task<TicketResult>> mutation, CancellationToken ct)
    {
        await using var session = await store.LockAsync(ct);
        var ticket = await session.FindByChannelAsync(channelId, ct);
        if (ticket is null) return new(false, "This channel is not a ticket managed by this bot.");
        if (_archiveDeletions.ContainsKey(ticket.Id)) return new(false, "Archive cleanup is in progress for this ticket; try again shortly.");
        // Lock contention can outlive a role change or guild departure. Authorize the current member.
        var currentActor = await discord.ActorAsync(actor.UserId, ct);
        return await mutation(session, ticket, currentActor);
    }

    public async Task ReconcileRetainedPermissionsAsync(CancellationToken ct = default)
    {
        Guid[] ticketIds;
        await using (var batch = await store.LockAsync(ct))
            ticketIds = (await batch.GetTicketsAsync(ct))
                .Where(ticket => ticket.State is TicketState.Open or TicketState.Closed)
                .Select(ticket => ticket.Id).ToArray();
        foreach (var id in ticketIds)
        {
            ct.ThrowIfCancellationRequested();
            using var progressTimeout = new MaintenanceProgressTimeout(clock, ct);
            var token = progressTimeout.Token;
            await using var session = await store.LockAsync(token);
            var ticket = await WithProgressAsync(session.GetTicketAsync(id, token), progressTimeout.ReportProgress);
            if (ticket is null || ticket.State is not (TicketState.Open or TicketState.Closed)) continue;
            // A missing channel is handled by maintenance; an inaccessible/unmanaged channel must fail readiness.
            if (await WithProgressAsync(discord.ExistsAsync(ticket, token), progressTimeout.ReportProgress))
                await WithProgressAsync(discord.ReconcilePermissionsAsync(ticket, token), progressTimeout.ReportProgress);
        }
    }
    public async Task MaintainAsync(CancellationToken ct = default)
    {
        Guid[] ticketIds;
        await using (var batch = await store.LockAsync(ct))
            ticketIds = (await batch.GetTicketsAsync(ct)).Select(ticket => ticket.Id).ToArray();
        // Resume after the last attempted ticket if the overall sweep was interrupted.
        var previousIndex = _lastMaintenanceTicketId is { } previous ? Array.IndexOf(ticketIds, previous) : -1;
        var start = previousIndex + 1;
        foreach (var id in ticketIds.Skip(start).Concat(ticketIds.Take(start)))
        {
            ct.ThrowIfCancellationRequested();
            _lastMaintenanceTicketId = id;
            using var progressTimeout = new MaintenanceProgressTimeout(clock, ct);
            try
            {
                await MaintainTicketAsync(id, progressTimeout.Token, progressTimeout.ReportProgress);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                await RecordMaintenanceFailureAsync(id, ex, ct);
            }
        }
        // Every expiration owner uses the retrying pass. A peer may skip an occupied guard, but its owner
        // retains failed IDs and retries them after later entries instead of dropping a one-shot failure.
        await ExpireArchivesAsync(ticketIds, ct);
    }

    public async Task ExpireArchivesAsync(CancellationToken ct = default)
    {
        Guid[] ticketIds;
        await using (var batch = await store.LockAsync(ct))
            ticketIds = (await batch.GetTicketsAsync(ct)).Where(ArchiveHasExpired).Select(ticket => ticket.Id).ToArray();
        await ExpireArchivesAsync(ticketIds, ct);
    }

    private async Task ExpireArchivesAsync(Guid[] ticketIds, CancellationToken ct)
    {
        for (var attempt = 0; attempt < 3 && ticketIds.Length > 0; attempt++)
        {
            var failed = new List<Guid>();
            foreach (var id in ticketIds)
            {
                ct.ThrowIfCancellationRequested();
                using var progressTimeout = new MaintenanceProgressTimeout(clock, ct);
                try { await ExpireArchiveAsync(id, progressTimeout.Token, progressTimeout.ReportProgress); }
                catch (Exception ex) when (!ct.IsCancellationRequested)
                {
                    // Finish later entries before retrying only the failed IDs with fresh eligibility/ownership.
                    await RecordMaintenanceFailureAsync(id, ex, ct);
                    failed.Add(id);
                }
            }
            ticketIds = failed.ToArray();
            if (ticketIds.Length > 0 && attempt < 2)
                await Task.Delay(TimeSpan.FromSeconds(5 * (attempt + 1)), clock, ct);
        }
    }

    private async Task ExpireArchiveAsync(Guid id, CancellationToken ct, Action progress)
    {
        var claimed = false;
        try
        {
            Ticket ticket;
            await using (var session = await store.LockAsync(ct))
            {
                var found = await session.GetTicketAsync(id, ct);
                if (found is null || _exports.ContainsKey(id) || !ArchiveHasExpired(found) ||
                    !_archiveDeletions.TryAdd(id, 0)) return;
                claimed = true;
                ticket = found;
            }
            // Only this ticket's archive and retention controls are guarded during the long traversal.
            // The durable path remains owned after cancellation, a failed delete or an interrupted save.
            await archive.DeleteAsync(ticket.ArchivePath!, ct, progress);
            ct.ThrowIfCancellationRequested();
            await using var completed = await store.LockAsync(ct);
            var current = await completed.GetTicketAsync(id, ct);
            if (current is not null && current.ArchivePath == ticket.ArchivePath && ArchiveHasExpired(current))
                await completed.SaveAsync(current with { ArchivePath = null, ArchiveSnapshotPath = null,
                    ArchiveComplete = false, LastError = null }, "archive-expired", null, ct);
        }
        finally { if (claimed) _archiveDeletions.TryRemove(id, out _); }
    }
    private bool ArchiveHasExpired(Ticket ticket) => !ticket.Hold && ticket.ArchivePath is not null &&
        ticket.State is (TicketState.Closed or TicketState.Closing or TicketState.Deleting or TicketState.Deleted) &&
        ticket.ArchiveExpiresAt <= clock.GetUtcNow();

    private async Task RecordMaintenanceFailureAsync(Guid id, Exception ex, CancellationToken ct)
    {
        logger?.LogWarning(ex, "Ticket {TicketId} maintenance failed; retained for retry", id);
        await using var session = await store.LockAsync(ct);
        // Keep the last durable archive and state even when a later remote operation timed out.
        var durable = await session.GetTicketAsync(id, ct);
        const string error = "Maintenance failed; retry scheduled. Check worker logs.";
        if (durable is not null && durable.LastError != error)
            await session.SaveAsync(durable with { LastError = error }, "maintenance-failed", null, ct);
    }

    private async Task MaintainTicketAsync(Guid id, CancellationToken ct, Action progress)
    {
        var exporting = false;
        try
        {
            Ticket ticket;
            await using (var session = await store.LockAsync(ct))
            {
                var found = await session.GetTicketAsync(id, ct);
                if (found is null) return;
                ticket = found;
                if (_archiveDeletions.ContainsKey(id) || _creations.ContainsKey(id)) return;
                if (ticket.State != TicketState.Deleted && ticket.ChannelId.HasValue && !await WithProgressAsync(discord.ExistsAsync(ticket, ct), progress))
                {
                    var now = clock.GetUtcNow();
                    ticket = ticket with { State = TicketState.Deleted, ClosedAt = ticket.ClosedAt ?? now, DeleteAfter = null,
                        ArchiveExpiresAt = ticket.ArchiveExpiresAt ?? now.AddDays(Options.ArchiveRetentionDays),
                        LastError = "Channel was removed externally; any previously saved archive is retained until its expiration." };
                    await session.SaveAsync(ticket, "missing-channel-reconciled", null, ct);
                    await NotifyAsync($"Ticket {ticket.Number} channel was removed externally; open-ticket capacity released.", ct);
                }
                // Reconcile held and not-yet-due tickets too, without moving channels or restoring a deleting freeze.
                if (ticket.State is TicketState.Open or TicketState.Closed)
                    await WithProgressAsync(discord.ReconcilePermissionsAsync(ticket, ct), progress);
                // Manual exports in Open/Closed retain that state policy; deleting exports keep their freeze.
                if (_exports.ContainsKey(id)) return;
                if (ticket.State != TicketState.Creating)
                {
                    if (ticket.State == TicketState.Closing)
                    {
                        await WithProgressAsync(discord.CloseAsync(ticket, ct), progress);
                        ticket = ticket with { State = TicketState.Closed, LastError = null };
                        await session.SaveAsync(ticket, "close-reconciled", null, ct);
                    }
                    if (ticket.State == TicketState.Reopening)
                    {
                        await WithProgressAsync(discord.OpenAsync(ticket, false, ct), progress);
                        ticket = ticket with { State = TicketState.Open, LastError = null };
                        await session.SaveAsync(ticket, "reopen-reconciled", null, ct);
                    }
                    if (ticket.Hold) return;
                    if (ticket.State == TicketState.Deleted) return;
                    if (ticket.State == TicketState.Closed && ticket.DeleteAfter <= clock.GetUtcNow())
                    {
                        ticket = ticket with { State = TicketState.Deleting };
                        await session.SaveAsync(ticket, "deleting", null, ct);
                    }
                    if (ticket.State != TicketState.Deleting) return;
                    await WithProgressAsync(discord.FreezeAsync(ticket, ct), progress);
                    if (!_exports.TryAdd(id, 0)) return;
                    exporting = true;
                    // Include partial/completed files in retention even if the final export save never commits.
                    if (ticket.ArchivePath is null)
                    {
                        ticket = ticket with { ArchivePath = archive.GetArchivePath(ticket) };
                        await session.SaveAsync(ticket, "archive-pending", null, ct);
                    }
                }
            }
            if (ticket.State == TicketState.Creating)
            {
                await ResumeCreationAsync(ticket.Id, ct, progress);
                return;
            }
            // Pagination and attachment downloads can take minutes; do not hold the guild ticket lock.
            await WithProgressAsync(PrunePublishedSnapshotsAsync(ticket, ct, progress), progress);
            var snapshot = await WithProgressAsync(discord.ReadTranscriptAsync(ticket, ct, progress), progress);
            var archivePath = await WithProgressAsync(archive.ExportAsync(ticket, snapshot, ct, progress), progress);
            Ticket published;
            await using (var saveSession = await store.LockAsync(ct))
            {
                var current = await saveSession.GetTicketAsync(id, ct) ?? throw new InvalidOperationException("Ticket record disappeared.");
                published = current with { ArchiveSnapshotPath = archivePath, ArchiveComplete = true, LastError = null };
                await saveSession.SaveAsync(published, "cleanup-exported", null, ct);
            }
            await PrunePublishedSnapshotsAsync(published, ct, progress);
            // A stable head alone misses edits/deletions of older messages during pagination or downloads.
            var verified = await WithProgressAsync(discord.ReadTranscriptAsync(ticket, ct, progress), progress);
            if (!SameTranscript(snapshot, verified))
                throw new InvalidOperationException("Ticket transcript changed during archival; retrying before deletion.");
            await using var deleteSession = await store.LockAsync(ct);
            var latest = await deleteSession.GetTicketAsync(id, ct) ?? throw new InvalidOperationException("Ticket record disappeared.");
            if (latest.State != TicketState.Deleting || latest.Hold) return;
            if (await WithProgressAsync(discord.LastMessageIdAsync(latest, ct), progress) != verified.LastMessageId)
                throw new InvalidOperationException("Ticket received new messages during archival; retrying before deletion.");
            ct.ThrowIfCancellationRequested();
            await WithProgressAsync(discord.DeleteAsync(latest, ct), progress);
            await deleteSession.SaveAsync(latest with { State = TicketState.Deleted }, "deleted", null, ct);
            await NotifyAsync($"Ticket {latest.Number} archived and deleted.", ct);
        }
        finally { if (exporting) _exports.TryRemove(id, out _); }
    }

    private async Task PrunePublishedSnapshotsAsync(Ticket ticket, CancellationToken ct, Action? progress = null)
    {
        progress ??= static () => { };
        try { await archive.PruneSnapshotsAsync(ticket, ct, progress); }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            // Invalid retained metadata must not prevent a fresh replacement. All files remain owned for retention.
            logger?.LogWarning(ex, "Ticket {TicketId} superseded snapshots retained for retry", ticket.Id);
        }
    }

    private static async Task<T> WithProgressAsync<T>(Task<T> operation, Action progress)
    {
        var result = await operation;
        progress();
        return result;
    }

    private static async Task WithProgressAsync(Task operation, Action progress)
    {
        await operation;
        progress();
    }

    // Slow, advancing scans can exceed a total deadline; only stalled work should release the sweep.
    private sealed class MaintenanceProgressTimeout : IDisposable
    {
        private readonly object _gate = new();
        private readonly TimeProvider _clock;
        private readonly CancellationTokenSource _inactivity = new();
        private readonly CancellationTokenSource _linked;
        private readonly ITimer _timer;
        private DateTimeOffset _lastProgress;
        private bool _disposed;
        public CancellationToken Token => _linked.Token;

        public MaintenanceProgressTimeout(TimeProvider clock, CancellationToken caller)
        {
            _clock = clock;
            _lastProgress = clock.GetUtcNow();
            _linked = CancellationTokenSource.CreateLinkedTokenSource(caller, _inactivity.Token);
            _timer = clock.CreateTimer(CheckInactivity, null, TicketMaintenanceInactivityTimeout, Timeout.InfiniteTimeSpan);
        }

        public void ReportProgress()
        {
            lock (_gate)
            {
                if (_disposed || _inactivity.IsCancellationRequested) return;
                _lastProgress = _clock.GetUtcNow();
                _timer.Change(TicketMaintenanceInactivityTimeout, Timeout.InfiniteTimeSpan);
            }
        }

        private void CheckInactivity(object? _)
        {
            lock (_gate)
            {
                if (_disposed || _inactivity.IsCancellationRequested) return;
                var remaining = TicketMaintenanceInactivityTimeout - (_clock.GetUtcNow() - _lastProgress);
                if (remaining <= TimeSpan.Zero) _inactivity.Cancel();
                else _timer.Change(remaining, Timeout.InfiniteTimeSpan);
            }
        }

        public void Dispose()
        {
            lock (_gate)
            {
                _disposed = true;
                _timer.Dispose();
                _linked.Dispose();
                _inactivity.Dispose();
            }
        }
    }
    private static bool SameTranscript(TranscriptSnapshot archived, TranscriptSnapshot current)
    {
        if (archived.LastMessageId != current.LastMessageId || archived.Messages.Count != current.Messages.Count) return false;
        return archived.Messages.OrderBy(message => message.Id).Zip(current.Messages.OrderBy(message => message.Id))
            .All(pair => pair.First.Id == pair.Second.Id && pair.First.AuthorId == pair.Second.AuthorId &&
                pair.First.AuthorName == pair.Second.AuthorName && pair.First.Content == pair.Second.Content &&
                pair.First.Timestamp == pair.Second.Timestamp && pair.First.EmbedsJson == pair.Second.EmbedsJson &&
                pair.First.MetadataJson == pair.Second.MetadataJson &&
                pair.First.Attachments.OrderBy(attachment => attachment.Id).Select(AttachmentIdentity)
                    .SequenceEqual(pair.Second.Attachments.OrderBy(attachment => attachment.Id).Select(AttachmentIdentity)));
    }

    private static (ulong Id, string FileName, long Size, string Resource) AttachmentIdentity(TranscriptAttachment attachment)
    {
        // Discord refreshes signed CDN URL query parameters; those are not attachment edits.
        var resource = Uri.TryCreate(attachment.Url, UriKind.Absolute, out var uri)
            ? uri.GetLeftPart(UriPartial.Path) : attachment.Url;
        return (attachment.Id, attachment.FileName, attachment.Size, resource);
    }

    private async Task NotifyAsync(string message, CancellationToken ct)
    {
        try { await discord.LogAsync(message, ct); }
        catch (Exception ex) when (!ct.IsCancellationRequested) { logger?.LogWarning(ex, "Ticket log delivery failed"); }
    }
}
