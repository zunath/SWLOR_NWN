using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class DeflectingAuraStatusEffect : StatusEffectBase
    {
        public override string Name => "Deflecting Aura";
        public override EffectIconType Icon => EffectIconType.DeflectingAuraStatusEffect;
        public DeflectingAuraStatusEffect()
        {
            StatGroup.Stats[StatType.RangedDeflection] = 15;
        }

    }
}
