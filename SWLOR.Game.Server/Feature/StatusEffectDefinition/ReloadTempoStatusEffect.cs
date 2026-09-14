using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    /// <summary>
    /// Anchors the Reload Tempo icon identity for the gameplay-icon pipeline. Shared combat applies
    /// LimitedHasteStatusEffect with this icon selected through StatType metadata.
    /// </summary>
    public sealed class ReloadTempoStatusEffect : StatusEffectBase
    {
        public override string Name => "Reload Tempo";
        public override EffectIconType Icon => EffectIconType.ReloadTempoStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.None;
        public override bool PersistsOnLogout => false;
        public override bool SendsApplicationMessage => false;
        public override bool SendsWornOffMessage => false;
    }
}
