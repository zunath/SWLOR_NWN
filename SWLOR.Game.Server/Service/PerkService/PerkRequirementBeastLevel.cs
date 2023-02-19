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

        public string CheckRequirements(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var dbBeast = DB.Get<Beast>(dbPlayer.ActiveBeastId);

            if (dbBeast == null)
                return "You do not have a beast tamed.";

            if (dbBeast.Level < _requiredLevel)
                return $"Your beast's level is too low. (Its level is {dbBeast.Level} versus required level {_requiredLevel})";

            return string.Empty;
        }

        public string RequirementText => $"Beast Level {_requiredLevel}";
    }
}
