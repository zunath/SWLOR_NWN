using SWLOR.Game.Server.Core.NWNX.Enum;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>One call the chat instance made to <c>ChatPlugin.SendMessage</c>.</summary>
/// <param name="Channel">The channel the message was sent on.</param>
/// <param name="Message">The finished text, color tokens included.</param>
/// <param name="Sender">The creature the client shows as the speaker.</param>
/// <param name="Target">The creature the message was sent to.</param>
public sealed record SentMessage(ChatChannel Channel, string Message, uint Sender, uint Target);
