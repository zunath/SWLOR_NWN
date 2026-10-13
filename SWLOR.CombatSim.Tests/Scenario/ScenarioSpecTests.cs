using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.Scenario;

namespace SWLOR.CombatSim.Tests.Scenario
{
    [TestFixture]
    public class ScenarioSpecTests
    {
        private string _directory;

        [SetUp]
        public void CreateTemporaryDirectory()
        {
            _directory = Path.Combine(Path.GetTempPath(), "combatsim-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
        }

        [TearDown]
        public void DeleteTemporaryDirectory()
        {
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, true);
        }

        [Test]
        public void Defaults_AreSixHundredSecondsInTenthSecondStepsAndPvp()
        {
            var scenario = new ScenarioSpec();

            scenario.DurationSeconds.Should().Be(600f);
            scenario.TimeStepSeconds.Should().Be(0.1f);
            scenario.IsPvp.Should().BeTrue();
        }

        [Test]
        public void Load_ReadsTeamsMembersAndAbilitiesFromJson()
        {
            var path = WriteScenario("""
                {
                  "name": "Sample",
                  "durationSeconds": 120,
                  "isPvp": false,
                  "teams": [
                    { "name": "A", "targetPolicy": "Focus", "focusTarget": "Boss",
                      "members": [ { "name": "Medic", "maxHp": 900, "willpower": 20, "perks": [ "DefensiveHarmony" ],
                                     "abilities": [ { "feat": "MedKit4", "target": "Self", "belowHpPercent": 75 } ] } ] },
                    { "name": "B", "members": [ { "name": "Boss", "count": 2, "isNpc": true, "maxHp": 5000 } ] }
                  ]
                }
                """);

            var scenario = ScenarioSpec.Load(path);

            scenario.Name.Should().Be("Sample");
            scenario.DurationSeconds.Should().Be(120f);
            scenario.IsPvp.Should().BeFalse();
            scenario.Teams.Should().HaveCount(2);
            scenario.Teams[0].TargetPolicy.Should().Be(TargetPolicy.Focus);
            scenario.Teams[0].FocusTarget.Should().Be("Boss");
            var medic = scenario.Teams[0].Members.Should().ContainSingle().Subject;
            medic.MaxHp.Should().Be(900);
            medic.Willpower.Should().Be(20);
            medic.Perks.Should().Equal("DefensiveHarmony");
            medic.Abilities.Should().ContainSingle().Which.Should().BeEquivalentTo(
                new AbilityUseSpec { Feat = "MedKit4", Target = AbilityTarget.Self, BelowHpPercent = 75f });
            scenario.Teams[1].Members[0].Count.Should().Be(2);
            scenario.Teams[1].Members[0].IsNpc.Should().BeTrue();
        }

        [Test]
        public void Load_ReadsTargetingCadenceAndEngagementSettings()
        {
            var path = WriteScenario("""
                {
                  "name": "Staged",
                  "teams": [
                    { "name": "A", "targetPolicy": "Coordinated", "retargetSeconds": 12.5,
                      "members": [ { "name": "Healer", "joinAtSeconds": 30,
                                     "abilities": [ { "feat": "ForceSanctuary1", "target": "Self", "maxTargets": 3, "minIntervalSeconds": 45 },
                                                    { "feat": "MedKit4", "target": "Self" } ] } ] },
                    { "name": "B", "members": [ { "name": "Boss", "isNpc": true } ] }
                  ]
                }
                """);

            var scenario = ScenarioSpec.Load(path);

            scenario.Teams[0].TargetPolicy.Should().Be(TargetPolicy.Coordinated);
            scenario.Teams[0].RetargetSeconds.Should().Be(12.5f);
            var healer = scenario.Teams[0].Members.Should().ContainSingle().Subject;
            healer.JoinAtSeconds.Should().Be(30f);
            healer.Abilities[0].MaxTargets.Should().Be(3);
            healer.Abilities[0].MinIntervalSeconds.Should().Be(45f);
            healer.Abilities[1].MaxTargets.Should().BeNull("the whole team is reached by default");
            healer.Abilities[1].MinIntervalSeconds.Should().Be(0f);
            scenario.Teams[1].RetargetSeconds.Should().Be(0f, "targets are kept until they die by default");
            scenario.Teams[1].Members[0].JoinAtSeconds.Should().Be(0f);
        }

        [Test]
        public void Load_WithFewerThanTwoTeams_Throws()
        {
            var path = WriteScenario("""{ "name": "Lonely", "teams": [ { "name": "A", "members": [] } ] }""");

            var act = () => ScenarioSpec.Load(path);

            act.Should().Throw<InvalidDataException>().WithMessage("*at least two teams*");
        }

        [Test]
        public void Load_WithNoTeams_Throws()
        {
            var path = WriteScenario("""{ "name": "Empty" }""");

            var act = () => ScenarioSpec.Load(path);

            act.Should().Throw<InvalidDataException>();
        }

        private string WriteScenario(string json)
        {
            var path = Path.Combine(_directory, "scenario.json");
            File.WriteAllText(path, json);
            return path;
        }
    }
}
