using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    /// <summary>
    /// Last Bastion's threat anchor: applied to enemies near the caster's defensive line, causing
    /// them to generate additional enmity toward the caster (the status source) for the duration.
    /// </summary>
    public sealed class LastBastionStatusEffect : StatusEffectBase
    {
        private const int EnmityTowardSourcePercent = 25;

        public override string Name => "Last Bastion";
        public override EffectIconType Icon => EffectIconType.LastBastionStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override bool PersistsOnLogout => false;

        public LastBastionStatusEffect()
        {
            StatGroup.Stats[StatType.EnmityToStatusSourcePercentAdjustment] = EnmityTowardSourcePercent;
        }
    }
}
