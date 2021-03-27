using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatusEffectService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public class BattleInsightStatusEffectDefinition: IStatusEffectListDefinition
    {
        public Dictionary<StatusEffectType, StatusEffectDetail> BuildStatusEffects()
        {
            var builder = new StatusEffectBuilder();
            BattleInsight1(builder);
            BattleInsight2(builder);

            return builder.Build();
        }

        private void BattleInsight1(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.BattleInsight1)
                .Name("Battle Insight I")
                .EffectIcon(17) // 17 = Dazed
                .GrantAction((source, target, length) =>
                {
                    var effect = EffectAttackDecrease(5);
                    effect = EffectLinkEffects(effect, EffectACDecrease(5));
                    effect = TagEffect(effect, "StatusEffectType." + StatusEffectType.BattleInsight2);
                    ApplyEffectToObject(DurationType.Permanent, effect, source);

                    CombatPoint.AddCombatPointToAllTagged(target, SkillType.Force, 3);
                })
                .TickAction((source, target) =>
                {
                    var party = Party.GetAllPartyMembersWithinRange(source, RadiusSize.Medium);

                    foreach (var player in party)
                    {
                        var effect = EffectAttackIncrease(3);
                        effect = EffectLinkEffects(effect, EffectACIncrease(3));
                        effect = TagEffect(effect, "StatusEffectType." + StatusEffectType.BattleInsight2);
                        ApplyEffectToObject(DurationType.Temporary, effect, player, 6f);
                    }
                })
                .RemoveAction((target) =>
                {
                    RemoveEffectByTag(target, "StatusEffectType." + StatusEffectType.BattleInsight1);
                });
        }
        private void BattleInsight2(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.BattleInsight2)
                .Name("Battle Insight II")
                .EffectIcon(17) // 17 = Dazed
                .GrantAction((source, target, length) =>
                {
                    var effect = EffectAttackDecrease(8);
                    effect = EffectLinkEffects(effect, EffectACDecrease(8));
                    effect = TagEffect(effect, "StatusEffectType." + StatusEffectType.BattleInsight2);
                    ApplyEffectToObject(DurationType.Permanent, effect, source);

                    CombatPoint.AddCombatPointToAllTagged(target, SkillType.Force, 3);
                })
                .TickAction((source, target) =>
                {
                    var party = Party.GetAllPartyMembersWithinRange(source, RadiusSize.Medium);

                    foreach (var player in party)
                    {
                        var effect = EffectAttackIncrease(6);
                        effect = EffectLinkEffects(effect, EffectACIncrease(6));
                        effect = TagEffect(effect, "StatusEffectType." + StatusEffectType.BattleInsight2);
                        ApplyEffectToObject(DurationType.Temporary, effect, player, 6f);
                    }
                })
                .RemoveAction((target) =>
                {
                    RemoveEffectByTag(target, "StatusEffectType." + StatusEffectType.BattleInsight2);
                });
        }
    }
}
