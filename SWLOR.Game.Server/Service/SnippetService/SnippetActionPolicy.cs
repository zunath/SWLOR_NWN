using System;

namespace SWLOR.Game.Server.Service.SnippetService
{
    /// <summary>
    /// Shared authoring and runtime rules for snippet outcomes whose completion can safely be
    /// represented by a permanent per-player marker.
    /// </summary>
    public static class SnippetActionPolicy
    {
        public static bool CanRunOncePerPlayer(string actionKey)
        {
            if (string.IsNullOrWhiteSpace(actionKey))
                return false;

            // These outcomes open or begin flows rather than completing a permanent reward at the
            // moment the snippet returns. In particular, advancing the final state of a quest may
            // only open reward selection, which the player can close without completing.
            return actionKey is not
                "action-open-store" and not
                "action-teleport" and not
                "action-request-quest-items" and not
                "action-advance-quest";
        }
    }
}
