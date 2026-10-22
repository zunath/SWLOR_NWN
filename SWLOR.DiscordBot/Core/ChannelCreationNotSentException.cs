namespace SWLOR.DiscordBot.Core;

// Only the Discord adapter may assert that a channel POST was never sent or was definitively rejected.
public sealed class ChannelCreationNotSentException(Exception innerException)
    : Exception("Discord channel creation was not submitted or was definitively rejected.", innerException)
{
}
