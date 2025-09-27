using SWLOR.Component.StatusEffect.Contracts;
using SWLOR.Component.StatusEffect.Service;
using SWLOR.NWN.API.NWScript.Constants;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Combat.ValueObjects;
using SWLOR.Shared.Domain.Communication.Contracts;
using SWLOR.Shared.Domain.Skill.Enums;
using SWLOR.Shared.Domain.StatusEffect.Enums;
using SWLOR.Shared.Domain.StatusEffect.ValueObjects;

namespace SWLOR.Component.StatusEffect.Feature.StatusEffectDefinition
{
    public class BattleInsightStatusEffectDefinition: IStatusEffectListDefinition
    {
        private readonly ICombatPointService _combatPointService;
        private readonly IEnmityService _enmityService;
        private readonly IPartyService _partyService;

        public BattleInsightStatusEffectDefinition(
            ICombatPointService combatPointService, 
            IEnmityService enmityService,
            IPartyService partyService)
        {
            _combatPointService = combatPointService;
            _enmityService = enmityService;
            _partyService = partyService;
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

                    var party = _partyService.GetAllPartyMembersWithinRange(source, RadiusSize.Medium);

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
                    _combatPointService.AddCombatPointToAllTagged(source, SkillType.Force, 3);
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

                    var party = _partyService.GetAllPartyMembersWithinRange(source, RadiusSize.Medium);

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
                    _combatPointService.AddCombatPointToAllTagged(source, SkillType.Force, 3);
                });
        }
    }
}
