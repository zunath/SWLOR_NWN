using System;

namespace SWLOR.Game.Server.Service.CombatService
{
    /// <summary>
    /// Explicit impact source; never changes equipment or another hand's captured values. The current source of each actor
    /// is state of the combat queries service (<c>Domain/Combat/CombatQueriesService.cs</c>), reached through <c>Combat</c>.
    /// </summary>
    public sealed class WeaponImpactProfile : IDisposable
    {
        private readonly uint _actor;
        private readonly WeaponImpactProfile _previous;
        public WeaponShotProfile Weapon { get; }
        public WeaponImpactActivation Activation { get; }
        public int AccuracyAdjustment { get; }
        public int VolleyIndex { get; }
        public int VolleyCount { get; }

        public WeaponImpactProfile(uint actor, WeaponShotProfile weapon, WeaponImpactActivation activation,
            int accuracyAdjustment = 0, int volleyIndex = 0, int volleyCount = 1)
        {
            _actor = actor;
            _previous = Get(actor);
            Weapon = weapon;
            Activation = activation;
            AccuracyAdjustment = accuracyAdjustment;
            VolleyIndex = volleyIndex;
            VolleyCount = volleyCount;
            Combat.SetWeaponImpactProfile(actor, this);
        }

        public static WeaponImpactProfile Get(uint actor) => Combat.GetWeaponImpactProfile(actor);
        public void Dispose()
        {
            if (_previous != null) Combat.SetWeaponImpactProfile(_actor, _previous);
            else Combat.RemoveWeaponImpactProfile(_actor);
        }
    }
}
