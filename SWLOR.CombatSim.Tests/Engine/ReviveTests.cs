using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.Data;
using SWLOR.CombatSim.Engine;
using SWLOR.CombatSim.Rules;
using SWLOR.CombatSim.Scenario;
using SWLOR.CombatSim.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.CombatSim.Tests.Engine
{
    [TestFixture]
    public class ReviveTests
    {
        private const double ResuscitationCastSeconds = 4.0;

        [Test]
        public void PveRevive_SkipsALockedOutBodyForOneThatCanBeRevived()
        {
            var medic = TestCombatants.Medic("Medic", TestCombatants.Use("Resuscitation1", AbilityTarget.DeadAlly));
            var simulation = TestScenarios.Spawn(
                TestScenarios.TwoTeams(
                    false,
                    new[] { medic, new CombatantSpec { Name = "Revived", MaxHp = 1000 }, new CombatantSpec { Name = "Fresh", MaxHp = 1000 } },
                    new[] { TestCombatants.Dummy("Enemy", true) }),
                RuleSets.Tuned(),
                _catalog);
            var enemy = simulation.Get("Enemy");
            var revived = simulation.Get("Revived");
            var fresh = simulation.Get("Fresh");

            simulation.ApplyDamage(enemy, revived, TestCombatants.EffectivelyUnlimited);
            SimulationClock.RunUntil(simulation, () => revived.Stats.RevivesReceived == 1, 10, "the first revive");

            // Dies again inside its five-minute lockout, before the other ally, so it is the oldest body.
            simulation.ApplyDamage(enemy, revived, TestCombatants.EffectivelyUnlimited);
            SimulationClock.RunUntil(simulation, simulation.Now + 1);
            simulation.ApplyDamage(enemy, fresh, TestCombatants.EffectivelyUnlimited);

            SimulationClock.RunUntil(simulation, () => fresh.Stats.RevivesReceived == 1, ResuscitationRecastSeconds + 20, "the eligible body revived");
            revived.IsDead.Should().BeTrue("its lockout still holds");
        }
        private const double ResuscitationRecastSeconds = 60.0;
        private const double PveLockoutSeconds = 300.0;
        private const double PvpPartyDamageWindowSeconds = 15.0;

        private AbilityCatalog _catalog;

        [OneTimeSetUp]
        public void LoadCatalog()
        {
            _catalog = AbilityCatalog.Load();
        }

        [Test]
        public void CurrentRules_AMedicRevivesADeadAllyMidFightInPvp()
        {
            var (simulation, medic, ally, enemy) = SpawnAndKillAlly(RuleSets.Current(), isPvp: true);

            SimulationClock.RunUntil(simulation, () => !ally.IsDead, 15, "ally revived");

            enemy.IsDead.Should().BeFalse("the revive happened with an enemy still standing");
            ally.Stats.RevivesReceived.Should().Be(1);
            ally.Hp.Should().Be(1, "the game heals 0.1 seconds after the revive");
            SimulationClock.RunUntil(simulation, simulation.Now + 0.2);
            ally.Hp.Should().Be(251, "1 HP from the revive plus the 20% heal of 200 scaled by 26 Willpower");
            medic.Stats.BlockedRevives.Should().Be(0);
        }

        [Test]
        public void Tuned_RevivesAreBlockedInPvpWhileAnEnemyIsAlive()
        {
            var (simulation, medic, ally, _) = SpawnAndKillAlly(RuleSets.Tuned(), isPvp: true);

            SimulationClock.RunUntil(simulation, 60);

            ally.IsDead.Should().BeTrue();
            ally.Stats.RevivesReceived.Should().Be(0);
            medic.Stats.BlockedRevives.Should().BeGreaterThan(0);
            medic.Stats.AbilitiesUsed.Should().Be(0);
            medic.Stats.StmSpent.Should().Be(0, "a blocked revive costs nothing");
        }

        [TestCase(0.1)]
        [TestCase(0.05)]
        public void Tuned_BlockedReviveAttempts_AreCountedOncePerReactionWindow_WhateverTheTimeStep(double step)
        {
            var (simulation, medic, _, _) = SpawnAndKillAlly(RuleSets.Tuned(), isPvp: true);

            SimulationClock.RunUntil(simulation, 60, step);

            medic.Stats.BlockedRevives.Should().BeInRange(59, 61, "one attempt a second (the medic's reaction time) for a minute");
        }

        [Test]
        public void TunedPvp_RevivesAreAlsoBlockedInPvpWhileAnEnemyIsAlive()
        {
            var (simulation, _, ally, _) = SpawnAndKillAlly(RuleSets.TunedPvp(), isPvp: true);

            SimulationClock.RunUntil(simulation, 60);

            ally.IsDead.Should().BeTrue();
            ally.Stats.RevivesReceived.Should().Be(0);
        }

        [Test]
        public void Tuned_PvpRevivesWorkOnceNoEnemyIsLeft()
        {
            var (simulation, medic, ally, enemy) = SpawnAndKillAlly(RuleSets.Tuned(), isPvp: true);
            SimulationClock.RunUntil(simulation, 20);
            ally.IsDead.Should().BeTrue();

            simulation.ApplyDamage(medic, enemy, TestCombatants.EffectivelyUnlimited * 2.0);
            enemy.IsDead.Should().BeTrue();
            SimulationClock.RunUntil(simulation, () => !ally.IsDead, 60, "ally revived after the enemy fell");

            ally.Stats.RevivesReceived.Should().Be(1);
        }

        [Test]
        public void Tuned_PvpRevivesWaitUntilTheLivingPartyHasNotBeenHitForFifteenSeconds()
        {
            var (simulation, medic, ally, enemy) = SpawnAndKillAlly(RuleSets.Tuned(), isPvp: true);
            simulation.ApplyDamage(enemy, medic, 10);
            simulation.ApplyDamage(medic, enemy, TestCombatants.EffectivelyUnlimited * 2.0);
            var lastHitAt = simulation.Now;

            SimulationClock.RunUntil(simulation, lastHitAt + PvpPartyDamageWindowSeconds - 1);
            ally.IsDead.Should().BeTrue();
            medic.Casting.Should().BeNull("no cast may start inside the damage window");

            SimulationClock.RunUntil(simulation, () => !ally.IsDead, 60, "ally revived after the window passed");
            ally.Stats.RevivesReceived.Should().Be(1);
        }

        [Test]
        public void Tuned_InPve_TheFirstReviveTakesTheFourSecondCast()
        {
            var (simulation, medic, ally, _) = SpawnAndKillAlly(RuleSets.Tuned(), isPvp: false);
            SimulationClock.RunUntil(simulation, () => medic.Casting != null, 10, "revive cast started");
            var castStartedAt = simulation.Now;

            SimulationClock.RunUntil(simulation, () => !ally.IsDead, 20, "ally revived");

            (simulation.Now - castStartedAt).Should().BeInRange(ResuscitationCastSeconds - 0.3, ResuscitationCastSeconds + 0.3);
            ally.Stats.RevivesReceived.Should().Be(1);
        }

        [Test]
        public void Tuned_InPve_ASecondReviveOfTheSameTargetWithinFiveMinutesIsBlocked()
        {
            var (simulation, medic, ally, enemy) = SpawnAndKillAlly(RuleSets.Tuned(), isPvp: false);
            SimulationClock.RunUntil(simulation, () => !ally.IsDead, 20, "first revive");
            var revivedAt = simulation.Now;

            // Past the 60 second recast, but well inside the five minute lockout.
            SimulationClock.RunUntil(simulation, revivedAt + ResuscitationRecastSeconds + 10);
            simulation.ApplyDamage(enemy, ally, TestCombatants.EffectivelyUnlimited);
            ally.IsDead.Should().BeTrue();
            SimulationClock.RunUntil(simulation, revivedAt + PveLockoutSeconds - 20);

            ally.IsDead.Should().BeTrue("the target is still locked out");
            ally.Stats.RevivesReceived.Should().Be(1);
            medic.Stats.BlockedRevives.Should().BeGreaterThan(0);

            SimulationClock.RunUntil(simulation, () => !ally.IsDead, revivedAt + PveLockoutSeconds + 30, "revive once the lockout ended");
            (simulation.Now - revivedAt).Should().BeGreaterThanOrEqualTo(PveLockoutSeconds);
            ally.Stats.RevivesReceived.Should().Be(2);
        }

        [Test]
        public void CurrentRules_InPve_ASecondReviveOfTheSameTargetIsAllowedAfterTheRecast()
        {
            var (simulation, medic, ally, enemy) = SpawnAndKillAlly(RuleSets.Current(), isPvp: false);
            SimulationClock.RunUntil(simulation, () => !ally.IsDead, 20, "first revive");
            var revivedAt = simulation.Now;

            SimulationClock.RunUntil(simulation, revivedAt + ResuscitationRecastSeconds + 10);
            simulation.ApplyDamage(enemy, ally, TestCombatants.EffectivelyUnlimited);
            SimulationClock.RunUntil(simulation, () => !ally.IsDead, revivedAt + 120, "second revive");

            ally.Stats.RevivesReceived.Should().Be(2);
            medic.Stats.BlockedRevives.Should().Be(0);
        }

        [Test]
        public void Tuned_InPve_DamageDuringTheCastInterruptsItAndCostsNothing()
        {
            var rules = RuleSets.Tuned();
            var (simulation, medic, ally, enemy) = SpawnAndKillAlly(rules, isPvp: false);
            SimulationClock.RunUntil(simulation, () => medic.Casting != null, 10, "revive cast started");

            SimulationClock.RunUntil(simulation, simulation.Now + 2.0);
            simulation.ApplyDamage(enemy, medic, 10);

            medic.Casting.Should().BeNull();
            medic.Stats.InterruptedCasts.Should().Be(1);
            medic.Stats.StmSpent.Should().Be(0);
            ally.IsDead.Should().BeTrue();

            // Left alone, the medic starts over and finishes the revive.
            SimulationClock.RunUntil(simulation, () => !ally.IsDead, 30, "revive after the interruption");
            ally.Stats.RevivesReceived.Should().Be(1);
            medic.Stats.StmSpent.Should().Be(rules.GetStmCost(_catalog.Get(FeatType.Resuscitation2)), "only the finished cast was paid for");
        }

        [Test]
        public void CurrentRules_InPve_DamageDoesNotInterruptTheRevive()
        {
            var (simulation, medic, ally, enemy) = SpawnAndKillAlly(RuleSets.Current(), isPvp: false);
            SimulationClock.RunUntil(simulation, () => medic.Casting != null, 10, "revive cast started");

            SimulationClock.RunUntil(simulation, simulation.Now + 2.0);
            simulation.ApplyDamage(enemy, medic, 10);

            medic.Casting.Should().NotBeNull();
            medic.Stats.InterruptedCasts.Should().Be(0);
            SimulationClock.RunUntil(simulation, () => !ally.IsDead, 30, "revive despite the damage");
            ally.Stats.RevivesReceived.Should().Be(1);
        }

        [Test]
        public void TunedPvp_AReviveFailsWhenItLands_IfAnEnemyJoinedDuringTheCast()
        {
            var medic = TestCombatants.Medic("Medic", TestCombatants.Use("Resuscitation2", AbilityTarget.DeadAlly));
            var ally = new CombatantSpec { Name = "Ally", MaxHp = 1000 };
            var enemy = TestCombatants.Dummy("Enemy");
            enemy.JoinAtSeconds = 2f;
            var simulation = TestScenarios.Spawn(TestScenarios.TwoTeams(true, new[] { medic, ally }, new[] { enemy }), RuleSets.Tuned(), _catalog);
            simulation.ApplyDamage(simulation.Get("Enemy"), simulation.Get("Ally"), TestCombatants.EffectivelyUnlimited);
            var caster = simulation.Get("Medic");

            SimulationClock.RunUntil(simulation, () => caster.Casting != null, 2, "the revive cast to start before the enemy joins");
            SimulationClock.RunUntil(simulation, simulation.Now + ResuscitationCastSeconds + 1);

            simulation.Get("Ally").IsDead.Should().BeTrue("the PvP gate is checked again when the cast lands");
            caster.Stats.StmSpent.Should().Be(0, "a revive that fails when it lands spends nothing");
            caster.Stats.BlockedRevives.Should().BeGreaterThan(0);
        }

        [Test]
        public void ADeadPlayer_KeepsRegainingFpAndStamina()
        {
            var fallen = new CombatantSpec
            {
                Name = "Fallen", MaxHp = 1000, MaxFp = 100, FpRegenPer30Seconds = 10, MaxStm = 100, StmRegenPer30Seconds = 10
            };
            var simulation = TestScenarios.Spawn(
                TestScenarios.TwoTeams(false, new[] { fallen, new CombatantSpec { Name = "Ally", MaxHp = 1000 } }, new[] { TestCombatants.Dummy("Enemy", true) }),
                RuleSets.Current(),
                _catalog);
            var player = simulation.Get("Fallen");
            player.Fp = 50;
            player.Stm = 50;
            simulation.ApplyDamage(simulation.Get("Enemy"), player, TestCombatants.EffectivelyUnlimited);

            SimulationClock.RunUntil(simulation, 40);

            player.IsDead.Should().BeTrue();
            player.Fp.Should().BeGreaterThan(50, "the game's regeneration heartbeat has no dead-player check");
            player.Stm.Should().BeGreaterThan(50);
        }

        [Test]
        public void TunedPvp_AnNpcEnemyDoesNotBlockRevives_AndAnNpcKillHasNoDamageWindow()
        {
            var medic = TestCombatants.Medic("Medic", TestCombatants.Use("Resuscitation2", AbilityTarget.DeadAlly));
            var ally = new CombatantSpec { Name = "Ally", MaxHp = 1000 };
            var simulation = TestScenarios.Spawn(
                TestScenarios.TwoTeams(true, new[] { medic, ally }, new[] { TestCombatants.Dummy("Creature", isNpc: true) }),
                RuleSets.Tuned(),
                _catalog);
            var creature = simulation.Get("Creature");
            var revived = simulation.Get("Ally");
            simulation.ApplyDamage(creature, revived, TestCombatants.EffectivelyUnlimited);
            simulation.ApplyDamage(creature, simulation.Get("Medic"), 1);

            SimulationClock.RunUntil(simulation, () => !revived.IsDead, 10, "the ally revived while the NPC still stands");

            creature.IsDead.Should().BeFalse();
            revived.ReviveLockedUntil.Should().BeGreaterThan(simulation.Now, "a player revived mid-fight starts the 5-minute lockout");
        }

        private (Simulation Simulation, Combatant Medic, Combatant Ally, Combatant Enemy) SpawnAndKillAlly(RuleSet rules, bool isPvp)
        {
            var medic = TestCombatants.Medic("Medic", TestCombatants.Use("Resuscitation2", AbilityTarget.DeadAlly));
            var ally = new CombatantSpec { Name = "Ally", MaxHp = 1000 };
            var enemy = TestCombatants.Dummy("Enemy", isNpc: !isPvp);
            var simulation = TestScenarios.Spawn(TestScenarios.TwoTeams(isPvp, new[] { medic, ally }, new[] { enemy }), rules, _catalog);

            simulation.ApplyDamage(simulation.Get("Enemy"), simulation.Get("Ally"), TestCombatants.EffectivelyUnlimited);
            simulation.Get("Ally").IsDead.Should().BeTrue("the fixture starts with a dead ally");
            return (simulation, simulation.Get("Medic"), simulation.Get("Ally"), simulation.Get("Enemy"));
        }
    }
}
