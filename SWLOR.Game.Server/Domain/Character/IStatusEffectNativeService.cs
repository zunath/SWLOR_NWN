namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// The native effect that carries each status effect and the scripts it runs: applying, replacing and reapplying it, the apply,
    /// remove and interval scripts, and the ticks. Part 5 of the status effect split; it holds no state and reaches the trackers and
    /// the removal through <see cref="IStatusEffectService"/>.
    /// </summary>
    public interface IStatusEffectNativeService
    {
        /// <summary>
        /// Handle when a status effect is applied (called by NWN script)
        /// </summary>
        void OnApplyStatusEffect();

        /// <summary>
        /// Handle when a status effect is removed (called by NWN script)
        /// </summary>
        void OnRemoveStatusEffect();

        /// <summary>
        /// Handle status effect interval processing (called by NWN script)
        /// </summary>
        void OnStatusEffectInterval();

        /// <summary>Makes sure the creature running the apply script has a status effect tracker.</summary>
        void OnApplyNWNStatusEffect(uint player);

        /// <summary>Runs when the engine removes a native status effect: drops the tracker of a creature that lost them all, or settles one effect whose native timer is gone.</summary>
        void OnRemoveNWNStatusEffect(uint player);

        /// <summary>Runs the tick of every ticking effect of a creature, or of the one the interval script names.</summary>
        void OnNWNStatusEffectInterval(uint creature);

        /// <summary>Applies the native effect of every status effect a creature holds again, and its movement rate when one adjusts a stat.</summary>
        void ReapplyNWNEffects(uint creature, CreatureStatusEffect effects);

        /// <summary>Applies the native effect of a status effect and tracks it, so a restart or logout finds it.</summary>
        void ApplyTrackedNWNEffect(uint creature, IStatusEffect statusEffect, int durationTicks, bool isPermanent);

        /// <summary>Replaces the native effect of a status effect with one for the given duration, as a restart, refresh or extension does.</summary>
        void ReplaceNativeStatusEffect(uint creature, IStatusEffect statusEffect, int durationTicks, bool isPermanent);
    }
}
