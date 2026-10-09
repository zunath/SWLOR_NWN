namespace SWLOR.DiscordBot.Discord;

// Only locally authored validation messages belong here; HTTP payloads and credentials must stay sanitized.
internal sealed class DiscordValidationException(string message) : InvalidOperationException(message);
