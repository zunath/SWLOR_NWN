using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class MarkTarget2StatusEffect : SocialScalingStatusEffectBase
    {
        public override string Name => "Mark Target II";
        public override EffectIconType Icon => EffectIconType.DamageIncrease;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Trauma;
        public override bool PersistsOnLogout => false;
        public override List<Type> LessPowerfulEffectTypes { get; } = new List<Type>
        {
            typeof(MarkTarget1StatusEffect),
        };

        protected override void Apply(uint creature, int durationTicks)
        {
            StatGroup.Stats[StatType.DamageTakenFromStatusSourcePartyPercentAdjustment] = ScaleBySourceSocial(12, 15);
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = -ScaleBySourceSocial(10, 12);
        }
    }
}
