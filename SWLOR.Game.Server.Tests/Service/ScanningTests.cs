using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.ShipModuleDefinition;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Scanning and exploration sites (plan section 24, W12a): the Probe Launcher's signal, the Scanner tab's information
/// thresholds, hidden signatures, Jump to Signature, Sensor Sweep, site content and the payout per hour. Numbers come
/// from tools/SpaceRevampBalance/scanning.py (ScanningTuning).
/// </summary>
public class ScanningTests
{
    private static readonly DateTime Now = new(2026, 10, 27, 12, 0, 0, DateTimeKind.Utc);

    private static ScanSignature Signature(SignatureType type = SignatureType.CombatSite, int tier = 3) =>
        SignatureSiteRules.Create("sig-1", "hutlar_orbit", tier, type, 100f, 250f, 0f, Now);

    private static string RepositoryFile(params string[] parts)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        directory.Should().NotBeNull();
        return Path.Combine(new[] { directory!.FullName }.Concat(parts).ToArray());
    }

    // Probe Launcher

    [Test]
    public void ProbeLauncher_FamilyIsAMidSlotPilotingToggle()
    {
        var steps = RevampModuleCatalog.Steps.Where(x => x.Family == ShipModuleFamily.ProbeLauncher).ToList();
        steps.Select(x => x.Rank).Should().Equal(0, 5, 10, 15, 20, 25, 30, 35, 40, 45);
        steps.Should().OnlyContain(x => x.Row == ShipModulePowerType.Mid && x.Skill == SkillType.Piloting &&
                                        x.Activation == ShipModuleActivation.Toggle && x.CycleSeconds == 10.0 &&
                                        x.CapacitorCost == 4 && !x.Capital && x.Texture == "iit_smprobe");
        steps.Select(x => x.ScanStrength).Should().Equal(10, 20, 30, 40, 50, 60, 70, 80, 90, 100);
        steps.Select(x => x.SweepRange).Should().Equal(100, 110, 120, 130, 140, 150, 160, 170, 180, 190);
        ((int)ShipModuleFamily.ProbeLauncher).Should().Be(43);
        ((int)ShipModuleType.ProbeLauncher).Should().Be(24);
    }

    [Test]
    public void ProbeLauncher_RecipesFollowTheComponentRecipes_AndUseSensorSuites()
    {
        var steps = RevampModuleCatalog.Steps.Where(x => x.Family == ShipModuleFamily.ProbeLauncher).ToList();
        steps.Select(x => (int)x.RecipeType).Should().Equal(Enumerable.Range(7488, 10));
        ((int)RecipeType.ShipComponentLifeSupportSystem5).Should().Be(7487, "no earlier recipe moved");
        foreach (var step in steps)
        {
            var tier = Math.Min(5, step.Rank / 10 + 1);
            step.Components.Should().Contain(x => x.Resref == $"sc_sensor_{tier}" && x.Quantity == 1);
        }
    }

    [Test]
    public void ProbeLauncher_IsSelfCentered_AndNotAWeapon()
    {
        ShipToggleRules.GetTargetMode(ShipModuleType.ProbeLauncher).Should().Be(ShipToggleTargetMode.SelfCentered);
        ShipToggleRules.IsOffensive(ShipModuleType.ProbeLauncher).Should().BeFalse();
    }

    // Signal

    [Test]
    public void Signal_GrowsWithEspionage_HullEnvironmentAndEffectiveness()
    {
        ScanningRules.SignalPerCycle(60, 0, 0, 0, 0, 1.0).Should().Be(60);
        ScanningRules.SignalPerCycle(60, 25, 0, 0, 0, 1.0).Should().BeApproximately(75, 1e-9);
        ScanningRules.SignalPerCycle(60, 50, 0, 0, 0, 1.0).Should().BeApproximately(90, 1e-9);
        ScanningRules.SignalPerCycle(60, 80, 0, 0, 0, 1.0).Should().BeApproximately(90, 1e-9, "Espionage caps at rank 50");
        ScanningRules.SignalPerCycle(60, 0, 25, 0, 0, 1.0).Should().BeApproximately(75, 1e-9);
        ScanningRules.SignalPerCycle(60, 0, 0, -50, 0, 1.0).Should().BeApproximately(30, 1e-9, "a weak-scanning environment");
        ScanningRules.SignalPerCycle(60, 0, 0, 0, 0, 0.5).Should().BeApproximately(30, 1e-9, "a droid at half effectiveness");
        ScanningRules.SignalPerCycle(60, 0, 0, -200, 0, 1.0).Should().Be(0, "never negative");
    }

    [Test]
    public void Signal_SurveyProtocolHookAddsNothingYet()
    {
        SpaceCrew.GetDroidSurveyBonusPercent(null).Should().Be(0);
        SpaceCrew.GetDroidSurveyBonusPercent(new ShipDroidCrew()).Should().Be(0);
    }

    [Test]
    public void Signal_EnvironmentStatIsDeclared()
    {
        ((int)StatType.SpaceScanStrengthPercent).Should().Be(1221);
    }

    [Test]
    public void HullScanBonus_ExplorerChissAndHyperionAddTogether()
    {
        ScanningRules.HullScanBonusPercent(ShipRole.Fighter, ShipManufacturer.Republic, "sd_x").Should().Be(0);
        ScanningRules.HullScanBonusPercent(ShipRole.Explorer, ShipManufacturer.Corellian, "sd_x").Should().Be(25);
        ScanningRules.HullScanBonusPercent(ShipRole.Fighter, ShipManufacturer.Chiss, "sd_x").Should().Be(15);
        ScanningRules.HullScanBonusPercent(ShipRole.Explorer, ShipManufacturer.Chiss, "sd_chisshyperion").Should().Be(65);
        ScanningRules.ShowsTierEarly("sd_chisshyperion").Should().BeTrue();
        ScanningRules.ShowsTierEarly("sd_striker").Should().BeFalse();
    }

    [Test]
    public void SignalRequired_ScalesWithTierAndType()
    {
        ScanningRules.SignalRequired(SignatureType.CacheSite, 1).Should().Be(240);
        ScanningRules.SignalRequired(SignatureType.CacheSite, 3).Should().Be(720);
        ScanningRules.SignalRequired(SignatureType.CombatSite, 3).Should().BeApproximately(576, 1e-9);
        ScanningRules.SignalRequired(SignatureType.Derelict, 5).Should().BeApproximately(1440, 1e-9);
    }

    [Test]
    public void Signal_AMatchedProbeResolvesADifficultyOneSiteInTwelveCycles()
    {
        for (var tier = 1; tier <= 5; tier++)
        {
            var strength = RevampModuleCatalog.Steps.Single(x => x.Family == ShipModuleFamily.ProbeLauncher && x.Rank == 10 * tier - 5).ScanStrength;
            var signature = Signature(SignatureType.CacheSite, tier);
            var cycles = 0;
            while (ScanningRules.Percent(signature, "pilot") < 100)
            {
                ScanningRules.AddSignal(signature, "pilot", ScanningRules.SignalPerCycle(strength, 0, 0, 0, 0, 1.0));
                cycles++;
            }

            cycles.Should().Be(12, $"tier {tier}");
        }
    }

    [Test]
    public void Signal_AccumulatesPerScanner_AndCapsAtOneHundred()
    {
        var signature = Signature(SignatureType.CacheSite, 1);
        ScanningRules.AddSignal(signature, "a", 60).Should().Be((0, 25));
        ScanningRules.AddSignal(signature, "a", 60).Should().Be((25, 50));
        ScanningRules.Percent(signature, "b").Should().Be(0, "another ship's signal is its own");
        ScanningRules.AddSignal(signature, "a", 1000).Should().Be((50, 100));
        signature.Signal["a"].Should().Be(signature.SignalRequired);
        ScanningRules.AddSignal(signature, "a", 0).Should().Be((100, 100));
    }

    // Information thresholds

    [Test]
    public void InfoLevels_FollowThePlanThresholds()
    {
        ScanningRules.InfoLevel(0).Should().Be(SignatureInfoLevel.Unknown);
        ScanningRules.InfoLevel(24).Should().Be(SignatureInfoLevel.Unknown);
        ScanningRules.InfoLevel(25).Should().Be(SignatureInfoLevel.Type);
        ScanningRules.InfoLevel(50).Should().Be(SignatureInfoLevel.Tier);
        ScanningRules.InfoLevel(75).Should().Be(SignatureInfoLevel.Threat);
        ScanningRules.InfoLevel(99).Should().Be(SignatureInfoLevel.Threat);
        ScanningRules.InfoLevel(100).Should().Be(SignatureInfoLevel.Located);
        ScanningRules.CrossedLevel(20, 30).Should().Be(SignatureInfoLevel.Type);
        ScanningRules.CrossedLevel(30, 40).Should().Be(SignatureInfoLevel.Unknown);
        ScanningRules.CrossedLevel(20, 80).Should().Be(SignatureInfoLevel.Threat);
    }

    [Test]
    public void ScannerRow_ShowsMoreAsStrengthGrows()
    {
        var signature = Signature(SignatureType.CombatSite, 3);
        const float width = 320f, height = 320f;

        ScanningRules.RowName(signature, 10, false).Should().Be("Unknown signal");
        ScanningRules.RowDetail(signature, 10, width, height).Should().Be("Keep scanning");
        ScanningRules.RowName(signature, 30, false).Should().Be("Combat Site");
        ScanningRules.RowName(signature, 30, true).Should().Be("Combat Site (Tier 3)", "the Hyperion shows the tier from 25%");
        ScanningRules.RowDetail(signature, 30, width, height).Should().Be("Keep scanning");
        ScanningRules.RowName(signature, 50, false).Should().Be("Combat Site (Tier 3)");
        ScanningRules.RowDetail(signature, 50, width, height).Should().Be("North-west sector");
        ScanningRules.RowDetail(signature, 80, width, height).Should().Be("North-west sector, Hostile squad");
        ScanningRules.RowTooltip(signature, 80, width, height).Should().Contain("Loot: Credits and salvage");
        ScanningRules.RowDetail(signature, 100, width, height).Should().Be("At 100, 250, Hostile squad");
    }

    [Test]
    public void Sector_UsesAThreeByThreeGrid_NorthUp()
    {
        ScanningRules.Sector(10, 310, 320, 320).Should().Be("north-west");
        ScanningRules.Sector(160, 310, 320, 320).Should().Be("north");
        ScanningRules.Sector(310, 10, 320, 320).Should().Be("south-east");
        ScanningRules.Sector(160, 160, 320, 320).Should().Be("central");
        ScanningRules.Sector(10, 160, 320, 320).Should().Be("west");
    }

    [Test]
    public void ThresholdMessages_NameWhatWasLearned()
    {
        var signature = Signature(SignatureType.GasSite, 4);
        ScanningRules.ThresholdMessage(signature, SignatureInfoLevel.Unknown, false).Should().BeNull();
        ScanningRules.ThresholdMessage(signature, SignatureInfoLevel.Type, false).Should().Be("Signature resolved: Gas Site.");
        ScanningRules.ThresholdMessage(signature, SignatureInfoLevel.Located, false).Should().StartWith("Signature located: Gas Site (Tier 4).");
    }

    // Hidden until 100%

    [Test]
    public void Signature_StaysHiddenUntilAPartyReachesOneHundred()
    {
        var signature = Signature();
        signature.IsRevealed.Should().BeFalse();
        signature.Objectives.Should().BeEmpty();
        ScanningRules.AddSignal(signature, "a", signature.SignalRequired * 0.99);
        ScanningRules.Percent(signature, "a").Should().Be(99);
        ScanningRules.CanJump(signature, new[] { "a" }).Should().BeFalse("nothing exists before 100%");
    }

    [Test]
    public void Signature_TheProbeCycleOpensTheSiteOnlyAtOneHundred()
    {
        var source = File.ReadAllText(RepositoryFile("SWLOR.Game.Server", "Service", "SpaceScanning.cs")).Replace("\r\n", "\n");
        source.Should().Contain("if (after >= ScanningTuning.InfoLocatedPercent && !signature.IsRevealed)\n                    RevealSite(signature, ship);");
        var sites = File.ReadAllText(RepositoryFile("SWLOR.Game.Server", "Service", "SpaceScanSites.cs"));
        sites.Should().Contain("private static void RevealSite(ScanSignature signature, uint revealer)");
        System.Text.RegularExpressions.Regex.Matches(source + sites, @"RevealSite\(signature, ").Count.Should().Be(1,
            "the probe cycle at 100% is the only caller");
    }

    [Test]
    public void ScannerRows_ListOnlySignaturesThePartyHasSignalOn()
    {
        var a = Signature(SignatureType.CombatSite);
        var b = SignatureSiteRules.Create("sig-2", "hutlar_orbit", 3, SignatureType.CacheSite, 10f, 10f, 0f, Now);
        var c = SignatureSiteRules.Create("sig-3", "hutlar_orbit", 3, SignatureType.Derelict, 10f, 10f, 0f, Now);
        ScanningRules.AddSignal(a, "friend", a.SignalRequired * 0.5);
        ScanningRules.AddSignal(b, "me", b.SignalRequired * 0.3);
        ScanningRules.AddSignal(c, "stranger", c.SignalRequired);

        var rows = ScanningRules.BuildRows(new[] { a, b, c }, new[] { "me", "friend" }, false, 320f, 320f);
        rows.Select(x => x.SignatureId).Should().Equal("sig-1", "sig-2");
        rows[0].Percent.Should().Be(50);
        rows.Should().OnlyContain(x => !x.CanJump);
    }

    // Jump to Signature

    [Test]
    public void Jump_AnyoneInTheScanningPartyMayJump_OthersMustScan()
    {
        var signature = Signature();
        ScanningRules.AddSignal(signature, "scanner", signature.SignalRequired);
        signature.RevealedAt = Now;

        ScanningRules.CanJump(signature, new[] { "partner", "scanner" }).Should().BeTrue();
        ScanningRules.CanJump(signature, new[] { "outsider" }).Should().BeFalse();
        signature.Completed = true;
        ScanningRules.CanJump(signature, new[] { "scanner" }).Should().BeFalse("a completed site is closed");
    }

    [Test]
    public void Jump_StartRefusals()
    {
        var signature = Signature();
        SignatureJumpRules.GetStartRefusal(false, signature, "hutlar_orbit", true, false).Should().Be(SignatureJumpRefusal.NotPiloting);
        SignatureJumpRules.GetStartRefusal(true, null, "hutlar_orbit", true, false).Should().Be(SignatureJumpRefusal.NotFound);
        SignatureJumpRules.GetStartRefusal(true, signature, "viscaraorbit", true, false).Should().Be(SignatureJumpRefusal.OtherArea);
        SignatureJumpRules.GetStartRefusal(true, signature, "hutlar_orbit", false, false).Should().Be(SignatureJumpRefusal.NotLocated);
        SignatureJumpRules.GetStartRefusal(true, signature, "hutlar_orbit", true, true).Should().Be(SignatureJumpRefusal.AlreadySpooling);
        SignatureJumpRules.GetStartRefusal(true, signature, "hutlar_orbit", true, false).Should().Be(SignatureJumpRefusal.None);
    }

    [Test]
    public void Jump_SpoolTakesFiveSeconds_AndDamageOrInterdictionBreaksIt()
    {
        ScanningTuning.JumpSpoolSeconds.Should().Be(5.0);
        SignatureJumpRules.IsSpoolDone(Now, Now.AddSeconds(4.9)).Should().BeFalse();
        SignatureJumpRules.IsSpoolDone(Now, Now.AddSeconds(5)).Should().BeTrue();
        SignatureJumpRules.GetCompletionRefusal(true, false, true, true).Should().Be(SignatureJumpRefusal.Damaged);
        SignatureJumpRules.GetCompletionRefusal(false, true, true, true).Should().Be(SignatureJumpRefusal.Interdicted);
        SignatureJumpRules.GetCompletionRefusal(false, false, false, true).Should().Be(SignatureJumpRefusal.SiteClosed);
        SignatureJumpRules.GetCompletionRefusal(false, false, true, false).Should().Be(SignatureJumpRefusal.SiteClosed);
        SignatureJumpRules.GetCompletionRefusal(false, false, true, true).Should().Be(SignatureJumpRefusal.None);
        foreach (var refusal in Enum.GetValues<SignatureJumpRefusal>().Where(x => x != SignatureJumpRefusal.None))
            SignatureJumpRules.Message(refusal).Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public void Jump_ShipDamageInterruptsTheSpool()
    {
        var space = File.ReadAllText(RepositoryFile("SWLOR.Game.Server", "Service", "Space.cs"));
        var damage = space[space.IndexOf("public static (int ShieldDamage, int HullDamage) ApplyShipDamage(", StringComparison.Ordinal)..];
        damage = damage[..damage.IndexOf("// Apply death once the hull has reached zero.", StringComparison.Ordinal)];
        damage.Should().Contain("InterruptSignatureJump(target);");
    }

    [Test]
    public void Jump_ArrivesTwentyMetersFromTheSiteOnTheNearSide()
    {
        var (x, y) = SignatureJumpRules.Arrival(0f, 0f, 100f, 0f);
        x.Should().BeApproximately(80f, 0.01f);
        y.Should().BeApproximately(0f, 0.01f);
        var (sx, sy) = SignatureJumpRules.Arrival(50f, 50f, 50f, 50f);
        (sx, sy).Should().Be((70f, 50f));
    }

    [Test]
    public void Jump_FeatFollowsTheModuleFeats()
    {
        ((int)FeatType.ShipJumpToSignature).Should().Be(3358);
        var feats = File.ReadAllLines(RepositoryFile("SWLOR_Haks", "sw_2da", "feat.2da"));
        var row = feats.Single(x => x.StartsWith("3358 ", StringComparison.Ordinal));
        row.Should().Contain("ShipJumpToSignature").And.Contain("16970116").And.Contain("16970117").And.Contain("ife_sigjump");
    }

    // Sensor Sweep

    [Test]
    public void Sweep_HasItsOwnCooldown()
    {
        SensorSweepRules.IsReady(null, Now).Should().BeTrue();
        SensorSweepRules.SecondsLeft(null, Now).Should().Be(0);
        SensorSweepRules.IsReady(Now, Now.AddSeconds(59)).Should().BeFalse();
        SensorSweepRules.SecondsLeft(Now, Now.AddSeconds(59)).Should().Be(1);
        SensorSweepRules.IsReady(Now, Now.AddSeconds(60)).Should().BeTrue();
        SensorSweepRules.IsResultFresh(Now, Now.AddSeconds(59)).Should().BeTrue();
        SensorSweepRules.IsResultFresh(Now, Now.AddSeconds(60)).Should().BeFalse();
    }

    [Test]
    public void Sweep_ListsEveryShipInRange_CloakedIncluded_NearestFirst()
    {
        SensorSweepRules.Range(0).Should().Be(100);
        SensorSweepRules.Range(45).Should().Be(190);
        SensorSweepRules.Includes(99f, 100, false).Should().BeTrue();
        SensorSweepRules.Includes(99f, 100, true).Should().BeTrue();
        SensorSweepRules.Includes(101f, 100, false).Should().BeFalse();

        var contacts = Enumerable.Range(0, 30).Select(i => new SensorSweepContact { Name = $"Ship {i}", Distance = 100 - i }).ToList();
        var ordered = SensorSweepRules.Order(contacts);
        ordered.Should().HaveCount(ScanningTuning.SweepMaximumShips);
        ordered[0].Distance.Should().Be(71);
        SensorSweepRules.Describe(new SensorSweepContact { Name = "Raider", Distance = 42.4f, Size = ShipSize.Small, Hostile = true, Cloaked = true })
            .Should().Be("Raider (Small, 42 m), hostile, cloaked");
    }

    // Spawning and despawning

    [Test]
    public void Spawning_CoversTheOrbitsAndZones_WithMoreSignaturesInZones()
    {
        ScanningTuning.Areas.Should().HaveCount(13);
        ScanningTuning.Areas.Where(x => x.AreaResref is "space_viscbelt" or "space_midrimneb" or "space_korrdebris")
            .Should().OnlyContain(x => x.Signatures == 4);
        ScanningTuning.Areas.Where(x => x.AreaResref is not ("space_viscbelt" or "space_midrimneb" or "space_korrdebris"))
            .Should().OnlyContain(x => x.Signatures == 3);
        SignatureSiteRules.GetArea("hutlar_orbit")!.Tier.Should().Be(3);
        SignatureSiteRules.GetArea("zomb_abanstation").Should().BeNull();
    }

    [Test]
    public void Spawning_NoRiftsOrDataSitesYet_GasFromTierThree()
    {
        for (var tier = 1; tier <= 6; tier++)
        {
            var weights = SignatureSiteRules.GetWeights(tier);
            weights[SignatureType.Rift].Should().Be(0);
            weights[SignatureType.DataSite].Should().Be(0);
            weights[SignatureType.GasSite].Should().Be(tier >= 3 ? 20 : 0);

            var seen = new HashSet<SignatureType>();
            var total = weights.Values.Sum();
            for (var roll = 0; roll < total; roll++)
                seen.Add(SignatureSiteRules.PickType(tier, _ => roll));

            seen.Should().NotContain(SignatureType.Rift).And.NotContain(SignatureType.DataSite).And.NotContain(SignatureType.Invalid);
            seen.Contains(SignatureType.GasSite).Should().Be(tier >= 3);
        }
    }

    [Test]
    public void Spawning_SiteContentExistsForEveryTier()
    {
        var root = RepositoryFile("Module");
        for (var tier = 1; tier <= 6; tier++)
        {
            foreach (var tag in SignatureSiteRules.CombatSquad(tier).Concat(SignatureSiteRules.DerelictDefenses(tier)))
                File.Exists(Path.Combine(root, "utc", $"{tag}.utc.json")).Should().BeTrue(tag);

            File.Exists(Path.Combine(root, "utp", $"{SignatureSiteRules.GasCloud(tier)}.utp.json")).Should().BeTrue();
            SignatureSiteRules.CombatSquad(tier).Should().OnlyContain(x => x.StartsWith($"t{tier}", StringComparison.Ordinal));
        }

        File.Exists(Path.Combine(root, "utp", $"{ScanningTuning.CacheResref}.utp.json")).Should().BeTrue();
        SignatureSiteRules.CacheLootTable(3).Should().Be("SPACE_ELITE_T3");
    }

    [Test]
    public void Despawning_HiddenSignaturesFade_OpenSitesTimeOut_CompletedSitesEnd()
    {
        var signature = Signature();
        signature.ExpiresAt.Should().Be(Now.AddSeconds(3600));
        SignatureSiteRules.IsOver(signature, Now.AddSeconds(3599)).Should().BeFalse();
        SignatureSiteRules.IsOver(signature, Now.AddSeconds(3600)).Should().BeTrue();

        signature.RevealedAt = Now.AddSeconds(3500);
        SignatureSiteRules.IsOver(signature, Now.AddSeconds(3600)).Should().BeFalse("an open site lasts its own lifetime");
        SignatureSiteRules.IsOver(signature, Now.AddSeconds(3500 + 1200)).Should().BeTrue();

        var done = Signature();
        done.Completed = true;
        SignatureSiteRules.IsOver(done, Now).Should().BeTrue();
    }

    [Test]
    public void Sites_CacheSpawnsOnceTheObjectivesAreDone()
    {
        var combat = Signature(SignatureType.CombatSite);
        SignatureSiteRules.ShouldSpawnCache(combat, 0).Should().BeFalse("a hidden site has no cache");
        combat.RevealedAt = Now;
        SignatureSiteRules.ShouldSpawnCache(combat, 2).Should().BeFalse();
        SignatureSiteRules.ShouldSpawnCache(combat, 0).Should().BeTrue();
        combat.CacheSpawned = true;
        SignatureSiteRules.ShouldSpawnCache(combat, 0).Should().BeFalse();

        var cache = Signature(SignatureType.CacheSite);
        cache.RevealedAt = Now;
        SignatureSiteRules.ShouldSpawnCache(cache, 0).Should().BeFalse("a cache site is its own cache");
    }

    [Test]
    public void Sites_RingOffsetsSpreadTheContent()
    {
        SignatureSiteRules.RingOffset(0, 1, 10f).Should().Be((0f, 0f));
        var (x, y) = SignatureSiteRules.RingOffset(1, 4, 10f);
        x.Should().BeApproximately(0f, 0.01f);
        y.Should().BeApproximately(10f, 0.01f);
    }

    // Payout

    [Test]
    public void Payout_ExplorationPaysOneHundredToOneHundredThirtyPercentOfItsTiersCombat()
    {
        for (var tier = 1; tier <= 6; tier++)
            ScanningPayoutRules.RatioToCombat(tier).Should().BeInRange(1.0, 1.3, $"tier {tier}");

        ScanningTuning.CombatCreditsPerHour[3].Should().BeApproximately(8500, 1);
        ScanningPayoutRules.CreditsPerHour(3).Should().BeInRange(8500, 8500 * 1.3);
    }

    [Test]
    public void Payout_EveryTypePaysAboutTheSamePerHour()
    {
        for (var tier = 1; tier <= 6; tier++)
        {
            foreach (var (type, weight) in SignatureSiteRules.GetWeights(tier).Where(x => x.Value > 0))
            {
                var perHour = ScanningPayoutRules.SiteValue(type, tier) / ScanningPayoutRules.SiteMinutes(type, tier) * 60;
                (perHour / ScanningTuning.CombatCreditsPerHour[tier]).Should().BeInRange(1.0, 1.3, $"{type} tier {tier}");
            }
        }
    }

    [Test]
    public void Payout_CachesPayCredits()
    {
        SignatureSiteRules.SiteCredits(SignatureType.CacheSite, 3).Should().BeGreaterThan(0);
        SignatureSiteRules.SiteCredits(SignatureType.Rift, 3).Should().Be(0);
        foreach (var credits in ScanningTuning.SiteCredits.Values.SelectMany(x => x.Skip(1)))
            (credits % 25).Should().Be(0);
    }

    // HUD

    [Test]
    public void ScannerHud_Texts()
    {
        ScannerHudRules.Summary(0, false).Should().Contain("Fit a Probe Launcher");
        ScannerHudRules.Summary(0, true).Should().Contain("Switch on your Probe Launcher");
        ScannerHudRules.Summary(1, true).Should().Be("1 signature");
        ScannerHudRules.Summary(3, true).Should().Be("3 signatures");
        ScannerHudRules.SweepText(true, 0).Should().Be("Sensor Sweep");
        ScannerHudRules.SweepText(true, 12).Should().Be("Sensor Sweep (12 s)");
        ScannerHudRules.SweepText(false, 0).Should().Contain("no Probe Launcher");
    }
}
