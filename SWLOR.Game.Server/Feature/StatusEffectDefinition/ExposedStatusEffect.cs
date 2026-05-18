using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ExposedStatusEffect : StatusEffectBase
    {
        private readonly int _defensePercent;

        public override string Name => "Exposed";
        public override EffectIconType Icon => EffectIconType.ExposedStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Trauma;
        public ExposedStatusEffect()
            : this(-15)
        {
        }

        public ExposedStatusEffect(int defensePercent)
        {
            _defensePercent = defensePercent;
            StatGroup.Stats[StatType.DefensePercentAdjustment] = defensePercent;
        }

        public override IStatusEffect Clone()
        {
            return new ExposedStatusEffect(_defensePercent);
        }
    }
}
