using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Domain.Economy
{
    /// <summary>
    /// The Smuggler's Moon Casino slot machine: who can use it, what a roll pays and how a spin settles credits. The
    /// static facade <c>Service.CasinoSlot</c> forwards to the container's instance of this interface and keeps the
    /// event handler and the constants.
    /// </summary>
    public interface ICasinoSlotService
    {
        /// <summary>
        /// Whether an area, a placeable blueprint and a placeable tag name the one casino slot machine.
        /// </summary>
        /// <param name="areaResref">The resref of the area the placeable is in.</param>
        /// <param name="resref">The resref of the placeable.</param>
        /// <param name="tag">The tag of the placeable.</param>
        /// <returns>true when all three name the casino machine.</returns>
        bool IsCasinoMachine(string areaResref, string resref, string tag);

        /// <summary>
        /// The credits a roll pays: 0, 100, 500, 1,000 or the maximum payout.
        /// </summary>
        /// <param name="roll">A roll from 0 up to, but not including, 10,000.</param>
        /// <returns>The payout in credits.</returns>
        int GetPayout(int roll);

        /// <summary>
        /// When a player used the machine, opens the slot machine window for them.
        /// </summary>
        void OnUsed();

        /// <summary>
        /// When the player may spin again, or <see cref="DateTime.MinValue"/> when they have no casino account.
        /// </summary>
        /// <param name="player">The player to look up.</param>
        /// <returns>The time of the next spin, in UTC.</returns>
        DateTime GetNextSpinUtc(uint player);

        /// <summary>
        /// Spins the machine for a player: takes the stake, settles the payout and records the spin.
        /// </summary>
        /// <param name="player">The player spinning.</param>
        /// <param name="machine">The slot machine.</param>
        /// <returns>The outcome, or why the spin did not happen.</returns>
        CasinoSpinResult Spin(uint player, uint machine);
    }
}
