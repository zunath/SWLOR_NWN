using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    /// <summary>
    /// Anchors the Snap Roll icon identity for the gameplay-icon pipeline. Shared combat applies
    /// RangedDeflectionStatusEffect with this icon selected through StatType metadata.
    /// </summary>
    public sealed class SnapRollStatusEffect : StatusEffectBase
    {
        public override string Name => "Snap Roll";
        public override EffectIconType Icon => EffectIconType.SnapRollStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.None;
        public override bool PersistsOnLogout => false;
        public override bool SendsApplicationMessage => false;
        public override bool SendsWornOffMessage => false;
    }
}
