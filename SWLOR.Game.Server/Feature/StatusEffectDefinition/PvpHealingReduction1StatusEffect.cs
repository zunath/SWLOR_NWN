using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    /// <summary>Step 1 of the PvP healing reduction; the timer counts down to the next step.</summary>
    public sealed class PvpHealingReduction1StatusEffect : PvpCombatStatusEffectBase
    {
        public override string Name => "PvP Healing Reduction I";
        public override EffectIconType Icon => EffectIconType.PvpHealingReduction1StatusEffect;
    }
}
