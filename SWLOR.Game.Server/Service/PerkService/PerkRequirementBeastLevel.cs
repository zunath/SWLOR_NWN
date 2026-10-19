using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.PerkService
{
    public class PerkRequirementBeastLevel: IPerkRequirement
    {
        private readonly int _requiredLevel;

        public PerkRequirementBeastLevel(int requiredLevel)
        {
            _requiredLevel = requiredLevel;
        }

        public PerkRequirementCategory Category => PerkRequirementCategory.BeastLevel;

        public string CheckRequirements(uint player)
        {
            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
            var dbBeast = string.IsNullOrWhiteSpace(dbPlayer?.ActiveBeastId) ? null : DB.Get<Beast>(dbPlayer.ActiveBeastId);
            return CheckRequirements(dbPlayer, dbBeast);
        }

        public string CheckRequirements(Player dbPlayer, Beast dbBeast)
        {
            if (dbPlayer == null || string.IsNullOrWhiteSpace(dbPlayer.ActiveBeastId))
                return "You do not have a beast tamed.";

            if (dbBeast == null)
                return "You do not have a beast tamed.";

            if (dbBeast.Level < _requiredLevel)
                return $"Your beast's level is too low. (Its level is {dbBeast.Level} versus required level {_requiredLevel})";

            return string.Empty;
        }

        public string RequirementText => $"Beast Level {_requiredLevel}";
    }
}
