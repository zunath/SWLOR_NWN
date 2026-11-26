using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Tests.Support;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Per-skill rank limits: XP awards (through the production GiveSkillXP flow compiled by
/// <see cref="SkillXPAwardTests"/>), decay, the skill cap block, and the RP XP limit helpers.
/// </summary>
[NonParallelizable]
public class SkillRankLimitTests
{
    private const uint NoSource = uint.MaxValue;
    private Type _harness = null!;
    private Dictionary<SkillType, SkillAttribute> _cache = null!;
    private Dictionary<SkillType, SkillAttribute> _original = null!;

    private FacadeContainer _facades = null!;
    private IDisposable _facadeScope = null!;

    [OneTimeSetUp]
    public void BuildFacades() => _facades = new FacadeContainer();

    [OneTimeTearDown]
    public void DisposeFacades() => _facades.Dispose();

    /// <summary>Populates the skill cache from production metadata without starting the NWN engine.</summary>
    [SetUp]
    public void SetUp()
    {
        _facadeScope = _facades.Install();
        _harness = SkillXPAwardTests.Harness;
        var service = _facades.Get<ISkillService>();
        _cache = (Dictionary<SkillType, SkillAttribute>)service.GetType()
            .GetField("_allSkills", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(service)!;
        _original = new(_cache);
        _cache.Clear();
        foreach (var type in Enum.GetValues<SkillType>())
            _cache[type] = type.GetAttribute<SkillType, SkillAttribute>();
        Relog();
    }

    /// <summary>Restores the shared cache so this fixture cannot affect other tests.</summary>
    [TearDown]
    public void TearDown()
    {
        _cache.Clear();
        foreach (var (type, detail) in _original)
            _cache[type] = detail;
        _facadeScope.Dispose();
    }

    // ---- GiveSkillXP: gains ----

    [Test]
    public void Gain_StopsAtTheLimit_DiscardsLeftoverXP_AndReturnsOnlyTheXPUsed()
    {
        var player = CreatePlayer(10, (SkillType.Mimicry, S(10, limit: 11)));
        var used = Award(player, SkillType.Mimicry, SWLOR.Game.Server.Domain.Character.SkillXPChart.GetRequiredXP(10) + 500);

        player.Skills[SkillType.Mimicry].Rank.Should().Be(11);
        player.Skills[SkillType.Mimicry].XP.Should().Be(0);
        used.Should().Be(SWLOR.Game.Server.Domain.Character.SkillXPChart.GetRequiredXP(10));
        WarningKeys.Should().BeEmpty();
    }

    [Test]
    public void Gain_StoppedByTheLimit_DoesNotCountXPAlreadyOnTheSkill()
    {
        var player = CreatePlayer(10, (SkillType.Mimicry, S(10, xp: 20, limit: 11)));
        var used = Award(player, SkillType.Mimicry, SWLOR.Game.Server.Domain.Character.SkillXPChart.GetRequiredXP(10) + 100);

        player.Skills[SkillType.Mimicry].Rank.Should().Be(11);
        player.Skills[SkillType.Mimicry].XP.Should().Be(0);
        used.Should().Be(SWLOR.Game.Server.Domain.Character.SkillXPChart.GetRequiredXP(10) - 20);
    }

    [Test]
    public void Gain_CrossesSeveralRanksBelowTheLimit()
    {
        var player = CreatePlayer(10, (SkillType.Mimicry, S(10, limit: 20)));
        var xp = SWLOR.Game.Server.Domain.Character.SkillXPChart.GetRequiredXP(10) + SWLOR.Game.Server.Domain.Character.SkillXPChart.GetRequiredXP(11) + 7;
        var used = Award(player, SkillType.Mimicry, xp);

        player.Skills[SkillType.Mimicry].Rank.Should().Be(12);
        player.Skills[SkillType.Mimicry].XP.Should().Be(7);
        used.Should().Be(xp);
        WarningKeys.Should().BeEmpty();
    }

    [Test]
    public void Gain_AtTheLimit_AddsNothing_WarnsOnce_AndReturnsZero()
    {
        var player = CreatePlayer(11, (SkillType.Mimicry, S(11, xp: 5, limit: 11)));
        var used = Award(player, SkillType.Mimicry, 100);

        used.Should().Be(0);
        player.Skills[SkillType.Mimicry].Rank.Should().Be(11);
        player.Skills[SkillType.Mimicry].XP.Should().Be(0);
        WarningKeys.Should().Equal("SKILL_LIMIT");
        Warnings.Should().ContainSingle().Which.Should().Contain("limit of 11");
        Messages.Should().BeEmpty();
        Field<List<int>>("QueuedXP").Should().BeEmpty();
        Field<int>("Refreshes").Should().Be(1, "the cleared progress must reach an open Skills window");
    }

    [Test]
    public void Gain_AtTheLimitWithoutProgress_DoesNotRefreshTheSkillsWindow()
    {
        var player = CreatePlayer(11, (SkillType.Mimicry, S(11, limit: 11)));
        Award(player, SkillType.Mimicry, 100);

        Field<int>("Refreshes").Should().Be(0);
    }

    [Test]
    public void Gain_WithALimitBelowTheCurrentRank_KeepsTheRanksAndGainsNothing()
    {
        var player = CreatePlayer(20, (SkillType.Mimicry, S(20, xp: 9, limit: 10)));
        var used = Award(player, SkillType.Mimicry, 100);

        used.Should().Be(0);
        player.Skills[SkillType.Mimicry].Rank.Should().Be(20);
        player.Skills[SkillType.Mimicry].XP.Should().Be(0);
        player.TotalSPAcquired.Should().Be(0);
        WarningKeys.Should().Equal("SKILL_LIMIT");
    }

    [Test]
    public void Gain_WithALimitOfZero_BlocksANewSkill()
    {
        var player = CreatePlayer(0, (SkillType.Mimicry, S(0, limit: 0)));
        Award(player, SkillType.Mimicry, 100).Should().Be(0);

        player.Skills[SkillType.Mimicry].Rank.Should().Be(0);
        WarningKeys.Should().Equal("SKILL_LIMIT");
    }

    // ---- GiveSkillXP: debt ----

    [Test]
    public void AtTheLimit_RepaysXPDebt_AndReturnsTheDebtRepaid()
    {
        var player = CreatePlayer(11, (SkillType.Mimicry, S(11, limit: 11)));
        player.XPDebt = 40;
        var used = Award(player, SkillType.Mimicry, 100);

        used.Should().Be(40);
        player.XPDebt.Should().Be(0);
        player.Skills[SkillType.Mimicry].XP.Should().Be(0);
        WarningKeys.Should().Equal("SKILL_LIMIT");
    }

    [Test]
    public void AtTheLimit_AwardSmallerThanTheDebt_IsFullyUsedWithoutAWarning()
    {
        var player = CreatePlayer(11, (SkillType.Mimicry, S(11, limit: 11)));
        player.XPDebt = 150;
        var used = Award(player, SkillType.Mimicry, 100);

        used.Should().Be(100);
        player.XPDebt.Should().Be(50);
        WarningKeys.Should().BeEmpty();
    }

    [Test]
    public void BelowTheLimit_ReturnValueIncludesDebtRepaidAndXPApplied()
    {
        var player = CreatePlayer(10, (SkillType.Mimicry, S(10, limit: 20)));
        player.XPDebt = 30;
        var used = Award(player, SkillType.Mimicry, 100);

        used.Should().Be(100);
        player.XPDebt.Should().Be(0);
        player.Skills[SkillType.Mimicry].XP.Should().Be(70);
    }

    [Test]
    public void StoppedByTheLimit_ReturnValueIsDebtPlusXPUsedToReachTheLimit()
    {
        var player = CreatePlayer(10, (SkillType.Mimicry, S(10, limit: 11)));
        player.XPDebt = 25;
        var used = Award(player, SkillType.Mimicry, 25 + SWLOR.Game.Server.Domain.Character.SkillXPChart.GetRequiredXP(10) + 300);

        used.Should().Be(25 + SWLOR.Game.Server.Domain.Character.SkillXPChart.GetRequiredXP(10));
        player.Skills[SkillType.Mimicry].Rank.Should().Be(11);
    }

    // ---- GiveSkillXP: return value without a limit ----

    [Test]
    public void NormalAward_ReturnsTheFullAward_WithOrWithoutAMessageSource()
    {
        var player = CreatePlayer(10, (SkillType.Mimicry, S(10)));
        Award(player, SkillType.Mimicry, 100).Should().Be(100);
        Award(player, SkillType.Mimicry, 50, 10u).Should().Be(50);
        player.Skills[SkillType.Mimicry].XP.Should().Be(150);
    }

    [Test]
    public void AwardStoppedAtMaxRank_ReturnsOnlyTheXPNeededForTheLastRank()
    {
        var player = CreatePlayer(49, (SkillType.Mimicry, S(49)));
        Award(player, SkillType.Mimicry, SWLOR.Game.Server.Domain.Character.SkillXPChart.GetRequiredXP(49) + 100).Should().Be(SWLOR.Game.Server.Domain.Character.SkillXPChart.GetRequiredXP(49));
        player.Skills[SkillType.Mimicry].Rank.Should().Be(50);
        player.Skills[SkillType.Mimicry].XP.Should().Be(0);
    }

    [Test]
    public void MaxRankedSkill_ReturnsOnlyTheDebtRepaid()
    {
        var player = CreatePlayer(50, (SkillType.Mimicry, S(50)));
        player.XPDebt = 30;
        Award(player, SkillType.Mimicry, 100).Should().Be(30);
        WarningKeys.Should().BeEmpty();
    }

    [Test]
    public void ZeroOrInvalidAwards_ReturnZero()
    {
        var player = CreatePlayer(10, (SkillType.Mimicry, S(10)));
        Award(player, SkillType.Mimicry, 0).Should().Be(0);
        Award(player, SkillType.Invalid, 100).Should().Be(0);
    }

    // ---- SKILL_LIMIT warning ----

    [TestCase(50)]
    [TestCase(80)]
    public void MaxedSkill_NeverSendsTheLimitWarning(int limit)
    {
        var player = CreatePlayer(50, (SkillType.Mimicry, S(50, limit: limit)));
        Award(player, SkillType.Mimicry, 100).Should().Be(0);

        WarningKeys.Should().BeEmpty();
        SkillRank.GetRankCeiling(player.Skills[SkillType.Mimicry], SkillType.Mimicry.GetAttribute<SkillType, SkillAttribute>())
            .Should().Be(50);
    }

    [Test]
    public void LimitWarning_IsOnlySentWhenTheLimitIsBelowMaxRank()
    {
        var below = CreatePlayer(49, (SkillType.Mimicry, S(49, limit: 49)));
        Award(below, SkillType.Mimicry, 100);
        WarningKeys.Should().Equal("SKILL_LIMIT");

        var maxed = CreatePlayer(50, (SkillType.Mimicry, S(50)));
        Award(maxed, SkillType.Mimicry, 100);
        WarningKeys.Should().BeEmpty();
    }

    [Test]
    public void LimitWarning_IsSentOncePerSkillPerLogin()
    {
        var player = CreatePlayer(28, (SkillType.Leadership, S(28, limit: 28)));

        Award(player, SkillType.Leadership, 100).Should().Be(0);
        WarningKeys.Should().Equal("SKILL_LIMIT");
        Warnings.Should().ContainSingle().Which.Should().Contain("limit of 28");

        for (var award = 0; award < 5; award++)
        {
            Award(player, SkillType.Leadership, 100).Should().Be(0);
            WarningKeys.Should().BeEmpty("the player already learned why this skill stopped gaining XP");
        }

        player.Skills[SkillType.Leadership].Rank.Should().Be(28);
        player.Skills[SkillType.Leadership].XP.Should().Be(0);
    }

    [Test]
    public void LimitWarning_AnotherLimitedSkill_StillWarnsOnce()
    {
        var player = CreatePlayer(48,
            (SkillType.Leadership, S(28, limit: 28)),
            (SkillType.Unarmed, S(20, limit: 20)));

        Award(player, SkillType.Leadership, 100);
        WarningKeys.Should().Equal("SKILL_LIMIT");

        Award(player, SkillType.Unarmed, 100);
        WarningKeys.Should().Equal("SKILL_LIMIT");
        Warnings.Should().ContainSingle().Which.Should().Contain("Unarmed").And.Contain("limit of 20");

        Award(player, SkillType.Unarmed, 100);
        WarningKeys.Should().BeEmpty();
        Award(player, SkillType.Leadership, 100);
        WarningKeys.Should().BeEmpty();
    }

    [Test]
    public void LimitWarning_IsSentAgainAfterRelogging()
    {
        var player = CreatePlayer(28, (SkillType.Leadership, S(28, limit: 28)));
        Award(player, SkillType.Leadership, 100);
        Award(player, SkillType.Leadership, 100);
        WarningKeys.Should().BeEmpty();

        Relog();

        Award(player, SkillType.Leadership, 100);
        WarningKeys.Should().Equal("SKILL_LIMIT");
        Award(player, SkillType.Leadership, 100);
        WarningKeys.Should().BeEmpty();
    }

    [TestCase(nameof(SkillXPService.ResetLimitWarningsOnEnter), ScriptName.OnModuleEnter, "GetEnteringObject()")]
    [TestCase(nameof(SkillXPService.ResetLimitWarningsOnExit), ScriptName.OnModuleExit, "GetExitingObject()")]
    public void LimitWarning_IsForgottenWhenThePlayerEntersOrLeaves(string handler, string script, string playerSource)
    {
        typeof(SkillXPService).GetMethod(handler)!.GetCustomAttributes<NWNEventHandler>()
            .Should().ContainSingle(attribute => attribute.Script == script);

        CharacterCompanionsSource.Skill.Member(handler)
            .Should().Contain(playerSource).And.Contain("ForgetLimitWarnings(GetObjectUUID(player))");
    }

    // ---- GiveSkillXP: languages ----

    [Test]
    public void Language_AtItsLimit_GainsNothing_WithoutAWarning()
    {
        var player = CreatePlayer(0, (SkillType.Mirialan, S(0, xp: 5, limit: 0)));
        var used = Award(player, SkillType.Mirialan, 100);

        used.Should().Be(0);
        player.Skills[SkillType.Mirialan].Rank.Should().Be(0);
        player.Skills[SkillType.Mirialan].XP.Should().Be(0);
        WarningKeys.Should().BeEmpty("overheard speech would otherwise warn every time someone speaks the language");
        Messages.Should().BeEmpty();
    }

    [Test]
    public void Language_StopsAtItsLimit_MidAward()
    {
        var player = CreatePlayer(0, (SkillType.Mirialan, S(5, limit: 6)));
        var used = Award(player, SkillType.Mirialan, SWLOR.Game.Server.Domain.Character.SkillXPChart.GetRequiredXP(5) + SWLOR.Game.Server.Domain.Character.SkillXPChart.GetRequiredXP(6));

        used.Should().Be(SWLOR.Game.Server.Domain.Character.SkillXPChart.GetRequiredXP(5));
        player.Skills[SkillType.Mirialan].Rank.Should().Be(6);
        player.Skills[SkillType.Mirialan].XP.Should().Be(0);
        WarningKeys.Should().BeEmpty();
    }

    [Test]
    public void Language_WithoutALimit_StillGainsRanks()
    {
        var player = CreatePlayer(0, (SkillType.Mirialan, S(5)));
        var xp = SWLOR.Game.Server.Domain.Character.SkillXPChart.GetRequiredXP(5) + 10;
        Award(player, SkillType.Mirialan, xp).Should().Be(xp);
        player.Skills[SkillType.Mirialan].Rank.Should().Be(6);
        player.Skills[SkillType.Mirialan].XP.Should().Be(10);
    }

    [Test]
    public void GetRankLimit_AppliesToLanguages_AndIsClampedToEachSkillsMaxRank()
    {
        var language = SkillType.Mirialan.GetAttribute<SkillType, SkillAttribute>();
        var combat = SkillType.Mimicry.GetAttribute<SkillType, SkillAttribute>();

        language.MaxRank.Should().Be(20);
        SkillRank.GetRankLimit(S(5, limit: 1), language).Should().Be(1);
        SkillRank.GetRankLimit(S(5, limit: 35), language).Should().Be(20);
        SkillRank.GetRankCeiling(S(5, limit: 1), language).Should().Be(1);
        SkillRank.GetRankCeiling(S(5), language).Should().Be(language.MaxRank);
        SkillRank.GetRankLimit(S(5), combat).Should().BeNull();
        SkillRank.GetRankLimit(S(5, limit: 12), combat).Should().Be(12);
        SkillRank.GetRankLimit(S(5, limit: 0), combat).Should().Be(0);
        SkillRank.GetRankLimit(S(5, limit: -3), combat).Should().Be(0);
        SkillRank.GetRankLimit(S(5, limit: 999), combat).Should().Be(combat.MaxRank);
        SkillRank.GetRankCeiling(S(5), combat).Should().Be(combat.MaxRank);
        SkillRank.GetRankCeiling(S(5, limit: 12), combat).Should().Be(12);
    }

    // ---- Decay ----

    [Test]
    public void Decay_PicksAboveLimitSkillsFirst()
    {
        var player = CreatePlayer(SkillService.SkillCap,
            (SkillType.Mimicry, S(10)),
            (SkillType.Unarmed, S(20, limit: 15)),
            (SkillType.TwinPistol, S(20)),
            (SkillType.BeastMastery, S(20, limit: 20)));
        var used = Award(player, SkillType.Mimicry, SWLOR.Game.Server.Domain.Character.SkillXPChart.GetRequiredXP(10));

        used.Should().Be(SWLOR.Game.Server.Domain.Character.SkillXPChart.GetRequiredXP(10));
        player.Skills[SkillType.Mimicry].Rank.Should().Be(11);
        player.Skills[SkillType.Unarmed].Rank.Should().Be(19);
        player.Skills[SkillType.TwinPistol].Rank.Should().Be(20);
        player.Skills[SkillType.BeastMastery].Rank.Should().Be(20);
        SkillRank.GetTotalContributingSkillRanks(player).Should().Be(SkillService.SkillCap);
    }

    [Test]
    public void Decay_NeverGoesBelowALimit_ThenMovesToUnlimitedSkills()
    {
        var player = CreatePlayer(SkillService.SkillCap,
            (SkillType.Mimicry, S(10)),
            (SkillType.Unarmed, S(16, limit: 15)),
            (SkillType.TwinPistol, S(20)),
            (SkillType.BeastMastery, S(20, limit: 20)));
        var xp = SWLOR.Game.Server.Domain.Character.SkillXPChart.GetRequiredXP(10) + SWLOR.Game.Server.Domain.Character.SkillXPChart.GetRequiredXP(11) + SWLOR.Game.Server.Domain.Character.SkillXPChart.GetRequiredXP(12);
        Award(player, SkillType.Mimicry, xp).Should().Be(xp);

        player.Skills[SkillType.Mimicry].Rank.Should().Be(13);
        player.Skills[SkillType.Unarmed].Rank.Should().Be(15);
        player.Skills[SkillType.TwinPistol].Rank.Should().Be(18);
        player.Skills[SkillType.BeastMastery].Rank.Should().Be(20);
        SkillRank.GetTotalContributingSkillRanks(player).Should().Be(SkillService.SkillCap);
    }

    [Test]
    public void Decay_StopsGainingOnceTheOnlyDecayCandidateReachesItsLimit()
    {
        var player = CreatePlayer(SkillService.SkillCap,
            (SkillType.Mimicry, S(10)),
            (SkillType.Unarmed, S(16, limit: 15)));
        var xp = SWLOR.Game.Server.Domain.Character.SkillXPChart.GetRequiredXP(10) + SWLOR.Game.Server.Domain.Character.SkillXPChart.GetRequiredXP(11) * 3;
        var used = Award(player, SkillType.Mimicry, xp);

        player.Skills[SkillType.Mimicry].Rank.Should().Be(11);
        player.Skills[SkillType.Mimicry].XP.Should().Be(0);
        player.Skills[SkillType.Unarmed].Rank.Should().Be(15);
        SkillRank.GetTotalContributingSkillRanks(player).Should().Be(SkillService.SkillCap);
        used.Should().Be(SWLOR.Game.Server.Domain.Character.SkillXPChart.GetRequiredXP(10));
    }

    [Test]
    public void GetDecayCandidates_ListsAboveLimitSkillsFirst_ThenUnlimitedSkills()
    {
        var player = new Player();
        player.Skills[SkillType.Mimicry] = S(10);
        player.Skills[SkillType.Unarmed] = S(20, limit: 15);          // above its limit
        player.Skills[SkillType.TwinPistol] = S(20);                  // no limit
        player.Skills[SkillType.BeastMastery] = S(20, limit: 20);     // at its limit
        player.Skills[SkillType.Piloting] = S(10, limit: 30);         // below its limit
        player.Skills[SkillType.Armor] = S(10, locked: true);         // locked
        player.Skills[SkillType.Throwing] = S(0);                       // rank 0
        player.Skills[SkillType.Mirialan] = S(5, limit: 1);           // a language: never decays

        DecayCandidates(player, SkillType.Mimicry).Should().Equal(SkillType.Unarmed);

        player.Skills[SkillType.Unarmed].Rank = 15;
        DecayCandidates(player, SkillType.Mimicry).Should().Equal(SkillType.TwinPistol);
    }

    [Test]
    public void GetDecayCandidates_NeverIncludesTheAwardedSkill_AndCanBeEmpty()
    {
        var player = new Player();
        player.Skills[SkillType.Mimicry] = S(10);
        player.Skills[SkillType.Unarmed] = S(20, limit: 20);
        player.Skills[SkillType.TwinPistol] = S(20, limit: 25);

        DecayCandidates(player, SkillType.Mimicry).Should().BeEmpty();
        DecayCandidates(player, SkillType.Unarmed).Should().Equal(SkillType.Mimicry);

        player.Skills[SkillType.TwinPistol].Rank = 30;
        DecayCandidates(player, SkillType.Unarmed).Should().Equal(SkillType.TwinPistol);
    }

    // ---- Skill cap block ----

    [Test]
    public void AtTheCap_WhenEveryOtherSkillIsLockedOrAtItsLimit_XPIsBlocked()
    {
        var player = CreatePlayer(SkillService.SkillCap,
            (SkillType.Mimicry, S(10, xp: 3)),
            (SkillType.Unarmed, S(20, limit: 20)),
            (SkillType.TwinPistol, S(20, locked: true)));
        var used = Award(player, SkillType.Mimicry, 100);

        used.Should().Be(0);
        WarningKeys.Should().Equal("SKILL_CAP");
        Warnings.Should().ContainSingle().Which.Should().Contain("locked or at their limits");
        Messages.Should().BeEmpty();
        player.Skills[SkillType.Mimicry].Rank.Should().Be(10);
        player.Skills[SkillType.Mimicry].XP.Should().Be(3);
        player.Skills[SkillType.Unarmed].Rank.Should().Be(20);
    }

    [Test]
    public void AtTheCap_BlockedAward_KeepsDebtRepaidAndReturnsIt()
    {
        var player = CreatePlayer(SkillService.SkillCap,
            (SkillType.Mimicry, S(10)),
            (SkillType.Unarmed, S(20, limit: 20)));
        player.XPDebt = 30;
        var used = Award(player, SkillType.Mimicry, 100);

        used.Should().Be(30);
        player.XPDebt.Should().Be(0);
        Field<int>("Saves").Should().BeGreaterThan(0);
        WarningKeys.Should().Equal("SKILL_CAP");
        player.Skills[SkillType.Mimicry].Rank.Should().Be(10);
    }

    [Test]
    public void AtTheCap_BlockedAward_WithNoDebtReturnsZeroAndSavesNothing()
    {
        var player = CreatePlayer(SkillService.SkillCap,
            (SkillType.Mimicry, S(10)),
            (SkillType.Unarmed, S(20, limit: 20)));
        Award(player, SkillType.Mimicry, 100).Should().Be(0);
        Field<int>("Saves").Should().Be(0);
    }

    [Test]
    public void AtTheCap_AnAboveLimitSkillKeepsXPFlowing()
    {
        var player = CreatePlayer(SkillService.SkillCap,
            (SkillType.Mimicry, S(10)),
            (SkillType.Unarmed, S(21, limit: 20)));
        Award(player, SkillType.Mimicry, 100).Should().Be(100);

        WarningKeys.Should().BeEmpty();
        player.Skills[SkillType.Mimicry].XP.Should().Be(100);
    }

    [Test]
    public void IsBlockedBySkillCap_RequiresTheCap_AndNoDecayCandidate()
    {
        var blocked = CreatePlayer(SkillService.SkillCap,
            (SkillType.Mimicry, S(10)),
            (SkillType.Unarmed, S(20, limit: 20)));
        SkillRank.IsBlockedBySkillCap(blocked, SkillType.Mimicry).Should().BeTrue();

        // Removing the other skill's limit frees it to decay.
        blocked.Skills[SkillType.Unarmed].RankLimit = null;
        SkillRank.IsBlockedBySkillCap(blocked, SkillType.Mimicry).Should().BeFalse();

        // Below the cap nothing is blocked.
        var belowCap = CreatePlayer(SkillService.SkillCap - 1,
            (SkillType.Mimicry, S(10)),
            (SkillType.Unarmed, S(20, limit: 20)));
        SkillRank.IsBlockedBySkillCap(belowCap, SkillType.Mimicry).Should().BeFalse();

        // Languages do not count toward the cap, so the cap never blocks them.
        var language = CreatePlayer(SkillService.SkillCap, (SkillType.Mirialan, S(5)), (SkillType.Unarmed, S(20, limit: 20)));
        SkillRank.IsBlockedBySkillCap(language, SkillType.Mirialan).Should().BeFalse();
    }

    // ---- RP XP: GetMaxDistributableXP ----

    [Test]
    public void MaxDistributableXP_IsZeroAtTheLimit_AboveTheLimit_AndAtMaxRank()
    {
        var player = CreatePlayer(81,
            (SkillType.Mimicry, S(11, limit: 11)),
            (SkillType.Unarmed, S(20, xp: 4, limit: 10)),
            (SkillType.TwinPistol, S(50)));

        SkillRank.GetMaxDistributableXP(player, SkillType.Mimicry).Should().Be(0);
        SkillRank.GetMaxDistributableXP(player, SkillType.Unarmed).Should().Be(0);
        SkillRank.GetMaxDistributableXP(player, SkillType.TwinPistol).Should().Be(0);
        SkillRank.GetMaxDistributableXP(player, SkillType.Invalid).Should().Be(0);
    }

    [Test]
    public void MaxDistributableXP_IsCappedAtTheXPNeededToReachTheLimit()
    {
        var player = CreatePlayer(20,
            (SkillType.Mimicry, S(10, xp: 30, limit: 12)),
            (SkillType.Unarmed, S(10, xp: 30)));

        SkillRank.GetMaxDistributableXP(player, SkillType.Mimicry)
            .Should().Be(SWLOR.Game.Server.Domain.Character.SkillXPChart.GetRequiredXP(10) - 30 + SWLOR.Game.Server.Domain.Character.SkillXPChart.GetRequiredXP(11));

        var expectedToMax = Enumerable.Range(10, 40).Sum(level => Skill.GetRequiredXP(level)) - 30;
        SkillRank.GetMaxDistributableXP(player, SkillType.Unarmed).Should().Be(expectedToMax);
    }

    [Test]
    public void MaxDistributableXP_IsZeroWhileTheSkillCapBlocksTheSkill()
    {
        var blocked = CreatePlayer(SkillService.SkillCap,
            (SkillType.Mimicry, S(10)),
            (SkillType.Unarmed, S(20, limit: 20)),
            (SkillType.TwinPistol, S(20, locked: true)));
        SkillRank.GetMaxDistributableXP(blocked, SkillType.Mimicry).Should().Be(0);

        blocked.Skills[SkillType.Unarmed].RankLimit = null;
        SkillRank.GetMaxDistributableXP(blocked, SkillType.Mimicry).Should().BeGreaterThan(0);
    }

    [Test]
    public void MaxDistributableXP_AtTheCap_StopsAtTheRanksDecayCanSupply()
    {
        // Unarmed can give up 1 rank above its limit and Throwing 2 unlimited ranks: 3 ranks of Mimicry in all.
        var player = CreatePlayer(SkillService.SkillCap,
            (SkillType.Mimicry, S(10, xp: 30)),
            (SkillType.Unarmed, S(21, limit: 20)),
            (SkillType.Throwing, S(2)));
        var expected = SWLOR.Game.Server.Domain.Character.SkillXPChart.GetRequiredXP(10) - 30 + SWLOR.Game.Server.Domain.Character.SkillXPChart.GetRequiredXP(11) + SWLOR.Game.Server.Domain.Character.SkillXPChart.GetRequiredXP(12);

        SkillRank.GetMaxDistributableXP(player, SkillType.Mimicry).Should().Be(expected);
        Award(player, SkillType.Mimicry, expected).Should().Be(expected);
        player.Skills[SkillType.Mimicry].Rank.Should().Be(13);
        player.Skills[SkillType.Unarmed].Rank.Should().Be(20);
        player.Skills[SkillType.Throwing].Rank.Should().Be(0);
    }

    [TestCase(0, 0)]
    [TestCase(20, 20)]
    [TestCase(35, 35)]
    [TestCase(null, 0)]
    public void AtTheCap_ALimitBelowTheRank_MovesRanksFromThatSkillIntoAnother(int? limit, int remainingRank)
    {
        // Everything else is locked: the unlocked skill's ranks above its limit are all that can move.
        var player = CreatePlayer(SkillService.SkillCap,
            (SkillType.Piloting, S(36, limit: limit)),
            (SkillType.Espionage, S(8)));
        var movableRanks = 36 - remainingRank;
        var expected = Enumerable.Range(8, movableRanks).Sum(SWLOR.Game.Server.Domain.Character.SkillXPChart.GetRequiredXP);

        SkillRank.GetMaxDistributableXP(player, SkillType.Espionage).Should().Be(expected);
        Award(player, SkillType.Espionage, expected).Should().Be(expected);
        player.Skills[SkillType.Espionage].Rank.Should().Be(8 + movableRanks);
        player.Skills[SkillType.Piloting].Rank.Should().Be(remainingRank);
    }

    [TestCase(36)]
    [TestCase(50)]
    public void AtTheCap_ALimitAtOrAboveTheRank_LetsNothingMove(int limit)
    {
        var player = CreatePlayer(SkillService.SkillCap,
            (SkillType.Piloting, S(36, limit: limit)),
            (SkillType.Espionage, S(8)));

        SkillRank.GetMaxDistributableXP(player, SkillType.Espionage).Should().Be(0);
        Award(player, SkillType.Espionage, 1000).Should().Be(0);
        player.Skills[SkillType.Piloting].Rank.Should().Be(36);
    }

    [Test]
    public void MaxDistributableXP_AcceptsOneXPToCompleteARankTheCapHeldBack()
    {
        var player = CreatePlayer(SkillService.SkillCap,
            (SkillType.Mimicry, S(10, xp: SWLOR.Game.Server.Domain.Character.SkillXPChart.GetRequiredXP(10))),
            (SkillType.Unarmed, S(21, limit: 20)));

        SkillRank.GetMaxDistributableXP(player, SkillType.Mimicry).Should().Be(1);
        Award(player, SkillType.Mimicry, 1);
        player.Skills[SkillType.Mimicry].Rank.Should().Be(11);
        player.Skills[SkillType.Unarmed].Rank.Should().Be(20);
    }

    [Test]
    public void MaxDistributableXP_BelowTheCap_CountsFreeRanksBeforeDecay()
    {
        var player = CreatePlayer(SkillService.SkillCap - 1,
            (SkillType.Mimicry, S(10)),
            (SkillType.Unarmed, S(21, limit: 20)));

        SkillRank.GetMaxDistributableXP(player, SkillType.Mimicry)
            .Should().Be(SWLOR.Game.Server.Domain.Character.SkillXPChart.GetRequiredXP(10) + SWLOR.Game.Server.Domain.Character.SkillXPChart.GetRequiredXP(11));
    }

    [Test]
    public void MaxDistributableXP_RespectsALanguageLimit()
    {
        var atLimit = CreatePlayer(0, (SkillType.Mirialan, S(5, limit: 5)));
        SkillRank.GetMaxDistributableXP(atLimit, SkillType.Mirialan).Should().Be(0);

        var belowLimit = CreatePlayer(0, (SkillType.Mirialan, S(5, limit: 6)));
        SkillRank.GetMaxDistributableXP(belowLimit, SkillType.Mirialan).Should().Be(SWLOR.Game.Server.Domain.Character.SkillXPChart.GetRequiredXP(5));
    }

    // ---- RP XP window and rebuild (source shape; no engine seam) ----

    [Test]
    public void RPXPWindow_RefusesBeforeAwarding_CapsTheAmount_AndDebitsOnlyTheAcceptedXP()
    {
        var source = File.ReadAllText(Path.Combine(FindRepositoryRoot().FullName, "SWLOR.Game.Server",
            "Feature", "GuiDefinition", "ViewModel", "DistributeRPXPViewModel.cs"));
        var confirm = ExtractMethod(source, "public Action OnClickConfirm() => () =>");

        var refuse = confirm.IndexOf("SkillRank.GetMaxDistributableXP(dbPlayer, _skillType)", StringComparison.Ordinal);
        var cap = confirm.IndexOf("amount = Math.Min(amount, maxDistributableXP);", StringComparison.Ordinal);
        var award = confirm.IndexOf("var acceptedXP = SkillXP.GiveSkillXP(Player, _skillType, amount, true, false);", StringComparison.Ordinal);
        var guard = confirm.IndexOf("if (acceptedXP > 0)", StringComparison.Ordinal);
        var debit = confirm.IndexOf("dbPlayer.UnallocatedXP -=", StringComparison.Ordinal);

        refuse.Should().BeGreaterThan(0);
        cap.Should().BeGreaterThan(refuse);
        award.Should().BeGreaterThan(cap);
        guard.Should().BeGreaterThan(award);
        debit.Should().BeGreaterThan(guard);
        confirm.Should().Contain("dbPlayer.UnallocatedXP -= Math.Min(acceptedXP, dbPlayer.UnallocatedXP);");
        confirm.Split("UnallocatedXP -=").Length.Should().Be(2, "RP XP is only debited once, after the award");
        confirm.Substring(refuse, cap - refuse).Should().Contain("return;");
    }

    [Test]
    public void RPXPWindow_RechecksItsLimitAtConfirm_BeforeRefusing()
    {
        var source = File.ReadAllText(Path.Combine(FindRepositoryRoot().FullName, "SWLOR.Game.Server",
            "Feature", "GuiDefinition", "ViewModel", "DistributeRPXPViewModel.cs"));
        var confirm = ExtractMethod(source, "public Action OnClickConfirm() => () =>");

        var recheck = confirm.IndexOf("_maxDistributableXP = GetMaxDistributableXP();", StringComparison.Ordinal);
        var refuse = confirm.IndexOf("if (_maxDistributableXP <= 0)", StringComparison.Ordinal);

        recheck.Should().BeGreaterThan(0);
        refuse.Should().BeGreaterThan(recheck, "a limit cached when the window opened must not refuse XP");
    }

    [TestCase("protected override void OnClientPropertyUpdated(string propertyName)", "DB.Set(dbPlayer);")]
    [TestCase("public Action ToggleDecayLock() => () =>", "DB.Set(dbPlayer);")]
    public void SkillsWindow_LimitAndLockChanges_RefreshAnOpenRPXPWindow(string signature, string save)
    {
        var source = File.ReadAllText(Path.Combine(FindRepositoryRoot().FullName, "SWLOR.Game.Server",
            "Feature", "GuiDefinition", "ViewModel", "SkillsViewModel.cs"));
        var method = ExtractMethod(source, signature);

        var saved = method.IndexOf(save, StringComparison.Ordinal);
        var refresh = method.IndexOf("Gui.PublishRefreshEvent(Player, new RPXPRefreshEvent());", StringComparison.Ordinal);

        saved.Should().BeGreaterThan(0);
        refresh.Should().BeGreaterThan(saved, "the RP XP window rechecks its limit from the saved record");
    }

    [Test]
    public void FullRebuild_RefundAllSkills_LeavesRankLimitsUntouched()
    {
        var source = File.ReadAllText(Path.Combine(FindRepositoryRoot().FullName, "SWLOR.Game.Server",
            "Feature", "GuiDefinition", "ViewModel", "CharacterFullRebuildViewModel.cs"));
        var refund = ExtractMethod(source, "void RefundAllSkills()");

        refund.Should().Contain("Rank = 0;");
        refund.Should().Contain("XP = 0;");
        refund.Should().Contain("IsLocked = false;");
        refund.Should().NotContain("RankLimit");
    }

    // ---- Helpers ----

    private static PlayerSkill S(int rank, bool locked = false, int? limit = null, int xp = 0) =>
        new() { Rank = rank, XP = xp, IsLocked = locked, RankLimit = limit };

    /// <summary>
    /// Builds a player with the given skills, plus locked unlimited filler skills so the contributing ranks total
    /// <paramref name="totalRanks"/>. Filler can never decay, so only the listed skills take part.
    /// </summary>
    private static Player CreatePlayer(int totalRanks, params (SkillType Type, PlayerSkill Skill)[] skills)
    {
        var player = new Player();
        foreach (var (type, skill) in skills)
            player.Skills[type] = skill;

        var remaining = totalRanks - SkillRank.GetTotalContributingSkillRanks(player);
        foreach (var type in Enum.GetValues<SkillType>().Where(type => !player.Skills.ContainsKey(type)))
        {
            if (remaining <= 0)
                break;

            var detail = type.GetAttribute<SkillType, SkillAttribute>();
            if (!detail.ContributesToSkillCap)
                continue;

            var rank = Math.Min(remaining, detail.MaxRank);
            player.Skills[type] = S(rank, locked: true);
            remaining -= rank;
        }

        remaining.Should().Be(0);
        return player;
    }

    private int Award(Player player, SkillType skill, int xp, uint source = NoSource) =>
        (int)_harness.GetMethod("AwardSkill")!.Invoke(null, new object[] { player, skill, xp, source })!;

    private List<SkillType> DecayCandidates(Player player, SkillType skill)
    {
        return _facades.Get<ISkillRankService>().GetDecayCandidates(player, skill);
    }

    private void Relog() => _harness.GetMethod("Relog")!.Invoke(null, null);

    private T Field<T>(string name) => (T)_harness.GetField(name)!.GetValue(null)!;
    private List<string> Warnings => Field<List<string>>("Warnings");
    private List<string> WarningKeys => Field<List<string>>("WarningKeys");
    private List<string> Messages => Field<List<string>>("Messages");

    private static string ExtractMethod(string source, string signature)
    {
        var signatureIndex = source.IndexOf(signature, StringComparison.Ordinal);
        signatureIndex.Should().BeGreaterThanOrEqualTo(0);

        var openBraceIndex = source.IndexOf('{', signatureIndex);
        var depth = 0;
        for (var index = openBraceIndex; index < source.Length; index++)
        {
            if (source[index] == '{')
            {
                depth++;
            }
            else if (source[index] == '}')
            {
                depth--;
                if (depth == 0)
                    return source.Substring(signatureIndex, index - signatureIndex + 1);
            }
        }

        throw new InvalidOperationException($"Could not extract method '{signature}'.");
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")) &&
                Directory.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server")))
                return directory;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }
}
