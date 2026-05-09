using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public abstract class StasisFieldStatusEffectBase : StatusEffectBase
    {
        protected abstract int DefenseBonus { get; }

        public override string Name => "Stasis Field";
        public override EffectIconType Icon => EffectIconType.ACIncrease;

        protected override void Apply(uint creature, int durationTicks)
        {
            StatGroup.Stats[StatType.Defense] = DefenseBonus;
        }
    }
}
