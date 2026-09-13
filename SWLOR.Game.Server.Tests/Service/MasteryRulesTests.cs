using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.MasteryService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Tests.Service;

public class MasteryRulesTests
{
    private static readonly DateTime UtcNow = new DateTime(2026, 7, 18, 0, 0, 0, DateTimeKind.Utc);

    private static PlayerMasteryProfile CreateProfile(int lifetimeLevelsTrained = 0)
    {
        return new PlayerMasteryProfile("test-player")
        {
            LifetimeLevelsTrained = lifetimeLevelsTrained
        };
    }

    private static Mastery CreateMastery(MasteryRarityType rarity = MasteryRarityType.Standard, SkillType? skill = null, string name = "Test Mastery")
    {
        return new Mastery
        {
            Name = name,
            Rarity = rarity,
            AssociatedSkill = skill
        };
    }

    #region GetTrainingDuration / DetermineTrainingSource

    [Test]
    public void GetTrainingDuration_FirstEverLevel_Returns14Days()
    {
        var profile = CreateProfile(lifetimeLevelsTrained: 0);

        MasteryRules.GetTrainingDuration(profile, 1, false, false).Should().Be(14);
        MasteryRules.DetermineTrainingSource(profile, 1, false, false).Should().Be(MasteryTrainingSource.Standard14);
    }

    [Test]
    public void GetTrainingDuration_SecondEverLevel_Returns21Days()
    {
        var profile = CreateProfile(lifetimeLevelsTrained: 1);

        MasteryRules.GetTrainingDuration(profile, 1, false, false).Should().Be(21);
        MasteryRules.DetermineTrainingSource(profile, 1, false, false).Should().Be(MasteryTrainingSource.Standard21);
    }

    [Test]
    public void GetTrainingDuration_ThirdOrLaterEverLevel_Returns28Days()
    {
        var profile = CreateProfile(lifetimeLevelsTrained: 2);
        MasteryRules.GetTrainingDuration(profile, 1, false, false).Should().Be(28);

        var laterProfile = CreateProfile(lifetimeLevelsTrained: 9);
        MasteryRules.GetTrainingDuration(laterProfile, 2, false, false).Should().Be(28);
        MasteryRules.DetermineTrainingSource(laterProfile, 2, false, false).Should().Be(MasteryTrainingSource.Standard28);
    }

    [Test]
    public void GetTrainingDuration_TierFive_Returns152Days()
    {
        var profile = CreateProfile(lifetimeLevelsTrained: 4);

        MasteryRules.GetTrainingDuration(profile, 5, false, false).Should().Be(152);
        MasteryRules.DetermineTrainingSource(profile, 5, false, false).Should().Be(MasteryTrainingSource.Tier5);
    }

    [Test]
    public void GetTrainingDuration_QuickSlot_Returns7Days()
    {
        var profile = CreateProfile(lifetimeLevelsTrained: 5);

        MasteryRules.GetTrainingDuration(profile, 3, true, false).Should().Be(7);
        MasteryRules.DetermineTrainingSource(profile, 3, true, false).Should().Be(MasteryTrainingSource.QuickSlot);
    }

    [Test]
    public void GetTrainingDuration_QuickSlotOnFirstEverLevel_StillReturns7Days()
    {
        // Quick Slot always wins over the standard bracket, even on someone's 1st level ever.
        var profile = CreateProfile(lifetimeLevelsTrained: 0);

        MasteryRules.GetTrainingDuration(profile, 1, true, false).Should().Be(7);
    }

    [Test]
    public void GetTrainingDuration_TierFiveWithQuickSlot_Returns131Days()
    {
        var profile = CreateProfile(lifetimeLevelsTrained: 4);

        MasteryRules.GetTrainingDuration(profile, 5, true, false).Should().Be(131);
        MasteryRules.DetermineTrainingSource(profile, 5, true, false).Should().Be(MasteryTrainingSource.QuickSlot);
    }

    [Test]
    public void GetTrainingDuration_Instant_Returns0DaysRegardlessOfOtherModifiers()
    {
        var profile = CreateProfile(lifetimeLevelsTrained: 0);

        MasteryRules.GetTrainingDuration(profile, 5, true, true, isInstant: true).Should().Be(0);
        MasteryRules.DetermineTrainingSource(profile, 5, true, true, isInstant: true).Should().Be(MasteryTrainingSource.Instant);
    }

    [Test]
    public void GetTrainingDuration_RetrainCredit7Available_Returns7Days()
    {
        var profile = CreateProfile(lifetimeLevelsTrained: 5);
        profile.RetrainCredits7 = 1;

        MasteryRules.GetTrainingDuration(profile, 2, false, true).Should().Be(7);
        MasteryRules.DetermineTrainingSource(profile, 2, false, true).Should().Be(MasteryTrainingSource.Retrain7);
    }

    [Test]
    public void GetTrainingDuration_RetrainCredit14AvailableAndNo7Credit_Returns14Days()
    {
        var profile = CreateProfile(lifetimeLevelsTrained: 5);
        profile.RetrainCredits14 = 1;

        MasteryRules.GetTrainingDuration(profile, 2, false, true).Should().Be(14);
        MasteryRules.DetermineTrainingSource(profile, 2, false, true).Should().Be(MasteryTrainingSource.Retrain14);
    }

    [Test]
    public void GetTrainingDuration_RetrainCreditRequestedButNoneAvailable_FallsBackToStandardBracket()
    {
        var profile = CreateProfile(lifetimeLevelsTrained: 0);

        MasteryRules.GetTrainingDuration(profile, 1, false, true).Should().Be(14);
    }

    [Test]
    public void GetTrainingDuration_RetrainCreditDoesNotDiscountTierFive()
    {
        var profile = CreateProfile(lifetimeLevelsTrained: 4);
        profile.RetrainCredits7 = 1;

        MasteryRules.GetTrainingDuration(profile, 5, false, true).Should().Be(152);
    }

    #endregion

    #region CanUseQuickSlot (gate shared by EnqueueTraining and Mastery.ApproveRequest)

    [Test]
    public void CanUseQuickSlot_ZeroAvailableAndRequested_ReturnsFalse()
    {
        var profile = CreateProfile();
        profile.QuickSlotsAvailable = 0;

        MasteryRules.CanUseQuickSlot(profile, useQuickSlot: true, isInstant: false).Should().BeFalse();
    }

    [Test]
    public void CanUseQuickSlot_OneAvailableAndRequested_ReturnsTrue()
    {
        var profile = CreateProfile();
        profile.QuickSlotsAvailable = 1;

        MasteryRules.CanUseQuickSlot(profile, useQuickSlot: true, isInstant: false).Should().BeTrue();
    }

    [Test]
    public void CanUseQuickSlot_NotRequested_ReturnsTrueRegardlessOfAvailability()
    {
        var profile = CreateProfile();
        profile.QuickSlotsAvailable = 0;

        MasteryRules.CanUseQuickSlot(profile, useQuickSlot: false, isInstant: false).Should().BeTrue();
    }

    [Test]
    public void CanUseQuickSlot_InstantGrantWithStaleQuickSlotFlagAndZeroAvailable_ReturnsTrue()
    {
        // An instant grant never actually spends a Quick Slot (see ResolveTraining), so a
        // stale useQuickSlot:true flag alongside it must not trip the zero-slot rejection.
        var profile = CreateProfile();
        profile.QuickSlotsAvailable = 0;

        MasteryRules.CanUseQuickSlot(profile, useQuickSlot: true, isInstant: true).Should().BeTrue();
    }

    #endregion

    #region FindDuplicatePendingRequest (Mastery.SubmitRequest service-level dedup)

    private static MasteryRequest CreateRequest(
        MasteryRequestStatus status,
        MasteryRequestType type,
        int targetTier,
        string masteryId = "mastery-a",
        string customName = "")
    {
        return new MasteryRequest
        {
            Status = status,
            Type = type,
            TargetTier = targetTier,
            MasteryId = masteryId,
            CustomName = customName
        };
    }

    [Test]
    public void FindDuplicatePendingRequest_IdenticalPendingRequestExists_ReturnsIt()
    {
        var existing = CreateRequest(MasteryRequestStatus.Pending, MasteryRequestType.NewMastery, 1);

        var duplicate = MasteryRules.FindDuplicatePendingRequest(
            new List<MasteryRequest> { existing }, MasteryRequestType.NewMastery, "mastery-a", null, 1);

        duplicate.Should().BeSameAs(existing);
    }

    [Test]
    public void FindDuplicatePendingRequest_IdenticalInReviewRequestExists_ReturnsIt()
    {
        var existing = CreateRequest(MasteryRequestStatus.InReview, MasteryRequestType.NewMastery, 1);

        var duplicate = MasteryRules.FindDuplicatePendingRequest(
            new List<MasteryRequest> { existing }, MasteryRequestType.NewMastery, "mastery-a", null, 1);

        duplicate.Should().BeSameAs(existing);
    }

    [Test]
    public void FindDuplicatePendingRequest_OnlyDecidedRequestExists_ReturnsNull()
    {
        var approved = CreateRequest(MasteryRequestStatus.Approved, MasteryRequestType.NewMastery, 1);
        var denied = CreateRequest(MasteryRequestStatus.Denied, MasteryRequestType.NewMastery, 1);
        var cancelled = CreateRequest(MasteryRequestStatus.Cancelled, MasteryRequestType.NewMastery, 1);

        var duplicate = MasteryRules.FindDuplicatePendingRequest(
            new List<MasteryRequest> { approved, denied, cancelled }, MasteryRequestType.NewMastery, "mastery-a", null, 1);

        duplicate.Should().BeNull();
    }

    [Test]
    public void FindDuplicatePendingRequest_DifferentTargetTier_ReturnsNull()
    {
        var existing = CreateRequest(MasteryRequestStatus.Pending, MasteryRequestType.RankUp, 2);

        var duplicate = MasteryRules.FindDuplicatePendingRequest(
            new List<MasteryRequest> { existing }, MasteryRequestType.RankUp, "mastery-a", null, 3);

        duplicate.Should().BeNull();
    }

    [Test]
    public void FindDuplicatePendingRequest_DifferentMasteryId_ReturnsNull()
    {
        var existing = CreateRequest(MasteryRequestStatus.Pending, MasteryRequestType.NewMastery, 1, masteryId: "mastery-a");

        var duplicate = MasteryRules.FindDuplicatePendingRequest(
            new List<MasteryRequest> { existing }, MasteryRequestType.NewMastery, "mastery-b", null, 1);

        duplicate.Should().BeNull();
    }

    [Test]
    public void FindDuplicatePendingRequest_IdenticalCustomRequestMatchedByNameCaseInsensitive_ReturnsIt()
    {
        var existing = CreateRequest(MasteryRequestStatus.Pending, MasteryRequestType.Custom, 1, masteryId: "", customName: "Sabacc Sharking");

        var duplicate = MasteryRules.FindDuplicatePendingRequest(
            new List<MasteryRequest> { existing }, MasteryRequestType.Custom, null, "SABACC SHARKING", 1);

        duplicate.Should().BeSameAs(existing);
    }

    [Test]
    public void FindDuplicatePendingRequest_NoExistingRequests_ReturnsNull()
    {
        MasteryRules.FindDuplicatePendingRequest(new List<MasteryRequest>(), MasteryRequestType.NewMastery, "mastery-a", null, 1)
            .Should().BeNull();

        MasteryRules.FindDuplicatePendingRequest(null, MasteryRequestType.NewMastery, "mastery-a", null, 1)
            .Should().BeNull();
    }

    #endregion

    #region ShouldUseRetrainCredit (Fix 1 - automatic retrain-credit consumption)

    [Test]
    public void ShouldUseRetrainCredit_SevenDayCreditAvailable_ReturnsTrue()
    {
        var profile = CreateProfile();
        profile.RetrainCredits7 = 1;

        MasteryRules.ShouldUseRetrainCredit(profile, 2, false, false).Should().BeTrue();
    }

    [Test]
    public void ShouldUseRetrainCredit_FourteenDayCreditAvailable_ReturnsTrue()
    {
        var profile = CreateProfile();
        profile.RetrainCredits14 = 1;

        MasteryRules.ShouldUseRetrainCredit(profile, 2, false, false).Should().BeTrue();
    }

    [Test]
    public void ShouldUseRetrainCredit_NoCreditsAvailable_ReturnsFalse()
    {
        var profile = CreateProfile();

        MasteryRules.ShouldUseRetrainCredit(profile, 2, false, false).Should().BeFalse();
    }

    [Test]
    public void ShouldUseRetrainCredit_QuickSlotRequested_ReturnsFalseEvenWithCreditsAvailable()
    {
        var profile = CreateProfile();
        profile.RetrainCredits7 = 1;

        MasteryRules.ShouldUseRetrainCredit(profile, 2, true, false).Should().BeFalse();
    }

    [Test]
    public void ShouldUseRetrainCredit_InstantGrant_ReturnsFalseEvenWithCreditsAvailable()
    {
        var profile = CreateProfile();
        profile.RetrainCredits7 = 1;

        MasteryRules.ShouldUseRetrainCredit(profile, 2, false, true).Should().BeFalse();
    }

    [Test]
    public void ShouldUseRetrainCredit_TierFive_ReturnsFalseEvenWithCreditsAvailable()
    {
        var profile = CreateProfile();
        profile.RetrainCredits7 = 1;
        profile.RetrainCredits14 = 1;

        MasteryRules.ShouldUseRetrainCredit(profile, 5, false, false).Should().BeFalse();
    }

    [Test]
    public void AbandonThenApprove_ComputedUseRetrainCredit_ConsumesTheCreditAndAppliesTheDiscountedDuration()
    {
        // Simulates the exact flow Fix 1 wires up: a character abandons a tier (earning a
        // retrain credit), then a later approval must automatically spend that credit and
        // get the discounted duration - nothing should ever hardcode useRetrainCredit:false.
        var profile = CreateProfile(lifetimeLevelsTrained: 5);
        profile.Masteries["mastery-a"] = new PlayerMasteryLevel
        {
            Tier = 1,
            TierHistory = { new MasteryTierRecord { Tier = 1, Source = MasteryTrainingSource.QuickSlot } }
        };

        MasteryRules.Abandon(profile, "mastery-a", 1, new MasteryActor("Staffer", "cdkey1"), "abandoned", UtcNow);
        profile.RetrainCredits7.Should().Be(1);

        var useQuickSlot = false;
        var isInstant = false;
        var targetTier = 1;
        var useRetrainCredit = MasteryRules.ShouldUseRetrainCredit(profile, targetTier, useQuickSlot, isInstant);
        useRetrainCredit.Should().BeTrue();

        var entry = MasteryRules.EnqueueTraining(profile, "mastery-a", targetTier, useQuickSlot, useRetrainCredit, isInstant,
            new MasteryActor("Staffer", "cdkey1"), "Approved", "request-1", UtcNow);

        entry.Source.Should().Be(MasteryTrainingSource.Retrain7);
        entry.DurationDays.Should().Be(7);
        profile.RetrainCredits7.Should().Be(0);
    }

    [Test]
    public void AbandonThenApprove_BothSevenAndFourteenDayCreditsAvailable_PrefersTheSevenDayCredit()
    {
        var profile = CreateProfile(lifetimeLevelsTrained: 5);
        profile.RetrainCredits7 = 1;
        profile.RetrainCredits14 = 1;

        var useRetrainCredit = MasteryRules.ShouldUseRetrainCredit(profile, 1, false, false);
        var entry = MasteryRules.EnqueueTraining(profile, "mastery-a", 1, false, useRetrainCredit, false,
            new MasteryActor("Staffer", "cdkey1"), "Approved", "request-1", UtcNow);

        entry.Source.Should().Be(MasteryTrainingSource.Retrain7);
        entry.DurationDays.Should().Be(7);
        profile.RetrainCredits7.Should().Be(0);
        profile.RetrainCredits14.Should().Be(1);
    }

    #endregion

    #region Level totals / 17-level cap

    [Test]
    public void GetEarnedLevelTotal_SumsTierAcrossEveryOwnedMastery()
    {
        var profile = CreateProfile();
        profile.Masteries["a"] = new PlayerMasteryLevel { Tier = 3 };
        profile.Masteries["b"] = new PlayerMasteryLevel { Tier = 4 };

        MasteryRules.GetEarnedLevelTotal(profile).Should().Be(7);
    }

    [Test]
    public void GetProjectedLevelTotal_AddsOnePerQueuedEntry()
    {
        var profile = CreateProfile();
        profile.Masteries["a"] = new PlayerMasteryLevel { Tier = 3 };
        profile.TrainingQueue.Add(new MasteryTrainingEntry { MasteryId = "a", TargetTier = 4 });
        profile.TrainingQueue.Add(new MasteryTrainingEntry { MasteryId = "b", TargetTier = 1 });

        MasteryRules.GetProjectedLevelTotal(profile).Should().Be(5);
    }

    [Test]
    public void ValidateRequest_ProjectedTotalPlusOneExceedsCap_ReturnsLevelCapWarning()
    {
        var profile = CreateProfile();
        profile.Masteries["a"] = new PlayerMasteryLevel { Tier = 5 };
        profile.Masteries["b"] = new PlayerMasteryLevel { Tier = 5 };
        profile.Masteries["c"] = new PlayerMasteryLevel { Tier = 5 };
        profile.Masteries["d"] = new PlayerMasteryLevel { Tier = 2 };
        // 5 + 5 + 5 + 2 = 17 already at the cap; one more level should warn.

        var mastery = CreateMastery(name: "New Mastery");

        var violations = MasteryRules.ValidateRequest(profile, null, mastery, 1, UtcNow.AddDays(-30), UtcNow, null);

        violations.Should().Contain(v => v.RuleType == MasteryRuleType.LevelCap && !v.IsBlocking);
    }

    [Test]
    public void ValidateRequest_WellUnderCap_DoesNotReturnLevelCapWarning()
    {
        var profile = CreateProfile();
        profile.Masteries["a"] = new PlayerMasteryLevel { Tier = 2 };
        var mastery = CreateMastery(name: "New Mastery");

        var violations = MasteryRules.ValidateRequest(profile, null, mastery, 1, UtcNow.AddDays(-30), UtcNow, null);

        violations.Should().NotContain(v => v.RuleType == MasteryRuleType.LevelCap);
    }

    [Test]
    public void GetProjectedLevelTotal_QueuedEntryOverridesMultipleTiersPastCurrent_ProjectsTheTargetTierNotJustPlusOne()
    {
        // An overridden tier-progression jump can queue an entry more than one tier past
        // the character's current tier for that mastery - e.g. 14 earned levels elsewhere
        // plus a tier-4 entry queued for a brand new mastery actually completes at 18, not
        // "14 + 1 queued entry = 15". Counting queued entries instead of their target tier
        // would silently let this bypass the cap warning.
        var profile = CreateProfile();
        profile.Masteries["a"] = new PlayerMasteryLevel { Tier = 5 };
        profile.Masteries["b"] = new PlayerMasteryLevel { Tier = 5 };
        profile.Masteries["c"] = new PlayerMasteryLevel { Tier = 4 };
        profile.TrainingQueue.Add(new MasteryTrainingEntry { MasteryId = "new-mastery", TargetTier = 4 });

        MasteryRules.GetProjectedLevelTotal(profile).Should().Be(18);
    }

    [Test]
    public void ValidateRequest_ProspectiveRequestWouldOverrideJumpPastCap_ReturnsLevelCapWarning()
    {
        var profile = CreateProfile();
        profile.Masteries["a"] = new PlayerMasteryLevel { Tier = 5 };
        profile.Masteries["b"] = new PlayerMasteryLevel { Tier = 5 };
        profile.Masteries["c"] = new PlayerMasteryLevel { Tier = 4 };
        // 5 + 5 + 4 = 14 earned. A prospective tier-4 request for a brand new mastery
        // projects to 18, which must warn even though it's a single request/entry.
        var mastery = CreateMastery(name: "New Mastery");
        mastery.Id = "new-mastery";

        var violations = MasteryRules.ValidateRequest(profile, null, mastery, 4, UtcNow.AddDays(-30), UtcNow, null);

        violations.Should().Contain(v => v.RuleType == MasteryRuleType.LevelCap && !v.IsBlocking);
    }

    #endregion

    #region Target tier range (blocking)

    [Test]
    public void ValidateRequest_TargetTierZero_ReturnsBlockingInvalidTierViolation()
    {
        var profile = CreateProfile();
        var mastery = CreateMastery(name: "Ranged Mastery");

        var violations = MasteryRules.ValidateRequest(profile, null, mastery, 0, UtcNow.AddDays(-30), UtcNow, null);

        violations.Should().Contain(v => v.RuleType == MasteryRuleType.InvalidTier && v.IsBlocking);
    }

    [Test]
    public void ValidateRequest_TargetTierSix_ReturnsBlockingInvalidTierViolation()
    {
        var profile = CreateProfile();
        var mastery = CreateMastery(name: "Ranged Mastery");
        mastery.Id = "ranged-mastery";
        profile.Masteries["ranged-mastery"] = new PlayerMasteryLevel { Tier = 5 };

        var violations = MasteryRules.ValidateRequest(profile, null, mastery, 6, UtcNow.AddDays(-30), UtcNow, null);

        violations.Should().Contain(v => v.RuleType == MasteryRuleType.InvalidTier && v.IsBlocking);
    }

    [Test]
    public void ValidateRequest_TargetTierWithinOneToFive_NeverReturnsInvalidTierViolation()
    {
        var profile = CreateProfile();
        var mastery = CreateMastery(name: "Ranged Mastery");

        for (var tier = 1; tier <= 5; tier++)
        {
            var violations = MasteryRules.ValidateRequest(profile, null, mastery, tier, UtcNow.AddDays(-30), UtcNow, null);
            violations.Should().NotContain(v => v.RuleType == MasteryRuleType.InvalidTier);
        }
    }

    #endregion

    #region CanReviewRequest (only Pending/InReview may be approved or denied)

    [Test]
    public void CanReviewRequest_PendingOrInReview_ReturnsTrue()
    {
        MasteryRules.CanReviewRequest(MasteryRequestStatus.Pending).Should().BeTrue();
        MasteryRules.CanReviewRequest(MasteryRequestStatus.InReview).Should().BeTrue();
    }

    [Test]
    public void CanReviewRequest_Cancelled_ReturnsFalse()
    {
        // A stale staff window must never be able to resurrect a request the player
        // already cancelled - approving/denying it would enqueue unwanted training or
        // reverse the player's own decision.
        MasteryRules.CanReviewRequest(MasteryRequestStatus.Cancelled).Should().BeFalse();
    }

    [Test]
    public void CanReviewRequest_AlreadyApprovedOrDenied_ReturnsFalse()
    {
        MasteryRules.CanReviewRequest(MasteryRequestStatus.Approved).Should().BeFalse();
        MasteryRules.CanReviewRequest(MasteryRequestStatus.Denied).Should().BeFalse();
    }

    #endregion

    #region Queue cap

    [Test]
    public void ValidateRequest_QueueAtMax_ReturnsQueueFullWarning()
    {
        var profile = CreateProfile();
        profile.TrainingQueue.Add(new MasteryTrainingEntry { MasteryId = "a" });
        profile.TrainingQueue.Add(new MasteryTrainingEntry { MasteryId = "b" });
        profile.TrainingQueue.Add(new MasteryTrainingEntry { MasteryId = "c" });
        var mastery = CreateMastery();

        var violations = MasteryRules.ValidateRequest(profile, null, mastery, 1, UtcNow.AddDays(-30), UtcNow, null);

        violations.Should().Contain(v => v.RuleType == MasteryRuleType.QueueFull && !v.IsBlocking);
    }

    [Test]
    public void ValidateRequest_QueueUnderMax_DoesNotReturnQueueFullWarning()
    {
        var profile = CreateProfile();
        profile.TrainingQueue.Add(new MasteryTrainingEntry { MasteryId = "a" });
        var mastery = CreateMastery();

        var violations = MasteryRules.ValidateRequest(profile, null, mastery, 1, UtcNow.AddDays(-30), UtcNow, null);

        violations.Should().NotContain(v => v.RuleType == MasteryRuleType.QueueFull);
    }

    #endregion

    #region Single Tier-5 rule

    [Test]
    public void ValidateRequest_RequestingTierFiveWhileAnotherMasteryIsAlreadyTierFive_ReturnsTier5ConflictWarning()
    {
        var profile = CreateProfile();
        profile.Masteries["other-mastery"] = new PlayerMasteryLevel { Tier = 5 };
        var mastery = CreateMastery(name: "This Mastery");
        mastery.Id = "this-mastery";

        var violations = MasteryRules.ValidateRequest(profile, null, mastery, 5, UtcNow.AddDays(-30), UtcNow, null);

        violations.Should().Contain(v => v.RuleType == MasteryRuleType.Tier5Conflict && !v.IsBlocking);
    }

    [Test]
    public void ValidateRequest_RequestingTierFiveWithNoOtherTierFiveMastery_DoesNotReturnTier5Conflict()
    {
        var profile = CreateProfile();
        profile.Masteries["this-mastery"] = new PlayerMasteryLevel { Tier = 4 };
        var mastery = CreateMastery(name: "This Mastery");
        mastery.Id = "this-mastery";

        var violations = MasteryRules.ValidateRequest(profile, null, mastery, 5, UtcNow.AddDays(-30), UtcNow, null);

        violations.Should().NotContain(v => v.RuleType == MasteryRuleType.Tier5Conflict);
    }

    [Test]
    public void ValidateRequest_AnotherMasteryHasAQueuedTierFiveEntry_ReturnsTier5ConflictWarning()
    {
        // Fix 3(a): a tier-5 slot reserved by in-flight (not yet earned) training must
        // also be treated as taken, not just an already-earned tier 5.
        var profile = CreateProfile();
        profile.TrainingQueue.Add(new MasteryTrainingEntry { MasteryId = "other-mastery", TargetTier = 5 });

        var mastery = CreateMastery(name: "This Mastery");
        mastery.Id = "this-mastery";

        var violations = MasteryRules.ValidateRequest(profile, null, mastery, 5, UtcNow.AddDays(-30), UtcNow, null);

        violations.Should().Contain(v => v.RuleType == MasteryRuleType.Tier5Conflict && !v.IsBlocking);
    }

    [Test]
    public void ValidateRequest_OwnMasteryHasAQueuedTierFiveEntry_DoesNotCountAsConflict()
    {
        var profile = CreateProfile();
        profile.TrainingQueue.Add(new MasteryTrainingEntry { MasteryId = "this-mastery", TargetTier = 5 });

        var mastery = CreateMastery(name: "This Mastery");
        mastery.Id = "this-mastery";

        var violations = MasteryRules.ValidateRequest(profile, null, mastery, 5, UtcNow.AddDays(-30), UtcNow, null);

        violations.Should().NotContain(v => v.RuleType == MasteryRuleType.Tier5Conflict);
    }

    #endregion

    #region Single Rare rule

    [Test]
    public void ValidateRequest_RareMasteryWhileAnotherRareAlreadyOwned_ReturnsRareConflictWarning()
    {
        var profile = CreateProfile();
        var existingRare = CreateMastery(rarity: MasteryRarityType.Rare, name: "Existing Rare");
        existingRare.Id = "existing-rare";
        profile.Masteries["existing-rare"] = new PlayerMasteryLevel { Tier = 2 };

        var newRare = CreateMastery(rarity: MasteryRarityType.Rare, name: "New Rare");
        newRare.Id = "new-rare";

        var ownedCatalog = new Dictionary<string, Mastery> { ["existing-rare"] = existingRare };

        var violations = MasteryRules.ValidateRequest(profile, ownedCatalog, newRare, 1, UtcNow.AddDays(-30), UtcNow, null);

        violations.Should().Contain(v => v.RuleType == MasteryRuleType.RareConflict && !v.IsBlocking);
    }

    [Test]
    public void ValidateRequest_RareMasteryWithNoOtherRareOwned_DoesNotReturnRareConflict()
    {
        var profile = CreateProfile();
        var newRare = CreateMastery(rarity: MasteryRarityType.Rare, name: "New Rare");
        newRare.Id = "new-rare";

        var violations = MasteryRules.ValidateRequest(profile, null, newRare, 1, UtcNow.AddDays(-30), UtcNow, null);

        violations.Should().NotContain(v => v.RuleType == MasteryRuleType.RareConflict);
    }

    [Test]
    public void ValidateRequest_RareRankUpOfAlreadyOwnedRare_DoesNotCountAsConflict()
    {
        var profile = CreateProfile();
        var owned = CreateMastery(rarity: MasteryRarityType.Rare, name: "Owned Rare");
        owned.Id = "owned-rare";
        profile.Masteries["owned-rare"] = new PlayerMasteryLevel { Tier = 1 };

        var ownedCatalog = new Dictionary<string, Mastery> { ["owned-rare"] = owned };

        var violations = MasteryRules.ValidateRequest(profile, ownedCatalog, owned, 2, UtcNow.AddDays(-30), UtcNow, null);

        violations.Should().NotContain(v => v.RuleType == MasteryRuleType.RareConflict);
    }

    [Test]
    public void ValidateRequest_OwnedCatalogIncludesAQueuedNotYetEarnedRareMastery_ReturnsRareConflictWarning()
    {
        // Fix 3(b): Mastery.GetOwnedMasteryCatalog now includes catalog entries for
        // masteries with a queued/active training entry, not just already-earned ones -
        // this proves ValidateRequest correctly flags a conflict once such an entry is
        // present in the owned-catalog lookup it's given, matching a Rare slot reserved
        // by in-flight (not yet earned) training.
        var profile = CreateProfile();
        profile.TrainingQueue.Add(new MasteryTrainingEntry { MasteryId = "queued-rare", TargetTier = 1 });

        var queuedRare = CreateMastery(rarity: MasteryRarityType.Rare, name: "Queued Rare");
        queuedRare.Id = "queued-rare";

        var newRare = CreateMastery(rarity: MasteryRarityType.Rare, name: "New Rare");
        newRare.Id = "new-rare";

        var ownedCatalog = new Dictionary<string, Mastery> { ["queued-rare"] = queuedRare };

        var violations = MasteryRules.ValidateRequest(profile, ownedCatalog, newRare, 1, UtcNow.AddDays(-30), UtcNow, null);

        violations.Should().Contain(v => v.RuleType == MasteryRuleType.RareConflict && !v.IsBlocking);
    }

    #endregion

    #region Off-limit blocking

    [Test]
    public void ValidateRequest_OffLimitMastery_ReturnsBlockingViolation()
    {
        var profile = CreateProfile();
        var mastery = CreateMastery(rarity: MasteryRarityType.OffLimit);

        var violations = MasteryRules.ValidateRequest(profile, null, mastery, 1, UtcNow.AddDays(-30), UtcNow, null);

        violations.Should().Contain(v => v.RuleType == MasteryRuleType.OffLimit && v.IsBlocking);
    }

    [Test]
    public void ValidateRequest_EveryNonOffLimitViolation_IsNotBlocking()
    {
        var profile = CreateProfile();
        profile.TrainingQueue.Add(new MasteryTrainingEntry());
        profile.TrainingQueue.Add(new MasteryTrainingEntry());
        profile.TrainingQueue.Add(new MasteryTrainingEntry());
        var mastery = CreateMastery(skill: SkillType.Lightsaber);

        var violations = MasteryRules.ValidateRequest(profile, null, mastery, 5, UtcNow, UtcNow, 0);

        violations.Where(v => v.RuleType != MasteryRuleType.OffLimit).Should().OnlyContain(v => !v.IsBlocking);
    }

    #endregion

    #region Tier progression (current + 1 only)

    [Test]
    public void ValidateRequest_TargetTierNotCurrentPlusOne_ReturnsTierProgressionWarning()
    {
        var profile = CreateProfile();
        var mastery = CreateMastery(name: "Progression Mastery");
        mastery.Id = "progression-mastery";
        profile.Masteries["progression-mastery"] = new PlayerMasteryLevel { Tier = 1 };

        var violations = MasteryRules.ValidateRequest(profile, null, mastery, 3, UtcNow.AddDays(-30), UtcNow, null);

        violations.Should().Contain(v => v.RuleType == MasteryRuleType.TierProgression && !v.IsBlocking);
    }

    [Test]
    public void ValidateRequest_TargetTierIsCurrentPlusOne_DoesNotReturnTierProgressionViolation()
    {
        var profile = CreateProfile();
        var mastery = CreateMastery(name: "Progression Mastery");
        mastery.Id = "progression-mastery";
        profile.Masteries["progression-mastery"] = new PlayerMasteryLevel { Tier = 1 };

        var violations = MasteryRules.ValidateRequest(profile, null, mastery, 2, UtcNow.AddDays(-30), UtcNow, null);

        violations.Should().NotContain(v => v.RuleType == MasteryRuleType.TierProgression);
    }

    [Test]
    public void ValidateRequest_NewMasteryRequestingTierOne_DoesNotReturnTierProgressionViolation()
    {
        var profile = CreateProfile();
        var mastery = CreateMastery(name: "Brand New Mastery");
        mastery.Id = "brand-new";

        var violations = MasteryRules.ValidateRequest(profile, null, mastery, 1, UtcNow.AddDays(-30), UtcNow, null);

        violations.Should().NotContain(v => v.RuleType == MasteryRuleType.TierProgression);
    }

    #endregion

    #region Eligibility: age, skill rank, null skill

    [Test]
    public void ValidateRequest_CharacterYoungerThan14Days_ReturnsCharacterAgeWarning()
    {
        var profile = CreateProfile();
        var mastery = CreateMastery();

        var violations = MasteryRules.ValidateRequest(profile, null, mastery, 1, UtcNow.AddDays(-5), UtcNow, null);

        violations.Should().Contain(v => v.RuleType == MasteryRuleType.CharacterAge && !v.IsBlocking);
    }

    [Test]
    public void ValidateRequest_CharacterAtLeast14DaysOld_DoesNotReturnCharacterAgeViolation()
    {
        var profile = CreateProfile();
        var mastery = CreateMastery();

        var violations = MasteryRules.ValidateRequest(profile, null, mastery, 1, UtcNow.AddDays(-14), UtcNow, null);

        violations.Should().NotContain(v => v.RuleType == MasteryRuleType.CharacterAge);
    }

    [Test]
    public void ValidateRequest_AssociatedSkillBelowRank50_ReturnsSkillRankWarning()
    {
        var profile = CreateProfile();
        var mastery = CreateMastery(skill: SkillType.Lightsaber);

        var violations = MasteryRules.ValidateRequest(profile, null, mastery, 1, UtcNow.AddDays(-30), UtcNow, 25);

        violations.Should().Contain(v => v.RuleType == MasteryRuleType.SkillRank && !v.IsBlocking);
    }

    [Test]
    public void ValidateRequest_AssociatedSkillAtRank50_DoesNotReturnSkillRankViolation()
    {
        var profile = CreateProfile();
        var mastery = CreateMastery(skill: SkillType.Lightsaber);

        var violations = MasteryRules.ValidateRequest(profile, null, mastery, 1, UtcNow.AddDays(-30), UtcNow, 50);

        violations.Should().NotContain(v => v.RuleType == MasteryRuleType.SkillRank);
    }

    [Test]
    public void ValidateRequest_NoAssociatedSkill_NeverReturnsSkillRankViolationRegardlessOfNullRank()
    {
        var profile = CreateProfile();
        var mastery = CreateMastery(skill: null);

        var violations = MasteryRules.ValidateRequest(profile, null, mastery, 1, UtcNow.AddDays(-30), UtcNow, null);

        violations.Should().NotContain(v => v.RuleType == MasteryRuleType.SkillRank);
    }

    #endregion

    #region Queue evaluation

    [Test]
    public void EvaluateTrainingQueue_EntryNotYetFinished_DoesNotComplete()
    {
        var profile = CreateProfile();
        profile.TrainingQueue.Add(new MasteryTrainingEntry
        {
            MasteryId = "a",
            TargetTier = 1,
            StartDate = UtcNow,
            DurationDays = 14
        });

        var completed = MasteryRules.EvaluateTrainingQueue(profile, UtcNow.AddDays(5));

        completed.Should().BeEmpty();
        profile.TrainingQueue.Should().ContainSingle();
        profile.Masteries.Should().BeEmpty();
    }

    [Test]
    public void EvaluateTrainingQueue_SingleFinishedEntry_GrantsTierAndClearsQueue()
    {
        var profile = CreateProfile();
        var start = UtcNow;
        profile.TrainingQueue.Add(new MasteryTrainingEntry
        {
            MasteryId = "mastery-a",
            TargetTier = 1,
            StartDate = start,
            DurationDays = 14,
            Source = MasteryTrainingSource.Standard14
        });

        var completed = MasteryRules.EvaluateTrainingQueue(profile, start.AddDays(14));

        completed.Should().ContainSingle();
        profile.TrainingQueue.Should().BeEmpty();
        profile.Masteries.Should().ContainKey("mastery-a");
        profile.Masteries["mastery-a"].Tier.Should().Be(1);
        profile.LifetimeLevelsTrained.Should().Be(1);
    }

    [Test]
    public void EvaluateTrainingQueue_MultipleEntriesCompleteInOnePass_NextStartsAtPreviousFinishNotNow()
    {
        var profile = CreateProfile();
        var start = UtcNow;

        // Entry 1: 14 days. Entry 2 queued right after (start date will be rebased as
        // entry 1 completes) for another 14 days. Both should complete in a single
        // evaluation performed well after both would have finished.
        profile.TrainingQueue.Add(new MasteryTrainingEntry
        {
            MasteryId = "mastery-a",
            TargetTier = 1,
            StartDate = start,
            DurationDays = 14,
            Source = MasteryTrainingSource.Standard14
        });
        profile.TrainingQueue.Add(new MasteryTrainingEntry
        {
            MasteryId = "mastery-b",
            TargetTier = 1,
            StartDate = start, // stale/tentative - EvaluateTrainingQueue must rebase this
            DurationDays = 14,
            Source = MasteryTrainingSource.Standard21
        });

        var completed = MasteryRules.EvaluateTrainingQueue(profile, start.AddDays(60));

        completed.Should().HaveCount(2);
        profile.TrainingQueue.Should().BeEmpty();
        profile.Masteries.Should().ContainKeys("mastery-a", "mastery-b");

        // Entry B's granted date must be entry A's finish date (start + 14), not "now".
        var entryBFinish = profile.Masteries["mastery-b"].TierHistory.Single().DateEarned;
        entryBFinish.Should().Be(start.AddDays(14).AddDays(14));
        profile.LifetimeLevelsTrained.Should().Be(2);
    }

    [Test]
    public void EvaluateTrainingQueue_ReductionDaysShortenTheActiveEntry()
    {
        var profile = CreateProfile();
        var start = UtcNow;
        profile.TrainingQueue.Add(new MasteryTrainingEntry
        {
            MasteryId = "mastery-a",
            TargetTier = 1,
            StartDate = start,
            DurationDays = 14,
            ReductionDays = 7,
            Source = MasteryTrainingSource.Standard14
        });

        // Without the reduction this would not yet be finished (only 10 of 14 days).
        var completed = MasteryRules.EvaluateTrainingQueue(profile, start.AddDays(10));

        completed.Should().ContainSingle();
    }

    #endregion

    #region Retrain credits (all three 7-day conditions) + Quick Slot refund

    [Test]
    public void Abandon_TierTrainedViaQuickSlot_GrantsSevenDayCreditAndRefundsQuickSlot()
    {
        var profile = CreateProfile();
        profile.Masteries["mastery-a"] = new PlayerMasteryLevel
        {
            Tier = 1,
            TierHistory = { new MasteryTierRecord { Tier = 1, Source = MasteryTrainingSource.QuickSlot } }
        };

        var result = MasteryRules.Abandon(profile, "mastery-a", 1, new MasteryActor("Staffer", "cdkey1"), "test", UtcNow);

        result.Should().BeTrue();
        profile.RetrainCredits7.Should().Be(1);
        profile.RetrainCredits14.Should().Be(0);
        profile.QuickSlotsAvailable.Should().Be(1);
    }

    [Test]
    public void Abandon_FirstEverLevel_GrantsSevenDayCredit()
    {
        var profile = CreateProfile();
        profile.Masteries["mastery-a"] = new PlayerMasteryLevel
        {
            Tier = 1,
            TierHistory = { new MasteryTierRecord { Tier = 1, Source = MasteryTrainingSource.Standard14 } }
        };

        MasteryRules.Abandon(profile, "mastery-a", 1, new MasteryActor("Staffer", "cdkey1"), "test", UtcNow);

        profile.RetrainCredits7.Should().Be(1);
        profile.RetrainCredits14.Should().Be(0);
    }

    [Test]
    public void Abandon_SecondEverLevel_GrantsSevenDayCredit()
    {
        var profile = CreateProfile();
        profile.Masteries["mastery-a"] = new PlayerMasteryLevel
        {
            Tier = 2,
            TierHistory =
            {
                new MasteryTierRecord { Tier = 1, Source = MasteryTrainingSource.Standard14 },
                new MasteryTierRecord { Tier = 2, Source = MasteryTrainingSource.Standard21 }
            }
        };

        MasteryRules.Abandon(profile, "mastery-a", 2, new MasteryActor("Staffer", "cdkey1"), "test", UtcNow);

        profile.RetrainCredits7.Should().Be(1);
        profile.RetrainCredits14.Should().Be(0);
    }

    [Test]
    public void Abandon_InstantGrantedTier_GrantsSevenDayCredit()
    {
        var profile = CreateProfile();
        profile.Masteries["mastery-a"] = new PlayerMasteryLevel
        {
            Tier = 1,
            TierHistory = { new MasteryTierRecord { Tier = 1, Source = MasteryTrainingSource.Instant } }
        };

        MasteryRules.Abandon(profile, "mastery-a", 1, new MasteryActor("Staffer", "cdkey1"), "test", UtcNow);

        profile.RetrainCredits7.Should().Be(1);
        profile.RetrainCredits14.Should().Be(0);
    }

    [Test]
    public void Abandon_ThirdOrLaterStandardLevel_GrantsFourteenDayCreditInstead()
    {
        var profile = CreateProfile();
        profile.Masteries["mastery-a"] = new PlayerMasteryLevel
        {
            Tier = 3,
            TierHistory =
            {
                new MasteryTierRecord { Tier = 1, Source = MasteryTrainingSource.Standard14 },
                new MasteryTierRecord { Tier = 2, Source = MasteryTrainingSource.Standard21 },
                new MasteryTierRecord { Tier = 3, Source = MasteryTrainingSource.Standard28 }
            }
        };

        MasteryRules.Abandon(profile, "mastery-a", 3, new MasteryActor("Staffer", "cdkey1"), "test", UtcNow);

        profile.RetrainCredits14.Should().Be(1);
        profile.RetrainCredits7.Should().Be(0);
        profile.QuickSlotsAvailable.Should().Be(0);
    }

    [Test]
    public void Abandon_TierOne_RemovesMasteryEntirely()
    {
        var profile = CreateProfile();
        profile.Masteries["mastery-a"] = new PlayerMasteryLevel
        {
            Tier = 1,
            TierHistory = { new MasteryTierRecord { Tier = 1, Source = MasteryTrainingSource.Standard28 } }
        };

        MasteryRules.Abandon(profile, "mastery-a", 1, new MasteryActor("Staffer", "cdkey1"), "test", UtcNow);

        profile.Masteries.Should().NotContainKey("mastery-a");
    }

    [Test]
    public void Abandon_HigherTier_DecrementsTierButKeepsMastery()
    {
        var profile = CreateProfile();
        profile.Masteries["mastery-a"] = new PlayerMasteryLevel
        {
            Tier = 3,
            TierHistory =
            {
                new MasteryTierRecord { Tier = 1, Source = MasteryTrainingSource.Standard14 },
                new MasteryTierRecord { Tier = 2, Source = MasteryTrainingSource.Standard21 },
                new MasteryTierRecord { Tier = 3, Source = MasteryTrainingSource.Standard28 }
            }
        };

        MasteryRules.Abandon(profile, "mastery-a", 3, new MasteryActor("Staffer", "cdkey1"), "test", UtcNow);

        profile.Masteries.Should().ContainKey("mastery-a");
        profile.Masteries["mastery-a"].Tier.Should().Be(2);
    }

    [Test]
    public void Abandon_DuplicateTierHistoryRecordsForTheSameTier_OnlyRemovesTheMostRecentOne()
    {
        // Fix 8: a character can legitimately re-train the same tier more than once
        // (e.g. abandon then retrain back up to it) - RemoveAll would wipe every
        // duplicate-tier record instead of just the one actually being abandoned now.
        var profile = CreateProfile();
        var firstRecord = new MasteryTierRecord { Tier = 2, DateEarned = UtcNow.AddDays(-30), Source = MasteryTrainingSource.Standard21 };
        var secondRecord = new MasteryTierRecord { Tier = 2, DateEarned = UtcNow, Source = MasteryTrainingSource.QuickSlot };

        profile.Masteries["mastery-a"] = new PlayerMasteryLevel
        {
            Tier = 2,
            TierHistory = { new MasteryTierRecord { Tier = 1, Source = MasteryTrainingSource.Standard14 }, firstRecord, secondRecord }
        };

        MasteryRules.Abandon(profile, "mastery-a", 2, new MasteryActor("Staffer", "cdkey1"), "test", UtcNow);

        // Tier drops back to 1, and only the most recent tier-2 record (secondRecord) is
        // removed - the earlier tier-2 record and the tier-1 record both survive.
        profile.Masteries["mastery-a"].TierHistory.Should().HaveCount(2);
        profile.Masteries["mastery-a"].TierHistory.Should().Contain(firstRecord);
        profile.Masteries["mastery-a"].TierHistory.Should().NotContain(secondRecord);

        // The refund should be based on the removed (most recent, Quick-Slotted) record.
        profile.QuickSlotsAvailable.Should().Be(1);
        profile.RetrainCredits7.Should().Be(1);
    }

    [Test]
    public void Abandon_TierDoesNotMatchCurrentTier_ReturnsFalseAndDoesNotMutate()
    {
        var profile = CreateProfile();
        profile.Masteries["mastery-a"] = new PlayerMasteryLevel
        {
            Tier = 2,
            TierHistory = { new MasteryTierRecord { Tier = 2, Source = MasteryTrainingSource.Standard21 } }
        };

        var result = MasteryRules.Abandon(profile, "mastery-a", 1, new MasteryActor("Staffer", "cdkey1"), "test", UtcNow);

        result.Should().BeFalse();
        profile.Masteries["mastery-a"].Tier.Should().Be(2);
        profile.RetrainCredits7.Should().Be(0);
        profile.RetrainCredits14.Should().Be(0);
    }

    #endregion

    #region Audit entries on staff actions

    [Test]
    public void Abandon_AppendsAuditEntryWithActorAndReason()
    {
        var profile = CreateProfile();
        profile.Masteries["mastery-a"] = new PlayerMasteryLevel
        {
            Tier = 1,
            TierHistory = { new MasteryTierRecord { Tier = 1, Source = MasteryTrainingSource.Standard28 } }
        };

        MasteryRules.Abandon(profile, "mastery-a", 1, new MasteryActor("Staffer Name", "STAFF_CDKEY"), "Because reasons", UtcNow, "Revoke");

        profile.AuditLog.Should().ContainSingle();
        var entry = profile.AuditLog[0];
        entry.ActorName.Should().Be("Staffer Name");
        entry.ActorCDKey.Should().Be("STAFF_CDKEY");
        entry.Action.Should().Be("Revoke");
        entry.Reason.Should().Be("Because reasons");
        entry.Date.Should().Be(UtcNow);
    }

    [Test]
    public void GrantMastery_SetsAnyTierBypassingProgressionAndAppendsAudit()
    {
        var profile = CreateProfile();

        MasteryRules.GrantMastery(profile, "mastery-a", 4, new MasteryActor("Staffer", "cdkey1"), "Direct grant", UtcNow);

        profile.Masteries["mastery-a"].Tier.Should().Be(4);
        profile.AuditLog.Should().ContainSingle(e => e.Action == "Grant" && e.Reason == "Direct grant");
    }

    [Test]
    public void ReduceActiveTrainingTime_IncreasesReductionDaysOnActiveEntryAndAppendsAudit()
    {
        var profile = CreateProfile();
        profile.TrainingQueue.Add(new MasteryTrainingEntry { MasteryId = "a", DurationDays = 28, ReductionDays = 0 });

        var result = MasteryRules.ReduceActiveTrainingTime(profile, 5, new MasteryActor("Staffer", "cdkey1"), "Event participation", UtcNow);

        result.Should().BeTrue();
        profile.TrainingQueue[0].ReductionDays.Should().Be(5);
        profile.AuditLog.Should().ContainSingle(e => e.Action == "Reduce" && e.Reason == "Event participation");
    }

    [Test]
    public void ReduceActiveTrainingTime_EmptyQueue_ReturnsFalseAndDoesNotAppendAudit()
    {
        var profile = CreateProfile();

        var result = MasteryRules.ReduceActiveTrainingTime(profile, 5, new MasteryActor("Staffer", "cdkey1"), "reason", UtcNow);

        result.Should().BeFalse();
        profile.AuditLog.Should().BeEmpty();
    }

    [Test]
    public void ReduceActiveTrainingTime_ZeroDays_ReturnsFalseAndDoesNotAppendAudit()
    {
        var profile = CreateProfile();
        profile.TrainingQueue.Add(new MasteryTrainingEntry { MasteryId = "a", DurationDays = 28, ReductionDays = 0 });

        var result = MasteryRules.ReduceActiveTrainingTime(profile, 0, new MasteryActor("Staffer", "cdkey1"), "reason", UtcNow);

        result.Should().BeFalse();
        profile.TrainingQueue[0].ReductionDays.Should().Be(0);
        profile.AuditLog.Should().BeEmpty();
    }

    [Test]
    public void ReduceActiveTrainingTime_NegativeDays_ReturnsFalseAndDoesNotExtendTraining()
    {
        // A negative reduction would otherwise extend training instead of shortening it.
        var profile = CreateProfile();
        profile.TrainingQueue.Add(new MasteryTrainingEntry { MasteryId = "a", DurationDays = 28, ReductionDays = 0 });

        var result = MasteryRules.ReduceActiveTrainingTime(profile, -5, new MasteryActor("Staffer", "cdkey1"), "reason", UtcNow);

        result.Should().BeFalse();
        profile.TrainingQueue[0].ReductionDays.Should().Be(0);
        profile.AuditLog.Should().BeEmpty();
    }

    [Test]
    public void ReduceActiveTrainingTime_ReductionExceedsRemainingDuration_ClampsSoFinishNeverPrecedesStartDate()
    {
        var profile = CreateProfile();
        profile.TrainingQueue.Add(new MasteryTrainingEntry { MasteryId = "a", StartDate = UtcNow, DurationDays = 10, ReductionDays = 5 });
        // Only 5 days remain (10 - 5) - requesting a 20-day reduction must clamp to 5, not
        // push ReductionDays past DurationDays and land the finish date before StartDate.

        var result = MasteryRules.ReduceActiveTrainingTime(profile, 20, new MasteryActor("Staffer", "cdkey1"), "reason", UtcNow);

        result.Should().BeTrue();
        profile.TrainingQueue[0].ReductionDays.Should().Be(10);
        var finish = profile.TrainingQueue[0].StartDate.AddDays(
            profile.TrainingQueue[0].DurationDays - profile.TrainingQueue[0].ReductionDays);
        finish.Should().Be(profile.TrainingQueue[0].StartDate);
    }

    [Test]
    public void ReduceActiveTrainingTime_NoRemainingDuration_ReturnsFalseAndDoesNotAppendAudit()
    {
        var profile = CreateProfile();
        profile.TrainingQueue.Add(new MasteryTrainingEntry { MasteryId = "a", StartDate = UtcNow, DurationDays = 10, ReductionDays = 10 });

        var result = MasteryRules.ReduceActiveTrainingTime(profile, 3, new MasteryActor("Staffer", "cdkey1"), "reason", UtcNow);

        result.Should().BeFalse();
        profile.AuditLog.Should().BeEmpty();
    }

    [Test]
    public void AwardQuickSlot_IncrementsAvailableSlotsAndAppendsAudit()
    {
        var profile = CreateProfile();

        MasteryRules.AwardQuickSlot(profile, new MasteryActor("Staffer", "cdkey1"), "Event reward", UtcNow);

        profile.QuickSlotsAvailable.Should().Be(1);
        profile.AuditLog.Should().ContainSingle(e => e.Action == "QuickSlotAward" && e.Reason == "Event reward");
    }

    [Test]
    public void EnqueueTraining_ApprovingWithQuickSlot_SpendsSlotAndAppendsBothAuditEntries()
    {
        var profile = CreateProfile(lifetimeLevelsTrained: 5);
        profile.QuickSlotsAvailable = 1;

        var entry = MasteryRules.EnqueueTraining(profile, "mastery-a", 1, true, false, false, new MasteryActor("Staffer", "cdkey1"), "Approved", "request-1", UtcNow);

        entry.Source.Should().Be(MasteryTrainingSource.QuickSlot);
        entry.DurationDays.Should().Be(7);
        profile.QuickSlotsAvailable.Should().Be(0);
        profile.AuditLog.Should().Contain(e => e.Action == "QuickSlotSpend");
        profile.AuditLog.Should().Contain(e => e.Action == "Approve");
    }

    [Test]
    public void EnqueueTraining_SecondEntry_StartsAtFirstEntrysFinish()
    {
        var profile = CreateProfile(lifetimeLevelsTrained: 0);

        var first = MasteryRules.EnqueueTraining(profile, "mastery-a", 1, false, false, false, new MasteryActor("Staffer", "cdkey1"), "reason", "request-1", UtcNow);
        var second = MasteryRules.EnqueueTraining(profile, "mastery-b", 1, false, false, false, new MasteryActor("Staffer", "cdkey1"), "reason", "request-2", UtcNow);

        second.StartDate.Should().Be(first.StartDate.AddDays(first.DurationDays));
    }

    [Test]
    public void EnqueueTraining_ThreeBackToBackApprovals_UseSequential14_21_28DayBrackets()
    {
        // Fix: LifetimeLevelsTrained only increments on completion, so a fresh character
        // approved for three masteries before any of them complete used to get 14/14/14
        // instead of 14/21/28 - the bracket must also count levels still queued.
        var profile = CreateProfile(lifetimeLevelsTrained: 0);
        var actor = new MasteryActor("Staffer", "cdkey1");

        var first = MasteryRules.EnqueueTraining(profile, "mastery-a", 1, false, false, false, actor, "reason", "request-1", UtcNow);
        var second = MasteryRules.EnqueueTraining(profile, "mastery-b", 1, false, false, false, actor, "reason", "request-2", UtcNow);
        var third = MasteryRules.EnqueueTraining(profile, "mastery-c", 1, false, false, false, actor, "reason", "request-3", UtcNow);

        first.Source.Should().Be(MasteryTrainingSource.Standard14);
        first.DurationDays.Should().Be(14);
        second.Source.Should().Be(MasteryTrainingSource.Standard21);
        second.DurationDays.Should().Be(21);
        third.Source.Should().Be(MasteryTrainingSource.Standard28);
        third.DurationDays.Should().Be(28);
    }

    [Test]
    public void EnqueueTraining_CancellingASecondQueuedEntry_FreesItsBracketSlotForTheNextApproval()
    {
        // The cancelled entry never incremented LifetimeLevelsTrained (it was still
        // queued, never completed), so simply leaving the queue must free its bracket
        // slot naturally - a subsequent third approval should land in the 21d bracket
        // the cancelled entry vacated, not fall through to 28d.
        var profile = CreateProfile(lifetimeLevelsTrained: 0);
        var actor = new MasteryActor("Staffer", "cdkey1");

        var first = MasteryRules.EnqueueTraining(profile, "mastery-a", 1, false, false, false, actor, "reason", "request-1", UtcNow);
        var second = MasteryRules.EnqueueTraining(profile, "mastery-b", 1, false, false, false, actor, "reason", "request-2", UtcNow);
        first.DurationDays.Should().Be(14);
        second.DurationDays.Should().Be(21);

        MasteryRules.AbandonTrainingEntry(profile, 1, actor, "No longer wanted", UtcNow);
        profile.LifetimeLevelsTrained.Should().Be(0);

        var third = MasteryRules.EnqueueTraining(profile, "mastery-c", 1, false, false, false, actor, "reason", "request-3", UtcNow);

        third.Source.Should().Be(MasteryTrainingSource.Standard21);
        third.DurationDays.Should().Be(21);
    }

    [Test]
    public void EnqueueTraining_InstantWithNonEmptyQueue_GrantsTierNowAndLeavesQueueUntouched()
    {
        // Fix 2: an instant grant must bypass the queue entirely rather than sit behind
        // whatever is already active/queued.
        var profile = CreateProfile(lifetimeLevelsTrained: 0);
        profile.TrainingQueue.Add(new MasteryTrainingEntry
        {
            MasteryId = "mastery-active",
            TargetTier = 1,
            StartDate = UtcNow,
            DurationDays = 14,
            Source = MasteryTrainingSource.Standard14
        });

        var entry = MasteryRules.EnqueueTraining(profile, "mastery-instant", 1, false, false, true,
            new MasteryActor("Staffer", "cdkey1"), "Instant grant", "request-1", UtcNow);

        entry.Source.Should().Be(MasteryTrainingSource.Instant);
        entry.DurationDays.Should().Be(0);

        profile.Masteries.Should().ContainKey("mastery-instant");
        profile.Masteries["mastery-instant"].Tier.Should().Be(1);
        profile.Masteries["mastery-instant"].TierHistory.Should().ContainSingle(r => r.Source == MasteryTrainingSource.Instant);
        profile.LifetimeLevelsTrained.Should().Be(1);

        // The pre-existing queue entry must be completely unaffected.
        profile.TrainingQueue.Should().ContainSingle();
        profile.TrainingQueue[0].MasteryId.Should().Be("mastery-active");
        profile.TrainingQueue[0].StartDate.Should().Be(UtcNow);
        profile.TrainingQueue[0].DurationDays.Should().Be(14);

        profile.AuditLog.Should().ContainSingle(e => e.Action == "Approve" && e.Reason == "Instant grant");
    }

    [Test]
    public void EnqueueTraining_InstantWithEmptyQueue_GrantsTierImmediately()
    {
        var profile = CreateProfile(lifetimeLevelsTrained: 0);

        var entry = MasteryRules.EnqueueTraining(profile, "mastery-a", 1, false, false, true,
            new MasteryActor("Staffer", "cdkey1"), "reason", "request-1", UtcNow);

        entry.Source.Should().Be(MasteryTrainingSource.Instant);
        profile.Masteries["mastery-a"].Tier.Should().Be(1);
        profile.TrainingQueue.Should().BeEmpty();
    }

    [Test]
    public void EnqueueTraining_QuickSlotRequestedWithZeroAvailable_ReturnsNullAndQueuesNothing()
    {
        // A stale or direct call must never be able to get the discounted Quick Slot
        // duration for free just because it passed useQuickSlot:true - ResolveTraining
        // used to grant the 7/131-day duration unconditionally, with consumption only
        // gated separately, so this had to be rejected before any queue mutation at all.
        var profile = CreateProfile(lifetimeLevelsTrained: 5);
        profile.QuickSlotsAvailable = 0;

        var entry = MasteryRules.EnqueueTraining(profile, "mastery-a", 1, true, false, false,
            new MasteryActor("Staffer", "cdkey1"), "reason", "request-1", UtcNow);

        entry.Should().BeNull();
        profile.TrainingQueue.Should().BeEmpty();
        profile.QuickSlotsAvailable.Should().Be(0);
        profile.AuditLog.Should().BeEmpty();
    }

    [Test]
    public void EnqueueTraining_QuickSlotRequestedWithOneAvailable_StillSucceeds()
    {
        var profile = CreateProfile(lifetimeLevelsTrained: 5);
        profile.QuickSlotsAvailable = 1;

        var entry = MasteryRules.EnqueueTraining(profile, "mastery-a", 1, true, false, false,
            new MasteryActor("Staffer", "cdkey1"), "reason", "request-1", UtcNow);

        entry.Should().NotBeNull();
        entry.Source.Should().Be(MasteryTrainingSource.QuickSlot);
        entry.DurationDays.Should().Be(7);
        profile.QuickSlotsAvailable.Should().Be(0);
    }

    [Test]
    public void EnqueueTraining_InstantWithStaleQuickSlotFlagAndZeroAvailable_StillGrantsImmediately()
    {
        // Instant grants require no Quick Slot at all (see ResolveTraining), so a stale
        // useQuickSlot:true flag on an instant grant must not trip the zero-Quick-Slot
        // rejection meant for non-instant requests.
        var profile = CreateProfile(lifetimeLevelsTrained: 0);
        profile.QuickSlotsAvailable = 0;

        var entry = MasteryRules.EnqueueTraining(profile, "mastery-a", 1, true, false, true,
            new MasteryActor("Staffer", "cdkey1"), "Instant grant", "request-1", UtcNow);

        entry.Should().NotBeNull();
        entry.Source.Should().Be(MasteryTrainingSource.Instant);
        profile.Masteries["mastery-a"].Tier.Should().Be(1);
        profile.TrainingQueue.Should().BeEmpty();
        profile.QuickSlotsAvailable.Should().Be(0);
    }

    #endregion

    #region AbandonTrainingEntry (Phase 3 - cancelling a not-yet-completed entry)

    [Test]
    public void AbandonTrainingEntry_CancelsActiveEntry_NextEntryStartsNowAndNoTierGranted()
    {
        var profile = CreateProfile();
        var start = UtcNow.AddDays(-3);
        profile.TrainingQueue.Add(new MasteryTrainingEntry { MasteryId = "a", TargetTier = 1, StartDate = start, DurationDays = 14 });
        profile.TrainingQueue.Add(new MasteryTrainingEntry { MasteryId = "b", TargetTier = 1, StartDate = start.AddDays(14), DurationDays = 21 });

        var result = MasteryRules.AbandonTrainingEntry(profile, 0, new MasteryActor("Staffer", "cdkey1"), "No longer relevant", UtcNow);

        result.Should().BeTrue();
        profile.TrainingQueue.Should().ContainSingle();
        profile.TrainingQueue[0].MasteryId.Should().Be("b");
        profile.TrainingQueue[0].StartDate.Should().Be(UtcNow);
        profile.Masteries.Should().BeEmpty();
    }

    [Test]
    public void AbandonTrainingEntry_CancelsQueuedEntry_ActiveEntryTimelineUnaffected()
    {
        var profile = CreateProfile();
        var start = UtcNow.AddDays(-3);
        profile.TrainingQueue.Add(new MasteryTrainingEntry { MasteryId = "a", TargetTier = 1, StartDate = start, DurationDays = 14 });
        profile.TrainingQueue.Add(new MasteryTrainingEntry { MasteryId = "b", TargetTier = 1, StartDate = start.AddDays(14), DurationDays = 21 });

        var result = MasteryRules.AbandonTrainingEntry(profile, 1, new MasteryActor("Staffer", "cdkey1"), "No longer relevant", UtcNow);

        result.Should().BeTrue();
        profile.TrainingQueue.Should().ContainSingle();
        profile.TrainingQueue[0].MasteryId.Should().Be("a");
        profile.TrainingQueue[0].StartDate.Should().Be(start);
    }

    [Test]
    public void AbandonTrainingEntry_QuickSlotSourcedEntry_RefundsTheQuickSlot()
    {
        var profile = CreateProfile();
        profile.QuickSlotsAvailable = 0;
        profile.TrainingQueue.Add(new MasteryTrainingEntry { MasteryId = "a", TargetTier = 1, StartDate = UtcNow, DurationDays = 7, Source = MasteryTrainingSource.QuickSlot });

        MasteryRules.AbandonTrainingEntry(profile, 0, new MasteryActor("Staffer", "cdkey1"), "reason", UtcNow);

        profile.QuickSlotsAvailable.Should().Be(1);
    }

    [Test]
    public void AbandonTrainingEntry_Retrain7SourcedEntry_RefundsTheSevenDayCredit()
    {
        var profile = CreateProfile();
        profile.RetrainCredits7 = 0;
        profile.TrainingQueue.Add(new MasteryTrainingEntry { MasteryId = "a", TargetTier = 1, StartDate = UtcNow, DurationDays = 7, Source = MasteryTrainingSource.Retrain7 });

        MasteryRules.AbandonTrainingEntry(profile, 0, new MasteryActor("Staffer", "cdkey1"), "reason", UtcNow);

        profile.RetrainCredits7.Should().Be(1);
        profile.RetrainCredits14.Should().Be(0);
        profile.QuickSlotsAvailable.Should().Be(0);
    }

    [Test]
    public void AbandonTrainingEntry_Retrain14SourcedEntry_RefundsTheFourteenDayCredit()
    {
        var profile = CreateProfile();
        profile.RetrainCredits14 = 0;
        profile.TrainingQueue.Add(new MasteryTrainingEntry { MasteryId = "a", TargetTier = 1, StartDate = UtcNow, DurationDays = 14, Source = MasteryTrainingSource.Retrain14 });

        MasteryRules.AbandonTrainingEntry(profile, 0, new MasteryActor("Staffer", "cdkey1"), "reason", UtcNow);

        profile.RetrainCredits14.Should().Be(1);
        profile.RetrainCredits7.Should().Be(0);
        profile.QuickSlotsAvailable.Should().Be(0);
    }

    [Test]
    public void AbandonTrainingEntry_AppendsAuditEntryWithActorAndReason()
    {
        var profile = CreateProfile();
        profile.TrainingQueue.Add(new MasteryTrainingEntry { MasteryId = "a", TargetTier = 1, StartDate = UtcNow, DurationDays = 14 });

        MasteryRules.AbandonTrainingEntry(profile, 0, new MasteryActor("Staffer Name", "STAFF_CDKEY"), "Because reasons", UtcNow);

        profile.AuditLog.Should().ContainSingle(e =>
            e.Action == "AbandonTraining" &&
            e.ActorName == "Staffer Name" &&
            e.ActorCDKey == "STAFF_CDKEY" &&
            e.Reason == "Because reasons");
    }

    [Test]
    public void AbandonTrainingEntry_IndexOutOfRange_ReturnsFalseAndDoesNotMutateProfile()
    {
        var profile = CreateProfile();
        profile.TrainingQueue.Add(new MasteryTrainingEntry { MasteryId = "a" });

        var result = MasteryRules.AbandonTrainingEntry(profile, 5, new MasteryActor("Staffer", "cdkey1"), "reason", UtcNow);

        result.Should().BeFalse();
        profile.TrainingQueue.Should().ContainSingle();
        profile.AuditLog.Should().BeEmpty();
    }

    #endregion

    #region ReorderQueueEntry (Phase 3 - DM examine queue reorder)

    [Test]
    public void ReorderQueueEntry_MoveSecondQueuedEntryUp_SwapsOrderAndRecomputesStartDates()
    {
        var profile = CreateProfile();
        var start = UtcNow;
        profile.TrainingQueue.Add(new MasteryTrainingEntry { MasteryId = "active", StartDate = start, DurationDays = 14 });
        profile.TrainingQueue.Add(new MasteryTrainingEntry { MasteryId = "first-queued", DurationDays = 21 });
        profile.TrainingQueue.Add(new MasteryTrainingEntry { MasteryId = "second-queued", DurationDays = 28 });

        var result = MasteryRules.ReorderQueueEntry(profile, 2, -1, new MasteryActor("Staffer", "cdkey1"), UtcNow);

        result.Should().BeTrue();
        profile.TrainingQueue[0].MasteryId.Should().Be("active");
        profile.TrainingQueue[1].MasteryId.Should().Be("second-queued");
        profile.TrainingQueue[2].MasteryId.Should().Be("first-queued");

        // Start dates cascade from the (unmoved) active entry so the queue stays strictly sequential.
        profile.TrainingQueue[1].StartDate.Should().Be(start.AddDays(14));
        profile.TrainingQueue[2].StartDate.Should().Be(start.AddDays(14).AddDays(28));
    }

    [Test]
    public void ReorderQueueEntry_CannotMoveTheActiveEntry()
    {
        var profile = CreateProfile();
        profile.TrainingQueue.Add(new MasteryTrainingEntry { MasteryId = "active" });
        profile.TrainingQueue.Add(new MasteryTrainingEntry { MasteryId = "queued" });

        var result = MasteryRules.ReorderQueueEntry(profile, 0, 1, new MasteryActor("Staffer", "cdkey1"), UtcNow);

        result.Should().BeFalse();
        profile.TrainingQueue[0].MasteryId.Should().Be("active");
    }

    [Test]
    public void ReorderQueueEntry_CannotMoveAnEntryIntoTheActiveSlot()
    {
        var profile = CreateProfile();
        profile.TrainingQueue.Add(new MasteryTrainingEntry { MasteryId = "active" });
        profile.TrainingQueue.Add(new MasteryTrainingEntry { MasteryId = "queued" });

        var result = MasteryRules.ReorderQueueEntry(profile, 1, -1, new MasteryActor("Staffer", "cdkey1"), UtcNow);

        result.Should().BeFalse();
        profile.TrainingQueue[1].MasteryId.Should().Be("queued");
    }

    [Test]
    public void ReorderQueueEntry_MoveBeyondQueueBounds_ReturnsFalse()
    {
        var profile = CreateProfile();
        profile.TrainingQueue.Add(new MasteryTrainingEntry { MasteryId = "active" });
        profile.TrainingQueue.Add(new MasteryTrainingEntry { MasteryId = "queued" });

        var result = MasteryRules.ReorderQueueEntry(profile, 1, 1, new MasteryActor("Staffer", "cdkey1"), UtcNow);

        result.Should().BeFalse();
    }

    [Test]
    public void ReorderQueueEntry_AppendsAuditEntry()
    {
        var profile = CreateProfile();
        profile.TrainingQueue.Add(new MasteryTrainingEntry { MasteryId = "active" });
        profile.TrainingQueue.Add(new MasteryTrainingEntry { MasteryId = "first-queued" });
        profile.TrainingQueue.Add(new MasteryTrainingEntry { MasteryId = "second-queued" });

        MasteryRules.ReorderQueueEntry(profile, 2, -1, new MasteryActor("Staffer Name", "STAFF_CDKEY"), UtcNow);

        profile.AuditLog.Should().ContainSingle(e =>
            e.Action == "Reorder" &&
            e.ActorName == "Staffer Name" &&
            e.ActorCDKey == "STAFF_CDKEY");
    }

    [TestCase(0)]
    [TestCase(2)]
    [TestCase(-2)]
    public void ReorderQueueEntry_DirectionOtherThanPlusOrMinusOne_ReturnsFalseWithNoMutationOrAudit(int direction)
    {
        // Direction 0 in particular used to pass both bounds checks unchanged (newIndex
        // == index) and swap the queued entry with itself - returning true and appending
        // a false "Reorder" audit entry despite nothing actually moving.
        var profile = CreateProfile();
        profile.TrainingQueue.Add(new MasteryTrainingEntry { MasteryId = "active" });
        profile.TrainingQueue.Add(new MasteryTrainingEntry { MasteryId = "first-queued" });
        profile.TrainingQueue.Add(new MasteryTrainingEntry { MasteryId = "second-queued" });

        var result = MasteryRules.ReorderQueueEntry(profile, 1, direction, new MasteryActor("Staffer", "cdkey1"), UtcNow);

        result.Should().BeFalse();
        profile.TrainingQueue[0].MasteryId.Should().Be("active");
        profile.TrainingQueue[1].MasteryId.Should().Be("first-queued");
        profile.TrainingQueue[2].MasteryId.Should().Be("second-queued");
        profile.AuditLog.Should().BeEmpty();
    }

    #endregion
}
