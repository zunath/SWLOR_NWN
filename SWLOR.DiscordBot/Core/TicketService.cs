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
    private TicketOptions Options => configuration.Tickets;
    public bool CanSupport(Actor actor) => actor.IsGuildOwner || actor.RoleIds.Intersect(Options.SupportRoleIds).Any();
    private bool Bypasses(Actor actor, bool? scope) => scope == true && actor.RoleIds.Intersect(Options.BypassRoleIds).Any();
    private static bool Active(Ticket t) => t.State is TicketState.Creating or TicketState.Open or TicketState.Closing or TicketState.Reopening;

    public async Task<TicketResult> OpenAsync(string panelId, Actor actor, string interactionId, CancellationToken ct = default)
    {
        if (!Options.Enabled) return new(false, "Ticketing is disabled.");
        var panel = Options.Panels.SingleOrDefault(p => p.Id == panelId);
        if (panel is null) return new(false, "This ticket panel is unavailable.");
        await using var session = await store.LockAsync(ct);
        var previous = await session.FindInteractionAsync(interactionId, ct);
        if (previous is not null)
        {
            if (previous.RequesterId != actor.UserId) return new(false, "This interaction belongs to another requester.");
            return previous.State == TicketState.Creating
                ? await ResumeCreationAsync(session, previous, ct)
                : new(true, "This interaction has already been handled.", previous);
        }
        var tickets = await session.GetTicketsAsync(ct);
        var pending = tickets.FirstOrDefault(t => t.RequesterId == actor.UserId && t.PanelId == panelId && t.State == TicketState.Creating);
        if (pending is not null) return await ResumeCreationAsync(session, pending, ct);
        var active = tickets.Where(Active).ToArray();
        if (!Bypasses(actor, Options.BypassMemberLimit) && active.Count(t => t.RequesterId == actor.UserId) >= Options.MemberLimit)
            return new(false, "You already have the maximum number of open tickets.", active.FirstOrDefault(t => t.RequesterId == actor.UserId));
        if (!Bypasses(actor, Options.BypassPanelLimit) && active.Count(t => t.PanelId == panelId) >= panel.OpenLimit)
            return new(false, "This ticket panel has reached its open-ticket limit.");
        if (!Bypasses(actor, Options.BypassGuildLimit) && active.Length >= Options.GuildLimit)
            return new(false, "The server has reached its open-ticket limit.");
        var ticket = await session.ReserveAsync(panelId, actor.UserId, interactionId, clock.GetUtcNow(), ct);
        return await ResumeCreationAsync(session, ticket, ct);
    }

    private async Task<TicketResult> ResumeCreationAsync(ITicketSession session, Ticket ticket, CancellationToken ct)
    {
        try
        {
            var channel = ticket.ChannelId ?? await discord.FindManagedChannelAsync(ticket.Id, ct) ?? await discord.CreateAsync(ticket, ct);
            ticket = ticket with { ChannelId = channel, LastError = null };
            await session.SaveAsync(ticket, "channel-bound", ticket.RequesterId, ct);
            await discord.OpenAsync(ticket, true, ct);
            ticket = ticket with { State = TicketState.Open };
            await session.SaveAsync(ticket, "opened", ticket.RequesterId, ct);
            await NotifyAsync($"Ticket {ticket.Number} opened.", ct);
            return new(true, $"Your ticket is ready: <#{channel}>.", ticket);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger?.LogWarning(ex, "Ticket {TicketId} creation will be reconciled", ticket.Id);
            await session.SaveAsync(ticket with { LastError = "Channel creation or opening failed; reconciliation pending." }, "creation-failed", null, ct);
            return new(false, "The ticket could not be opened yet. Staff can check the bot logs; retrying will recover this request.", ticket);
        }
    }

    public Task<TicketResult> CloseAsync(ulong channelId, Actor actor, CancellationToken ct = default) => MutateAsync(channelId, actor, async (session, ticket) =>
    {
        if (ticket.RequesterId != actor.UserId && !CanSupport(actor)) return new(false, "Only the requester or support staff can close this ticket.");
        if (ticket.State == TicketState.Closed) return new(true, "This ticket is already closed.", ticket);
        if (ticket.State is not (TicketState.Open or TicketState.Closing)) return new(false, "This ticket is busy; try again shortly.");
        var now = ticket.ClosedAt ?? clock.GetUtcNow();
        ticket = ticket with { State = TicketState.Closing, ClosedAt = now, DeleteAfter = now + Options.CleanupDelay, LastError = null,
            ArchiveExpiresAt = now.AddDays(Options.ArchiveRetentionDays) };
        await session.SaveAsync(ticket, "closing", actor.UserId, ct);
        await discord.CloseAsync(ticket, ct);
        ticket = ticket with { State = TicketState.Closed };
        await session.SaveAsync(ticket, "closed", actor.UserId, ct);
        await NotifyAsync($"Ticket {ticket.Number} closed.", ct);
        return new(true, "Ticket closed. Staff can reopen it before cleanup.", ticket);
    }, ct);

    public Task<TicketResult> RenameAsync(ulong channelId, Actor actor, string name, CancellationToken ct = default) => MutateAsync(channelId, actor, async (session, ticket) =>
    {
        if (!CanSupport(actor)) return new(false, "Only support staff can rename tickets.");
        if (ticket.State is not (TicketState.Open or TicketState.Closed)) return new(false, "This ticket is busy; try again shortly.");
        var normalized = Regex.Replace(name.Trim().ToLowerInvariant(), "[^a-z0-9_-]+", "-").Trim('-');
        if (normalized.Length is < 1 or > 100) return new(false, "Use a channel name between 1 and 100 characters.");
        await discord.RenameAsync(ticket, normalized, ct);
        await session.SaveAsync(ticket, "renamed", actor.UserId, ct);
        return new(true, "Ticket renamed.", ticket);
    }, ct);

    public Task<TicketResult> ReopenAsync(ulong channelId, Actor actor, CancellationToken ct = default) => MutateAsync(channelId, actor, async (session, ticket) =>
    {
        if (!CanSupport(actor)) return new(false, "Only support staff can reopen tickets.");
        if (ticket.State == TicketState.Open) return new(true, "This ticket is already open.", ticket);
        if (ticket.State is not (TicketState.Closed or TicketState.Reopening)) return new(false, "This ticket cannot be reopened while an operation is in progress.");
        var tickets = (await session.GetTicketsAsync(ct)).Where(t => t.Id != ticket.Id && Active(t)).ToArray();
        var panel = Options.Panels.Single(p => p.Id == ticket.PanelId);
        // Reopening must not exceed configured caps; a staff actor's bypass must not transfer to the requester.
        if (tickets.Count(t => t.RequesterId == ticket.RequesterId) >= Options.MemberLimit ||
            tickets.Count(t => t.PanelId == ticket.PanelId) >= panel.OpenLimit || tickets.Length >= Options.GuildLimit)
            return new(false, "Reopening would exceed an open-ticket limit.");
        ticket = ticket with { State = TicketState.Reopening, DeleteAfter = null, ClosedAt = null, ArchiveExpiresAt = null, LastError = null };
        await session.SaveAsync(ticket, "reopening", actor.UserId, ct);
        await discord.OpenAsync(ticket, false, ct);
        ticket = ticket with { State = TicketState.Open };
        await session.SaveAsync(ticket, "reopened", actor.UserId, ct);
        return new(true, "Ticket reopened; scheduled cleanup cancelled.", ticket);
    }, ct);

    public Task<TicketResult> SetHoldAsync(ulong channelId, Actor actor, bool hold, CancellationToken ct = default) => MutateAsync(channelId, actor, async (session, ticket) =>
    {
        if (!CanSupport(actor)) return new(false, "Only support staff can change archive holds.");
        if (ticket.State is TicketState.Deleting or TicketState.Deleted) return new(false, "Cleanup has already started.");
        ticket = ticket with { Hold = hold };
        await session.SaveAsync(ticket, hold ? "hold-set" : "hold-released", actor.UserId, ct);
        return new(true, hold ? "Cleanup and archive expiration are on hold." : "Hold released.", ticket);
    }, ct);

    public async Task<TicketResult> ExportAsync(ulong channelId, Actor actor, CancellationToken ct = default)
    {
        if (!Options.Enabled) return new(false, "Ticketing is disabled.");
        if (!CanSupport(actor)) return new(false, "Only support staff can export transcripts.");
        Guid? exportId = null;
        try
        {
            Ticket ticket;
            await using (var session = await store.LockAsync(ct))
            {
                var found = (await session.GetTicketsAsync(ct)).SingleOrDefault(t => t.ChannelId == channelId);
                if (found is null) return new(false, "This channel is not a ticket managed by this bot.");
                ticket = found;
                if (ticket.State is not (TicketState.Open or TicketState.Closed)) return new(false, "This ticket is busy; try again shortly.");
                if (!_exports.TryAdd(ticket.Id, 0)) return new(false, "A transcript export is already in progress for this ticket.");
                exportId = ticket.Id;
            }
            // Discord pagination and attachment downloads must not hold the shared database lock.
            var snapshot = await discord.ReadTranscriptAsync(ticket, ct);
            var path = await archive.ExportAsync(ticket, snapshot, ct);
            await using var saveSession = await store.LockAsync(ct);
            var current = (await saveSession.GetTicketsAsync(ct)).Single(t => t.Id == ticket.Id);
            // Preserve closes, reopens, holds, and deadlines changed while the snapshot was written.
            current = current with { ArchivePath = path };
            await saveSession.SaveAsync(current, "exported", actor.UserId, ct);
            return new(true, "Transcript archived.", current);
        }
        finally { if (exportId is { } id) _exports.TryRemove(id, out _); }
    }

    private async Task<TicketResult> MutateAsync(ulong channelId, Actor actor,
        Func<ITicketSession, Ticket, Task<TicketResult>> mutation, CancellationToken ct)
    {
        if (!Options.Enabled) return new(false, "Ticketing is disabled.");
        await using var session = await store.LockAsync(ct);
        var ticket = (await session.GetTicketsAsync(ct)).SingleOrDefault(t => t.ChannelId == channelId);
        if (ticket is null) return new(false, "This channel is not a ticket managed by this bot.");
        return await mutation(session, ticket);
    }

    public async Task MaintainAsync(CancellationToken ct = default)
    {
        if (!Options.Enabled) return;
        Guid[] ticketIds;
        await using (var batch = await store.LockAsync(ct))
            ticketIds = (await batch.GetTicketsAsync(ct)).Select(ticket => ticket.Id).ToArray();
        foreach (var id in ticketIds)
        {
            ct.ThrowIfCancellationRequested();
            // Keep a single ticket's mutations serialized, but let queued commands run between records.
            await using var session = await store.LockAsync(ct);
            // Do not race an export's archive files or delete its source channel; other tickets can proceed.
            if (_exports.ContainsKey(id)) continue;
            var ticket = (await session.GetTicketsAsync(ct)).SingleOrDefault(candidate => candidate.Id == id);
            if (ticket is null) continue;
            try
            {
                if (ticket.State != TicketState.Deleted && ticket.ChannelId.HasValue && !await discord.ExistsAsync(ticket, ct))
                {
                    var now = clock.GetUtcNow();
                    ticket = ticket with { State = TicketState.Deleted, ClosedAt = ticket.ClosedAt ?? now, DeleteAfter = null,
                        ArchiveExpiresAt = ticket.ArchiveExpiresAt ?? now.AddDays(Options.ArchiveRetentionDays),
                        LastError = "Channel was removed externally; any previously saved archive is retained until its expiration." };
                    await session.SaveAsync(ticket, "missing-channel-reconciled", null, ct);
                    await NotifyAsync($"Ticket {ticket.Number} channel was removed externally; open-ticket capacity released.", ct);
                }
                if (ticket.State == TicketState.Creating) { await ResumeCreationAsync(session, ticket, ct); continue; }
                if (ticket.State == TicketState.Closing)
                {
                    await discord.CloseAsync(ticket, ct);
                    ticket = ticket with { State = TicketState.Closed, LastError = null };
                    await session.SaveAsync(ticket, "close-reconciled", null, ct);
                }
                if (ticket.State == TicketState.Reopening)
                {
                    await discord.OpenAsync(ticket, false, ct);
                    ticket = ticket with { State = TicketState.Open, LastError = null };
                    await session.SaveAsync(ticket, "reopen-reconciled", null, ct);
                }
                if (ticket.Hold) continue;
                if (ticket.State == TicketState.Deleted)
                {
                    if (ticket.ArchivePath is not null && ticket.ArchiveExpiresAt <= clock.GetUtcNow())
                    {
                        await archive.DeleteAsync(ticket.ArchivePath, ct);
                        await session.SaveAsync(ticket with { ArchivePath = null }, "archive-expired", null, ct);
                    }
                    continue;
                }
                if (ticket.State == TicketState.Closed && ticket.DeleteAfter <= clock.GetUtcNow())
                {
                    ticket = ticket with { State = TicketState.Deleting };
                    await session.SaveAsync(ticket, "deleting", null, ct);
                }
                if (ticket.State != TicketState.Deleting) continue;
                await discord.FreezeAsync(ticket, ct);
                var snapshot = await discord.ReadTranscriptAsync(ticket, ct);
                var archivePath = await archive.ExportAsync(ticket, snapshot, ct);
                ticket = ticket with { ArchivePath = archivePath, LastError = null };
                await session.SaveAsync(ticket, "cleanup-exported", null, ct);
                if (await discord.LastMessageIdAsync(ticket, ct) != snapshot.LastMessageId)
                    throw new InvalidOperationException("Ticket received new messages during archival; retrying before deletion.");
                await discord.DeleteAsync(ticket, ct);
                await session.SaveAsync(ticket with { State = TicketState.Deleted }, "deleted", null, ct);
                await NotifyAsync($"Ticket {ticket.Number} archived and deleted.", ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger?.LogWarning(ex, "Ticket {TicketId} maintenance failed; channel retained", ticket.Id);
                // Read the last durable state: an export may have succeeded before a later API failure.
                var durable = (await session.GetTicketsAsync(ct)).Single(t => t.Id == ticket.Id);
                await session.SaveAsync(durable with { LastError = "Maintenance failed; retry scheduled. Check worker logs." }, "maintenance-failed", null, ct);
            }
        }
    }

    private async Task NotifyAsync(string message, CancellationToken ct)
    {
        try { await discord.LogAsync(message, ct); }
        catch (Exception ex) when (ex is not OperationCanceledException) { logger?.LogWarning(ex, "Ticket log delivery failed"); }
    }
}
