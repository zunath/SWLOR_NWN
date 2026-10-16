using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    /// <summary>A player in PvP combat; the timer counts down to the first healing reduction.</summary>
    public sealed class PvpCombatStatusEffect : PvpCombatStatusEffectBase
    {
        public override string Name => "PvP Combat";
        public override EffectIconType Icon => EffectIconType.PvpCombatStatusEffect;
    }
}
