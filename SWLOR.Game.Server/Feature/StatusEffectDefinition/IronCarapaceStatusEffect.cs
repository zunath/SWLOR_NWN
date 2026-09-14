using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class IronCarapaceStatusEffect : StatusEffectBase
    {
        public override string Name => "Iron Carapace";
        public override EffectIconType Icon => EffectIconType.IronCarapaceStatusEffect;

        public IronCarapaceStatusEffect()
        {
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = 15;
            StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = 10;
            StatGroup.Resists[ResistanceType.Trauma] = 25;
            StatGroup.Resists[ResistanceType.Fire] = 15;
            StatGroup.Resists[ResistanceType.Poison] = 15;
        }
    }
}
