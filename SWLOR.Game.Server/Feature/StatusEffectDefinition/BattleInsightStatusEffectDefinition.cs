using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Enums;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public class BattleInsightStatusEffectDefinition: IStatusEffectListDefinition
    {
        private readonly CombatPoint _combatPoint;
        private readonly IEnmityService _enmityService;

        public BattleInsightStatusEffectDefinition(CombatPoint combatPoint, IEnmityService enmityService)
        {
            _combatPoint = combatPoint;
            _enmityService = enmityService;
        }

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
                .EffectIcon(EffectIconType.Dazed)
                .CannotReplace(StatusEffectType.BattleInsight2)
                .TickAction((source, target, effectData) =>
                {
                    var effect = EffectAccuracyDecrease(5);
                    effect = EffectLinkEffects(effect, EffectACDecrease(5));
                    effect = TagEffect(effect, "StatusEffectType." + StatusEffectType.BattleInsight1);
                    ApplyEffectToObject(DurationType.Temporary, effect, source, 6f);

                    var party = Party.GetAllPartyMembersWithinRange(source, RadiusSize.Medium);

                    foreach (var player in party)
                    {
                        if (player == source)
                            continue;

                        effect = EffectAccuracyIncrease(3);
                        effect = EffectLinkEffects(effect, EffectACIncrease(3));
                        effect = TagEffect(effect, "StatusEffectType." + StatusEffectType.BattleInsight1);
                        ApplyEffectToObject(DurationType.Temporary, effect, player, 6f);
                    }

                    _enmityService.ModifyEnmityOnAll(source, 80);
                    _combatPoint.AddCombatPointToAllTagged(source, SkillType.Force, 3);
                });
        }
        private void BattleInsight2(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.BattleInsight2)
                .Name("Battle Insight II")
                .EffectIcon(EffectIconType.Dazed)
                .Replaces(StatusEffectType.BattleInsight1)
                .TickAction((source, target, effectData) =>
                {
                    var effect = EffectAccuracyDecrease(8);
                    effect = EffectLinkEffects(effect, EffectACDecrease(8));
                    effect = TagEffect(effect, "StatusEffectType." + StatusEffectType.BattleInsight2);
                    ApplyEffectToObject(DurationType.Temporary, effect, source, 6f);

                    var party = Party.GetAllPartyMembersWithinRange(source, RadiusSize.Medium);

                    foreach (var player in party)
                    {
                        if (player == source)
                            continue;

                        effect = EffectAccuracyIncrease(6);
                        effect = EffectLinkEffects(effect, EffectACIncrease(6));
                        effect = TagEffect(effect, "StatusEffectType." + StatusEffectType.BattleInsight2);
                        ApplyEffectToObject(DurationType.Temporary, effect, player, 6f);
                    }

                    _enmityService.ModifyEnmityOnAll(source, 120);
                    _combatPoint.AddCombatPointToAllTagged(source, SkillType.Force, 3);
                });
        }
    }
}
