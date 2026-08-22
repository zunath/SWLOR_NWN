using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class OverclockRoutineStatusEffect : StatusEffectBase
    {
        public override string Name => "Overclock Routine";
        public override EffectIconType Icon => EffectIconType.OverclockRoutineStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;

        public OverclockRoutineStatusEffect()
        {
            StatGroup.Stats[StatType.CombatReadinessPercent] = 4;
        }
    }
}
