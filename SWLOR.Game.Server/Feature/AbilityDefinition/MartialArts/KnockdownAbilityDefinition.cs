using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.MartialArts
{
    public class KnockdownAbilityDefinition: IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Knockdown();

            return _builder.Build();
        }

        private void Knockdown()
        {
            _builder.Create(FeatType.Knockdown, PerkType.Knockdown)
                .Name("Knockdown")
                .HasRecastDelay(RecastGroup.Knockdown, 60f)
                .IsWeaponAbility()
                .RequirementStamina(6)
                .HasImpactAction((activator, target, level) =>
                {
                    var isHit = Random.D100(1) <= 60;
                    if (!isHit) return;

                    ApplyEffectToObject(DurationType.Temporary, EffectKnockdown(), target, 12f);
                });
        }
    }
}
