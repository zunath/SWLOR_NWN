namespace SWLOR.Game.Server.Service.PlayerListService
{
    /// <summary>
    /// Opt-in flags a player shows next to their character in the player list.
    /// Values are persisted; never renumber or reuse them.
    /// </summary>
    public enum PlayerListFlagType
    {
        [PlayerListFlag("General RP", "General RP: open to roleplay. Feel free to approach.", "pl_flag_rp")]
        GeneralRP = 1,

        [PlayerListFlag("PvP", "PvP: open to player-versus-player conflict.", "pl_flag_pvp")]
        PvP = 2,

        [PlayerListFlag("DM", "DM: looking for DM events, plots, or help with a scene.", "pl_flag_dm")]
        DM = 3,

        [PlayerListFlag("Crafting", "Crafting: offering or looking for crafting services.", "pl_flag_craft")]
        Crafting = 4,
    }
}
