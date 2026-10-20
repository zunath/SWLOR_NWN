using System;
using System.Collections.Generic;
using System.Threading;
using NWN.Native.API;
using SWLOR.Game.Server.Service.CombatService;

namespace SWLOR.Game.Server.Native
{
    /// <summary>Selects a simulated launcher's profile without changing native equipment.</summary>
    public static class RangedWeaponAttackContext
    {
        public sealed class Roll
        {
            public long Id { get; } = Interlocked.Increment(ref _nextRoll);
            public uint Weapon { get; init; }
            public uint Target { get; init; }
            public int AttackIndex { get; init; }
            public bool Ordinary { get; set; }
            public bool Completed { get; set; }
            public PairedRangedOrdinarySnapshot Bonuses { get; set; }
            public int Damage { get; set; }
            public CombatDamageType DamageType { get; set; }
        }

        private sealed class Scope : IDisposable
        {
            private readonly uint _actor;
            private readonly WeaponShotProfile _previous;
            public Scope(uint actor, WeaponShotProfile weapon)
            {
                _actor = actor;
                _previous = _weapons.GetValueOrDefault(actor);
                _weapons[actor] = weapon;
            }
            public void Dispose()
            {
                if (_previous != null) _weapons[_actor] = _previous;
                else _weapons.Remove(_actor);
            }
        }

        private static long _nextRoll;
        private static readonly Dictionary<uint, WeaponShotProfile> _weapons = new();
        private static readonly Dictionary<uint, Roll> _rolls = new();

        public static IDisposable SelectProfile(uint actor, WeaponShotProfile weapon) => new Scope(actor, weapon);
        public static WeaponShotProfile GetProfile(uint actor) => _weapons.GetValueOrDefault(actor);
        public static bool IsSimulated(uint actor) => _weapons.ContainsKey(actor);
        public static CNWSItem GetWeapon(uint actor, CNWSItem nativeWeapon) =>
            _weapons.TryGetValue(actor, out var weapon)
                ? NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(weapon.Weapon)?.AsNWSItem()
                : nativeWeapon;

        public static Roll BeginRoll(uint actor, uint weapon, uint target, int attackIndex)
        {
            var roll = new Roll { Weapon = weapon, Target = target, AttackIndex = attackIndex };
            _rolls[actor] = roll;
            return roll;
        }

        public static Roll GetRoll(uint actor, uint target, int attackIndex) =>
            _rolls.TryGetValue(actor, out var roll) && roll.Target == target && roll.AttackIndex == attackIndex
                ? roll : null;

        public static void Clear(uint actor)
        {
            _weapons.Remove(actor);
            _rolls.Remove(actor);
        }
    }
}
