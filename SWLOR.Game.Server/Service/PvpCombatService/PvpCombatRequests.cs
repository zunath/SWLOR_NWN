namespace SWLOR.Game.Server.Service.PvpCombatService
{
    /// <summary>
    /// The changes a player asked for while their PvP combat window was open (the requested state), which wait until it
    /// ends before they take effect (the effective state). Saved on the player so a logout or restart in between does not
    /// lose them. See <see cref="PvpCombatWindowRules"/>.
    /// </summary>
    public class PvpCombatRequests
    {
        public PvpCombatRequests()
        {
            QueuedDisguiseId = string.Empty;
        }

        /// <summary>The player asked to turn the PvP flag off for the identity they are showing.</summary>
        public bool IsFlagOffQueued { get; set; }

        /// <summary>The player asked to change the identity they show.</summary>
        public bool IsIdentityChangeQueued { get; set; }

        /// <summary>The disguise the player asked to show, or empty for their own character.</summary>
        public string QueuedDisguiseId { get; set; }

        public bool HasAny => IsFlagOffQueued || IsIdentityChangeQueued;

        public void Clear()
        {
            IsFlagOffQueued = false;
            IsIdentityChangeQueued = false;
            QueuedDisguiseId = string.Empty;
        }
    }
}
