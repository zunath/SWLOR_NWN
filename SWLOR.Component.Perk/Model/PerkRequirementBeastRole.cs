using Microsoft.Extensions.DependencyInjection;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Beasts.Contracts;
using SWLOR.Shared.Domain.Beasts.Enums;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Entities;

namespace SWLOR.Component.Perk.Model
{
    public class PerkRequirementBeastRole: IPerkRequirement
    {
        private readonly IDatabaseService _db;
        private readonly BeastRoleType _requiredRole;
        private readonly IServiceProvider _serviceProvider;

        public PerkRequirementBeastRole(IDatabaseService db, BeastRoleType requiredRole, IServiceProvider serviceProvider)
        {
            _db = db;
            _requiredRole = requiredRole;
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded service to break circular dependency
        private IBeastMasteryService BeastMastery => _serviceProvider.GetRequiredService<IBeastMasteryService>();

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
