using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Tests.Service;

public class AuraAnnouncementTests
{
    [Test]
    public void AuraDetail_StartsWithNobodyAnnouncedTo()
    {
        new PlayerAuraDetail(typeof(RallyingStandard1StatusEffect), true, true, false).AnnouncedTo.Should().BeEmpty();
    }

    [Test]
    public void EachAuraKeepsItsOwnAnnouncedList_SoSwitchingOneOffLeavesTheOthers()
    {
        var rallying = new PlayerAuraDetail(typeof(RallyingStandard1StatusEffect), true, true, false);
        var watchful = new PlayerAuraDetail(typeof(WatchfulPresence1StatusEffect), true, true, false);

        rallying.AnnouncedTo.Add(0x01000002);

        watchful.AnnouncedTo.Should().BeEmpty();
    }

    [Test]
    public void AuraStatusEffect_OnlySendsItsApplicationMessageWhenAnnounced()
    {
        new RallyingStandard1StatusEffect().SendsApplicationMessage.Should().BeTrue();
        new RallyingStandard1StatusEffect { AnnouncesApplication = false }.SendsApplicationMessage.Should().BeFalse();
    }

    [Test]
    public void EveryLeadershipAuraEffect_CanHaveItsRepeatedAnnouncementWithheld()
    {
        var prefixes = new[]
        {
            "RallyingStandard", "ChargeOrder", "CoordinatedFocus",
            "WatchfulPresence", "SteadyFormation", "FieldRecovery"
        };

        var leadershipAuras = typeof(RallyingStandard1StatusEffect).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && typeof(IStatusEffect).IsAssignableFrom(type) &&
                           type.Namespace == typeof(RallyingStandard1StatusEffect).Namespace &&
                           prefixes.Any(prefix => type.Name.StartsWith(prefix, StringComparison.Ordinal)) &&
                           type.Name.EndsWith("StatusEffect", StringComparison.Ordinal))
            .ToList();

        leadershipAuras.Should().HaveCountGreaterThanOrEqualTo(prefixes.Length);
        leadershipAuras.Should().OnlyContain(type => typeof(AuraStatusEffectBase).IsAssignableFrom(type),
            "Ability.ApplyAuraEffect can only withhold the notice on aura effects that expose AnnouncesApplication");
    }

    [Test]
    public void AbilityAuraApplication_AnnouncesOnlyToAlliesNotYetToldAboutTheAura()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        directory.Should().NotBeNull();

        var source = File.ReadAllText(Path.Combine(directory!.FullName, "SWLOR.Game.Server", "Service", "Ability.cs"));
        source.Should().Contain("aura.AnnouncesApplication = !detail.AnnouncedTo.Contains(recipient);");
        source.Should().Contain("detail?.AnnouncedTo.Add(recipient);");
        source.Should().Contain("aura.AnnouncedTo.Remove(target);");
    }
}
