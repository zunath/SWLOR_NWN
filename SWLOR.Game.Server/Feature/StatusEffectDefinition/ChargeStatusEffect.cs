using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ChargeStatusEffect : AuraStatusEffectBase
    {
        public override string Name => "Charge";
        public override EffectIconType Icon => EffectIconType.Charge;

        protected override void Apply(uint creature, int durationTicks)
        {
            StatGroup.Stats[StatType.MovementSpeedPercentAdjustment] = Perk.GetPerkLevel(Source, PerkType.Charge) switch
            {
                1 => 15,
                2 => 30,
                _ => 0
            };
        }
    }
}
