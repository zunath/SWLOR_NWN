namespace SWLOR.DiscordBot.Core;

public sealed record TicketResult(bool Success, string Message, Ticket? Ticket = null);
