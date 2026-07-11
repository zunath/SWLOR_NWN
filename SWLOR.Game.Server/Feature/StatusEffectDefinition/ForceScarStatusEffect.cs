using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    /// <summary>
    /// Passive effect applied while its Mimicry trait technique is equipped. Grants a flat offensive bonus.
    /// </summary>
    public sealed class ForceScarStatusEffect : StatusEffectBase
    {
        public override string Name => "Force Scar";
        public override EffectIconType Icon => EffectIconType.Invalid;

        public ForceScarStatusEffect()
        {
            StatGroup.Stats[StatType.ForceAttackPercentAdjustment] = 4;
        }
    }
}
