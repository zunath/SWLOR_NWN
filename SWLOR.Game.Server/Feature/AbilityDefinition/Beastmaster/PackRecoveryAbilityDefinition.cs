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
    public sealed class PackRecoveryAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            PackRecovery1(builder);

            return builder.Build();
        }

        private static void PackRecovery1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.PackRecovery1, PerkType.PackRecovery)
                .Name("Pack Recovery")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.PackRecovery, 90f)
                .SkillType(SkillType.BeastMastery)
                .IsSingleTargetAbility()
                .HasImpactAction(PackRecovery1ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(8);
        }

        private static void PackRecovery1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            foreach (var friendly in GetBeastAndMasterTargets(activator))
            {
                Stat.RestoreStamina(friendly, 4);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Restoration), friendly);
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
