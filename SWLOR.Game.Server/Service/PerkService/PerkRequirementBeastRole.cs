using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.BeastMasteryService;

namespace SWLOR.Game.Server.Service.PerkService
{
    public class PerkRequirementBeastRole: IPerkRequirement
    {
        private readonly BeastRoleType _requiredRole;

        public PerkRequirementBeastRole(BeastRoleType requiredRole)
        {
            _requiredRole = requiredRole;
        }

        public string CheckRequirements(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var dbBeast = DB.Get<Beast>(dbPlayer.ActiveBeastId);
            var roleDetail = BeastMastery.GetBeastRoleDetail(_requiredRole);

            if (dbBeast == null)
                return "You do not have a beast tamed.";

            var beastDetail = BeastMastery.GetBeastDetail(dbBeast.Type);
            if (beastDetail.Role != _requiredRole)
            {
                return $"Your beast must be of the following role type: {roleDetail.Name}";
            }

            return string.Empty;
        }

        public string RequirementText
        {
            get
            {
                var roleDetail = BeastMastery.GetBeastRoleDetail(_requiredRole);
                return $"Beast Role: {roleDetail.Name}";
            }
        }
    }
}
