using SWLOR.DiscordBot.Core;

namespace SWLOR.DiscordBot.Configuration;

internal static class TicketMaintenanceRequirements
{
    public static bool RequiresPanel(TicketState state) => state is TicketState.Creating or TicketState.Open or
        TicketState.Closing or TicketState.Closed or TicketState.Reopening;

    public static bool RequiresBypassRoles(BotConfiguration configuration, IReadOnlyList<Ticket> tickets) =>
        configuration.Tickets.Enabled || tickets.Any(ticket => RequiresPanel(ticket.State));

    public static IReadOnlyList<TicketPanelOptions> RequiredPanels(BotConfiguration configuration, IReadOnlyList<Ticket> tickets)
    {
        if (configuration.Tickets.Enabled) return configuration.Tickets.Panels;
        var ids = tickets.Where(ticket => RequiresPanel(ticket.State)).Select(ticket => ticket.PanelId)
            .ToHashSet(StringComparer.Ordinal);
        return (configuration.Tickets.Panels ?? []).Where(panel => panel is not null && ids.Contains(panel.Id)).ToArray();
    }

    public static IReadOnlyList<ulong> RequiredCategoryIds(BotConfiguration configuration, IReadOnlyList<Ticket> tickets)
    {
        var open = RequiredPanels(configuration, tickets).SelectMany(panel => panel.OpenCategoryIds);
        var closed = configuration.Tickets.Enabled || tickets.Any(ticket => RequiresPanel(ticket.State))
            ? new[] { configuration.Tickets.ClosedCategoryId } : [];
        return open.Concat(closed).Distinct().ToArray();
    }
}
