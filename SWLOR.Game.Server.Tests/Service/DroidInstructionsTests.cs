using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Tests.Service;

public class DroidInstructionsTests
{
    private DroidPerkCacheScope _perks;

    [SetUp]
    public void LoadDefinitions() => _perks = new DroidPerkCacheScope();

    [TearDown]
    public void RestoreDefinitions() => _perks?.Dispose();

    [Test]
    public void MigrationPreservesLearnedRanksButEnforcesOneActiveRankAndSlotBudget()
    {
        var droid = new ConstructedDroid
        {
            LearnedPerks = null,
            ActivePerks = new List<DroidPerk>
            {
                null, new(PerkType.MedKit, 1), new(PerkType.MedKit, 2), new(PerkType.MedKit, 2),
                new(PerkType.Provoke, 2), new(PerkType.DualWield, 1), new((PerkType)999999, 1)
            }
        };

        DroidInstructions.Normalize(droid, 3, 3).Should().BeTrue();
        droid.ActivePerks.Should().ContainSingle().Which.Should().BeEquivalentTo(new DroidPerk(PerkType.MedKit, 2));
        droid.LearnedPerks.Should().BeEquivalentTo(new[]
        {
            new DroidPerk(PerkType.MedKit, 1), new DroidPerk(PerkType.MedKit, 2), new DroidPerk(PerkType.Provoke, 2)
        });
        DroidInstructions.Normalize(droid, 3, 3).Should().BeFalse("a retry must preserve the same learned and active configuration");
    }

    [Test]
    public void MigrationClampsRetainedInstructionsAndDeactivatesRanksAboveControllerTier()
    {
        var droid = new ConstructedDroid
        {
            ActivePerks = new List<DroidPerk> { new(PerkType.MedKit, 99), new(PerkType.Provoke, 1) }
        };
        DroidInstructions.Normalize(droid, 1, 20).Should().BeTrue();
        droid.LearnedPerks.Should().ContainEquivalentOf(new DroidPerk(PerkType.MedKit, 4));
        droid.ActivePerks.Should().ContainSingle().Which.Perk.Should().Be(PerkType.Provoke);
        DroidInstructions.Normalize(droid, 1, 20).Should().BeFalse();
    }

    [Test]
    public void RuntimeRejectsUnsupportedRanksAndHonorsExactCapacity()
    {
        var instructions = new[]
        {
            new DroidPerk(PerkType.MedKit, 99), new DroidPerk(PerkType.DualWield, 1),
            new DroidPerk(PerkType.Provoke, 2), new DroidPerk(PerkType.MedKit, 1)
        };
        var active = DroidInstructions.SelectActive(instructions, 2, 3);
        active.Should().HaveCount(2);
        DroidInstructions.GetSlots(active).Should().Be(3);
        DroidInstructions.SelectActive(instructions, 5, 0).Should().BeEmpty();
        DroidInstructions.TryGetLevel(new DroidPerk(PerkType.MedKit, 99), out _).Should().BeFalse();
        DroidInstructions.TryGetLevel(new DroidPerk(PerkType.DualWield, 1), out _).Should().BeFalse();
    }
}
