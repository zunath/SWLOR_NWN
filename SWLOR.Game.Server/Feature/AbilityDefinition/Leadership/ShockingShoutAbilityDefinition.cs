using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Leadership
{
    public class ShockingShoutAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            ShockingShout();

            return _builder.Build();
        }

        private void ShockingShout()
        {
            _builder
                .Create(FeatType.ShockingShout, PerkType.ShockingShout)
                .Name("Shocking Shout")
                .Level(1)
                .HasRecastDelay(RecastGroup.ShockingShout, 120f)
                .HasActivationDelay(0.5f)
                .UsesAnimation(Animation.FireForgetTaunt)
                .HasImpactAction((activator, target, level, location) =>
                {
                    const float RangeMeters = 10f;
                    const int MaxTargets = 6;
                    var nth = 1;
                    var count = 0;

                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Fnf_Sound_Burst), activator);

                    var nearest = GetNearestCreature(CreatureType.IsAlive, 1, activator, nth);
                    while (GetIsObjectValid(nearest))
                    {

                        if (GetDistanceBetween(activator, nearest) > RangeMeters)
                        {
                            break;
                        }

                        if (GetIsReactionTypeHostile(nearest, activator))
                        {
                            count++;

                            var social = GetAbilityScore(activator, AbilityType.Social);
                            const float BaseDuration = 2f;

                            StatusEffect.ApplyStatusEffect(activator, nearest, typeof(StunnedStatusEffect), BaseDuration, CombatDamageType.Physical);
                            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Head_Sonic), nearest);

                            CombatPoint.AddCombatPoint(activator, nearest, SkillType.Leadership, 3);
                            Enmity.ModifyEnmity(activator, nearest, Stat.ScaleEffect(650, social));
                        }

                        if (count >= MaxTargets)
                        {
                            break;
                        }

                        nth++;
                        nearest = GetNearestCreature(CreatureType.IsAlive, 1, activator, nth);
                    }

                });
        }
    }
}
