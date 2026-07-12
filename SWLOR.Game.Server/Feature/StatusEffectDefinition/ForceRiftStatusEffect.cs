using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    /// <summary>
    /// Passive effect applied while its Mimicry trait technique is equipped. Grants a chance for landed hits to inflict its status effect.
    /// </summary>
    public sealed class ForceRiftStatusEffect : StatusEffectBase
    {
        public override string Name => "Force Rift";
        public override EffectIconType Icon => EffectIconType.Invalid;

        public ForceRiftStatusEffect()
        {
            StatGroup.Stats[StatType.DamageDealtSunderChance] = 24;
        }
    }
}
