using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class StimCanisterTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildArea(
                _builder,
                FeatType.StimCanisterTechnique,
                "Stim Canister",
                Animation.ThrowGrenade,
                InnateAbilityProfile.Mimicry,
                RecastGroup.StimCanister,
                1.2f,
                24f,
                8,
                20,
                10,
                typeof(PoisonStatusEffect),
                CombatImpactAreaShape.Sphere,
                4.0f,
                0f,
                CombatDamageType.Poison,
                ResistanceType.Poison,
                VisualEffect.Vfx_Imp_Poison_S,
                VisualEffect.Vfx_Fnf_Gas_Explosion_Acid,
                maxRange: 8f)
                .SkillType(SkillType.Mimicry)
                .Level(1)
                .CombatImpactDamageAbility(AbilityType.Might)
                .MimicryTechnique(FeatType.StimCanister, 3, 3)
                .HasTargetingSphere(
                    Spell.StimCanisterTechnique,
                    4.0f,
                    AbilityTargetingFlags.HarmsEnemies);

            return _builder.Build();
        }
    }
}
