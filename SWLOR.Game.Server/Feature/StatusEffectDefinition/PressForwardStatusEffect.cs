using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class PressForwardStatusEffect : StatusEffectBase
    {
        public override string Name => "Press Forward";
        public override EffectIconType Icon => EffectIconType.PressForwardStatusEffect;
        public override StatusEffectActivationType ActivationType => StatusEffectActivationType.Passive;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;
        public PressForwardStatusEffect() : this(15) { }
        public PressForwardStatusEffect(int haste) => StatGroup.Stats[StatType.AttackDelayReductionPercent] = haste;
        public override IStatusEffect Clone() => new PressForwardStatusEffect(StatGroup.Stats[StatType.AttackDelayReductionPercent]);
    }
}
