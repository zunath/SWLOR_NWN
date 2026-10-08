using Discord;

namespace SWLOR.DiscordBot.Discord;

internal static class TicketCommandRegistration
{
    internal static async Task RemoveDisabledIntakeAsync(
        IEnumerable<(string Name, ApplicationCommandType Type, Func<Task> Delete)> commands, CancellationToken ct)
    {
        foreach (var command in commands)
        {
            ct.ThrowIfCancellationRequested();
            if (command.Type == ApplicationCommandType.Slash && command.Name == "ticket-panel")
                await command.Delete();
        }
    }
}
