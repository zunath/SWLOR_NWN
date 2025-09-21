using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Data.Entity;

namespace SWLOR.Game.Server.Service.PerkService
{
    public class PerkRequirementBeastRole: IPerkRequirement
    {
        private readonly IDatabaseService _db;
        private readonly BeastRoleType _requiredRole;
        private readonly BeastMastery _beastMastery;

        public PerkRequirementBeastRole(IDatabaseService db, BeastRoleType requiredRole, BeastMastery beastMastery)
        {
            _db = db;
            _requiredRole = requiredRole;
            _beastMastery = beastMastery;
        }

        public string CheckRequirements(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);
            var dbBeast = _db.Get<Beast>(dbPlayer.ActiveBeastId);
            var roleDetail = _beastMastery.GetBeastRoleDetail(_requiredRole);

            if (dbBeast == null)
                return "You do not have a beast tamed.";

            var beastDetail = _beastMastery.GetBeastDetail(dbBeast.Type);
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
                var roleDetail = _beastMastery.GetBeastRoleDetail(_requiredRole);
                return $"Beast Role: {roleDetail.Name}";
            }
        }
    }
}
