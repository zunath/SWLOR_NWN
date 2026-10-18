using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    /// <summary>Step 4 of the PvP healing reduction; the timer counts down to the next step.</summary>
    public sealed class PvpHealingReduction4StatusEffect : PvpCombatStatusEffectBase
    {
        public override string Name => "PvP Healing Reduction IV";
        public override EffectIconType Icon => EffectIconType.PvpHealingReduction4StatusEffect;
    }
}
