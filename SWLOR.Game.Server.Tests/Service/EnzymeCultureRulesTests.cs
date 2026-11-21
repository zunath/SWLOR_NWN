using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.EnzymeCultureService;
using SWLOR.Game.Server.Service.FarmingService;
using SWLOR.Game.Server.Service.LivestockService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Player City Revamp, "Farming for other crafts": the Fermentation Vat's enzyme cultures. 15 enzymes, 2 days each, Agriculture 20,
/// 2 crops, 1 manure and Distilled Water, a hunted starter the first time the vat makes a color, one batch per CD key, and a batch
/// that is done by the clock after a restart.
/// </summary>
public class EnzymeCultureRulesTests
{
    private static readonly DateTime Started = new(2026, 11, 2, 8, 0, 0, DateTimeKind.Utc);

    private static FermentationJob JobOf(
        string vatId = "vat-1",
        string owner = "player-1",
        string cdKey = "KEY",
        DateTime? started = null,
        EnzymeType type = EnzymeType.Hydrolase,
        EnzymeColorType color = EnzymeColorType.Blue)
    {
        var start = started ?? Started;
        return new FermentationJob
        {
            Id = FermentationJob.KeyFor(vatId),
            VatId = vatId,
            OwnerPlayerId = owner,
            OwnerCdKey = cdKey,
            Type = type,
            Color = color,
            StartedUtc = start,
            CompleteUtc = EnzymeCultureRules.GetCompleteUtc(start),
            CropResref = "v_orange",
            ManureResref = "manure_t1",
            WaterResref = EnzymeCultureRules.WaterResref,
        };
    }

    /// <summary>A start that is allowed in every respect but the one a test changes.</summary>
    private static FermentationRefusal Start(
        bool isPlayer = true,
        bool vatExists = true,
        bool hasAccess = true,
        FermentationJob vatJob = null,
        IEnumerable<FermentationJob> cdKeyJobs = null,
        DateTime? now = null,
        EnzymeType type = EnzymeType.Hydrolase,
        EnzymeColorType color = EnzymeColorType.Blue,
        int rank = 20,
        bool cropIsUsable = true,
        int crops = 2,
        bool manure = true,
        int water = 1,
        IEnumerable<string> starters = null,
        bool hasStarterItem = true)
    {
        return EnzymeCultureRules.CheckStart(isPlayer, vatExists, hasAccess, vatJob, cdKeyJobs ?? new List<FermentationJob>(), now ?? Started,
            type, color, rank, cropIsUsable, crops, manure, water, starters ?? new List<string>(), hasStarterItem);
    }

    // The 15 enzymes

    [Test]
    public void ThereAreFifteenEnzymes_ThreeTypesOfFiveColors()
    {
        EnzymeCultureRules.Types.Should().Equal(EnzymeType.Hydrolase, EnzymeType.Isomerase, EnzymeType.Lyase);
        EnzymeCultureRules.GetAll().Should().HaveCount(15).And.OnlyHaveUniqueItems();
        foreach (var type in EnzymeCultureRules.Types)
            EnzymeCultureRules.GetColors(type).Should().HaveCount(5).And.OnlyHaveUniqueItems(type.ToString());

        EnzymeCultureRules.GetColors(EnzymeType.Hydrolase).Should().BeEquivalentTo(new[]
            { EnzymeColorType.Blue, EnzymeColorType.Orange, EnzymeColorType.Red, EnzymeColorType.Purple, EnzymeColorType.White });
        EnzymeCultureRules.GetColors(EnzymeType.Isomerase).Should().BeEquivalentTo(new[]
            { EnzymeColorType.Orange, EnzymeColorType.Blue, EnzymeColorType.Green, EnzymeColorType.Red, EnzymeColorType.Yellow });
        EnzymeCultureRules.GetColors(EnzymeType.Lyase).Should().BeEquivalentTo(new[]
            { EnzymeColorType.Green, EnzymeColorType.Blue, EnzymeColorType.Orange, EnzymeColorType.Red, EnzymeColorType.Black });
        EnzymeCultureRules.GetColors(EnzymeType.Invalid).Should().BeEmpty();
    }

    [Test]
    public void OnlyAColorOfTheTypeIsAnEnzyme()
    {
        EnzymeCultureRules.IsEnzyme(EnzymeType.Hydrolase, EnzymeColorType.Purple).Should().BeTrue();
        EnzymeCultureRules.IsEnzyme(EnzymeType.Hydrolase, EnzymeColorType.Black).Should().BeFalse("black is a lyase color");
        EnzymeCultureRules.IsEnzyme(EnzymeType.Lyase, EnzymeColorType.Purple).Should().BeFalse();
        EnzymeCultureRules.IsEnzyme(EnzymeType.Isomerase, EnzymeColorType.Invalid).Should().BeFalse();
        EnzymeCultureRules.IsEnzyme(EnzymeType.Invalid, EnzymeColorType.Blue).Should().BeFalse();
    }

    [Test]
    public void TheEnzymeResrefIsTheTypeAndColorInLowerCase_AndMatchesTheBeastMasteryPrefixes()
    {
        EnzymeCultureRules.GetResref(EnzymeType.Hydrolase, EnzymeColorType.Blue).Should().Be("hydrolase_blue");
        EnzymeCultureRules.GetResref(EnzymeType.Lyase, EnzymeColorType.Black).Should().Be("lyase_black");
        EnzymeCultureRules.GetResref(EnzymeType.Isomerase, EnzymeColorType.Yellow).Should().Be("isomerase_yellow");

        foreach (var (type, color) in EnzymeCultureRules.GetAll())
        {
            var prefix = type switch
            {
                EnzymeType.Hydrolase => BeastMastery.HydrolaseResrefPrefix,
                EnzymeType.Isomerase => BeastMastery.IsomeraseResrefPrefix,
                _ => BeastMastery.LyaseResrefPrefix
            };
            EnzymeCultureRules.GetResref(type, color).Should().StartWith(prefix);
            EnzymeCultureRules.GetResref(type, color).Length.Should().BeLessThanOrEqualTo(16);
        }
    }

    [Test]
    public void TheNameReadsTypeThenColor()
    {
        EnzymeCultureRules.GetName(EnzymeType.Lyase, EnzymeColorType.Green).Should().Be("Lyase - Green");
    }

    // The constants

    [Test]
    public void ABatchTakesTwoDaysAndNeedsAgricultureTwenty()
    {
        EnzymeCultureRules.Hours.Should().Be(48);
        EnzymeCultureRules.RequiredAgricultureRank.Should().Be(20);
        EnzymeCultureRules.RecipeLevel.Should().Be(20);
        EnzymeCultureRules.CropsPerBatch.Should().Be(2);
        EnzymeCultureRules.ManurePerBatch.Should().Be(1);
        EnzymeCultureRules.WaterPerBatch.Should().Be(1);
        EnzymeCultureRules.WaterResref.Should().Be("distilled_water");
        EnzymeCultureRules.MaxRunningPerCdKey.Should().Be(1);
    }

    [Test]
    public void TheLoneVatMakesHalfAnEnzymeADay_TheBudgetsFigure()
    {
        // 1 vat per CD key, one enzyme per 2 days: the plan's 0.5 enzymes a day, however the CD key is split across characters.
        (EnzymeCultureRules.MaxRunningPerCdKey * 24.0 / EnzymeCultureRules.Hours).Should().Be(0.5);
    }

    // Starting a batch: the refusals, in order

    [Test]
    public void AFullyEquippedStartIsAllowed()
    {
        Start().Should().Be(FermentationRefusal.None);
        Start(starters: new[] { "hydrolase_blue" }, hasStarterItem: false).Should().Be(FermentationRefusal.None, "a color the vat has copied needs no starter");
    }

    [Test]
    public void EachRefusalComesBeforeTheNextInTheDocumentedOrder()
    {
        var running = new[] { JobOf(vatId: "other", started: Started.AddHours(-1)) };

        Start(isPlayer: false, vatExists: false).Should().Be(FermentationRefusal.NotAPlayer);
        Start(vatExists: false, hasAccess: false).Should().Be(FermentationRefusal.NoVat);
        Start(hasAccess: false, type: EnzymeType.Invalid).Should().Be(FermentationRefusal.NoAccess);
        Start(type: EnzymeType.Lyase, color: EnzymeColorType.Purple, vatJob: JobOf()).Should().Be(FermentationRefusal.UnknownEnzyme);
        Start(vatJob: JobOf(), cdKeyJobs: running).Should().Be(FermentationRefusal.VatBusy);
        Start(cdKeyJobs: running, rank: 1).Should().Be(FermentationRefusal.CdKeyBusy);
        Start(rank: 19, cropIsUsable: false).Should().Be(FermentationRefusal.SkillTooLow);
        Start(cropIsUsable: false, crops: 0).Should().Be(FermentationRefusal.NotACrop);
        Start(crops: 1, manure: false).Should().Be(FermentationRefusal.NoCrops);
        Start(manure: false, water: 0).Should().Be(FermentationRefusal.NoManure);
        Start(water: 0, hasStarterItem: false).Should().Be(FermentationRefusal.NoWater);
        Start(hasStarterItem: false).Should().Be(FermentationRefusal.NoStarter);
    }

    [TestCase(19, false)]
    [TestCase(20, true)]
    [TestCase(21, true)]
    [TestCase(50, true)]
    [TestCase(0, false)]
    public void AgricultureTwentyOpensTheCultures(int rank, bool allowed)
    {
        (Start(rank: rank) == FermentationRefusal.None).Should().Be(allowed);
        if (!allowed)
            Start(rank: rank).Should().Be(FermentationRefusal.SkillTooLow);
    }

    [TestCase(0, false)]
    [TestCase(1, false)]
    [TestCase(2, true)]
    [TestCase(9, true)]
    public void ABatchNeedsTwoCropsOfOneKind(int crops, bool allowed)
    {
        (Start(crops: crops) == FermentationRefusal.None).Should().Be(allowed);
    }

    [Test]
    public void ABatchNeedsManureAndDistilledWater()
    {
        Start(manure: false).Should().Be(FermentationRefusal.NoManure);
        Start(water: 0).Should().Be(FermentationRefusal.NoWater);
        Start(water: 3).Should().Be(FermentationRefusal.None);
    }

    [Test]
    public void OnlyTheFarmsCropsGoInABatch_NeverASapling()
    {
        EnzymeCultureRules.IsCultureCrop(null).Should().BeFalse();
        EnzymeCultureRules.IsCultureCrop(new CropDefinition { Name = "Tomato" }).Should().BeTrue();
        EnzymeCultureRules.IsCultureCrop(new CropDefinition { Name = "Wood Tree", IsSapling = true }).Should().BeFalse();
    }

    // The starter rule

    [Test]
    public void TheFirstBatchOfAColorNeedsAHuntedStarter_TheVatRemembersItAfter()
    {
        var starters = new List<string>();

        EnzymeCultureRules.HasStarter(starters, EnzymeType.Hydrolase, EnzymeColorType.Blue).Should().BeFalse();
        Start(starters: starters, hasStarterItem: false).Should().Be(FermentationRefusal.NoStarter);
        Start(starters: starters, hasStarterItem: true).Should().Be(FermentationRefusal.None);

        starters = EnzymeCultureRules.WithStarter(starters, EnzymeType.Hydrolase, EnzymeColorType.Blue);

        starters.Should().Equal("hydrolase_blue");
        EnzymeCultureRules.HasStarter(starters, EnzymeType.Hydrolase, EnzymeColorType.Blue).Should().BeTrue();
        Start(starters: starters, hasStarterItem: false).Should().Be(FermentationRefusal.None, "the vat copies the color now");
    }

    [Test]
    public void AStarterIsPerTypeAndColor_NotPerType_NorPerColor()
    {
        var starters = new[] { "hydrolase_blue" };

        Start(starters: starters, type: EnzymeType.Hydrolase, color: EnzymeColorType.Orange, hasStarterItem: false).Should().Be(FermentationRefusal.NoStarter, "another color of the type");
        Start(starters: starters, type: EnzymeType.Lyase, color: EnzymeColorType.Blue, hasStarterItem: false).Should().Be(FermentationRefusal.NoStarter, "the same color of another type");
        Start(starters: starters, type: EnzymeType.Hydrolase, color: EnzymeColorType.Blue, hasStarterItem: false).Should().Be(FermentationRefusal.None);
    }

    [Test]
    public void TheVatRemembersAllFifteenOnceEachHasBeenShown()
    {
        var starters = new List<string>();
        foreach (var (type, color) in EnzymeCultureRules.GetAll())
            starters = EnzymeCultureRules.WithStarter(starters, type, color);

        starters.Should().HaveCount(15).And.OnlyHaveUniqueItems();
        foreach (var (type, color) in EnzymeCultureRules.GetAll())
            EnzymeCultureRules.HasStarter(starters, type, color).Should().BeTrue();
    }

    [Test]
    public void RememberingAColorTwiceDoesNotListItTwice_AndNeverChangesTheListItWasGiven()
    {
        var original = new List<string> { "lyase_green" };

        var once = EnzymeCultureRules.WithStarter(original, EnzymeType.Lyase, EnzymeColorType.Green);
        var added = EnzymeCultureRules.WithStarter(original, EnzymeType.Lyase, EnzymeColorType.Black);

        once.Should().Equal("lyase_green");
        added.Should().Equal("lyase_green", "lyase_black");
        original.Should().Equal("lyase_green");
        EnzymeCultureRules.WithStarter(null, EnzymeType.Lyase, EnzymeColorType.Red).Should().Equal("lyase_red");
        EnzymeCultureRules.HasStarter(null, EnzymeType.Lyase, EnzymeColorType.Red).Should().BeFalse();
    }

    [Test]
    public void OnlyAHuntedEnzymeOfTheExactKindCanBeAStarter()
    {
        EnzymeCultureRules.CanBeStarter("hydrolase_blue", false, EnzymeType.Hydrolase, EnzymeColorType.Blue).Should().BeTrue();
        EnzymeCultureRules.CanBeStarter("HYDROLASE_BLUE", false, EnzymeType.Hydrolase, EnzymeColorType.Blue).Should().BeTrue();
        EnzymeCultureRules.CanBeStarter("hydrolase_blue", true, EnzymeType.Hydrolase, EnzymeColorType.Blue).Should().BeFalse("a cultured enzyme cannot start a vat");
        EnzymeCultureRules.CanBeStarter("hydrolase_red", false, EnzymeType.Hydrolase, EnzymeColorType.Blue).Should().BeFalse();
        EnzymeCultureRules.CanBeStarter("lyase_blue", false, EnzymeType.Hydrolase, EnzymeColorType.Blue).Should().BeFalse();
        EnzymeCultureRules.CanBeStarter("v_orange", false, EnzymeType.Hydrolase, EnzymeColorType.Blue).Should().BeFalse();
    }

    // One batch per CD key, and one per vat

    [Test]
    public void ACdKeyRunsOneBatchAtATime_AcrossItsVatsAndCharacters()
    {
        var other = new[] { JobOf(vatId: "other-vat", owner: "alt-character", started: Started.AddHours(-1)) };

        Start(cdKeyJobs: other).Should().Be(FermentationRefusal.CdKeyBusy);
        Start(cdKeyJobs: Array.Empty<FermentationJob>()).Should().Be(FermentationRefusal.None);
    }

    [Test]
    public void AFinishedBatchWaitingToBeCollectedDoesNotBlockTheKeysNextBatchInAnotherVat()
    {
        var finished = new[] { JobOf(vatId: "other-vat", started: Started.AddHours(-49)) };

        EnzymeCultureRules.CountRunning(finished, Started).Should().Be(0);
        Start(cdKeyJobs: finished).Should().Be(FermentationRefusal.None);
    }

    [Test]
    public void AVatHoldsOneBatchUntilItIsCollected_RunningOrDone()
    {
        Start(vatJob: JobOf(started: Started.AddHours(-1))).Should().Be(FermentationRefusal.VatBusy);
        Start(vatJob: JobOf(started: Started.AddHours(-100))).Should().Be(FermentationRefusal.VatBusy, "a finished batch still fills the vat");
    }

    [Test]
    public void CountingRunningBatchesIgnoresFinishedOnesAndMissingLists()
    {
        var jobs = new[]
        {
            JobOf(vatId: "a", started: Started.AddHours(-1)),
            JobOf(vatId: "b", started: Started.AddHours(-47)),
            JobOf(vatId: "c", started: Started.AddHours(-48)),
            JobOf(vatId: "d", started: Started.AddHours(-90)),
        };

        EnzymeCultureRules.CountRunning(jobs, Started).Should().Be(2);
        EnzymeCultureRules.CountRunning(null, Started).Should().Be(0);
        EnzymeCultureRules.CountRunning(Array.Empty<FermentationJob>(), Started).Should().Be(0);
    }

    // Time: the clock decides, so a restart changes nothing

    [Test]
    public void ABatchIsReadyFortyEightHoursAfterItStarts()
    {
        var job = JobOf();

        job.CompleteUtc.Should().Be(Started.AddHours(48));
        EnzymeCultureRules.IsComplete(job, Started).Should().BeFalse();
        EnzymeCultureRules.IsComplete(job, Started.AddHours(47).AddMinutes(59)).Should().BeFalse();
        EnzymeCultureRules.IsComplete(job, Started.AddHours(48)).Should().BeTrue();
        EnzymeCultureRules.IsRunning(job, Started.AddHours(47)).Should().BeTrue();
        EnzymeCultureRules.IsRunning(job, Started.AddHours(48)).Should().BeFalse();
        EnzymeCultureRules.GetTimeLeft(job, Started.AddHours(30)).Should().Be(TimeSpan.FromHours(18));
        EnzymeCultureRules.GetTimeLeft(job, Started.AddHours(60)).Should().Be(TimeSpan.Zero);
        EnzymeCultureRules.IsComplete(null, Started).Should().BeFalse();
        EnzymeCultureRules.IsRunning(null, Started).Should().BeFalse();
        EnzymeCultureRules.GetTimeLeft(null, Started).Should().Be(TimeSpan.Zero);
    }

    [Test]
    public void ABatchFinishesOnTimeAcrossARestart_AfterTheRecordsAreSavedAndLoaded()
    {
        var job = JobOf(started: Started);
        var vat = new FermentationVat { Id = FermentationVat.KeyFor("vat-1"), VatId = "vat-1", Starters = new List<string> { "hydrolase_blue", "lyase_red" } };

        // The record is saved as JSON and read back by a new process, the way the server's database does.
        var reloadedJob = JsonConvert.DeserializeObject<FermentationJob>(JsonConvert.SerializeObject(job))!;
        var reloadedVat = JsonConvert.DeserializeObject<FermentationVat>(JsonConvert.SerializeObject(vat))!;

        reloadedJob.CompleteUtc.Should().Be(job.CompleteUtc);
        reloadedJob.StartedUtc.Should().Be(job.StartedUtc);
        reloadedJob.Type.Should().Be(EnzymeType.Hydrolase);
        reloadedJob.Color.Should().Be(EnzymeColorType.Blue);
        reloadedJob.OwnerPlayerId.Should().Be("player-1");
        reloadedVat.Starters.Should().Equal("hydrolase_blue", "lyase_red");

        // Down for the whole run, it is done the moment the server is back; down for part of it, it still has its time left.
        EnzymeCultureRules.IsComplete(reloadedJob, Started.AddDays(5)).Should().BeTrue("the server was down past the deadline");
        EnzymeCultureRules.IsComplete(reloadedJob, Started.AddHours(20)).Should().BeFalse();
        EnzymeCultureRules.GetTimeLeft(reloadedJob, Started.AddHours(20)).Should().Be(TimeSpan.FromHours(28));
        EnzymeCultureRules.CheckCollect(true, true, reloadedJob, "player-1", Started.AddDays(5)).Should().Be(FermentationRefusal.None);
    }

    // Collecting

    [Test]
    public void OnlyTheOwnerCollectsAFinishedBatch()
    {
        var job = JobOf();
        var done = Started.AddHours(48);

        EnzymeCultureRules.CheckCollect(true, true, job, "player-1", done).Should().Be(FermentationRefusal.None);
        EnzymeCultureRules.CheckCollect(true, true, job, "someone-else", done).Should().Be(FermentationRefusal.NotTheOwner);
        EnzymeCultureRules.CheckCollect(true, true, job, "player-1", done.AddMinutes(-1)).Should().Be(FermentationRefusal.NotDone);
        EnzymeCultureRules.CheckCollect(true, true, null, "player-1", done).Should().Be(FermentationRefusal.NoBatch);
        EnzymeCultureRules.CheckCollect(true, false, job, "player-1", done).Should().Be(FermentationRefusal.NoVat);
        EnzymeCultureRules.CheckCollect(false, true, job, string.Empty, done).Should().Be(FermentationRefusal.NotAPlayer);
    }

    [Test]
    public void ABatchsOwnerIsToldItIsNotDoneBeforeTheyAreToldItIsNotTheirs()
    {
        EnzymeCultureRules.CheckCollect(true, true, JobOf(), "someone-else", Started.AddHours(1)).Should().Be(FermentationRefusal.NotTheOwner);
        EnzymeCultureRules.CheckCollect(true, true, JobOf(), "player-1", Started.AddHours(1)).Should().Be(FermentationRefusal.NotDone);
    }

    // The manure

    [Test]
    public void TheBatchUsesTheLowestTierOfManureTheCharacterCarries()
    {
        var carried = new Dictionary<string, int> { ["manure_t3"] = 2, ["manure_t5"] = 9, ["manure_t2"] = 1 };

        EnzymeCultureRules.PickManure(resref => carried.GetValueOrDefault(resref)).Should().Be("manure_t2");
        carried.Remove("manure_t2");
        EnzymeCultureRules.PickManure(resref => carried.GetValueOrDefault(resref)).Should().Be("manure_t3");
        carried.Clear();
        EnzymeCultureRules.PickManure(resref => carried.GetValueOrDefault(resref)).Should().BeEmpty();
        EnzymeCultureRules.PickManure(resref => resref == "manure_t1" ? 1 : 0).Should().Be("manure_t1");
    }

    [Test]
    public void TheManureResrefsAreTheLivestockOnes()
    {
        for (var tier = 1; tier <= FarmingRules.Tiers; tier++)
            EnzymeCultureRules.PickManure(resref => resref == LivestockRules.GetManureResref(tier) ? 1 : 0).Should().Be($"manure_t{tier}");
    }

    // Pay

    [TestCase(1)]
    [TestCase(10)]
    [TestCase(20)]
    [TestCase(25)]
    [TestCase(40)]
    [TestCase(50)]
    public void ABatchPaysWhatALevelTwentyAgricultureRecipePays(int rank)
    {
        var recipe = new RecipeDetail { Skill = SkillType.Agriculture, Level = 20 };

        EnzymeCultureRules.GetXp(rank).Should().Be(CraftMinigameXp.BaseXp(recipe, rank));
        EnzymeCultureRules.GetXp(rank).Should().Be(
            CraftMinigameXp.ScaleBaseXp(
                CraftMinigameXp.ScaleBaseXp(Skill.GetDeltaXP(20 - rank), CraftMinigameXp.MultiplierFor(CraftMinigameType.Kitchen, 20)),
                CraftMinigameXp.PaceMultiplierFor(SkillType.Agriculture)));
    }

    [Test]
    public void TheXpFallsAsTheFarmerOutgrowsLevelTwenty_AndIsPaidAtTheFirstRank()
    {
        EnzymeCultureRules.GetXp(20).Should().BeGreaterThan(0);
        EnzymeCultureRules.GetXp(20).Should().BeGreaterThanOrEqualTo(EnzymeCultureRules.GetXp(30));
        EnzymeCultureRules.GetXp(30).Should().BeGreaterThanOrEqualTo(EnzymeCultureRules.GetXp(50));
        EnzymeCultureRules.GetXp(1).Should().BeGreaterThanOrEqualTo(EnzymeCultureRules.GetXp(20));
    }

    // When the vat goes away

    [Test]
    public void ARunningBatchReturnsItsInputs_AFinishedOneDeliversItsEnzyme()
    {
        var job = JobOf();

        EnzymeCultureRules.GetRecovery(job, Started.AddHours(1)).Should().Be(FermentationRecovery.ReturnInputs);
        EnzymeCultureRules.GetRecovery(job, Started.AddHours(47).AddMinutes(59)).Should().Be(FermentationRecovery.ReturnInputs);
        EnzymeCultureRules.GetRecovery(job, Started.AddHours(48)).Should().Be(FermentationRecovery.DeliverEnzyme);
        EnzymeCultureRules.GetRecovery(job, Started.AddDays(30)).Should().Be(FermentationRecovery.DeliverEnzyme, "a server that stayed down delivers it the same");
    }

    // Text

    [Test]
    public void EveryRefusalHasAPlainAsciiMessage()
    {
        foreach (var refusal in Enum.GetValues<FermentationRefusal>().Where(refusal => refusal != FermentationRefusal.None))
            EnzymeCultureRules.Describe(refusal).Should().NotBeNullOrWhiteSpace().And.MatchRegex("^[\\x20-\\x7E]+$", refusal.ToString());

        EnzymeCultureRules.Describe(FermentationRefusal.None).Should().BeEmpty();
        FermentationResult.Refused(FermentationRefusal.NoWater).Message.Should().Be(EnzymeCultureRules.Describe(FermentationRefusal.NoWater));
        FermentationResult.Refused(FermentationRefusal.NoWater, "custom").Message.Should().Be("custom");
        FermentationResult.Success("done").Succeeded.Should().BeTrue();
        FermentationResult.Refused(FermentationRefusal.NoWater).Succeeded.Should().BeFalse();
    }

    [Test]
    public void TheMessagesNameTheEnzymeAndTheTime()
    {
        EnzymeCultureRules.DescribeStarted(EnzymeType.Lyase, EnzymeColorType.Black, true).Should()
            .Contain("Lyase - Black").And.Contain("2d 0h").And.Contain("starter").And.MatchRegex("^[\\x20-\\x7E]+$");
        EnzymeCultureRules.DescribeStarted(EnzymeType.Lyase, EnzymeColorType.Black, false).Should().NotContain("starter");
        EnzymeCultureRules.DescribeCollected(EnzymeType.Isomerase, EnzymeColorType.Red, false).Should().Contain("Isomerase - Red").And.NotContain("overflow");
        EnzymeCultureRules.DescribeCollected(EnzymeType.Isomerase, EnzymeColorType.Red, true).Should().Contain("/overflow");

        var job = JobOf();
        EnzymeCultureRules.DescribeBatch(null, Started, false).Should().Be("The vat is empty.");
        EnzymeCultureRules.DescribeBatch(job, Started.AddHours(30), true).Should().Contain("Hydrolase - Blue").And.Contain("18h 0m");
        EnzymeCultureRules.DescribeBatch(job, Started.AddHours(48), true).Should().Contain("ready to collect");
        EnzymeCultureRules.DescribeBatch(job, Started.AddHours(48), false).Should().Contain("waiting for its owner");
    }

    [Test]
    public void TheVatListsTheColorsItHasCopiedForEachType()
    {
        EnzymeCultureRules.DescribeStarters(null).Should().Equal("Hydrolase: none", "Isomerase: none", "Lyase: none");
        EnzymeCultureRules.DescribeStarters(new[] { "hydrolase_red", "hydrolase_blue", "lyase_black" })
            .Should().Equal("Hydrolase: Blue, Red", "Isomerase: none", "Lyase: Black");
    }
}
