using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class FlashGrenade1StatusEffect : StatusEffectBase
    {
        private readonly int _hitChancePenaltyPercent;

        public override string Name => "Flash Grenade I";
        public override EffectIconType Icon => EffectIconType.AttackDecrease;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Disruption;
        public override bool PersistsOnLogout => false;
        public override List<Type> MorePowerfulEffectTypes { get; } = new List<Type>
        {
            typeof(FlashGrenade2StatusEffect),
        };

        public FlashGrenade1StatusEffect()
            : this(8)
        {
        }

        public FlashGrenade1StatusEffect(int hitChancePenaltyPercent)
        {
            _hitChancePenaltyPercent = Math.Abs(hitChancePenaltyPercent);
            StatGroup.Stats[StatType.AbilityHitChancePercentAdjustment] = -_hitChancePenaltyPercent;
        }

        public override IStatusEffect Clone()
        {
            return new FlashGrenade1StatusEffect(_hitChancePenaltyPercent);
        }
    }
}
