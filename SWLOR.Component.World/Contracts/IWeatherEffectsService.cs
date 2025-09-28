using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Component.World.Contracts
{
    /// <summary>
    /// Service for applying weather effects to creatures and handling weather-related damage.
    /// </summary>
    public interface IWeatherEffectsService
    {
        /// <summary>
        /// Applies weather effects to a creature.
        /// </summary>
        /// <param name="oCreature">The creature to apply effects to.</param>
        void DoWeatherEffects(uint oCreature);

        /// <summary>
        /// Applies acid rain damage to a target.
        /// </summary>
        /// <param name="oTarget">The target to apply acid damage to.</param>
        /// <param name="oArea">The area where the acid rain is occurring.</param>
        void ApplyAcid(uint oTarget, uint oArea);

        /// <summary>
        /// Applies cold damage to a target.
        /// </summary>
        /// <param name="oTarget">The target to apply cold damage to.</param>
        /// <param name="oArea">The area where the cold is occurring.</param>
        void ApplyCold(uint oTarget, uint oArea);

        /// <summary>
        /// Applies sandstorm damage to a target.
        /// </summary>
        /// <param name="oTarget">The target to apply sandstorm damage to.</param>
        /// <param name="oArea">The area where the sandstorm is occurring.</param>
        void ApplySandstorm(uint oTarget, uint oArea);

        /// <summary>
        /// Applies snowstorm damage to a target.
        /// </summary>
        /// <param name="oTarget">The target to apply snowstorm damage to.</param>
        /// <param name="oArea">The area where the snowstorm is occurring.</param>
        void ApplySnowstorm(uint oTarget, uint oArea);

        /// <summary>
        /// Handles wind knockdown effects on combat round end.
        /// </summary>
        /// <param name="oCreature">The creature to check for wind effects.</param>
        void OnCombatRoundEnd(uint oCreature);

        /// <summary>
        /// Creates a thunderstorm effect in an area.
        /// </summary>
        /// <param name="oArea">The area to create thunderstorm in.</param>
        void Thunderstorm(uint oArea);
    }
}
