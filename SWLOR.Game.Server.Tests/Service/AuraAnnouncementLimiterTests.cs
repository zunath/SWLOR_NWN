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

    [SetUp]
    public void SetUp() => AuraAnnouncementLimiter.Clear();

    [TearDown]
    public void TearDown() => AuraAnnouncementLimiter.Clear();

    private static bool Apply(uint source, uint recipient, Type type)
    {
        var announces = AuraAnnouncementLimiter.IsFirstAnnouncement(source, recipient, type);
        AuraAnnouncementLimiter.RecordApplied(source, recipient, type);
        return announces;
    }

    [Test]
    public void AllyRepeatedlyCrossingTheAuraEdge_IsAnnouncedOnlyTheFirstTime()
    {
        var type = typeof(RallyingStandard1StatusEffect);
        Apply(Leader, Ally, type).Should().BeTrue();

        for (var i = 0; i < 20; i++)
            Apply(Leader, Ally, type).Should().BeFalse($"re-entry {i + 1} is the same aura the ally was already told about");
    }

    [Test]
    public void ANoticeIsNotConsumedByAnApplicationThatFailed()
    {
        var type = typeof(RallyingStandard1StatusEffect);

        AuraAnnouncementLimiter.IsFirstAnnouncement(Leader, Ally, type).Should().BeTrue();
        AuraAnnouncementLimiter.IsFirstAnnouncement(Leader, Ally, type).Should().BeTrue(
            "nothing is recorded until the aura is actually applied");
    }

    [Test]
    public void DifferentAlliesSourcesAndAurasAreLimitedIndependently()
    {
        var rallying = typeof(RallyingStandard1StatusEffect);
        Apply(Leader, Ally, rallying).Should().BeTrue();

        Apply(Leader, OtherAlly, rallying).Should().BeTrue();
        Apply(OtherLeader, Ally, rallying).Should().BeTrue();
        Apply(Leader, Ally, typeof(WatchfulPresence1StatusEffect)).Should().BeTrue();
    }

    [Test]
    public void SwitchingAnAuraOffAndOn_AnnouncesItAgain()
    {
        var type = typeof(RallyingStandard1StatusEffect);
        Apply(Leader, Ally, type).Should().BeTrue();
        Apply(OtherLeader, Ally, type).Should().BeTrue();

        AuraAnnouncementLimiter.ClearSource(Leader);

        Apply(Leader, Ally, type).Should().BeTrue();
        Apply(OtherLeader, Ally, type).Should().BeFalse();
    }

    [Test]
    public void SwitchingOneAuraOff_LeavesTheLeadersOtherAurasLimited()
    {
        var rallying = typeof(RallyingStandard1StatusEffect);
        var watchful = typeof(WatchfulPresence1StatusEffect);
        Apply(Leader, Ally, rallying).Should().BeTrue();
        Apply(Leader, Ally, watchful).Should().BeTrue();

        AuraAnnouncementLimiter.ClearAura(Leader, rallying);

        Apply(Leader, Ally, rallying).Should().BeTrue();
        Apply(Leader, Ally, watchful).Should().BeFalse();
    }

    [Test]
    public void ARecipientWhoLeavesTheArea_IsAnnouncedAgainOnReturn()
    {
        var type = typeof(RallyingStandard1StatusEffect);
        Apply(Leader, Ally, type).Should().BeTrue();

        AuraAnnouncementLimiter.ClearRecipient(Ally);

        Apply(Leader, Ally, type).Should().BeTrue();
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
        source.Should().Contain("aura.AnnouncesApplication = AuraAnnouncementLimiter.IsFirstAnnouncement(source, recipient, type);");
        source.Should().Contain("AuraAnnouncementLimiter.RecordApplied(source, recipient, type);");
        source.Should().Contain("AuraAnnouncementLimiter.ClearAura(activator, type);");
        source.Should().Contain("AuraAnnouncementLimiter.ClearAura(activator, removeType);");
        source.Should().Contain("AuraAnnouncementLimiter.ClearSource(activator);");
        source.Should().Contain("AuraAnnouncementLimiter.ClearRecipient(target);");
    }
}
