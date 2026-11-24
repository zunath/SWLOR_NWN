using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Domain.Creatures
{
    /// <summary>
    /// The registered companions of the companion control and what is known about each: whether a creature is a droid or
    /// player beast under a player's control, its mode and whether its abilities are on. Moved here unchanged from
    /// <see cref="CompanionControlService"/>; the other parts read and change a companion's state through this one.
    /// </summary>
    public class CompanionControlStoreService : ICompanionControlStoreService
    {
        private readonly Dictionary<uint, CompanionControlState> _states = new();

        private readonly ICompanionLookup _companions;

        public CompanionControlStoreService(ICompanionLookup companions)
        {
            _companions = companions;
        }

        public bool IsRegisteredCompanion(uint creature)
        {
            return _states.ContainsKey(creature);
        }

        public bool IsControlledCompanion(uint creature)
        {
            if (!GetIsObjectValid(creature))
                return false;

            var master = GetMaster(creature);
            return GetIsObjectValid(master) &&
                   GetIsPC(master) &&
                   (IsDroid(creature) || _companions.IsPlayerBeast(creature));
        }

        public CompanionMode GetMode(uint companion)
        {
            return GetState(companion).Mode;
        }

        public bool AreAbilitiesEnabled(uint companion)
        {
            return !IsRegisteredCompanion(companion) || GetState(companion).AbilitiesEnabled;
        }

        public CompanionControlState GetState(uint companion)
        {
            if (!_states.TryGetValue(companion, out var state))
            {
                state = new CompanionControlState
                {
                    LastMasterArea = GetArea(GetMaster(companion))
                };
                _states[companion] = state;
            }

            return state;
        }

        public void Register(uint companion)
        {
            _states[companion] = new CompanionControlState
            {
                LastMasterArea = GetArea(GetMaster(companion))
            };
        }

        public void Remove(uint companion)
        {
            _states.Remove(companion);
        }

        public bool TryGetState(uint companion, out CompanionControlState state)
        {
            return _states.TryGetValue(companion, out state);
        }

        // The protected virtual method below has the original call as its body, so the server behaves as it did.
        // It names a service that is not converted yet, so a test can answer it.

        protected virtual bool IsDroid(uint creature) => _companions.IsDroid(creature);
    }
}
