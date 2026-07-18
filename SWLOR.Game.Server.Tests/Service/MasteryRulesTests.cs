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

    #endregion
}
