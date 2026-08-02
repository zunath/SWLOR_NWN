using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.ConversationService
{
    /// <summary>
    /// What the NUI conversation window needs from an active interaction. Authored graph sessions
    /// and code-driven menus implement the same small contract without depending on Aurora DLG.
    /// </summary>
    public interface IConversationSession
    {
        ConversationContext Context { get; }
        ConversationNode CurrentNode { get; }
        IReadOnlyList<ConversationTextBlock> CurrentText { get; }
        IReadOnlyList<ConversationChoice> VisibleChoices { get; }
        bool HasEnded { get; }
        string Title { get; }
        ConversationSelectionResult SelectChoice(int visibleChoiceIndex);
        string ResolveText(string text);
        void End(ConversationEndReason reason);
    }
}
