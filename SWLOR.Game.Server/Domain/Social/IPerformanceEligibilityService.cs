using SWLOR.Game.Server.Domain.Social;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>
    /// Whether a player may start a performance, and which instrument they play it on. A part of the Performance service.
    /// </summary>
    public interface IPerformanceEligibilityService
    {
        /// <summary>
        /// The reason the player cannot start a performance of the kind now, or null when they can. A musician's instrument
        /// family, slot and item come back for the session.
        /// </summary>
        string GetStartError(
            uint player,
            PerformanceKind kind,
            out PerformanceInstrumentFamily family,
            out InventorySlot instrumentSlot,
            out uint instrument);

        /// <summary>
        /// Whether the player may play music right now and which instrument family they are using.
        /// An equipped instrument from <see cref="PerformanceInstrumentCatalog"/> sets the family. For tests, the module or player
        /// local int PERFORMANCE_ALLOW_NO_INSTRUMENT lets anyone play; a local string PERFORMANCE_INSTRUMENT_FAMILY on the player or
        /// module then picks the family (Strings by default).
        /// </summary>
        bool HasInstrumentEquipped(uint player, out PerformanceInstrumentFamily family);

        /// <summary>One slot lookup per beat. Sessions started under the no-instrument test allowance have no item to check.</summary>
        bool IsInstrumentStillEquipped(PerformanceSession session);
    }
}
