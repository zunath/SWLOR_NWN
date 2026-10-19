using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Tests.Service;

public class AuraAnnouncementLimiterTests
{
    private const uint Leader = 0x01000001;
    private const uint Ally = 0x01000002;
    private const uint OtherAlly = 0x01000003;
    private const uint OtherLeader = 0x01000004;
    private const long Second = TimeSpan.TicksPerSecond;
    private const long Start = 1_000_000 * Second;

    [SetUp]
    public void SetUp() => AuraAnnouncementLimiter.Clear();

    [TearDown]
    public void TearDown() => AuraAnnouncementLimiter.Clear();

    [Test]
    public void AllyRepeatedlyCrossingTheAuraEdge_IsAnnouncedOncePerInterval()
    {
        var type = typeof(RallyingStandard1StatusEffect);
        AuraAnnouncementLimiter.TryAnnounce(Leader, Ally, type, Start).Should().BeTrue();

        for (var i = 1; i < AuraAnnouncementLimiter.IntervalSeconds; i++)
        {
            AuraAnnouncementLimiter.TryAnnounce(Leader, Ally, type, Start + i * Second)
                .Should().BeFalse($"re-entry {i} seconds later is the same aura the ally was just told about");
        }

        AuraAnnouncementLimiter.TryAnnounce(Leader, Ally, type, Start + AuraAnnouncementLimiter.IntervalSeconds * Second)
            .Should().BeTrue("the aura is announced again once the interval has passed");
    }

    [Test]
    public void DifferentAlliesSourcesAndAurasAreLimitedIndependently()
    {
        var rallying = typeof(RallyingStandard1StatusEffect);
        AuraAnnouncementLimiter.TryAnnounce(Leader, Ally, rallying, Start).Should().BeTrue();

        AuraAnnouncementLimiter.TryAnnounce(Leader, OtherAlly, rallying, Start).Should().BeTrue();
        AuraAnnouncementLimiter.TryAnnounce(OtherLeader, Ally, rallying, Start).Should().BeTrue();
        AuraAnnouncementLimiter.TryAnnounce(Leader, Ally, typeof(WatchfulPresence1StatusEffect), Start).Should().BeTrue();
    }

    [Test]
    public void SwitchingAnAuraOffAndOn_AnnouncesItAgain()
    {
        var type = typeof(RallyingStandard1StatusEffect);
        AuraAnnouncementLimiter.TryAnnounce(Leader, Ally, type, Start).Should().BeTrue();
        AuraAnnouncementLimiter.TryAnnounce(OtherLeader, Ally, type, Start).Should().BeTrue();

        AuraAnnouncementLimiter.ClearSource(Leader);

        AuraAnnouncementLimiter.TryAnnounce(Leader, Ally, type, Start + Second).Should().BeTrue();
        AuraAnnouncementLimiter.TryAnnounce(OtherLeader, Ally, type, Start + Second).Should().BeFalse();
    }

    [Test]
    public void SwitchingOneAuraOff_LeavesTheLeadersOtherAurasLimited()
    {
        var rallying = typeof(RallyingStandard1StatusEffect);
        var watchful = typeof(WatchfulPresence1StatusEffect);
        AuraAnnouncementLimiter.TryAnnounce(Leader, Ally, rallying, Start).Should().BeTrue();
        AuraAnnouncementLimiter.TryAnnounce(Leader, Ally, watchful, Start).Should().BeTrue();

        AuraAnnouncementLimiter.ClearAura(Leader, rallying);

        AuraAnnouncementLimiter.TryAnnounce(Leader, Ally, rallying, Start + Second).Should().BeTrue();
        AuraAnnouncementLimiter.TryAnnounce(Leader, Ally, watchful, Start + Second).Should().BeFalse();
    }

    [Test]
    public void ARecipientWhoLeavesTheArea_IsAnnouncedAgainOnReturn()
    {
        var type = typeof(RallyingStandard1StatusEffect);
        AuraAnnouncementLimiter.TryAnnounce(Leader, Ally, type, Start).Should().BeTrue();

        AuraAnnouncementLimiter.ClearRecipient(Ally);

        AuraAnnouncementLimiter.TryAnnounce(Leader, Ally, type, Start + Second).Should().BeTrue();
    }

    [Test]
    public void AClockThatMovesBackwards_DoesNotSilenceTheAura()
    {
        var type = typeof(RallyingStandard1StatusEffect);
        AuraAnnouncementLimiter.TryAnnounce(Leader, Ally, type, Start).Should().BeTrue();

        AuraAnnouncementLimiter.TryAnnounce(Leader, Ally, type, Start - Second).Should().BeTrue();
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
    public void AbilityAuraApplication_ConsultsTheLimiterBeforeEachAnnouncement()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        directory.Should().NotBeNull();

        var source = File.ReadAllText(Path.Combine(directory!.FullName, "SWLOR.Game.Server", "Service", "Ability.cs"));
        source.Should().Contain("aura.AnnouncesApplication = AuraAnnouncementLimiter.TryAnnounce(source, recipient, type);");
        source.Should().Contain("AuraAnnouncementLimiter.ClearAura(activator, type);");
        source.Should().Contain("AuraAnnouncementLimiter.ClearAura(activator, removeType);");
        source.Should().Contain("AuraAnnouncementLimiter.ClearSource(activator);");
        source.Should().Contain("AuraAnnouncementLimiter.ClearRecipient(target);");
    }
}
