//using Random = SWLOR.Game.Server.Service.Random;
using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Creature;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.AbilityDefinition
{
    public class ForceMindAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<Feat, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            ForceMind1(builder);
            ForceMind2(builder);

            return builder.Build();
        }

        private static void ForceMind1(AbilityBuilder builder)
        {
            builder.Create(Feat.ForceMind1, PerkType.ForceMind)
                .Name("Force Mind I")
                .HasRecastDelay(RecastGroup.ForceMind, 60f * 5f)
                .HasActivationDelay(2.0f)
                .IsCastedAbility()
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level) =>
                {                    
                    // Damage user.
                    ApplyEffectToObject(DurationType.Instant, EffectDamage((int) (GetCurrentHitPoints(activator) * 0.25f)), activator);

                    // Recover FP on target.
                    Stat.RestoreFP(activator, (int)(GetCurrentHitPoints(activator) * 0.25f));

                    // Play VFX
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Head_Odd), target);

                    CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Force, 3);
                });
        }

        private static void ForceMind2(AbilityBuilder builder)
        {
            builder.Create(Feat.ForceMind2, PerkType.ForceMind)
                .Name("Force Mind II")
                .HasRecastDelay(RecastGroup.ForceMind, 60f * 5f)
                .HasActivationDelay(2.0f)
                .IsCastedAbility()
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level) =>
                {
                    // Damage user.
                    ApplyEffectToObject(DurationType.Instant, EffectDamage((int)(GetCurrentHitPoints(activator) * 0.5f)), activator);

                    // Recover FP on target.
                    Stat.RestoreFP(activator, (int)(GetCurrentHitPoints(activator) * 0.5f));

                    // Play VFX
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Head_Odd), target);

                    CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Force, 3);
                });
        }
    }
}