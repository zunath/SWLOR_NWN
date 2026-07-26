using SWLOR.Toolset.Domain.Documents;

namespace SWLOR.Toolset.Domain.Conversations
{
    /// <summary>Whether the Play-it editor can show a conversation honestly, and if not, why.</summary>
    public sealed record ConversationSupport(bool IsSupported, string Reason)
    {
        public static ConversationSupport Yes { get; } = new(true, string.Empty);
    }

    /// <summary>
    /// Decides whether a conversation is one Play-it can represent.
    /// </summary>
    /// <remarks>
    /// Play-it shows the dialogue as a player would hear it, which it can only do when it
    /// understands the rules deciding what a player hears. Those rules are the snippet system.
    /// <para>
    /// The line is drawn at the GUARDS, not at everything custom. A conversation whose visibility is
    /// decided by its own NWScript — the DMFI DM menus, the pazaak games — cannot be predicted at
    /// all: every branch would render "not simulated", which is technically shown and actually
    /// useless. Those are refused, and the reason names the script, and nothing is opened or
    /// changed.
    /// </para>
    /// <para>
    /// A custom ACTION script is a different matter and is allowed through. It does not affect what
    /// a player can see, only what happens afterwards — so the walk stays accurate, and the editor
    /// says "runs the script X" against that choice rather than pretending it is just talk. Refusing
    /// those too was the first version of this rule and it turned away 28 conversations including
    /// several ordinary ones; drawing the line at guards turns away 8.
    /// </para>
    /// </remarks>
    public static class ConversationCompatibility
    {
        public static ConversationSupport Check(DlgDocument document)
        {
            if (document.Openings.Count == 0)
            {
                return new ConversationSupport(false,
                    "This conversation has no opening, so it can never start. Play-it has nothing to show. "
                    + "It was most likely made outside the toolset.");
            }

            foreach (var link in document.AllLinks())
            {
                var active = link.Active;
                if (string.IsNullOrEmpty(active) || DlgDocument.IsConditionDispatcher(active))
                    continue;

                return new ConversationSupport(false,
                    $"This conversation decides what to show with its own script ('{active}') rather than "
                    + "with the snippet rules Play-it reads, so it cannot predict what a player would "
                    + "see. Nothing has been changed. Edit it outside the toolset, or convert the "
                    + "condition to a snippet first.");
            }

            return ConversationSupport.Yes;
        }
    }
}
