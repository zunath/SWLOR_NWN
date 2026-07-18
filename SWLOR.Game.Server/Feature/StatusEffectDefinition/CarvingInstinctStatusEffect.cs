using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    /// <summary>
    /// Passive effect applied while its Mimicry trait technique is equipped. Grants a chance for landed hits to inflict its status effect.
    /// </summary>
    public sealed class CarvingInstinctStatusEffect : StatusEffectBase
    {
        public override string Name => "Carving Instinct";
        public override EffectIconType Icon => EffectIconType.Invalid;

        public CarvingInstinctStatusEffect()
        {
            StatGroup.Stats[StatType.DamageDealtHemorrhageChance] = 9;
        }
    }
}
