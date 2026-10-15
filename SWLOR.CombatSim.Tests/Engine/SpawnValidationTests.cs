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
    public class SpawnValidationTests
    {
        private AbilityCatalog _catalog;

        [OneTimeSetUp]
        public void LoadCatalog()
        {
            _catalog = AbilityCatalog.Load();
        }

        [TestCase(0f)]
        [TestCase(-0.1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.Epsilon)]
        [TestCase(0.0001f)]
        [TestCase(0.11f)]
        [TestCase(10f)]
        public void Run_RejectsATimeStepOutsideTheSimulationsResolution(float step)
        {
            var scenario = Scenario(TestCombatants.Dummy("Player"));
            scenario.TimeStepSeconds = step;

            var act = () => new Simulation(scenario, RuleSets.PreTuning(), _catalog, 1).Run();

            act.Should().Throw<InvalidDataException>().WithMessage("*TimeStepSeconds*");
        }

        [TestCase(0f)]
        [TestCase(-5f)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(1e30f)]
        public void Run_RejectsANonPositiveDuration(float duration)
        {
            var scenario = Scenario(TestCombatants.Dummy("Player"));
            scenario.DurationSeconds = duration;

            var act = () => new Simulation(scenario, RuleSets.PreTuning(), _catalog, 1).Run();

            act.Should().Throw<InvalidDataException>().WithMessage("*DurationSeconds*");
        }

        [Test]
        public void Spawn_RejectsAnAbilityWhoseRecastCouldNotBeRead()
        {
            var catalog = AbilityCatalog.Load();
            var anger = catalog.Get(FeatType.Anger1);
            catalog.Add(new AbilityInfo { Feat = anger.Feat, Name = anger.Name, RecastGroup = anger.RecastGroup, STM = anger.STM, UnreadableTiming = "recast" });
            var use = TestCombatants.Use("Anger1", AbilityTarget.Enemy);
            use.Damage = new DamageSpec { Min = 1f, Max = 1f };

            var act = () => new Simulation(Scenario(TestCombatants.Medic("Beast", use)), RuleSets.PreTuning(), catalog, 1).Spawn();

            act.Should().Throw<InvalidDataException>().WithMessage("*Anger1*recast*game engine*");
        }

        [Test]
        public void Spawn_RejectsTwoCombatantsWithTheSameNameOnOneTeam()
        {
            var scenario = TestScenarios.TwoTeams(false, new[] { TestCombatants.Dummy("Fighter"), TestCombatants.Dummy("Fighter") }, new[] { TestCombatants.Dummy("Enemy", true) });

            var act = () => new Simulation(scenario, RuleSets.PreTuning(), _catalog, 1).Spawn();

            act.Should().Throw<InvalidDataException>().WithMessage("*First*Fighter*");
        }

        [Test]
        public void Spawn_RejectsAHitChanceOverrideThatNamesCombatantsOnTwoOpposingTeams()
        {
            var striker = TestCombatants.Striker("Striker");
            striker.AutoAttack.Damage.HitChanceVs = new Dictionary<string, float> { ["Tank"] = 0.5f };
            var scenario = ThreeTeams(striker, TestCombatants.Dummy("Tank", true), TestCombatants.Dummy("Tank", true));

            var act = () => new Simulation(scenario, RuleSets.PreTuning(), _catalog, 1).Spawn();

            act.Should().Throw<InvalidDataException>().WithMessage("*Striker*HitChanceVs*Tank*more than one opposing team*");
        }

        [Test]
        public void Spawn_RejectsAFocusTargetThatNamesCombatantsOnTwoOpposingTeams()
        {
            var scenario = ThreeTeams(TestCombatants.Striker("Striker"), TestCombatants.Dummy("Tank", true), TestCombatants.Dummy("Tank", true));
            scenario.Teams[0].TargetPolicy = TargetPolicy.Focus;
            scenario.Teams[0].FocusTarget = "Tank";

            var act = () => new Simulation(scenario, RuleSets.PreTuning(), _catalog, 1).Spawn();

            act.Should().Throw<InvalidDataException>().WithMessage("*FocusTarget*Tank*more than one opposing team*");
        }

        private static ScenarioSpec ThreeTeams(CombatantSpec first, CombatantSpec second, CombatantSpec third) =>
            new()
            {
                Name = "three teams",
                Teams = new List<TeamSpec>
                {
                    new() { Name = "One", Members = new List<CombatantSpec> { first } },
                    new() { Name = "Two", Members = new List<CombatantSpec> { second } },
                    new() { Name = "Three", Members = new List<CombatantSpec> { third } }
                }
            };

        [Test]
        public void Spawn_AllowsTheSameNameOnOpposingTeams()
        {
            var scenario = TestScenarios.TwoTeams(false, new[] { TestCombatants.Dummy("Fighter") }, new[] { TestCombatants.Dummy("Fighter") });

            var act = () => new Simulation(scenario, RuleSets.PreTuning(), _catalog, 1).Spawn();

            act.Should().NotThrow();
        }

        [Test]
        public void Spawn_RejectsAnUnknownPerk()
        {
            var player = TestCombatants.Dummy("Player");
            player.Perks = new List<string> { "NotARealPerk" };

            var act = () => new Simulation(Scenario(player), RuleSets.PreTuning(), _catalog, 1).Spawn();

            act.Should().Throw<InvalidDataException>().WithMessage("*NotARealPerk*");
        }

        [TestCase("hitChance", 2f)]
        [TestCase("critChance", -0.1f)]
        [TestCase("critMultiplier", 0.5f)]
        [TestCase("min", float.NaN)]
        [TestCase("max", -1f)]
        [TestCase("hits", 0f)]
        public void Spawn_RejectsAnAbilityDamageProfileOutOfRange(string field, float value)
        {
            var strike = TestCombatants.Use("ShatteringStrike1", AbilityTarget.Enemy);
            strike.Damage = new DamageSpec { Min = 1, Max = 2 };
            switch (field)
            {
                case "hitChance": strike.Damage.HitChance = value; break;
                case "critChance": strike.Damage.CritChance = value; break;
                case "critMultiplier": strike.Damage.CritMultiplier = value; break;
                case "min": strike.Damage.Min = value; break;
                case "max": strike.Damage.Max = value; break;
                case "hits": strike.Damage.Hits = (int)value; break;
            }

            var act = () => new Simulation(Scenario(TestCombatants.Medic("Medic", strike)), RuleSets.PreTuning(), _catalog, 1).Spawn();

            act.Should().Throw<InvalidDataException>().WithMessage("*ShatteringStrike1*damage profile*");
        }

        [Test]
        public void Spawn_RejectsAnUnknownTargetPolicy()
        {
            var scenario = Scenario(TestCombatants.Dummy("Player"));
            scenario.Teams[0].TargetPolicy = (TargetPolicy)99;

            var act = () => new Simulation(scenario, RuleSets.PreTuning(), _catalog, 1).Spawn();

            act.Should().Throw<InvalidDataException>().WithMessage("*TargetPolicy*");
        }

        [TestCase(null)]
        [TestCase("Tank")]
        [TestCase("Enemy")]
        public void Spawn_RejectsANamedAllyThatIsNotOnTheCastersTeam(string targetName)
        {
            var scenario = NamedAllyScenario(targetName, joinAtSeconds: 0f);

            var act = () => new Simulation(scenario, RuleSets.PreTuning(), _catalog, 1).Spawn();

            act.Should().Throw<InvalidDataException>().WithMessage("*NamedAlly*");
        }

        [Test]
        public void Spawn_AcceptsANamedAllyThatJoinsLater()
        {
            var scenario = NamedAllyScenario("Tank 2", joinAtSeconds: 30f);

            var act = () => new Simulation(scenario, RuleSets.PreTuning(), _catalog, 1).Spawn();

            act.Should().NotThrow();
        }

        private static ScenarioSpec NamedAllyScenario(string targetName, float joinAtSeconds) =>
            TestScenarios.TwoTeams(
                false,
                new[]
                {
                    TestCombatants.Medic("Medic", TestCombatants.Use("Benevolence1", AbilityTarget.NamedAlly, targetName: targetName)),
                    new CombatantSpec { Name = "Tank", MaxHp = 1000, Count = 2, JoinAtSeconds = joinAtSeconds }
                },
                new[] { TestCombatants.Dummy("Enemy", true) });

        [TestCase("Bos")]
        [TestCase("Striker")]
        public void Spawn_RejectsAHitChanceForANameThatIsNotAnOpponent(string name)
        {
            var striker = TestCombatants.Striker("Striker");
            striker.AutoAttack.Damage.HitChanceVs = new Dictionary<string, float> { [name] = 0.5f };
            var scenario = TestScenarios.TwoTeams(false, new[] { striker }, new[] { TestCombatants.Dummy("Boss", true) });

            var act = () => new Simulation(scenario, RuleSets.PreTuning(), _catalog, 1).Spawn();

            act.Should().Throw<InvalidDataException>().WithMessage("*HitChanceVs*");
        }

        [Test]
        public void Spawn_AcceptsAHitChanceForANumberedOpponent()
        {
            var striker = TestCombatants.Striker("Striker");
            striker.AutoAttack.Damage.HitChanceVs = new Dictionary<string, float> { ["Boss 2"] = 0.5f };
            var bosses = TestCombatants.Dummy("Boss", true);
            bosses.Count = 2;
            var scenario = TestScenarios.TwoTeams(false, new[] { striker }, new[] { bosses });

            var act = () => new Simulation(scenario, RuleSets.PreTuning(), _catalog, 1).Spawn();

            act.Should().NotThrow();
        }

        [TestCase("members")]
        [TestCase("member")]
        [TestCase("abilities")]
        [TestCase("ability")]
        [TestCase("perks")]
        public void Spawn_RejectsAMissingList_InsteadOfFailingOnANullReference(string gap)
        {
            var scenario = Scenario(TestCombatants.Dummy("Player"));
            var member = scenario.Teams[0].Members[0];
            switch (gap)
            {
                case "members": scenario.Teams[0].Members = null; break;
                case "member": scenario.Teams[0].Members.Add(null); break;
                case "abilities": member.Abilities = null; break;
                case "ability": member.Abilities.Add(null); break;
                case "perks": member.Perks = null; break;
            }

            var act = () => new Simulation(scenario, RuleSets.PreTuning(), _catalog, 1).Spawn();

            act.Should().Throw<InvalidDataException>();
        }

        [TestCase(0)]
        [TestCase(-2)]
        public void Spawn_RejectsACountBelowOne(int count)
        {
            var dummy = TestCombatants.Dummy("Player");
            dummy.Count = count;

            var act = () => new Simulation(Scenario(dummy), RuleSets.PreTuning(), _catalog, 1).Spawn();

            act.Should().Throw<InvalidDataException>().WithMessage("*Player*Count*");
        }

        [Test]
        public void Spawn_RejectsAnUnknownAbilityTarget()
        {
            var medic = TestCombatants.Medic("Medic", TestCombatants.Use("Benevolence1", (AbilityTarget)99));

            var act = () => new Simulation(Scenario(medic), RuleSets.PreTuning(), _catalog, 1).Spawn();

            act.Should().Throw<InvalidDataException>().WithMessage("*Benevolence1*Target*");
        }

        [Test]
        public void Spawn_RejectsAnAutoattackProfileOutOfRange()
        {
            var striker = TestCombatants.Striker("Striker");
            striker.AutoAttack.Damage.HitChance = 1.5f;

            var act = () => new Simulation(Scenario(striker), RuleSets.PreTuning(), _catalog, 1).Spawn();

            act.Should().Throw<InvalidDataException>().WithMessage("*Striker*autoattack*HitChance*");
        }

        [Test]
        public void Spawn_RejectsATimeStepLongerThanAnAbilityUsersReactionTime()
        {
            var medic = TestCombatants.Medic("Medic", TestCombatants.Use("Benevolence1", AbilityTarget.Self));
            medic.ReactionSeconds = 0.05f;
            var scenario = Scenario(medic);
            scenario.TimeStepSeconds = 0.1f;

            var act = () => new Simulation(scenario, RuleSets.PreTuning(), _catalog, 1).Spawn();

            act.Should().Throw<InvalidDataException>().WithMessage("*Medic*ReactionSeconds*time step*");
        }

        [TestCase(AbilityTarget.Self)]
        [TestCase(AbilityTarget.LowestAlly)]
        [TestCase(AbilityTarget.NamedAlly)]
        public void Spawn_RejectsANonEnemyTargetForAnAimedHostileCast(AbilityTarget target)
        {
            var use = TestCombatants.Use("ForceSpark1", target);
            use.TargetName = "Medic";
            use.Damage = new DamageSpec { Min = 10f, Max = 10f };
            var medic = TestCombatants.Medic("Medic", use);

            var act = () => new Simulation(Scenario(medic), RuleSets.PreTuning(), _catalog, 1).Spawn();

            act.Should().Throw<InvalidDataException>().WithMessage("*ForceSpark1*must be Enemy*");
        }

        [Test]
        public void Spawn_AcceptsASelfTargetForASelfCenteredHostileArea()
        {
            var use = TestCombatants.Use("SpinningWhirl1", AbilityTarget.Self);
            use.Damage = new DamageSpec { Min = 10f, Max = 10f };
            var medic = TestCombatants.Medic("Medic", use);

            var act = () => new Simulation(Scenario(medic), RuleSets.PreTuning(), _catalog, 1).Spawn();

            act.Should().NotThrow();
        }

        [TestCase("ForceSanctuary1")]
        [TestCase("GroupDeflector1")]
        [TestCase("KoltoMist1")]
        public void Spawn_RejectsAnEnemyTargetForAFriendlyZoneOrPartyEffect(string feat)
        {
            var medic = TestCombatants.Medic("Medic", TestCombatants.Use(feat, AbilityTarget.Enemy));

            var act = () => new Simulation(Scenario(medic), RuleSets.PreTuning(), _catalog, 1).Spawn();

            act.Should().Throw<InvalidDataException>().WithMessage($"*{feat}*friendly zone*");
        }

        [TestCase(256)]
        [TestCase(-1)]
        public void Spawn_RejectsAnAbilityScoreOutsideItsRange(int might)
        {
            var player = TestCombatants.Dummy("Player");
            player.Might = might;

            var act = () => new Simulation(Scenario(player), RuleSets.PreTuning(), _catalog, 1).Spawn();

            act.Should().Throw<InvalidDataException>().WithMessage("*Might*");
        }

        [TestCase("ForceSanctuary1")]
        [TestCase("GroupDeflector1")]
        [TestCase("AdrenalStim1")]
        [TestCase("RendingBiteTechnique")]
        public void Spawn_RejectsADamageProfileOnANonHostileAbility(string feat)
        {
            var use = TestCombatants.Use(feat, AbilityTarget.Self);
            use.Damage = new DamageSpec { Min = 10f, Max = 10f };
            var medic = TestCombatants.Medic("Medic", use);

            var act = () => new Simulation(Scenario(medic), RuleSets.PreTuning(), _catalog, 1).Spawn();

            act.Should().Throw<InvalidDataException>().WithMessage($"*{feat}*not a hostile ability*");
        }

        [TestCase(null)]
        [TestCase("  ")]
        public void Spawn_RejectsAMemberWithoutAName(string name)
        {
            var player = TestCombatants.Dummy("Player");
            player.Name = name;

            var act = () => new Simulation(Scenario(player), RuleSets.PreTuning(), _catalog, 1).Spawn();

            act.Should().Throw<InvalidDataException>().WithMessage("*Name*");
        }

        [Test]
        public void Spawn_RejectsAPerkListedTwice()
        {
            var player = TestCombatants.Dummy("Player");
            player.Perks = new List<string> { "DefensiveHarmony", "DefensiveHarmony" };

            var act = () => new Simulation(Scenario(player), RuleSets.PreTuning(), _catalog, 1).Spawn();

            act.Should().Throw<InvalidDataException>().WithMessage("*DefensiveHarmony*more than once*");
        }

        [Test]
        public void Spawn_RejectsACountTooLargeToSpawn()
        {
            var crowd = TestCombatants.Dummy("Crowd");
            crowd.Count = int.MaxValue;

            var act = () => new Simulation(Scenario(crowd), RuleSets.PreTuning(), _catalog, 1).Spawn();

            act.Should().Throw<InvalidDataException>().WithMessage("*Crowd*Count*");
        }

        [TestCase(21f)]
        [TestCase(1e20f)]
        public void Spawn_RejectsAnAttackRateTooFastToAdvanceTheClock(float attacksPerSecond)
        {
            var striker = TestCombatants.Striker("Striker", attacksPerSecond);

            var act = () => new Simulation(Scenario(striker), RuleSets.PreTuning(), _catalog, 1).Spawn();

            act.Should().Throw<InvalidDataException>().WithMessage("*Striker*AttacksPerSecond*");
        }

        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NaN)]
        public void Spawn_RejectsAReactionTimeThatIsNotFinite(float seconds)
        {
            var medic = TestCombatants.Medic("Medic", TestCombatants.Use("Benevolence1", AbilityTarget.Self));
            medic.ReactionSeconds = seconds;

            var act = () => new Simulation(Scenario(medic), RuleSets.PreTuning(), _catalog, 1).Spawn();

            act.Should().Throw<InvalidDataException>().WithMessage("*Medic*ReactionSeconds*");
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(-10f)]
        public void Spawn_RejectsABelowHpPercentThatIsNotAFiniteNonNegativeNumber(float threshold)
        {
            var medic = TestCombatants.Medic("Medic", TestCombatants.Use("Benevolence1", AbilityTarget.LowestAlly, belowHpPercent: threshold));

            var act = () => new Simulation(Scenario(medic), RuleSets.PreTuning(), _catalog, 1).Spawn();

            act.Should().Throw<InvalidDataException>().WithMessage("*Benevolence1*BelowHpPercent*");
        }

        [Test]
        public void Spawn_RejectsATeamWithNoMembers()
        {
            var scenario = Scenario(TestCombatants.Dummy("Player"));
            scenario.Teams[1].Members.Clear();

            var act = () => new Simulation(scenario, RuleSets.PreTuning(), _catalog, 1).Spawn();

            act.Should().Throw<InvalidDataException>().WithMessage("*member*");
        }

        [TestCase(0)]
        [TestCase(-5)]
        [TestCase(int.MaxValue)]
        public void Spawn_RejectsAMaxHpThatIsNotPositive(int maxHp)
        {
            var player = TestCombatants.Dummy("Player");
            player.MaxHp = maxHp;

            var act = () => new Simulation(Scenario(player), RuleSets.PreTuning(), _catalog, 1).Spawn();

            act.Should().Throw<InvalidDataException>().WithMessage("*Player*MaxHp*");
        }

        [TestCase("hpRegenHuge")]
        [TestCase("stmRegenHuge")]
        [TestCase("hpRegenFraction")]
        [TestCase("fpRegenFraction")]
        [TestCase("readinessAboveCap")]
        [TestCase("hpRegen")]
        [TestCase("fpRegen")]
        [TestCase("stmRegen")]
        [TestCase("maxFp")]
        [TestCase("readiness")]
        [TestCase("healingReceived")]
        [TestCase("outgoingBelowMinusHundred")]
        [TestCase("outgoingNegative")]
        [TestCase("receivedBelowMinusHundred")]
        [TestCase("damageReduction")]
        [TestCase("affinityHigh")]
        [TestCase("affinityLow")]
        [TestCase("outgoingHuge")]
        [TestCase("readinessHuge")]
        public void Spawn_RejectsACombatantValueOutsideItsRange(string field)
        {
            var player = TestCombatants.Dummy("Player");
            switch (field)
            {
                case "hpRegenHuge": player.HpRegenPer30Seconds = 1e20f; break;
                case "stmRegenHuge": player.StmRegenPer30Seconds = 2e6f; break;
                case "hpRegenFraction": player.HpRegenPer30Seconds = 0.5f; break;
                case "fpRegenFraction": player.FpRegenPer30Seconds = 1.5f; break;
                case "readinessAboveCap": player.CombatReadinessPercent = 16f; break;
                case "hpRegen": player.HpRegenPer30Seconds = -1f; break;
                case "fpRegen": player.FpRegenPer30Seconds = float.NaN; break;
                case "stmRegen": player.StmRegenPer30Seconds = float.PositiveInfinity; break;
                case "maxFp": player.MaxFp = -10; break;
                case "readiness": player.CombatReadinessPercent = float.NaN; break;
                case "healingReceived": player.HealingReceivedPercent = float.NegativeInfinity; break;
                case "outgoingBelowMinusHundred": player.OutgoingHealingPercent = -150f; break;
                case "outgoingNegative": player.OutgoingHealingPercent = -50f; break;
                case "receivedBelowMinusHundred": player.HealingReceivedPercent = -101f; break;
                case "damageReduction": player.DamageReductionPercent = 150f; break;
                case "affinityHigh": player.ForceAffinity = 11; break;
                case "affinityLow": player.ForceAffinity = -11; break;
                case "outgoingHuge": player.OutgoingHealingPercent = 1e20f; break;
                case "readinessHuge": player.CombatReadinessPercent = 5000f; break;
            }

            var act = () => new Simulation(Scenario(player), RuleSets.PreTuning(), _catalog, 1).Spawn();

            act.Should().Throw<InvalidDataException>().WithMessage("*Player*");
        }

        [TestCase("Benevolence1", AbilityTarget.Enemy)]
        [TestCase("GuardianWard1", AbilityTarget.Enemy)]
        [TestCase("Renewal1", AbilityTarget.Enemy)]
        [TestCase("MedKit1", AbilityTarget.DeadAlly)]
        [TestCase("Resuscitation1", AbilityTarget.Self)]
        [TestCase("Resuscitation1", AbilityTarget.Enemy)]
        public void Spawn_RejectsATargetTheEffectCannotHave(string feat, AbilityTarget target)
        {
            var medic = TestCombatants.Medic("Medic", TestCombatants.Use(feat, target));

            var act = () => new Simulation(Scenario(medic), RuleSets.PreTuning(), _catalog, 1).Spawn();

            act.Should().Throw<InvalidDataException>().WithMessage($"*{feat}*Target*");
        }

        [TestCase("KoltoMist1", AbilityTarget.Self)]
        [TestCase("Benevolence1", AbilityTarget.LowestAlly)]
        [TestCase("Resuscitation1", AbilityTarget.DeadAlly)]
        public void Spawn_AcceptsATargetTheEffectCanHave(string feat, AbilityTarget target)
        {
            var medic = TestCombatants.Medic("Medic", TestCombatants.Use(feat, target));

            var act = () => new Simulation(Scenario(medic), RuleSets.PreTuning(), _catalog, 1).Spawn();

            act.Should().NotThrow();
        }

        [Test]
        public void Spawn_AllowsATimeStepLongerThanTheReactionTime_ForCombatantsWithoutAbilities()
        {
            var striker = TestCombatants.Striker("Striker");
            striker.ReactionSeconds = 0.05f;
            var scenario = Scenario(striker);
            scenario.TimeStepSeconds = 0.1f;

            var act = () => new Simulation(scenario, RuleSets.PreTuning(), _catalog, 1).Spawn();

            act.Should().NotThrow("only ability decisions depend on the reaction time");
        }

        private static ScenarioSpec Scenario(CombatantSpec player) =>
            TestScenarios.TwoTeams(false, new[] { player }, new[] { TestCombatants.Dummy("Enemy", true) });
    }
}
