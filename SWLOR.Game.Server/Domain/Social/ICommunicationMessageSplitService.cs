using System.Collections.Generic;

namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>Splits a chat message into the runs that are translated and the runs that are colored as emotes.</summary>
    public interface ICommunicationMessageSplitService
    {
        /// <summary>Splits a message for a player whose emote style is Regular: emotes are marked with asterisks, brackets or double colons.</summary>
        List<ChatComponent> SplitMessageIntoComponents_Regular(string message);

        /// <summary>Splits a message for a player whose emote style is Novel: speech is in quotation marks and brackets mark emotes.</summary>
        List<ChatComponent> SplitMessageIntoComponents_Novel(string message);
    }
}
