using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// The cloaks running on ships and the weapon locks left behind by decloaking. Pure bookkeeping with no engine
    /// calls: the Space service adds visibility, effects and messages around it.
    /// </summary>
    public class ShipCloakBook
    {
        private readonly Dictionary<uint, ShipCloakState> _states = new();
        private readonly Dictionary<uint, DateTime> _locks = new();

        public int Count => _states.Count;

        public List<uint> Ships => _states.Keys.ToList();

        public bool TryGet(uint ship, out ShipCloakState state) => _states.TryGetValue(ship, out state);

        /// <summary>Starts a ship's cloak. A ship that already has one keeps it.</summary>
        public ShipCloakState Begin(uint ship, DateTime now, ShipCloakProfile profile, int slot)
        {
            if (_states.TryGetValue(ship, out var existing))
                return existing;

            _locks.Remove(ship);
            var state = new ShipCloakState { StartedAt = now, Profile = profile, Slot = slot };
            _states[ship] = state;
            return state;
        }

        public ShipCloakPhase GetPhase(uint ship, DateTime now) =>
            _states.TryGetValue(ship, out var state)
                ? ShipCloakRules.GetPhase(state.StartedAt, state.Profile.EngageSeconds, now)
                : ShipCloakPhase.Off;

        /// <summary>
        /// Ends a ship's cloak. One that had vanished locks the ship's weapons; one still engaging locks nothing.
        /// </summary>
        public ShipCloakEnd End(uint ship, DateTime now)
        {
            if (!_states.TryGetValue(ship, out var state))
                return ShipCloakEnd.None;

            var phase = ShipCloakRules.GetPhase(state.StartedAt, state.Profile.EngageSeconds, now);
            _states.Remove(ship);
            if (phase != ShipCloakPhase.Cloaked)
                return ShipCloakEnd.Cancelled;

            _locks[ship] = ShipCloakRules.GetFiringUnlockTime(now);
            return ShipCloakEnd.Decloaked;
        }

        /// <summary>Forgets a ship completely (it left space), lock included.</summary>
        public void Clear(uint ship)
        {
            _states.Remove(ship);
            _locks.Remove(ship);
        }

        /// <summary>Whole seconds of the decloak lock left on a ship.</summary>
        public int GetLockSecondsLeft(uint ship, DateTime now)
        {
            if (!_locks.TryGetValue(ship, out var unlocksAt))
                return 0;

            if (now >= unlocksAt)
            {
                _locks.Remove(ship);
                return 0;
            }

            return ShipCloakRules.GetLockSecondsLeft(unlocksAt, now);
        }

        /// <summary>Whether the ship cannot fire a weapon: it is cloaking, cloaked or just decloaked.</summary>
        public bool IsFiringLocked(uint ship, DateTime now) =>
            ShipCloakRules.IsFiringLocked(GetPhase(ship, now), GetLockSecondsLeft(ship, now));

        /// <summary>Reveals a cloaked ship to observers until a time (a Sensor Sweep found it).</summary>
        public void Paint(uint ship, IEnumerable<uint> observers, DateTime until)
        {
            if (!_states.TryGetValue(ship, out var state))
                return;

            foreach (var observer in observers)
            {
                state.PaintedUntil[observer] = until;
            }
        }

        public bool IsPainted(uint ship, uint observer, DateTime now) =>
            _states.TryGetValue(ship, out var state) &&
            state.PaintedUntil.TryGetValue(observer, out var until) &&
            now < until;

        /// <summary>Whether an observer cannot see the ship through its cloak.</summary>
        public bool IsHiddenFrom(uint ship, uint observer, bool isAlly, double distance, DateTime now)
        {
            if (!_states.TryGetValue(ship, out var state))
                return false;

            return ShipCloakRules.IsHidden(
                ShipCloakRules.GetPhase(state.StartedAt, state.Profile.EngageSeconds, now),
                isAlly,
                distance,
                state.Profile.RevealMeters,
                IsPainted(ship, observer, now));
        }
    }
}
