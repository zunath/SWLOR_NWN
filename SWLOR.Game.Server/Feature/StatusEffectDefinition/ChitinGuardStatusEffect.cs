using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    /// <summary>
    /// Passive effect applied while the Chitin Guard Mimicry trait is equipped. A lighter, elemental
    /// shell than Iron Carapace: it leans into Force Defense and Fire/Poison resistance instead of raw
    /// physical bulk, so the two defensive traits play differently rather than being interchangeable.
    /// </summary>
    public sealed class ChitinGuardStatusEffect : StatusEffectBase
    {
        public override string Name => "Chitin Guard";
        public override EffectIconType Icon => EffectIconType.Invalid;

        public ChitinGuardStatusEffect()
        {
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = 10;
            StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = 15;
            StatGroup.Resists[ResistanceType.Fire] = 20;
            StatGroup.Resists[ResistanceType.Poison] = 20;
        }
    }
}
