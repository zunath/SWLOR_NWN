using System.Collections.Generic;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public class ForceInspirationAbilityDefinition: IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            ForceInspiration1(builder);
            ForceInspiration2(builder);
            ForceInspiration3(builder);

            return builder.Build();
        }

        private void Impact(uint activator, uint target, Type statusEffect)
        {
            var willpowerMod = GetAbilityScore(activator, AbilityType.Willpower);
            const float BaseLength = 900f;
            var length = BaseLength + willpowerMod * 15f;

            StatusEffect.ApplyStatusEffect(activator, target, statusEffect, length);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Globe_Use), target);

            // WIL/AGI from this buff change max FP/STM (Stat.GetMaxFP / GetMaxStamina). Refresh UI — otherwise
            // pool displays stay stale until something else fires (e.g. re-equipping gear).
            if (GetIsPC(target) && !GetIsDM(target) && !GetIsDMPossessed(target))
            {
                Gui.PublishRefreshEvent(target, new PlayerStatusRefreshEvent(PlayerStatusRefreshEvent.StatType.FP));
                Gui.PublishRefreshEvent(target, new PlayerStatusRefreshEvent(PlayerStatusRefreshEvent.StatType.STM));
            }
        }

        private void ForceInspiration1(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceInspiration1, PerkType.ForceInspiration)
                .Name("Force Inspiration I")
                .HasRecastDelay(RecastGroup.ForceInspiration, 30f)
                .HasActivationDelay(2f)
                .RequirementFP(5)
                .UsesAnimation(Animation.LoopingGetMid)
                .IsCastedAbility()
                .HasImpactAction((activator, target, _, _) =>
                {
                    Impact(activator, target, typeof(ForceInspiration1StatusEffect));
                });
        }
        private void ForceInspiration2(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceInspiration2, PerkType.ForceInspiration)
                .Name("Force Inspiration II")
                .HasRecastDelay(RecastGroup.ForceInspiration, 30f)
                .HasActivationDelay(2f)
                .RequirementFP(6)
                .UsesAnimation(Animation.LoopingGetMid)
                .IsCastedAbility()
                .HasImpactAction((activator, target, _, _) =>
                {
                    Impact(activator, target, typeof(ForceInspiration2StatusEffect));
                });
        }
        private void ForceInspiration3(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceInspiration3, PerkType.ForceInspiration)
                .Name("Force Inspiration III")
                .HasRecastDelay(RecastGroup.ForceInspiration, 30f)
                .HasActivationDelay(2f)
                .RequirementFP(7)
                .UsesAnimation(Animation.LoopingGetMid)
                .IsCastedAbility()
                .HasImpactAction((activator, target, _, _) =>
                {
                    Impact(activator, target, typeof(ForceInspiration3StatusEffect));
                });
        }
    }
}
