using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    /// <summary>Step 2 of the PvP healing reduction; the timer counts down to the next step.</summary>
    public sealed class PvpHealingReduction2StatusEffect : PvpCombatStatusEffectBase
    {
        public override string Name => "PvP Healing Reduction II";
        public override EffectIconType Icon => EffectIconType.PvpHealingReduction2StatusEffect;
    }
}
