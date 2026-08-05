using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class GrenadeBurstTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildArea(
                _builder,
                FeatType.GrenadeBurstTechnique,
                "Grenade Burst",
                Animation.ThrowGrenade,
                InnateAbilityProfile.Mimicry,
                RecastGroup.GrenadeBurst,
                1.5f,
                24f,
                8,
                28,
                30,
                typeof(BurnStatusEffect),
                CombatImpactAreaShape.Sphere,
                4.5f,
                0f,
                CombatDamageType.Fire,
                ResistanceType.Fire,
                VisualEffect.Vfx_Imp_Flame_M,
                VisualEffect.Vfx_Fnf_Gas_Explosion_Fire,
                maxRange: 10f,
                centerOnActivator: true)
                .SkillType(SkillType.Mimicry)
                .Level(1)
                .CombatImpactDamageAbility(AbilityType.Perception)
                .MimicryTechnique(FeatType.GrenadeBurst, 12, 3)
                .HasTargetingSphere(
                    Spell.GrenadeBurstTechnique,
                    4.5f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf);

            return _builder.Build();
        }
    }
}
