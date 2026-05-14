using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Beastmaster
{
    public sealed class AlphaRhythmAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            AlphaRhythm1(builder);

            return builder.Build();
        }

        private static void AlphaRhythm1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.AlphaRhythm1, PerkType.AlphaRhythm)
                .Name("Alpha Rhythm")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.AlphaRhythm, 120f)
                .SkillType(SkillType.BeastMastery)
                .IsSingleTargetAbility()
                .HasImpactAction(AlphaRhythm1ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(10);
        }

        private static void AlphaRhythm1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            foreach (var friendly in GetBeastMasterTargets(activator))
            {
                StatusEffect.ApplyStatusEffect(activator, friendly, typeof(AlphaRhythm1StatusEffect), 15f);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Holy_Aid), friendly);
            }
        }


        private static IEnumerable<uint> GetBeastAndMasterTargets(uint activator)
        {
            yield return activator;

            var master = GetMaster(activator);
            if (GetIsObjectValid(master))
                yield return master;
        }

        private static IEnumerable<uint> GetBeastMasterTargets(uint activator)
        {
            var master = GetMaster(activator);
            yield return GetIsObjectValid(master) ? master : activator;
        }
    }
}
