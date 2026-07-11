using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    /// <summary>
    /// Damage-type loadout set bonus ("elemental resonance"). Equipping multiple active techniques
    /// that share a damage type grants a scaling technique-potency bonus. Recomputed and re-applied
    /// by the Mimicry service whenever the equipped loadout changes; the magnitude is passed in.
    /// </summary>
    public sealed class TechniqueResonanceStatusEffect : StatusEffectBase
    {
        public override string Name => "Technique Resonance";
        public override EffectIconType Icon => EffectIconType.Invalid;

        // Parameterless constructor is required by the reflection-driven status-effect registry.
        public TechniqueResonanceStatusEffect()
            : this(0)
        {
        }

        public TechniqueResonanceStatusEffect(int potencyPercent)
        {
            StatGroup.Stats[StatType.MimicryPotencyPercent] = potencyPercent;
        }
    }
}
