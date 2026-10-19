namespace SWLOR.Game.Server.Service.PlayerListService
{
    /// <summary>
    /// The engine's party record for one online character, as the player list reads it.
    /// </summary>
    public class PlayerListPartyMembership
    {
        public uint Player { get; set; }
        public bool IsDM { get; set; }

        /// <summary>
        /// The player-party faction that lists this character, or null when it lists none. Standard and
        /// custom NPC factions never count, and neither does a faction id the member list does not back.
        /// </summary>
        public int? PartyFactionId { get; set; }

        /// <summary>
        /// The engine's GetFactionLeader for the character.
        /// </summary>
        public uint FactionLeader { get; set; }
    }
}
