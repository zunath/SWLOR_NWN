using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>One stat change a Wild Space environment puts on every ship in its system.</summary>
    public class WildSpaceEffect
    {
        public StatType Stat { get; init; }
        public int Amount { get; init; }

        /// <summary>Whether the change hurts the ship (the Raider Prowler ignores these).</summary>
        public bool Penalty { get; init; }
    }
}
