namespace SWLOR.Game.Server.Service.StatusEffectService
{
    /// <summary>
    /// A status effect that lasts a limited number of a ship's weapon cycles ("your next three weapon cycles").
    /// While it has cycles left, the ship's weapon cycles run with no extra delay.
    /// </summary>
    public interface ILimitedWeaponCycleStatusEffect : IStatusEffect
    {
        int RemainingCycles { get; }

        /// <summary>Spends one cycle. The effect ends when none are left.</summary>
        void ConsumeWeaponCycle();
    }
}
