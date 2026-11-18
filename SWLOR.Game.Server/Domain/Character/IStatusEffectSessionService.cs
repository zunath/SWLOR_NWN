namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// What survives logout, a planned restart and a hot reload: the effects a player keeps while away, the stances saved for a
    /// restart and the carried state a reload puts back. Part 7 of the status effect split.
    /// </summary>
    public interface IStatusEffectSessionService
    {
        /// <summary>When a player enters the server, moves the effects held since logout back and reapplies the NWN effect system.</summary>
        void OnPlayerEnter();

        /// <summary>When a player exits the server, holds their status effects until they return.</summary>
        void OnPlayerExit();

        /// <summary>Saves the stances and toggles of everyone online or holding effects from an earlier logout, so a server restart keeps them.</summary>
        void SaveTogglesForRestart();
    }
}
