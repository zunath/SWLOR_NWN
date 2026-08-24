using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    /// <summary>One Overwatch accuracy trigger waiting for the next ranged attack.</summary>
    [StatConfiguredIcon]
    public sealed class OverwatchStatusEffect : StatusEffectBase
    {
        public override string Name => "Overwatch";
        public override EffectIconType Icon => EffectIconType.TacticalUplinkStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;
        public override bool SendsApplicationMessage => false;
        public override bool SendsWornOffMessage => false;

        public override IStatusEffect Clone() => new OverwatchStatusEffect();
    }
}
