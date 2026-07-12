using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    /// <summary>
    /// Passive effect applied while the Glacial Slime Mimicry trait is equipped. The corrosive
    /// glacial slime grants a chance for landed hits to eat through the target's defenses (Sunder).
    /// </summary>
    public sealed class GlacialCorrosionStatusEffect : StatusEffectBase
    {
        public override string Name => "Glacial Corrosion";
        public override EffectIconType Icon => EffectIconType.Invalid;

        public GlacialCorrosionStatusEffect()
        {
            StatGroup.Stats[StatType.DamageDealtSunderChance] = 20;
        }
    }
}
