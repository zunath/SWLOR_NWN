using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// One pending delayed check per ship, such as the Ion Web expiry check. Each new check supersedes the ship's
    /// earlier one, so a callback that waited for a superseded check sees it is stale and exits instead of scheduling
    /// another.
    /// </summary>
    public sealed class ShipPendingChecks
    {
        private readonly Dictionary<uint, int> _tokens = new();

        /// <summary>Starts the ship's pending check, superseding any earlier one, and returns its token.</summary>
        public int Begin(uint ship)
        {
            var token = _tokens.TryGetValue(ship, out var current) ? current + 1 : 1;
            _tokens[ship] = token;
            return token;
        }

        /// <summary>Whether the token is still the ship's pending check.</summary>
        public bool IsCurrent(uint ship, int token) => _tokens.TryGetValue(ship, out var current) && current == token;

        /// <summary>Forgets the ship's pending check, so any callback still waiting for it exits.</summary>
        public void End(uint ship) => _tokens.Remove(ship);

        /// <summary>How many ships have a check pending.</summary>
        public int Count => _tokens.Count;
    }
}
