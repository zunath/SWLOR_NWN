using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    /// <summary>Step 3 of the PvP healing reduction; the timer counts down to the next step.</summary>
    public sealed class PvpHealingReduction3StatusEffect : PvpCombatStatusEffectBase
    {
        public override string Name => "PvP Healing Reduction III";
        public override EffectIconType Icon => EffectIconType.PvpHealingReduction3StatusEffect;
    }
}
