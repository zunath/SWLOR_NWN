using SWLOR.CombatSim.Data;
using SWLOR.CombatSim.Engine;
using SWLOR.CombatSim.Rules;
using SWLOR.CombatSim.Scenario;

namespace SWLOR.CombatSim.Tests.Support
{
    /// <summary>
    /// One combatant attacking another, with a third on the target's side ready to heal. The clock is driven by hand
    /// so a test can act at exact seconds of a fight.
    /// </summary>
    internal sealed class PvpFight
    {
        public const double WoundedHp = 500_000;
        private const int KeepAliveSeconds = 10;

        private readonly AbilityCatalog _catalog;
        private readonly RuleSet _rules;
        private Simulation _simulation;
        private Combatant _attacker;
        private Combatant _target;
        private Combatant _healer;
        private int _lastHitAt = -1;

        public PvpFight(AbilityCatalog catalog, RuleSet rules)
        {
            _catalog = catalog;
            _rules = rules;
        }

        public bool IsPvp { get; init; } = true;
        public bool AttackerIsNpc { get; init; }
        public bool TargetIsNpc { get; init; }
        public float TargetHealingReceivedPercent { get; init; }
        public float TargetDamageReductionPercent { get; init; }

        /// <summary>One point of damage at the given second, enough to start or continue PvP combat.</summary>
        public void Hit(int second)
        {
            EnsureSpawned();
            SimulationClock.Set(_simulation, second);
            _simulation.ApplyDamage(_attacker, _target, 1);
            _lastHitAt = second;
        }

        /// <summary>Hits every few seconds from the previous hit (or from 0) up to <paramref name="seconds"/>, so the combat never lapses.</summary>
        public void FightUntil(int seconds)
        {
            var second = _lastHitAt < 0 ? 0 : _lastHitAt + KeepAliveSeconds;
            for (; second < seconds; second += KeepAliveSeconds)
                Hit(second);

            Hit(seconds);
        }

        /// <summary>Heals the wounded target at the given second and returns the HP actually gained.</summary>
        public double HealAt(int second, HealKind kind = HealKind.InstantAbility, int amount = 1000)
        {
            EnsureSpawned();
            SimulationClock.Set(_simulation, second);
            _target.Hp = WoundedHp;

            _simulation.ApplyHealing(_healer, _target, amount, kind);

            return _target.Hp - WoundedHp;
        }

        /// <summary>Damages the target at the given second and returns the HP it lost.</summary>
        public double DamageAt(int second, double amount = 100)
        {
            EnsureSpawned();
            SimulationClock.Set(_simulation, second);
            _target.Hp = WoundedHp;

            _simulation.ApplyDamage(_attacker, _target, amount);

            return WoundedHp - _target.Hp;
        }

        private void EnsureSpawned()
        {
            if (_simulation != null)
                return;

            var attacker = new CombatantSpec { Name = "Attacker", MaxHp = TestCombatants.EffectivelyUnlimited, IsNpc = AttackerIsNpc };
            var target = new CombatantSpec
            {
                Name = "Target",
                MaxHp = TestCombatants.EffectivelyUnlimited,
                IsNpc = TargetIsNpc,
                HealingReceivedPercent = TargetHealingReceivedPercent,
                DamageReductionPercent = TargetDamageReductionPercent
            };
            var healer = new CombatantSpec { Name = "Healer", MaxHp = 1000 };
            _simulation = TestScenarios.Spawn(
                TestScenarios.TwoTeams(IsPvp, new[] { target, healer }, new[] { attacker }), _rules, _catalog);
            _attacker = _simulation.Get("Attacker");
            _target = _simulation.Get("Target");
            _healer = _simulation.Get("Healer");
        }
    }
}
