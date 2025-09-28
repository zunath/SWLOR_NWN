using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.StatusEffect.Enums;
using SWLOR.Shared.Domain.StatusEffect.ValueObjects;

namespace SWLOR.Component.StatusEffect.Contracts
{
    /// <summary>
    /// Service responsible for managing status effect data and state.
    /// </summary>
    public interface IStatusEffectDataService
    {
        /// <summary>
        /// Gets the status effect details dictionary.
        /// </summary>
        Dictionary<StatusEffectType, StatusEffectDetail> StatusEffects { get; }

        /// <summary>
        /// Gets the creatures with status effects dictionary.
        /// </summary>
        Dictionary<uint, Dictionary<StatusEffectType, StatusEffectGroup>> CreaturesWithStatusEffects { get; }

        /// <summary>
        /// Gets the logged out players with effects dictionary.
        /// </summary>
        Dictionary<uint, Dictionary<StatusEffectType, StatusEffectGroup>> LoggedOutPlayersWithEffects { get; }

        /// <summary>
        /// Gets the effect icon to status effects mapping.
        /// </summary>
        Dictionary<EffectIconType, List<StatusEffectType>> EffectIconToStatusEffects { get; }

        /// <summary>
        /// Gets the ability increase icon type mapping.
        /// </summary>
        Dictionary<EffectIconType, AbilityType> AbilityIncreaseIconType { get; }

        /// <summary>
        /// Gets the effect icon to effect type mapping.
        /// </summary>
        Dictionary<EffectIconType, EffectScriptType> EffectIconToEffectType { get; }
    }

    /// <summary>
    /// Internal class for grouping status effect data.
    /// </summary>
    public class StatusEffectGroup
    {
        public uint Source { get; set; }
        public DateTime Expiration { get; set; }
        public FeatType ConcentrationFeatType { get; set; }
        public object EffectData { get; set; }
    }
}
