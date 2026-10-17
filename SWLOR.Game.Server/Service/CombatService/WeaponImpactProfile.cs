using System;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.CombatService
{
    /// <summary>Explicit impact source; never changes equipment or another hand's captured values.</summary>
    public sealed class WeaponImpactProfile : IDisposable
    {
        private static readonly Dictionary<uint, WeaponImpactProfile> Profiles = new();
        private readonly uint _actor;
        private readonly WeaponImpactProfile _previous;
        public WeaponShotProfile Weapon { get; }
        public WeaponImpactActivation Activation { get; }
        public int AccuracyAdjustment { get; }

        public WeaponImpactProfile(uint actor, WeaponShotProfile weapon, WeaponImpactActivation activation,
            int accuracyAdjustment = 0)
        {
            _actor = actor;
            _previous = Get(actor);
            Weapon = weapon;
            Activation = activation;
            AccuracyAdjustment = accuracyAdjustment;
            Profiles[actor] = this;
        }

        public static WeaponImpactProfile Get(uint actor) => Profiles.GetValueOrDefault(actor);
        public void Dispose()
        {
            if (_previous != null) Profiles[_actor] = _previous;
            else Profiles.Remove(_actor);
        }
    }
}
