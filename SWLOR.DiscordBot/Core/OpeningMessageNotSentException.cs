namespace SWLOR.DiscordBot.Core;

public sealed class OpeningMessageNotSentException(Exception innerException)
    : Exception("The ticket opening message was not submitted or was definitively rejected.", innerException)
{
}
