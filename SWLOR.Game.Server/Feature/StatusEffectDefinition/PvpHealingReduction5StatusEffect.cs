using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    /// <summary>Step 5 of the PvP healing reduction; the timer counts down to the next step.</summary>
    public sealed class PvpHealingReduction5StatusEffect : PvpCombatStatusEffectBase
    {
        public override string Name => "PvP Healing Reduction V";
        public override EffectIconType Icon => EffectIconType.PvpHealingReduction5StatusEffect;
    }
}
