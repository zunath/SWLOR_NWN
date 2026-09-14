using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceErosionStatusEffect : StatusEffectBase
    {
        private readonly int _fpLossPerTick;
        private readonly int _staminaLossPerTick;

        public override string Name => "Force Erosion";
        public override EffectIconType Icon => EffectIconType.ForceErosionStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Disruption;
        public override float Frequency => 1f;

        public ForceErosionStatusEffect()
        {
            StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = -10;
        }

        public ForceErosionStatusEffect(int fpLossPerTick)
            : this()
        {
            _fpLossPerTick = fpLossPerTick;
        }

        public ForceErosionStatusEffect(int fpLossPerTick, int staminaLossPerTick)
            : this(fpLossPerTick)
        {
            _staminaLossPerTick = staminaLossPerTick;
        }

        protected override void Tick(uint creature)
        {
            if (_fpLossPerTick > 0)
            {
                Stat.ReduceFP(creature, _fpLossPerTick);
            }

            if (_staminaLossPerTick > 0)
            {
                Stat.ReduceStamina(creature, _staminaLossPerTick);
            }
        }
    }
}
