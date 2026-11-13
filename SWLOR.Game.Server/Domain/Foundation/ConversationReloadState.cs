using System.Collections.Generic;

namespace SWLOR.Game.Server.Domain.Foundation
{
    /// <summary>
    /// A conversation a hot reload carries from the old build to the new one (<see cref="ConversationService.CarryForReload"/>): which
    /// registered graph, who the player talks to, where the window was shown, the line the player is at with its text and choices,
    /// and the session's tokens and state (strings only).
    /// </summary>
    public sealed class ConversationReloadState
    {
        public string GraphId { get; set; }
        public uint Owner { get; set; }
        public uint ShownOn { get; set; }
        public string NodeId { get; set; }
        public List<ConversationTextBlock> Text { get; set; } = new();
        public List<string> ChoiceIds { get; set; } = new();
        public Dictionary<string, string> Tokens { get; set; } = new();
        public Dictionary<string, string> State { get; set; } = new();
    }
}
