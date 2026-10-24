namespace SWLOR.Game.Server.Service.ChatPortraitTellService
{
    public enum ChatPortraitClick
    {
        /// <summary>No chat line from the slot is still recorded for the observer, so there is nothing to compare.</summary>
        Unrecorded,
        /// <summary>Every recorded line from the slot came from the character who holds it now.</summary>
        CurrentHolder,
        /// <summary>The slot's lines all came from characters who left; the current holder sent none of them.</summary>
        FormerHolder,
        /// <summary>The current holder and a former holder both sent lines from the slot, so the clicked line is unknown.</summary>
        Ambiguous,
    }
}
