namespace SWLOR.Game.Server.Service.DMToolService
{
    /// <summary>
    /// A Creature Manager creature a DM saved as a favorite. The record id identifies it; the name is only shown once
    /// the saved creature has been deleted.
    /// </summary>
    public class DMSavedCreatureFavorite
    {
        public string CreatureId { get; set; }
        public string Name { get; set; }

        public DMSavedCreatureFavorite()
        {
            CreatureId = string.Empty;
            Name = string.Empty;
        }

        public DMSavedCreatureFavorite(string creatureId, string name)
        {
            CreatureId = creatureId ?? string.Empty;
            Name = name ?? string.Empty;
        }
    }
}
