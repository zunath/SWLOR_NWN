using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CityBountyService;
using SWLOR.Game.Server.Service.CityTreasuryService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// City bounties' rules: consent at launch, what a post needs, whom a fall credits, which falls pay, the weekly limit per
/// identity, CD key and party rejection, and how a bounty ends in the city's escrow.
/// </summary>
public class BountyRulesTests
{
    private const uint Target = 10;
    private const uint Hunter = 20;
    private const uint Pet = 21;
    private const uint Stranger = 30;
    private const uint Invalid = 0x7F000000;

    private const string TargetId = "player-target";
    private const string HunterId = "player-hunter";
    private const string DisguiseId = "disguise-1";

    private static readonly DateTime Now = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);

    private static BountyIdentity Character => new(TargetId, string.Empty);
    private static BountyIdentity Disguise => new(TargetId, DisguiseId);

    // The numbers the plan sets

    [Test]
    public void ABounty_RunsSevenDays_AndAnIdentityIsClaimedAtMostOnceAWeek()
    {
        BountyRules.LengthDays.Should().Be(7);
        BountyRules.ClaimIntervalDays.Should().Be(7);
        BountyRules.GetExpiry(Now).Should().Be(Now.AddDays(7));
    }

    [Test]
    public void ABounty_IsExpiredFromItsExpiryOn()
    {
        var expires = BountyRules.GetExpiry(Now);

        BountyRules.IsExpired(expires, expires.AddSeconds(-1)).Should().BeFalse();
        BountyRules.IsExpired(expires, expires).Should().BeTrue();
        BountyRules.IsExpired(expires, expires.AddDays(1)).Should().BeTrue();
    }

    // Consent

    [Test]
    public void Nobody_HasAcceptedTheNoticeAtLaunch()
    {
        new Player().BountyNoticeAcceptedUtc.Should().BeNull();
        new PlayerDisguise().BountyNoticeAcceptedUtc.Should().BeNull();
        BountyRules.HasAccepted(new Player().BountyNoticeAcceptedUtc).Should().BeFalse();
    }

    [Test]
    public void AnIdentityAlreadyFlaggedAtLaunch_SeesTheNotice_AndCannotBeTargetedUntilItAccepts()
    {
        BountyRules.ShouldShowNotice(flagged: true, acceptedUtc: null).Should().BeTrue();
        BountyRules.GetTargetProblem(Post(accepted: false)).Should().Contain("has not accepted");
        BountyRules.GetTargetProblem(Post(accepted: true)).Should().BeEmpty();
    }

    [Test]
    public void TheNotice_ShowsOnlyForAFlaggedIdentityThatHasNotAccepted()
    {
        BountyRules.ShouldShowNotice(flagged: false, acceptedUtc: null).Should().BeFalse("an unflagged identity cannot be targeted anyway");
        BountyRules.ShouldShowNotice(flagged: true, acceptedUtc: Now).Should().BeFalse("it is a one-time notice");
        BountyRules.ShouldShowNotice(flagged: false, acceptedUtc: Now).Should().BeFalse();
    }

    // Posting

    private static BountyPostRequest Post(
        bool canPost = true,
        long reward = 5_000,
        long treasury = 20_000,
        bool flagged = true,
        bool accepted = true,
        bool hasBounty = false,
        DateTime? lastClaimed = null,
        bool isPoster = false)
    {
        return new BountyPostRequest
        {
            NowUtc = Now,
            PosterCanPost = canPost,
            TargetIsPoster = isPoster,
            Reward = reward,
            OrdinaryTreasury = treasury,
            TargetFlagged = flagged,
            TargetAccepted = accepted,
            TargetHasActiveBounty = hasBounty,
            TargetLastClaimedUtc = lastClaimed
        };
    }

    [Test]
    public void APost_NeedsNothingMoreWhenEveryConditionHolds()
    {
        BountyRules.CheckPost(Post()).Should().BeEmpty();
    }

    [Test]
    public void OnlyAnIdentityShowingThePvPFlag_CanBeTargeted()
    {
        BountyRules.CheckPost(Post(flagged: false)).Should().Contain("PvP flag");
    }

    [Test]
    public void OnlyThosePermittedToPost_CanPost()
    {
        BountyRules.CheckPost(Post(canPost: false)).Should().Contain("mayor or an officer");
    }

    [Test]
    public void AnIdentity_HasOneActiveBounty()
    {
        BountyRules.CheckPost(Post(hasBounty: true)).Should().Contain("already has a bounty");
    }

    [Test]
    public void AnIdentityClaimedThisWeek_CannotBeGivenAnotherBounty()
    {
        BountyRules.CheckPost(Post(lastClaimed: Now.AddDays(-6))).Should().Contain("claimed within the last 7 days");
        BountyRules.CheckPost(Post(lastClaimed: Now.AddDays(-7))).Should().BeEmpty("the week has passed");
    }

    [Test]
    public void ABounty_CannotBePostedOnYourself()
    {
        BountyRules.CheckPost(Post(isPoster: true)).Should().Contain("yourself");
    }

    [Test]
    public void TheReward_ComesFromTheOrdinaryTreasury_AndIsBounded()
    {
        BountyRules.CheckPost(Post(reward: 20_001, treasury: 20_000)).Should().Contain("Restricted funds cannot pay one");
        BountyRules.CheckPost(Post(reward: 20_000, treasury: 20_000)).Should().BeEmpty();
        BountyRules.CheckPost(Post(reward: BountyRules.MinimumReward - 1)).Should().Contain("must pay between");
        BountyRules.CheckPost(Post(reward: BountyRules.MinimumReward)).Should().BeEmpty();
        BountyRules.CheckPost(Post(reward: BountyRules.MaximumReward + 1, treasury: long.MaxValue)).Should().Contain("must pay between");
    }

    [TestCase("5000", 5000L)]
    [TestCase("5,000", 5000L)]
    [TestCase(" 12 500 ", 12500L)]
    [TestCase("1_000", 1000L)]
    public void ATypedReward_IsReadWithoutSeparators(string text, long expected)
    {
        BountyRules.TryParseReward(text, out var reward).Should().BeTrue();
        reward.Should().Be(expected);
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase("-5000")]
    [TestCase("5000.5")]
    [TestCase("lots")]
    [TestCase("99999999999999999999")]
    public void ATypedReward_ThatIsNotAWholeNumber_IsRefused(string text)
    {
        BountyRules.TryParseReward(text, out _).Should().BeFalse();
    }

    // Whom a fall credits

    private static bool TryResolve(uint hostile, out uint claimant)
    {
        var players = new HashSet<uint> { Target, Hunter, Stranger };
        var masters = new Dictionary<uint, uint> { [Pet] = Hunter };
        return BountyRules.TryResolveClaimant(
            hostile,
            creature => players.Contains(creature),
            creature => masters.GetValueOrDefault(creature, Invalid),
            out claimant);
    }

    [Test]
    public void APlayersOwnBlow_CreditsThePlayer()
    {
        TryResolve(Hunter, out var claimant).Should().BeTrue();
        claimant.Should().Be(Hunter);
    }

    [Test]
    public void APetsOrDroidsBlow_CreditsItsMaster()
    {
        TryResolve(Pet, out var claimant).Should().BeTrue();
        claimant.Should().Be(Hunter);
    }

    [Test]
    public void ADamageOverTime_CreditsWhoeverAppliedIt()
    {
        // A damaging effect arrives as its creator, so the hunter who applied the poison is the hostile actor.
        TryResolve(Hunter, out var claimant).Should().BeTrue();
        claimant.Should().Be(Hunter);
    }

    [TestCase(Invalid)]
    [TestCase(99u)]
    public void ANonPlayerWithoutAPlayerMaster_CreditsNobody(uint hostile)
    {
        TryResolve(hostile, out _).Should().BeFalse();
    }

    // Which falls pay

    private static BountyClaimContext Claim(
        BountyIdentity? bounty = null,
        BountyIdentity? shown = null,
        bool flagOn = true,
        bool windowOpen = false,
        bool inDuel = false,
        bool hasClaimant = true,
        string claimant = HunterId,
        string targetCd = "cd-target",
        string claimantCd = "cd-hunter",
        bool sharesParty = false,
        DateTime? lastClaimed = null,
        BountyState state = BountyState.Active,
        DateTime? expires = null)
    {
        var identity = bounty ?? Character;
        return new BountyClaimContext
        {
            NowUtc = Now,
            Bounty = identity,
            State = state,
            ExpiresUtc = expires ?? Now.AddDays(3),
            Shown = shown ?? identity,
            ShownFlagOn = flagOn,
            WindowOpen = windowOpen,
            InDuel = inDuel,
            HasClaimant = hasClaimant,
            ClaimantPlayerId = claimant,
            TargetCdKey = targetCd,
            ClaimantCdKey = claimantCd,
            SharesParty = sharesParty,
            LastClaimedUtc = lastClaimed
        };
    }

    [Test]
    public void ADefeatOfTheTarget_ByAnotherPlayer_Pays()
    {
        BountyRules.EvaluateClaim(Claim()).Should().Be(BountyClaimRefusal.None);
    }

    [Test]
    public void ABountyStillPays_WhenTheClaimantFellToo()
    {
        // The rules never ask whether the claimant stands, so both falling at once pays each bounty on the other.
        BountyRules.EvaluateClaim(Claim(claimant: HunterId)).Should().Be(BountyClaimRefusal.None);
        BountyRules.EvaluateClaim(Claim(bounty: new BountyIdentity(HunterId, string.Empty), claimant: TargetId))
            .Should().Be(BountyClaimRefusal.None);
    }

    [Test]
    public void ADuel_NeverPaysABounty()
    {
        BountyRules.EvaluateClaim(Claim(inDuel: true)).Should().Be(BountyClaimRefusal.Duel);
        BountyRules.EvaluateClaim(Claim(inDuel: true, windowOpen: true)).Should().Be(BountyClaimRefusal.Duel);
    }

    [Test]
    public void AFallWithNoPlayerCredited_PaysNobody()
    {
        BountyRules.EvaluateClaim(Claim(hasClaimant: false, claimant: string.Empty)).Should().Be(BountyClaimRefusal.NoClaimant);
    }

    [Test]
    public void TheTargetsOwnPlayer_CannotCollectTheirBounty()
    {
        BountyRules.EvaluateClaim(Claim(claimant: TargetId)).Should().Be(BountyClaimRefusal.OwnFall);
    }

    [Test]
    public void ATargetShowingAnotherIdentity_IsNotClaimable_InOrOutOfTheWindow()
    {
        BountyRules.EvaluateClaim(Claim(bounty: Character, shown: Disguise))
            .Should().Be(BountyClaimRefusal.OtherIdentity);
        BountyRules.EvaluateClaim(Claim(bounty: Character, shown: Disguise, windowOpen: true))
            .Should().Be(BountyClaimRefusal.OtherIdentity, "paying one identity's bounty for another's fall would link a disguise to its owner");
        BountyRules.EvaluateClaim(Claim(bounty: Disguise, shown: Character))
            .Should().Be(BountyClaimRefusal.OtherIdentity);
    }

    [Test]
    public void ABountyOnADisguise_PaysWhenThatDisguiseFalls()
    {
        BountyRules.EvaluateClaim(Claim(bounty: Disguise, shown: Disguise)).Should().Be(BountyClaimRefusal.None);
    }

    [Test]
    public void TheFlagBeingOff_BlocksTheClaim_UnlessTheCombatWindowIsOpen()
    {
        BountyRules.EvaluateClaim(Claim(flagOn: false)).Should().Be(BountyClaimRefusal.FlagOff);
        BountyRules.EvaluateClaim(Claim(flagOn: false, windowOpen: true)).Should().Be(BountyClaimRefusal.None);
        BountyRules.EvaluateClaim(Claim(flagOn: true, windowOpen: false)).Should().Be(BountyClaimRefusal.None);
    }

    [Test]
    public void AClaimantSharingTheTargetsCdKey_IsRejected()
    {
        BountyRules.EvaluateClaim(Claim(targetCd: "cd-shared", claimantCd: "cd-shared")).Should().Be(BountyClaimRefusal.SharedCdKey);
    }

    [Test]
    public void ACdKeyThatIsUnknown_IsNeverTreatedAsShared()
    {
        BountyRules.EvaluateClaim(Claim(targetCd: string.Empty, claimantCd: string.Empty)).Should().Be(BountyClaimRefusal.None);
    }

    [Test]
    public void AClaimantInTheTargetsParty_IsRejected()
    {
        BountyRules.EvaluateClaim(Claim(sharesParty: true)).Should().Be(BountyClaimRefusal.SameParty);
    }

    [Test]
    public void AnIdentity_CanBeClaimedAtMostOncePerWeek()
    {
        BountyRules.EvaluateClaim(Claim(lastClaimed: Now.AddDays(-3))).Should().Be(BountyClaimRefusal.ClaimedThisWeek);
        BountyRules.EvaluateClaim(Claim(lastClaimed: Now.AddDays(-7).AddSeconds(-1))).Should().Be(BountyClaimRefusal.None);
    }

    [Test]
    public void TheWeeklyLimit_IsPerIdentity_SoADisguiseAndItsCharacterAreCountedSeparately()
    {
        // The character was claimed yesterday. The limit is read from the identity the bounty is on, so a bounty on the
        // disguise, with no claim of its own, still pays.
        BountyRules.EvaluateClaim(Claim(bounty: Disguise, shown: Disguise, lastClaimed: null)).Should().Be(BountyClaimRefusal.None);
        BountyRules.EvaluateClaim(Claim(bounty: Character, shown: Character, lastClaimed: Now.AddDays(-1)))
            .Should().Be(BountyClaimRefusal.ClaimedThisWeek);
    }

    [Test]
    public void ABountyThatRanOutOrIsAlreadyBeingPaid_PaysNothing()
    {
        BountyRules.EvaluateClaim(Claim(expires: Now)).Should().Be(BountyClaimRefusal.Expired);
        BountyRules.EvaluateClaim(Claim(expires: Now.AddSeconds(-1))).Should().Be(BountyClaimRefusal.Expired);
        BountyRules.EvaluateClaim(Claim(state: BountyState.Paying)).Should().Be(BountyClaimRefusal.NotActive);
    }

    // The reward in escrow

    private static WorldProperty City(long treasury = 30_000, long restricted = 8_000)
    {
        return new WorldProperty { Id = "city-1", Treasury = treasury, RestrictedTreasury = restricted };
    }

    [Test]
    public void PostingABounty_HoldsTheRewardOutOfTheOrdinaryTreasuryOnly()
    {
        var city = City();

        var spend = CityTreasuryRules.PostEscrow(city, "bounty-1", 5_000, restrictedFirst: false);

        spend.Succeeded.Should().BeTrue();
        city.Treasury.Should().Be(25_000);
        city.RestrictedTreasury.Should().Be(8_000, "restricted income never pays a bounty");
        city.Escrow["bounty-1"].Unrestricted.Should().Be(5_000);
        city.Escrow["bounty-1"].Restricted.Should().Be(0);
    }

    [Test]
    public void ARewardLargerThanTheOrdinaryTreasury_CannotBeHeld_EvenWithRestrictedFunds()
    {
        var city = City(treasury: 4_000, restricted: 50_000);

        CityTreasuryRules.PostEscrow(city, "bounty-1", 5_000, restrictedFirst: false).Succeeded.Should().BeFalse();
        city.Escrow.Should().BeEmpty();
    }

    [Test]
    public void AnExpiredOrCancelledBounty_ReturnsItsRewardToTheTreasury_OnlyOnce()
    {
        var city = City();
        CityTreasuryRules.PostEscrow(city, "bounty-1", 5_000, restrictedFirst: false);

        var release = CityTreasuryRules.RefundEscrow(city, "bounty-1");

        release.Found.Should().BeTrue();
        release.Credits.Should().Be(5_000);
        city.Treasury.Should().Be(30_000);
        city.RestrictedTreasury.Should().Be(8_000);
        CityTreasuryRules.RefundEscrow(city, "bounty-1").Found.Should().BeFalse("a restart repeating the step finds nothing to return twice");
        city.Treasury.Should().Be(30_000);
    }

    [Test]
    public void AClaimedBounty_PaysTheRewardOutOfEscrow_WithoutTouchingEitherBalance_OnlyOnce()
    {
        var city = City();
        CityTreasuryRules.PostEscrow(city, "bounty-1", 5_000, restrictedFirst: false);

        var release = CityTreasuryRules.PayOutEscrow(city, "bounty-1");

        release.Credits.Should().Be(5_000);
        city.Treasury.Should().Be(25_000);
        city.RestrictedTreasury.Should().Be(8_000);
        CityTreasuryRules.PayOutEscrow(city, "bounty-1").Found.Should().BeFalse();
    }

    [Test]
    public void AMayorChange_DoesNotTouchABountysEscrow()
    {
        var city = City();
        CityTreasuryRules.PostEscrow(city, "bounty-1", 5_000, restrictedFirst: false);

        city.OwnerPlayerId = "new-mayor";

        city.Escrow.Should().ContainKey("bounty-1");
        CityTreasuryRules.GetSpendable(city, restrictedFirst: false).Should().Be(25_000, "nobody can spend or withdraw what is held");
    }
}
