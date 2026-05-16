using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class AdhesiveGrenadeSlowStatusEffect : StatusEffectBase
    {
        private readonly int _movementSpeedPenaltyPercent;

        public override string Name => "Adhesive Grenade";
        public override EffectIconType Icon => EffectIconType.MovementSpeedDecrease;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff | StatusEffectCategory.Control;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Mobility;
        public override bool PersistsOnLogout => false;

        public AdhesiveGrenadeSlowStatusEffect()
            : this(50)
        {
        }

        public AdhesiveGrenadeSlowStatusEffect(int movementSpeedPenaltyPercent)
        {
            _movementSpeedPenaltyPercent = Math.Abs(movementSpeedPenaltyPercent);
            StatGroup.Stats[StatType.MovementSpeedPercentAdjustment] = -_movementSpeedPenaltyPercent;
        }

        public override IStatusEffect Clone()
        {
            return new AdhesiveGrenadeSlowStatusEffect(_movementSpeedPenaltyPercent);
        }
    }
}
