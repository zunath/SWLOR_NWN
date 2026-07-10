using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class IronCarapaceTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var profile = InnateAbilityProfile.Mimicry;

            _builder
                .Create(FeatType.IronCarapaceTechnique, profile.PlayerPerkType)
                .Name("Iron Carapace Technique")
                .HasActivationDelay(1.0f)
                .HasRecastDelay(RecastGroup.IronCarapace, 15f)
                .UsesAnimation(Animation.ShieldWall)
                .IsCastedAbility()
                .RequirementStamina(5)
                .HasImpactAction((activator, target, level, location) =>
                {
                    StatusEffect.ApplyStatusEffect(activator, activator, new IronCarapaceStatusEffect(), 30f);
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Ac_Bonus), activator);
                })
                .SkillType(SkillType.Mimicry)
                .Level(2)
                .CombatImpactDamageAbility(AbilityType.Vitality)
                .MimicryTechnique(FeatType.IronCarapace, 2, 2);

            return _builder.Build();
        }
    }
}
