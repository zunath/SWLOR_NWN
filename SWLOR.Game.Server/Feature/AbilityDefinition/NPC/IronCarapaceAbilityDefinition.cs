using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class IronCarapaceAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var profile = InnateAbilityProfile.CreaturePhysical;

            _builder
                .Create(FeatType.IronCarapace, profile.PlayerPerkType)
                .Name("Iron Carapace")
                .HasActivationDelay(1.0f)
                .HasRecastDelay(RecastGroup.IronCarapace, 32f)
                .UsesAnimation(Animation.ShieldWall)
                .IsCastedAbility()
                .RequirementStamina(4)
                .HasImpactAction((activator, target, level, location) =>
                {
                    StatusEffect.ApplyStatusEffect(activator, activator, new IronCarapaceStatusEffect(), 30f);
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Ac_Bonus), activator);
                });

            return _builder.Build();
        }
    }
}
