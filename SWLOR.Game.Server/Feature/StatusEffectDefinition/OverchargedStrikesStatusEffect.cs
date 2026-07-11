using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    /// <summary>
    /// Passive effect applied while its Mimicry trait technique is equipped. Grants a chance for landed hits to inflict its status effect.
    /// </summary>
    public sealed class OverchargedStrikesStatusEffect : StatusEffectBase
    {
        public override string Name => "Overcharged Strikes";
        public override EffectIconType Icon => EffectIconType.Invalid;

        public OverchargedStrikesStatusEffect()
        {
            StatGroup.Stats[StatType.DamageDealtShockChance] = 20;
        }
    }
}
