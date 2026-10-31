using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.ShipComponentDefinition;
using SWLOR.Game.Server.Feature.ShipModuleDefinition;
using SWLOR.Game.Server.Feature.ShipRewardDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SlicingService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Slicing in space (plan section 24, W12b): the Data Analyzer, who slices aboard, the target tiers and rules kept
/// from the ground, data sites, locked caches, wreck data cores, customs spoofing, space lockboxes, the space reward
/// tables, Espionage XP and the exploration payout. Numbers come from tools/SpaceRevampBalance/slicing.py
/// (SpaceSlicingTuning).
/// </summary>
public class SpaceSlicingTests
{
    private static readonly DateTime Now = new(2026, 10, 27, 12, 0, 0, DateTimeKind.Utc);

    private static readonly SlicingSourceType[] RewardSources =
    {
        SlicingSourceType.DataCan, SlicingSourceType.LockedCache, SlicingSourceType.WreckDataCore, SlicingSourceType.SpaceLockbox,
    };

    private static string RepositoryFile(params string[] parts)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        directory.Should().NotBeNull();
        return Path.Combine(new[] { directory!.FullName }.Concat(parts).ToArray());
    }

    private static string ServerSource(params string[] parts) =>
        File.ReadAllText(RepositoryFile(new[] { "SWLOR.Game.Server" }.Concat(parts).ToArray()));

    /// <summary>A roll source that walks through a fixed list, then repeats its last value.</summary>
    private static Func<int, int> Rolls(params int[] values)
    {
        var index = 0;
        return max =>
        {
            var value = values[Math.Min(index, values.Length - 1)];
            index++;
            return Math.Clamp(value, 0, max - 1);
        };
    }

    // Data Analyzer

    [Test]
    public void DataAnalyzer_IsOneMidSlotEspionageModuleWithNoRankRequirement()
    {
        var steps = RevampModuleCatalog.Steps.Where(x => x.Family == ShipModuleFamily.DataAnalyzer).ToList();
        steps.Should().ContainSingle();
        var step = steps[0];
        step.Tag.Should().Be(SpaceSlicingTuning.AnalyzerTag);
        step.Rank.Should().Be(0, "any ship can fit it, with no rank requirement");
        step.Row.Should().Be(ShipModulePowerType.Mid);
        step.Skill.Should().Be(SkillType.Espionage);
        step.Activation.Should().Be(ShipModuleActivation.OnePress);
        step.Capital.Should().BeFalse();
        step.Range.Should().Be((int)SpaceSlicingTuning.AnalyzerRange);
        step.RecipeType.Should().Be(RecipeType.ShipDataAnalyzer00);
        ((int)step.RecipeType).Should().Be(7532, "after the blueprint update's block (7498-7531)");
        ((int)ShipModuleFamily.DataAnalyzer).Should().Be(44);
        step.Components.Should().Contain(x => x.Resref == "sc_sensor_1", "its recipe consumes a Sensor Suite");

        var blueprint = File.ReadAllText(RepositoryFile("Module", "uti", $"{step.Tag}.uti.json"));
        blueprint.Should().NotContain("\"PropertyName\"", "the blueprint carries no RequiresSkill property");
    }

    [Test]
    public void DataAnalyzer_RangeReplacesTheFiveMeterTerminalRangeForSpaceTargets()
    {
        SpaceSlicingTuning.AnalyzerRange.Should().Be(30f);
        SpaceSlicingRules.IsInAnalyzerRange(0f).Should().BeTrue();
        SpaceSlicingRules.IsInAnalyzerRange(30f).Should().BeTrue();
        SpaceSlicingRules.IsInAnalyzerRange(30.01f).Should().BeFalse();
        SpaceSlicingRules.IsInAnalyzerRange(-1f).Should().BeFalse("GetDistanceBetween returns -1 across areas");

        var session = ServerSource("Service", "SlicingService", "SlicingSession.cs");
        session.Should().Contain("GetDistanceBetween(player, target) > 5f", "ground terminals keep their 5 m range");
        ServerSource("Service", "SpaceSlicing.cs").Should().Contain("SpaceSlicingRules.IsInAnalyzerRange(GetDistanceBetween(ship, target))",
            "space targets measure the Data Analyzer's range from the ship, not from a crew member inside it");
    }

    [Test]
    public void DataAnalyzer_ValidatesThroughTheGroundSlicingRules()
    {
        var definition = ServerSource("Feature", "ShipModuleDefinition", "RevampModuleDefinition.cs");
        definition.Should().Contain("case ShipModuleFamily.DataAnalyzer:");
        definition.Should().Contain("Space.DataAnalyzerError(activator, target)");

        var space = ServerSource("Service", "SpaceSlicing.cs");
        space.Should().Contain("return SlicingSession.ValidateStart(slicer, target, source, tier);",
            "the analyzer checks rank, combat and range through the ground ValidateStart");
        space.Should().Contain("var slicer = SpaceCrew.GetStatSource(ship);", "whoever runs the module slices: the pilot or the Sensors crew member");
    }

    // Who slices, tiers and the rules kept from the ground

    [TestCase(10, false, 0, 3, ShipCrewSeat.Pilot)]
    [TestCase(2, true, 4, 3, ShipCrewSeat.Sensors)]
    [TestCase(3, true, 5, 3, ShipCrewSeat.Sensors)]
    [TestCase(5, true, 5, 3, ShipCrewSeat.Pilot)]
    [TestCase(5, true, 2, 3, ShipCrewSeat.Pilot)]
    [TestCase(1, true, 2, 3, ShipCrewSeat.Pilot)]
    [TestCase(1, false, 9, 3, ShipCrewSeat.Pilot)]
    public void WhoSlices_IsThePilotOrTheSensorsCrewWithTheirOwnSlicingRank(
        int pilotRank, bool hasCrew, int crewRank, int tier, ShipCrewSeat expected)
    {
        SpaceSlicingRules.PickSlicer(pilotRank, hasCrew, crewRank, tier).Should().Be(expected);
    }

    [Test]
    public void Tiers_AreTheAreaTierAtMostFiveAndWildSpaceIsFive()
    {
        for (var tier = 1; tier <= 6; tier++)
        {
            SpaceSlicingRules.TargetTier(tier, false).Should().Be(Math.Min(tier, 5));
            SpaceSlicingRules.TargetTier(tier, true).Should().Be(5, "Wild Space targets are tier 5");
        }

        SpaceSlicingRules.TargetTier(0, false).Should().Be(1);
    }

    [Test]
    public void Validation_KeepsTheRankGateAndNoSlicingInCombatWithTheShipCounted()
    {
        var session = ServerSource("Service", "SlicingService", "SlicingSession.cs");
        var validate = session[session.IndexOf("public static string ValidateStart", StringComparison.Ordinal)..];
        validate = validate[..validate.IndexOf("internal static string GetRankRequirementError", StringComparison.Ordinal)];

        validate.Should().Contain("if (tier < 1 || tier > 5)");
        validate.Should().Contain("if (IsInCombat(player, source))");
        validate.Should().Contain("if (Perk.GetPerkLevel(player, PerkType.Slicing) < tier)");
        validate.IndexOf("IsInCombat(player, source)", StringComparison.Ordinal).Should()
            .BeLessThan(validate.IndexOf("GetTargetError(player, target, source)", StringComparison.Ordinal));
        session.Should().Contain("GetIsInCombat(player) || SpaceSlicingRules.IsSpaceSource(source) && Space.IsSlicingShipInCombat(player)",
            "a crew slicer cannot slice while the ship fights");
        session.Should().Contain("if (IsInCombat(player, session.Source))", "a started attempt stops when combat starts");

        ServerSource("Service", "SpaceSlicing.cs").Should().Contain("return \"That module has no one to run it.\";");
    }

    [Test]
    public void Sources_AreNewValuesAfterTheGroundOnes()
    {
        ((int)SlicingSourceType.Lockbox).Should().Be(1);
        ((int)SlicingSourceType.Terminal).Should().Be(2);
        ((int)SlicingSourceType.DataCan).Should().Be(3);
        ((int)SlicingSourceType.LockedCache).Should().Be(4);
        ((int)SlicingSourceType.WreckDataCore).Should().Be(5);
        ((int)SlicingSourceType.CustomsScan).Should().Be(6);
        ((int)SlicingSourceType.SpaceLockbox).Should().Be(7);

        SpaceSlicingRules.IsSpaceSource(SlicingSourceType.Lockbox).Should().BeFalse();
        SpaceSlicingRules.IsSpaceSource(SlicingSourceType.Terminal).Should().BeFalse();
        foreach (var source in RewardSources.Append(SlicingSourceType.CustomsScan))
            SpaceSlicingRules.IsSpaceSource(source).Should().BeTrue(source.ToString());

        SpaceSlicingRules.UsesDataAnalyzer(SlicingSourceType.DataCan).Should().BeTrue();
        SpaceSlicingRules.UsesDataAnalyzer(SlicingSourceType.LockedCache).Should().BeTrue();
        SpaceSlicingRules.UsesDataAnalyzer(SlicingSourceType.WreckDataCore).Should().BeTrue();
        SpaceSlicingRules.UsesDataAnalyzer(SlicingSourceType.CustomsScan).Should().BeFalse();
        SpaceSlicingRules.UsesDataAnalyzer(SlicingSourceType.SpaceLockbox).Should().BeFalse();
        SpaceSlicingRules.IsPortable(SlicingSourceType.SpaceLockbox).Should().BeTrue();
        SpaceSlicingRules.IsPortable(SlicingSourceType.DataCan).Should().BeFalse();
    }

    // Starts

    [Test]
    public void Starts_EachSourceHasItsEntryPoint()
    {
        var space = ServerSource("Service", "SpaceSlicing.cs");
        space.Should().Contain("return (SlicingSourceType.DataCan, GetLocalInt(target, SlicingSession.TierVariable));");
        space.Should().Contain("return (SlicingSourceType.LockedCache, GetLocalInt(target, SlicingSession.TierVariable));");
        space.Should().Contain("return (SlicingSourceType.WreckDataCore, SpaceSlicingRules.TargetTier(wreck.Tier, IsWildSpace(GetArea(target))));");
        space.Should().Contain("Gui.TogglePlayerWindow(slicer, GuiWindowType.Slicing, new SlicingPayload(target, source, tier));");

        ServerSource("Service", "TradeCustoms.cs").Should().Contain("Space.StartSlicing(slicer, patrolShip, SlicingSourceType.CustomsScan, tier)");
        ServerSource("Feature", "ItemDefinition", "SpaceLockboxItemDefinition.cs")
            .Should().Contain("SlicingSession.TryStart(user, item, SlicingSourceType.SpaceLockbox, tier, out var error)");
        ServerSource("Feature", "SpaceObjectDefinition", "SalvageSpaceObjectDefinition.cs")
            .Should().Contain("_builder.Create(Space.DataCanTag)", "a click targets a data can for the Data Analyzer");
    }

    [Test]
    public void DataSites_SpawnDataCansAndTheirCacheAfterThem()
    {
        ScanningTuning.Weights[SignatureType.DataSite].Should().Be(15);
        SpaceSlicingTuning.DataSiteCans.Should().Be(3);
        ServerSource("Service", "SpaceScanSites.cs").Should().Contain("case SignatureType.DataSite:");

        var site = SignatureSiteRules.Create("sig-d", "hutlar_orbit", 3, SignatureType.DataSite, 0f, 0f, 0f, Now);
        site.RevealedAt = Now;
        SignatureSiteRules.ShouldSpawnCache(site, SpaceSlicingTuning.DataSiteCans).Should().BeFalse("its cans are its objectives");
        SignatureSiteRules.ShouldSpawnCache(site, 0).Should().BeTrue("the site cache appears once every can is sliced or lost");

        var can = File.ReadAllText(RepositoryFile("Module", "utp", $"{SpaceSlicingTuning.DataCanResref}.utp.json"));
        can.Should().Contain("\"value\": \"spc_target\"", "a click targets it, as a wreck");
        can.Should().Contain($"\"value\": \"{Space.DataCanTag}\"");
    }

    [Test]
    public void LockedCaches_AreAShareOfCacheSitesAndOnlyCacheSites()
    {
        SpaceSlicingTuning.LockedCachePercent.Should().Be(40);
        SpaceSlicingRules.RollsLockedCache(39).Should().BeTrue();
        SpaceSlicingRules.RollsLockedCache(40).Should().BeFalse();

        SignatureSiteRules.Create("a", "hutlar_orbit", 3, SignatureType.CacheSite, 0f, 0f, 0f, Now, true).Locked.Should().BeTrue();
        SignatureSiteRules.Create("b", "hutlar_orbit", 3, SignatureType.CombatSite, 0f, 0f, 0f, Now, true).Locked.Should().BeFalse();
        SignatureSiteRules.Create("c", "hutlar_orbit", 3, SignatureType.CacheSite, 0f, 0f, 0f, Now).Locked.Should().BeFalse();

        for (var tier = 1; tier <= 6; tier++)
        {
            SignatureSiteRules.LockedCacheCredits(tier).Should().BeGreaterThan(0, $"tier {tier}");
            (SignatureSiteRules.LockedCacheCredits(tier) % 25).Should().Be(0);
        }

        var sites = ServerSource("Service", "SpaceScanSites.cs");
        sites.Should().Contain("if (IsLockedCache(cache))", "a locked cache cannot be opened by using it");
    }

    // Rewards

    [Test]
    public void Rewards_EverySpaceSourcePaysCreditsAndOneBonusFromItsTable()
    {
        foreach (var source in RewardSources)
        {
            SlicingRewardCatalog.HasSpaceTable(source).Should().BeTrue(source.ToString());
            var kinds = SlicingRewardCatalog.GetSpaceBonusWeights(source).Keys.ToHashSet();
            for (var tier = 1; tier <= 5; tier++)
            {
                var baseCredits = SlicingRewardCatalog.GetSpaceCredits(source, tier);
                baseCredits.Should().BeGreaterThan(0);
                (baseCredits % 25).Should().Be(0);

                for (var seed = 0; seed < 200; seed++)
                {
                    var random = new System.Random(seed);
                    var rewards = SlicingReward.RollSpace(source, tier, max => random.Next(max));
                    rewards.Should().HaveCount(2, $"{source} T{tier}");
                    rewards[0].Kind.Should().Be(SpaceSlicingRewardKind.Credits);
                    rewards[0].Credits.Should().BeInRange(baseCredits * 75 / 100, baseCredits * 125 / 100);
                    kinds.Should().Contain(rewards[1].PickedKind, $"{source} T{tier}");
                }
            }
        }
    }

    [Test]
    public void Rewards_GroundTablesAreUnchanged()
    {
        SlicingRewardCatalog.Entries.Should().HaveCount(120);
        SlicingRewardCatalog.Entries.Should().OnlyContain(x => x.Source == SlicingSourceType.Lockbox || x.Source == SlicingSourceType.Terminal);
        SlicingRewardCatalog.HasSpaceTable(SlicingSourceType.Lockbox).Should().BeFalse();
        SlicingRewardCatalog.HasSpaceTable(SlicingSourceType.Terminal).Should().BeFalse();
    }

    [Test]
    public void Rewards_ComponentsCarryATraitOfTheirKindAndTier()
    {
        var weights = SlicingRewardCatalog.GetSpaceBonusWeights(SlicingSourceType.LockedCache);
        var rareStart = weights.Where(x => x.Key < SpaceSlicingRewardKind.RareComponent).Sum(x => x.Value);

        for (var tier = 1; tier <= 5; tier++)
        {
            var rare = SlicingReward.RollSpace(SlicingSourceType.LockedCache, tier, Rolls(0, rareStart, 0, 0))[1];
            rare.Kind.Should().Be(SpaceSlicingRewardKind.RareComponent);
            ShipTraitRules.IsComponent(rare.Resref).Should().BeTrue();
            ShipTraitRules.Get(rare.Trait).Kind.Should().Be(ShipTraitKind.Rare);
            RevampComponentCatalog.Items.Single(x => x.Resref == rare.Resref).Tier.Should().Be(tier);

            var common = SlicingReward.RollSpace(SlicingSourceType.LockedCache, tier, Rolls(0, 0, 0, 0))[1];
            common.Kind.Should().Be(SpaceSlicingRewardKind.Component);
            ShipTraitRules.Get(common.Trait).Kind.Should().Be(ShipTraitKind.Common);
        }
    }

    [Test]
    public void Rewards_SchematicsAreTheBlueprintUpdatesItemsAndEmptyPoolsAndAgentIntelFallBack()
    {
        foreach (var kind in new[] { SpaceSlicingRewardKind.DesignSchematic, SpaceSlicingRewardKind.RecipeUnlock, SpaceSlicingRewardKind.ShipSchematic })
        {
            var weights = SlicingRewardCatalog.GetSpaceBonusWeights(SlicingSourceType.WreckDataCore).OrderBy(x => (int)x.Key).ToList();
            var start = weights.TakeWhile(x => x.Key != kind).Sum(x => x.Value);
            var reward = SlicingReward.RollSpace(SlicingSourceType.WreckDataCore, 3, Rolls(0, start, 0, 0))[1];
            reward.PickedKind.Should().Be(kind);
            if (SlicingReward.GetSchematicPool(kind, 3).Count == 0)
                reward.Kind.Should().Be(SpaceSlicingTuning.FallbackKind, $"{kind} has no item at tier 3");
            else
                SlicingReward.GetSchematicPool(kind, 3).Should().Contain(reward.Resref);
        }

        SlicingReward.GetSchematicPool(SpaceSlicingRewardKind.DesignSchematic, 1).Should().NotBeEmpty().And.OnlyContain(x => x.StartsWith("dsgn_"));
        SlicingReward.GetSchematicPool(SpaceSlicingRewardKind.ShipSchematic, 1).Should().BeEmpty("no Ship Schematic is tier 1");
        SlicingReward.GetSchematicPool(SpaceSlicingRewardKind.ShipSchematic, 3).Should().NotBeEmpty().And.OnlyContain(x => x.StartsWith("schm_"));
        SlicingReward.GetSchematicPool(SpaceSlicingRewardKind.RecipeUnlock, 4).Should().BeEmpty("recipe unlocks come from tier 5 targets");
        SlicingReward.GetSchematicPool(SpaceSlicingRewardKind.RecipeUnlock, 5).Should().HaveCount(16).And.OnlyContain(x => x.StartsWith("recipe_"));
        var designsAtTwo = SlicingReward.GetSchematicPool(SpaceSlicingRewardKind.DesignSchematic, 2);
        RevampRewardCatalog.HullDesigns.Where(x => designsAtTwo.Contains(x.Resref)).Should().OnlyContain(x => x.Tier <= 2 && !x.WildSpaceOnly);

        var locked = SlicingRewardCatalog.GetSpaceBonusWeights(SlicingSourceType.LockedCache).OrderBy(x => (int)x.Key).ToList();
        var proto = locked.TakeWhile(x => x.Key != SpaceSlicingRewardKind.PrototypeTech).Sum(x => x.Value);
        SlicingReward.RollSpace(SlicingSourceType.LockedCache, 4, Rolls(0, proto, 0))[1].Resref.Should().Be(RevampRewardCatalog.PrototypeTechResref);

        var dataCan = SlicingRewardCatalog.GetSpaceBonusWeights(SlicingSourceType.DataCan).OrderBy(x => (int)x.Key).ToList();
        var intel = dataCan.TakeWhile(x => x.Key != SpaceSlicingRewardKind.AgentIntel).Sum(x => x.Value);
        SlicingReward.RollSpace(SlicingSourceType.DataCan, 3, Rolls(0, intel, 0, 0))[1].Kind.Should().Be(SpaceSlicingRewardKind.RareComponent);
        SlicingReward.RollSpace(SlicingSourceType.DataCan, 3, Rolls(0, intel, 0, 0), true)[1].Kind.Should().Be(SpaceSlicingRewardKind.AgentIntel,
            "the W15 hook turns agent intel on");
        ServerSource("Service", "SpaceSlicing.cs").Should().Contain("public static bool AgentIntelAvailable => true;", "faction agents turn intel into leads (W15a)");
        ServerSource("Service", "SpaceSlicing.cs").Should().Contain("FactionAgents.GrantIntel(slicer);");
    }

    [Test]
    public void Rewards_DataCoresHaveBetterSchematicOddsThanTheOtherSiteTargets()
    {
        double SchematicShare(SlicingSourceType source)
        {
            var weights = SlicingRewardCatalog.GetSpaceBonusWeights(source);
            var schematics = weights.Where(x => x.Key is SpaceSlicingRewardKind.DesignSchematic or SpaceSlicingRewardKind.RecipeUnlock
                or SpaceSlicingRewardKind.ShipSchematic).Sum(x => x.Value);
            return schematics / (double)weights.Values.Sum();
        }

        SchematicShare(SlicingSourceType.WreckDataCore).Should().BeGreaterThan(SchematicShare(SlicingSourceType.DataCan));
        SchematicShare(SlicingSourceType.WreckDataCore).Should().BeGreaterThan(SchematicShare(SlicingSourceType.LockedCache));
    }

    [Test]
    public void Rewards_CustomsScansPayNothingButTheCleanScan()
    {
        SlicingRewardCatalog.HasSpaceTable(SlicingSourceType.CustomsScan).Should().BeFalse();
        SlicingReward.RollSpace(SlicingSourceType.CustomsScan, 3, _ => 0).Should().BeEmpty();
    }

    // Customs spoofing

    [Test]
    public void Customs_SpoofSuccessIsACleanScan()
    {
        var outcome = SpaceSlicingRules.SpoofOutcome(true, 500, 10000);
        outcome.Clean.Should().BeTrue();
        outcome.Fine.Should().Be(0);
        outcome.ConfiscatesGoods.Should().BeFalse();
        outcome.Standing.Should().BeNull();
    }

    [TestCase(500, 10000, 500)]
    [TestCase(500, 120, 120)]
    [TestCase(500, 0, 0)]
    public void Customs_SpoofFailurePlaysOutAsBeingCaught(int fine, int gold, int expectedFine)
    {
        var outcome = SpaceSlicingRules.SpoofOutcome(false, fine, gold);
        outcome.Clean.Should().BeFalse();
        outcome.Fine.Should().Be(expectedFine);
        outcome.ConfiscatesGoods.Should().BeTrue();
        outcome.Standing.Should().Be(TradeStandingEvent.CaughtSmuggling, "the same standing loss as paying the fine");
    }

    [Test]
    public void Customs_SliceTheScanIsWiredAndGivingUpCountsAsCaught()
    {
        var customs = ServerSource("Service", "TradeCustoms.cs");
        customs.Should().NotContain("CanSliceCustomsScan(uint player) => false");
        customs.Should().Contain("SpaceSlicingRules.TargetTier(scan.Tier, false)", "the spoof is sliced at the patrol's tier");
        customs.Should().Contain("pending.Id == scan.Id && !pending.IsBeingSliced", "the answer timer waits for the slicer");

        ServerSource("Feature", "GuiDefinition", "CustomsScanDefinition.cs").Should().Contain(".BindOnClicked(model => model.OnClickSlice())");

        var session = ServerSource("Service", "SlicingService", "SlicingSession.cs");
        session.Should().Contain("if (session.HasCommitted || session.Source == SlicingSourceType.CustomsScan)");
        session.Should().Contain("Space.FailCustomsSlicing(session.Player);");
    }

    // Wreck data cores

    [Test]
    public void DataCores_AreOnEliteAceAndBossWrecks()
    {
        SpaceSlicingRules.HasDataCore(WreckDifficulty.Normal, false).Should().BeFalse();
        SpaceSlicingRules.HasDataCore(WreckDifficulty.Normal, true).Should().BeTrue("an ace carries one");
        SpaceSlicingRules.HasDataCore(WreckDifficulty.Elite, false).Should().BeTrue();
        SpaceSlicingRules.HasDataCore(WreckDifficulty.Boss, false).Should().BeTrue();
        ScanningTuning.WreckDifficultyLevel.Should().Be(WreckDifficulty.Elite, "a derelict's wreck carries a core");
    }

    [Test]
    public void DataCores_TheKillingPartysSixtySecondClaimCoversTheCore()
    {
        var claimants = new HashSet<string> { "killer", "wingman" };

        SpaceSlicingRules.CanSliceDataCore(claimants, new[] { "killer" }, Now, Now.AddSeconds(5)).Should().BeTrue();
        SpaceSlicingRules.CanSliceDataCore(claimants, new[] { "outsider", "wingman" }, Now, Now.AddSeconds(5)).Should().BeTrue(
            "the pilot or the Sensors slicer counts");
        SpaceSlicingRules.CanSliceDataCore(claimants, new[] { "outsider" }, Now, Now.AddSeconds(59)).Should().BeFalse();
        SpaceSlicingRules.CanSliceDataCore(claimants, new[] { "outsider" }, Now, Now.AddSeconds(60)).Should().BeTrue("after the claim anyone may");
        SpaceSlicingRules.CanSliceDataCore(new HashSet<string>(), new[] { "outsider" }, Now, Now).Should().BeTrue("an unclaimed wreck is open");
    }

    [Test]
    public void SlicingClaims_BlockOnlyALiveClaimHeldBySomeoneElse()
    {
        const int now = 1_000_000;

        Slicing.IsClaimHeldByOther("droid:ship-a", now - 5, "droid:ship-b", now).Should().BeTrue("a second ship's droid is refused");
        Slicing.IsClaimHeldByOther("droid:ship-a", now - 5, "player-b", now).Should().BeTrue("so is a player, the droid's own pilot included");
        Slicing.IsClaimHeldByOther("player-a", now - 5, "droid:ship-b", now).Should().BeTrue("a droid cannot take a player's live claim");
        Slicing.IsClaimHeldByOther("droid:ship-a", now - 5, "droid:ship-a", now).Should().BeFalse("the claimant's own claim");
        Slicing.IsClaimHeldByOther("droid:ship-a", now - Slicing.ClaimTimeoutSeconds, "player-b", now).Should().BeFalse("a lapsed claim");
        Slicing.IsClaimHeldByOther("", now, "player-b", now).Should().BeFalse("no claim");
    }

    [Test]
    public void DroidSlicing_TakesTheSharedClaimAndRevalidatesBeforeItResolves()
    {
        var source = ServerSource("Service", "SpaceSlicing.cs").Replace("\r\n", "\n");
        var session = ServerSource("Service", "SlicingService", "SlicingSession.cs").Replace("\r\n", "\n");

        // One claim mechanism: a player's attempt and a droid's slice both claim through TryClaim, with one busy message.
        session.Should().Contain("TryClaim(GetObjectUUID(player), target, source, tier, out error);")
            .And.Contain("public static bool TryClaimTarget(string claimant, uint target, SlicingSourceType source, int tier, out string error) =>\n            TryClaim(claimant, target, source, tier, out error);")
            .And.Contain("if (Slicing.IsClaimHeldByOther(owner, ownerAt, playerId, now))")
            .And.Contain("ReleaseClaim(playerId, session.Target);");

        // A second slicer is refused before the droid starts, and the start itself claims the target.
        var validate = Between(source, "private static string GetDroidSlicingError(", "private static string GetDroidSliceTargetError(");
        validate.Should().Contain("SlicingSession.GetClaimError(DroidClaimId(ship), target)");
        var start = Between(source, "public static void StartDataAnalyzer(", "private static void RunDroidSlicing(");
        start.IndexOf("SlicingSession.TryClaimTarget(claim, target, droidSource, droidTier, out var claimError)", StringComparison.Ordinal)
            .Should().BeGreaterThan(0)
            .And.BeLessThan(start.IndexOf("SetLocalString(ship, DroidSlicingChannelVariable, channel);", StringComparison.Ordinal));

        // Every way the channel ends releases the claim, and the target is revalidated before the slice resolves.
        var run = Between(source, "private static void RunDroidSlicing(", "private static void EndDroidSlicing(");
        run.Should().Contain("SlicingSession.ReleaseClaim(claim, target);\n                return;", "a ship gone mid-slice frees the target")
            .And.Contain("SlicingSession.TouchClaim(claim, target);");
        var broken = run.IndexOf("EndDroidSlicing(ship, target, claim);", StringComparison.Ordinal);
        var revalidate = run.IndexOf("var gone = GetDroidSliceTargetError(ship, target, source);", StringComparison.Ordinal);
        var stops = run.IndexOf("Your droid stops slicing. {gone}", StringComparison.Ordinal);
        var release = run.LastIndexOf("SlicingSession.ReleaseClaim(claim, target);", StringComparison.Ordinal);
        var resolve = run.IndexOf("CompleteSlicing(ship, target, source, tier);", StringComparison.Ordinal);
        broken.Should().BeGreaterThan(0);
        revalidate.Should().BeGreaterThan(broken);
        stops.Should().BeGreaterThan(revalidate);
        release.Should().BeGreaterThan(stops);
        resolve.Should().BeGreaterThan(release);
        Between(source, "private static void EndDroidSlicing(", "public static bool StartSlicing(")
            .Should().Contain("SlicingSession.ReleaseClaim(claim, target);");

        // The revalidation covers each source's own state: the core, the site and the cache still being what was sliced.
        var sliceTarget = Between(source, "private static string GetDroidSliceTargetError(", "/// <summary>\n        /// A Data Analyzer press");
        sliceTarget.Should().Contain("GetSourceTargetError(ship, ship, target, source)")
            .And.Contain("GetAnalyzerTarget(target).Source == source");

        // A single data core pays once, whoever's completion comes second.
        var complete = Between(source, "public static void CompleteSlicing(", "public static void OnSlicingTargetDestroyed(");
        complete.IndexOf("source == SlicingSourceType.WreckDataCore && GetWreck(target)?.HasDataCore != true", StringComparison.Ordinal)
            .Should().BeGreaterThan(0)
            .And.BeLessThan(complete.IndexOf("SlicingReward.RollSpace(", StringComparison.Ordinal));
    }

    private static string Between(string source, string start, string end)
    {
        var from = source.IndexOf(start, StringComparison.Ordinal);
        from.Should().BeGreaterThanOrEqualTo(0, start);
        var to = source.IndexOf(end, from + start.Length, StringComparison.Ordinal);
        to.Should().BeGreaterThan(from, end);
        return source[from..to];
    }

    // Space lockboxes

    [Test]
    public void Lockboxes_DropAtALowRateByDifficulty()
    {
        SpaceSlicingRules.LockboxDropPercent(WreckDifficulty.Normal).Should().Be(2);
        SpaceSlicingRules.LockboxDropPercent(WreckDifficulty.Elite).Should().Be(8);
        SpaceSlicingRules.LockboxDropPercent(WreckDifficulty.Boss).Should().Be(20);
        SpaceSlicingRules.RollsLockbox(WreckDifficulty.Normal, 1).Should().BeTrue();
        SpaceSlicingRules.RollsLockbox(WreckDifficulty.Normal, 2).Should().BeFalse();
        SpaceSlicingRules.RollsLockbox(WreckDifficulty.Boss, 19).Should().BeTrue();

        SpaceSlicingRules.LockboxResref(1).Should().Be("spc_lockbox_t1");
        SpaceSlicingRules.LockboxResref(5).Should().Be("spc_lockbox_t5");
        SpaceSlicingRules.LockboxResref(6).Should().Be("spc_lockbox_t5", "tier 6 ships drop tier 5 boxes");
        SpaceSlicingRules.LockboxTier("spc_lockbox_t3").Should().Be(3);
        SpaceSlicingRules.LockboxTier("lockbox_t3").Should().Be(0, "the ground lockboxes stay their own set");

        ServerSource("Service", "SpaceWrecks.cs").Should().Contain("RollSpaceLockbox(creature, difficulty, tier);");
    }

    [Test]
    public void Lockboxes_HoldComponentsAmmoCreditsAndRareTraitPartsNeverCargoOrSchematics()
    {
        var allowed = new[]
        {
            SpaceSlicingRewardKind.Credits, SpaceSlicingRewardKind.Component, SpaceSlicingRewardKind.RareComponent, SpaceSlicingRewardKind.Ammo,
        };
        SlicingRewardCatalog.GetSpaceBonusWeights(SlicingSourceType.SpaceLockbox).Keys.Should().BeSubsetOf(allowed);

        var ammo = SpaceSlicingTuning.Ammo.Skip(1).SelectMany(x => x).Select(x => x.Resref).ToHashSet();
        ammo.Should().BeSubsetOf(new[]
        {
            ShipModuleActions.MissileResref, ShipModuleActions.ProtonBombResref, ShipModuleActions.ConcussionMissileResref,
            ShipModuleActions.FuelCapsuleResref,
        });

        for (var tier = 1; tier <= 5; tier++)
        {
            for (var seed = 0; seed < 300; seed++)
            {
                var random = new System.Random(seed);
                foreach (var reward in SlicingReward.RollSpace(SlicingSourceType.SpaceLockbox, tier, max => random.Next(max)))
                {
                    reward.Kind.Should().BeOneOf(allowed);
                    if (reward.Kind is SpaceSlicingRewardKind.Component or SpaceSlicingRewardKind.RareComponent)
                        ShipTraitRules.IsComponent(reward.Resref).Should().BeTrue();
                    if (reward.Kind == SpaceSlicingRewardKind.Ammo)
                        ammo.Should().Contain(reward.Resref);
                }
            }
        }
    }

    [Test]
    public void Lockboxes_AreNewEss9ItemsBesideTheGroundOnes()
    {
        SpaceSlicingItems.Lockboxes.Should().Equal("spc_lockbox_t1", "spc_lockbox_t2", "spc_lockbox_t3", "spc_lockbox_t4", "spc_lockbox_t5");
        for (var tier = 1; tier <= 5; tier++)
        {
            var text = File.ReadAllText(RepositoryFile("Module", "uti", $"spc_lockbox_t{tier}.uti.json"));
            using var doc = System.Text.Json.JsonDocument.Parse(text);
            var root = doc.RootElement;
            root.GetProperty("BaseItem").GetProperty("value").GetInt32().Should().Be(523);
            root.GetProperty("ModelPart1").GetProperty("value").GetInt32().Should().Be(241);
            root.GetProperty("StackSize").GetProperty("value").GetInt32().Should().Be(1, "each box keeps its own board");
            root.GetProperty("PropertiesList").GetProperty("value").GetArrayLength().Should().Be(1, "the Activate Item power");
            root.GetProperty("LocalizedName").GetProperty("value").GetProperty("0").GetString()
                .Should().Be($"Sealed Ship Lockbox (Tier {tier})");
            File.Exists(RepositoryFile("Module", "uti", $"lockbox_t{tier}.uti.json")).Should().BeTrue("the ground lockboxes stay");
        }
    }

    // XP

    [Test]
    public void Xp_SpaceSlicingGivesEspionageXpNeverPiloting()
    {
        var session = ServerSource("Service", "SlicingService", "SlicingSession.cs");
        var complete = session[session.IndexOf("private static void Complete(", StringComparison.Ordinal)..];
        complete = complete[..complete.IndexOf("private static void Fail(", StringComparison.Ordinal)];
        complete.IndexOf("GrantXP(session.Player, session.Tier);", StringComparison.Ordinal).Should()
            .BeLessThan(complete.IndexOf("SpaceSlicingRules.IsSpaceSource(session.Source)", StringComparison.Ordinal),
                "space targets earn the same XP before their own completion");
        session.Should().Contain("var xp = Skill.CalculateEspionageXP(PerkType.Slicing, tier, dbSkill.Rank);");
        session.Should().Contain("Skill.GiveSkillXP(player, SkillType.Espionage, xp, false, false);");
        ServerSource("Service", "SpaceSlicing.cs").Should().NotContain("SkillType.Piloting");

        for (var tier = 1; tier <= 5; tier++)
            Skill.CalculateEspionageXP(PerkType.Slicing, tier, Skill.GetEspionageRequiredRank(PerkType.Slicing, tier))
                .Should().BeGreaterThan(0, $"tier {tier} at its unlock rank");
    }

    // Payout

    [Test]
    public void Payout_ExplorationStaysAboutOneHundredFifteenPercentOfItsTiersCombat()
    {
        for (var tier = 1; tier <= 6; tier++)
            ScanningPayoutRules.RatioToCombat(tier).Should().BeInRange(1.12, 1.18, $"tier {tier}");
    }

    [Test]
    public void Payout_DataSitesAndLockedCachesPayLikeEveryOtherSite()
    {
        for (var tier = 1; tier <= 6; tier++)
        {
            var dataSite = ScanningPayoutRules.SiteValue(SignatureType.DataSite, tier) / ScanningPayoutRules.SiteMinutes(SignatureType.DataSite, tier) * 60;
            (dataSite / ScanningTuning.CombatCreditsPerHour[tier]).Should().BeInRange(1.0, 1.3, $"data site tier {tier}");

            var locked = (ScanningTuning.LockedCacheOwnCredits[tier] + ScanningTuning.LockedCacheCredits[tier] + ScanningTuning.ElitePullCredits[tier])
                / (ScanningTuning.LockedCacheMinutes[tier] + ScanningTuning.OverheadMinutes[tier]) * 60;
            (locked / ScanningTuning.CombatCreditsPerHour[tier]).Should().BeInRange(1.0, 1.3, $"locked cache tier {tier}");
        }
    }
}
