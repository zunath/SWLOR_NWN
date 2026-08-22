using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.Espionage;
using SWLOR.Game.Server.Feature.ItemDefinition;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SlicingService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class EspionageSystemTests
{
    [Test]
    public void StealthAndDetectionRatings_UseTheCommittedAttributeFormulas()
    {
        Stat.CalculateDetectionRating(12, 9, 4, 10, false).Should().Be(35);
        Stat.CalculateDetectionRating(12, 9, 4, 10, true).Should().Be(40);
        Stat.CalculateStealthRating(12, 6, 10).Should().Be(40);

        Stat.CalculateDetectionRating(-20, 0, 0, 0, false).Should().Be(0);
        Stat.CalculateStealthRating(-20, 0, 0).Should().Be(0);
    }

    [Test]
    public void StealthScaling_ProtectsACommittedSneakWhileAllowingACommittedSpotterToCounter()
    {
        var baselineNpcDetection = Stat.CalculateDetectionRating(10, 10, 0, 0, false);
        var rankFourStealth = Stat.CalculateStealthRating(10, 0, 20);
        CalculateDetectionChance(baselineNpcDetection, rankFourStealth).Should().Be(0m);

        var committedSpotter = Stat.CalculateDetectionRating(27, 27, 0, 20, true);
        var committedSneak = Stat.CalculateStealthRating(27, 0, 20);
        CalculateDetectionChance(committedSpotter, committedSneak).Should().Be(0.7m);
    }

    [Test]
    public void NPCDetection_IsCappedWithoutLimitingCommittedPlayers()
    {
        Stat.MaximumNPCDetection.Should().Be(50);
        Stat.ApplyNPCDetectionCap(127, true).Should().Be(50);
        Stat.ApplyNPCDetectionCap(35, true).Should().Be(35);
        Stat.ApplyNPCDetectionCap(74, false).Should().Be(74);
        Stat.ApplyNPCDetectionCap(-10, true).Should().Be(0);

        var maximumRankFourStealth = Stat.CalculateStealthRating(27, 0, 20);
        CalculateDetectionChance(
                Stat.ApplyNPCDetectionCap(127, true),
                maximumRankFourStealth)
            .Should()
            .Be(0m, "even the strongest NPC must not automatically pierce maximum committed Stealth");

        var statSource = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "SWLOR.Game.Server",
            "Service",
            "Stat.cs"));
        statSource.Should().Contain("!GetIsDMPossessed(creature)",
            "DM-possessed creatures must retain uncapped staff Detection");
    }

    [Test]
    public void StealthPerks_GrantFlatRankBonusesAndSilentStrideOnlyBoostsMovementWhileHidden()
    {
        var stealth = BuildPerkWithout2daLookup(
            new EspionagePerkDefinition(),
            "Stealth",
            PerkType.Stealth);

        stealth.PerkLevels[1].StatBonuses.Single(x => x.Stat == StatType.ActiveStealthBonus).Calculate(0).Should().Be(5);
        stealth.PerkLevels[2].StatBonuses.Single(x => x.Stat == StatType.ActiveStealthBonus).Calculate(0).Should().Be(10);
        stealth.PerkLevels[3].StatBonuses.Single(x => x.Stat == StatType.ActiveStealthBonus).Calculate(0).Should().Be(15);
        stealth.PerkLevels[4].StatBonuses.Single(x => x.Stat == StatType.ActiveStealthBonus).Calculate(0).Should().Be(20);
        stealth.PerkLevels.Values
            .SelectMany(x => x.StatBonuses)
            .Should().NotContain(x => x.Stat == StatType.Stealth,
                "the rank bonus applies only while the native stealth mode is active");
        stealth.PurchasedTriggers.Should().ContainSingle()
            .Which.Method.Name.Should().Be("RefreshActiveStatusAfterPerkLevelChange");
        stealth.RefundedTriggers.Should().ContainSingle()
            .Which.Method.Name.Should().Be("RefreshActiveStatusAfterPerkLevelChange");

        var silentStride = BuildPerkWithout2daLookup(
            new EspionagePerkDefinition(),
            "SilentStride",
            PerkType.SilentStride).PerkLevels[1];
        silentStride.StatBonuses
            .Single(x => x.Stat == StatType.StealthMovementSpeedPercentAdjustment)
            .Calculate(0)
            .Should().Be(30);
        silentStride.StatBonuses
            .Single(x => x.Stat == StatType.StealthStaminaDrainReductionPercent)
            .Calculate(0)
            .Should().Be(20);

        var statusSource = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "SWLOR.Game.Server",
            "Feature",
            "StatusEffectDefinition",
            "StealthStatusEffect.cs"));
        statusSource.Should().Contain("StatType.StealthMovementSpeedPercentAdjustment");
        statusSource.Should().Contain("Stat.GetStatAdjustment(creature, StatType.ActiveStealthBonus)");
        statusSource.Should().Contain("StatGroup.Stats[StatType.Stealth] = stealthBonus;");
        statusSource.Should().Contain("StatGroup.Stats[StatType.MovementSpeedPercentAdjustment] = movementSpeedBonus;");

        var stealthServiceSource = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "SWLOR.Game.Server",
            "Service",
            "Stealth.cs"));
        var refreshStart = stealthServiceSource.IndexOf(
            "public static void RefreshActiveStatusAfterPerkLevelChange(uint creature)",
            StringComparison.Ordinal);
        var refreshEnd = stealthServiceSource.IndexOf(
            "public static void RecordPlayerCombatInitiation()",
            refreshStart,
            StringComparison.Ordinal);
        refreshStart.Should().BeGreaterThanOrEqualTo(0);
        refreshEnd.Should().BeGreaterThan(refreshStart);
        var refreshBody = stealthServiceSource[refreshStart..refreshEnd];
        refreshBody.Should().Contain("StatusEffect.RemoveStatusEffect<StealthStatusEffect>(creature);");
        refreshBody.Should().Contain("Perk.GetPerkLevel(creature, PerkType.Stealth) <= 0");
        refreshBody.Should().Contain("SetActionMode(creature, ActionMode.Stealth, false);");
        refreshBody.Should().Contain("StatusEffect.ApplyStatusEffect<StealthStatusEffect>(creature, creature, 0f);");
        refreshBody.IndexOf("StatusEffect.RemoveStatusEffect<StealthStatusEffect>(creature);", StringComparison.Ordinal)
            .Should().BeLessThan(refreshBody.IndexOf(
                "StatusEffect.ApplyStatusEffect<StealthStatusEffect>(creature, creature, 0f);",
                StringComparison.Ordinal),
                "a rank change must remove the old stat snapshot before applying the new one");
    }

    [Test]
    public void AlertnessRanks_ProvideTheDocumentedDetectionCounter()
    {
        var alertness = BuildPerkWithout2daLookup(
            new ArmorPerkDefinition(),
            "Alertness",
            PerkType.Alertness);

        alertness.PerkLevels[1].StatBonuses.Single(x => x.Stat == StatType.Detection).Calculate(0).Should().Be(10);
        alertness.PerkLevels[2].StatBonuses.Single(x => x.Stat == StatType.Detection).Calculate(0).Should().Be(15);
        alertness.PerkLevels[3].StatBonuses.Single(x => x.Stat == StatType.Detection).Calculate(0).Should().Be(20);
    }

    [Test]
    public void StealthPerk_UsesTheNativeActionWithoutGrantingADuplicateAbility()
    {
        var root = FindRepositoryRoot();
        var stealth = BuildPerkWithout2daLookup(
            new EspionagePerkDefinition(),
            "Stealth",
            PerkType.Stealth);
        stealth.PerkLevels.Values.Should().OnlyContain(level => level.GrantedFeats.Count == 0,
            "the NWN Stealth action is the sole player-facing toggle");
        stealth.HotBarActionModes.Should().ContainSingle()
            .Which.Should().Be(ActionMode.Stealth,
                "the perk metadata should declaratively add the native action to the hotbar");
        stealth.IconResref.Should().Be("ife_stealth1",
            "removing the duplicate ability must preserve Stealth's existing perk-menu artwork");
        File.Exists(Path.Combine(
                root,
                "SWLOR.Game.Server",
                "Feature",
                "AbilityDefinition",
                "Espionage",
                "StealthAbilityDefinition.cs"))
            .Should().BeFalse("Stealth must not have a duplicate custom ability definition");

        var stealthSource = File.ReadAllText(Path.Combine(
            root,
            "SWLOR.Game.Server",
            "Service",
            "Stealth.cs"));
        stealthSource.Should().Contain("ScriptName.OnStealthEnterBefore");
        stealthSource.Should().Contain("ScriptName.OnStealthEnterAfter");
        stealthSource.Should().Contain("ScriptName.OnStealthExitAfter");
        stealthSource.Should().Contain("!GetActionMode(creature, ActionMode.Stealth)");
        stealthSource.Should().Contain("Perk.GetPerkLevel(creature, PerkType.Stealth) > 0");
        stealthSource.Should().Contain("enteredDuringCombatWithoutWindow");
        stealthSource.Should().Contain("StatusEffect.RemoveStatusEffect<StealthStatusEffect>(creature);");

        var auditSource = File.ReadAllText(Path.Combine(
            root,
            "tools",
            "UpdateCombatUpgradeAudit.ps1"));
        auditSource.Should().Contain("Import-NativeActionModePerkNameIndex");
        auditSource.Should().Contain(".AutoAddActionModeToHotBar");
        auditSource.Should().Contain(
            "if ($isActiveType -and !$usesNativeActionMode -and !$abilityBaseNameIndex.ContainsKey($rowBaseName))",
            "native action-mode metadata should suppress only the custom-ability audit requirement");
    }

    [Test]
    public void SuccessfulSpotDetection_ExitsPlayerStealth()
    {
        var root = FindRepositoryRoot();
        var stealthSource = File.ReadAllText(Path.Combine(
            root,
            "SWLOR.Game.Server",
            "Service",
            "Stealth.cs"));
        var aiSource = File.ReadAllText(Path.Combine(
            root,
            "SWLOR.Game.Server",
            "Service",
            "AI.cs"));

        var resolveStart = stealthSource.IndexOf(
            "private static bool ResolveDetection(uint observer, uint target, bool acquireAggroOnDetection)",
            StringComparison.Ordinal);
        var resolveEnd = stealthSource.IndexOf(
            "private static void ExitDetectedPlayerStealth(uint observer, uint target)",
            resolveStart,
            StringComparison.Ordinal);
        resolveStart.Should().BeGreaterThanOrEqualTo(0);
        resolveEnd.Should().BeGreaterThan(resolveStart);
        var resolveDetection = stealthSource[resolveStart..resolveEnd];

        var exitIndex = resolveDetection.IndexOf("ExitDetectedPlayerStealth(observer, target);", StringComparison.Ordinal);
        var acquireGuardIndex = resolveDetection.IndexOf("if (acquireAggroOnDetection)", StringComparison.Ordinal);
        var acquireIndex = resolveDetection.IndexOf(
            "AI.TryAcquireAggroAfterDetection(observer, target);",
            StringComparison.Ordinal);

        resolveDetection.Should().Contain("if (detected)");
        exitIndex.Should().BeGreaterThanOrEqualTo(0);
        acquireGuardIndex.Should().BeGreaterThan(exitIndex);
        acquireIndex.Should().BeGreaterThan(acquireGuardIndex);
        stealthSource.Should().Contain("private static void ExitDetectedPlayerStealth(uint observer, uint target)");
        stealthSource.Should().Contain("!GetIsPC(target) ||");
        stealthSource.Should().Contain("GetIsDM(target) ||");
        stealthSource.Should().Contain("!GetActionMode(target, ActionMode.Stealth)");
        stealthSource.Should().Contain("SetActionMode(target, ActionMode.Stealth, false);");
        stealthSource.Should().Contain("DelayCommand(0f, () =>");
        stealthSource.Should().Contain("StatusEffect.RemoveStatusEffect<StealthStatusEffect>(target);");
        stealthSource.Should().Contain("ResolveDetection(observer, target, true)");
        stealthSource.Should().Contain("ResolveDetection(observer, target, false)");
        stealthSource.Should().Contain("Stealth perk refresh removed active status snapshot");
        stealthSource.Should().Contain("Stealth perk refresh reapplied active status snapshot");
        stealthSource.Should().Contain("Stealth perk full refund forced native stealth exit");
        stealthSource.Should().Contain("Stealth detection forced native stealth exit");
        aiSource.Should().Contain("public static void TryAcquireAggroAfterDetection(uint observer, uint target)");
        aiSource.Should().Contain("if (!IsAIEnabled(observer))");
        aiSource.Should().Contain("if (!TryAcquireAggro(observer, target))");
        var successfulAggroIndex = aiSource.IndexOf("if (!TryAcquireAggro(observer, target))", StringComparison.Ordinal);
        var successfulAggroLogIndex = aiSource.IndexOf("Stealth detection acquired normal proximity aggro", StringComparison.Ordinal);
        successfulAggroIndex.Should().BeGreaterThanOrEqualTo(0);
        successfulAggroLogIndex.Should().BeGreaterThan(successfulAggroIndex,
            "the handoff log must describe a completed acquisition, not a rejected attempt");
    }

    [Test]
    public void SlicingRanksGrantTheDocumentedTraceBonuses()
    {
        Slicing.GetTraceBonus(0, 0, 0).Should().Be(0);
        Slicing.GetTraceBonus(2, 0, 0).Should().Be(0);
        Slicing.GetTraceBonus(3, 0, 0).Should().Be(1);
        Slicing.GetTraceBonus(4, 0, 0).Should().Be(2);
        Slicing.GetTraceBonus(5, 0, 0).Should().Be(3);
    }

    [Test]
    public void SilentStrideReducesTheDrainRateRatherThanOnlyExtendingTheIntervalByTwentyPercent()
    {
        StealthStatusEffect.CalculateDrainFrequencySeconds(0).Should().BeApproximately(6f, 0.001f);
        StealthStatusEffect.CalculateDrainFrequencySeconds(20).Should().BeApproximately(7.5f, 0.001f);
        StealthStatusEffect.CalculateDrainFrequencySeconds(100).Should().BeApproximately(60f, 0.001f);
    }

    [Test]
    public void InfiltrationXp_UsesFullSuccessAndFifteenPercentDetectionFailureAwards()
    {
        EspionageInfiltration.RequiredTravelDistanceMeters.Should().Be(4f);
        EspionageInfiltration.DetectionFailureXpPercent.Should().Be(0.15f);
        EspionageInfiltration.HostileFactionId.Should().Be(1);

        EspionageInfiltration.CalculateXp(20, 20, false).Should().Be(600);
        EspionageInfiltration.CalculateXp(20, 20, true).Should().Be(90);
        EspionageInfiltration.CalculateXp(16, 20, false).Should().Be(76);
        EspionageInfiltration.CalculateXp(16, 20, true).Should().Be(11);
        EspionageInfiltration.CalculateXp(15, 20, false).Should().Be(0);
        EspionageInfiltration.CalculateXp(15, 20, true).Should().Be(0);
    }

    [Test]
    public void InfiltrationSuccess_RequiresDetectionEvasionTravelStealthAndNoCombatEnmity()
    {
        EspionageInfiltration.MeetsSuccessRequirements(true, 4f, true, false).Should().BeTrue();

        EspionageInfiltration.MeetsSuccessRequirements(false, 4f, true, false).Should().BeFalse();
        EspionageInfiltration.MeetsSuccessRequirements(true, 3.99f, true, false).Should().BeFalse();
        EspionageInfiltration.MeetsSuccessRequirements(true, 4f, false, false).Should().BeFalse();
        EspionageInfiltration.MeetsSuccessRequirements(true, 4f, true, true).Should().BeFalse();
    }

    [TestCase(true, false, true, false, false)]
    [TestCase(true, false, false, false, false)]
    [TestCase(true, false, true, true, true)]
    [TestCase(true, false, false, true, true)]
    [TestCase(false, false, true, false, true)]
    [TestCase(false, false, false, false, false)]
    [TestCase(false, false, true, true, true)]
    [TestCase(false, false, false, true, true)]
    [TestCase(true, true, true, false, true)]
    [TestCase(true, true, false, false, true)]
    [TestCase(false, true, false, false, true)]
    public void InfiltrationDetectionOutcome_RejectsPlayerInitiatedAndUnrelatedCombat(
        bool detected,
        bool playerInitiatedCombat,
        bool hasPairCombatEnmity,
        bool hasUnrelatedCombatEnmity,
        bool expected)
    {
        EspionageInfiltration.ShouldRejectDetectionOutcome(
                detected,
                playerInitiatedCombat,
                hasPairCombatEnmity,
                hasUnrelatedCombatEnmity)
            .Should()
            .Be(expected);
    }

    [Test]
    public void InfiltrationXp_IsDrivenByAggroAndDetectionEventsRatherThanElapsedStealthTime()
    {
        var root = FindRepositoryRoot();
        var stealthStatusSource = File.ReadAllText(Path.Combine(
            root,
            "SWLOR.Game.Server",
            "Feature",
            "StatusEffectDefinition",
            "StealthStatusEffect.cs"));
        var stealthSource = File.ReadAllText(Path.Combine(
            root,
            "SWLOR.Game.Server",
            "Service",
            "Stealth.cs"));
        var aiSource = File.ReadAllText(Path.Combine(
            root,
            "SWLOR.Game.Server",
            "Service",
            "AI.cs"));
        var infiltrationSource = File.ReadAllText(Path.Combine(
            root,
            "SWLOR.Game.Server",
            "Service",
            "EspionageInfiltration.cs"));

        stealthStatusSource.Should().NotContain("GrantTimeInStealthXP");
        stealthStatusSource.Should().NotContain("HostileScanRadiusMeters");
        stealthStatusSource.Should().Contain("EspionageInfiltration.UpdateMovement(creature);");
        stealthSource.Should().Contain("EspionageInfiltration.RecordDetection(observer, target, detected);");
        stealthSource.Should().Contain("[NWNEventHandler(ScriptName.OnCreatureAttackBefore)]");
        stealthSource.Should().Contain("EspionageInfiltration.RecordPlayerCombatInitiation(attacker);");
        stealthSource.Should().Contain("EspionageInfiltration.CancelPlayer(creature);");
        aiSource.Should().Contain("EspionageInfiltration.TryBegin(entering, self);");
        aiSource.Should().Contain("if (!Stealth.CanAcquireAggro(self, entering))");
        aiSource.Should().Contain("EspionageInfiltration.Complete(exiting, self);");
        aiSource.IndexOf("EspionageInfiltration.TryBegin(entering, self);", StringComparison.Ordinal)
            .Should().BeLessThan(
                aiSource.IndexOf("if (!Stealth.CanAcquireAggro(self, entering))", StringComparison.Ordinal),
                "the attempt must exist before the first opposed detection result is recorded");
        aiSource.IndexOf("if (!Stealth.CanAcquireAggro(self, entering))", StringComparison.Ordinal)
            .Should().BeLessThan(
                aiSource.IndexOf("TryAcquireAggro(self, entering);", StringComparison.Ordinal),
                "an undetected stealthed player must not enter the hostile's enmity table");
        infiltrationSource.Should().Contain("private const float MovementSampleIntervalSeconds = 1f;");
        infiltrationSource.Should().Contain("DelayCommand(MovementSampleIntervalSeconds, () => SampleMovement(player, samplerId));");
        infiltrationSource.Should().Contain("CreaturePlugin.GetFaction(npc) != HostileFactionId");
        infiltrationSource.Should().Contain("Enmity.HasNonProximityEnmity(npc)");
        infiltrationSource.Should().Contain("Enmity.HasNonProximityEnmity(target, observer)");
        infiltrationSource.Should().Contain("Enmity.HasNonProximityEnmityOutsidePair(target, observer)");
        infiltrationSource.Should().Contain("Enmity.HasNonProximityEnmityForCreature(player) ||");
        infiltrationSource.Should().Contain("var master = GetMaster(npc);");
    }

    [Test]
    public void LastingCoatingsRaisesChargesFromTwentyToThirty()
    {
        VenomCoatingItemDefinition.CalculateCharges(0).Should().Be(20);
        VenomCoatingItemDefinition.CalculateCharges(50).Should().Be(30);
        VenomCoatingItemDefinition.CalculateCharges(-50).Should().Be(20);
    }

    [Test]
    public void VenomExpertiseRaisesDamageWhileTierControlsDuration()
    {
        VenomStatusEffect.CalculateBaseDamagePerTick(0).Should().Be(8);
        VenomStatusEffect.CalculateBaseDamagePerTick(10).Should().Be(9);
        VenomStatusEffect.CalculateBaseDamagePerTick(20).Should().Be(10);
        VenomStatusEffect.CalculateBaseDamagePerTick(30).Should().Be(11);

        Poisons.GetVenomDurationSeconds(1).Should().BeApproximately(12f, 0.001f);
        Poisons.GetVenomDurationSeconds(3).Should().BeApproximately(24f, 0.001f);
        Poisons.GetVenomDurationSeconds(5).Should().BeApproximately(36f, 0.001f);
    }

    [Test]
    public void SlicingRankGate_IsCentralizedForLockboxesAndTerminals()
    {
        var root = FindRepositoryRoot();
        var references = Directory
            .EnumerateFiles(Path.Combine(root, "SWLOR.Game.Server"), "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.EndsWith("EspionagePerkDefinition.cs", StringComparison.Ordinal))
            .Where(file => !file.EndsWith("PerkType.cs", StringComparison.Ordinal))
            .Where(file => File.ReadAllText(file).Contains("PerkType.Slicing", StringComparison.Ordinal))
            .Select(file => Path.GetRelativePath(root, file))
            .ToArray();

        references.Should().Equal(Path.Combine(
            "SWLOR.Game.Server",
            "Service",
            "SlicingService",
            "SlicingSession.cs"));
    }

    [Test]
    public void EspionageActivePayloads_MatchTheReviewedBibleValues()
    {
        var root = FindRepositoryRoot();
        var folder = Path.Combine(root, "SWLOR.Game.Server", "Feature", "AbilityDefinition", "Espionage");
        var tacticalEscape = File.ReadAllText(Path.Combine(folder, "TacticalEscapeAbilityDefinition.cs"));
        var shadowStep = File.ReadAllText(Path.Combine(folder, "ShadowStepAbilityDefinition.cs"));
        var ghostProtocol = File.ReadAllText(Path.Combine(folder, "GhostProtocolAbilityDefinition.cs"));
        var razorTrap = File.ReadAllText(Path.Combine(folder, "RazorTrapAbilityDefinition.cs"));
        var shockTrap = File.ReadAllText(Path.Combine(folder, "ShockTrapAbilityDefinition.cs"));

        tacticalEscape.Should().Contain("private const float EvasionDurationSeconds = 30f;");
        tacticalEscape.Should().Contain("TacticalEscape(builder, FeatType.TacticalEscape1, \"Tactical Escape I\", 1, 8, 35, 8, false);");
        tacticalEscape.Should().Contain("TacticalEscape(builder, FeatType.TacticalEscape2, \"Tactical Escape II\", 2, 12, 60, 12, true);");
        tacticalEscape.Should().Contain("Enmity.ReduceEnmityOnAll(activator, enmityReductionPercent);");

        shadowStep.Should().Contain("private const float EvasionDurationSeconds = 30f;");
        shadowStep.Should().Contain("ShadowStep(builder, FeatType.ShadowStep1, \"Shadow Step I\", 1, 10, 10, false);");
        shadowStep.Should().Contain("ShadowStep(builder, FeatType.ShadowStep2, \"Shadow Step II\", 2, 14, 15, true);");
        shadowStep.Should().Contain(".HasMaxRange(5f)");
        shadowStep.Should().Contain("targetPosition.X - (float)Math.Cos(facingRadians) * ArrivalDistanceMeters");

        ghostProtocol.Should().Contain("private const int EnmityReductionPercent = 80;");
        ghostProtocol.Should().Contain("private const float StealthWindowSeconds = 30f;");
        ghostProtocol.Should().Contain("private const int PrimedBackAttackCriticalRate = 100;");
        ghostProtocol.Should().Contain("private const int PrimedBackAttackExposedPercent = 20;");
        ghostProtocol.Should().Contain("private const int PrimedBackAttackExposedDurationSeconds = 30;");

        razorTrap.Should().Contain("RazorTrap(builder, FeatType.RazorTrap1, Spell.RazorTrap1, \"Razor Trap I\", 1, 5, 14);");
        razorTrap.Should().Contain("RazorTrap(builder, FeatType.RazorTrap2, Spell.RazorTrap2, \"Razor Trap II\", 2, 7, 30);");
        razorTrap.Should().Contain("private const int StatusDurationSeconds = 30;");
        razorTrap.Should().Contain("CombatDamageType.Physical");
        razorTrap.Should().Contain("typeof(BleedStatusEffect)");

        shockTrap.Should().Contain("private const int BaseDamage = 22;");
        shockTrap.Should().Contain("private const int StatusDurationSeconds = 30;");
        shockTrap.Should().Contain("CombatDamageType.Electrical");
        shockTrap.Should().Contain("typeof(ShockStatusEffect)");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }

    private static decimal CalculateDetectionChance(int detection, int stealth)
    {
        var detectedOutcomes = 0;
        for (var detectionRoll = 1; detectionRoll <= 20; detectionRoll++)
        {
            for (var stealthRoll = 1; stealthRoll <= 20; stealthRoll++)
            {
                if (detectionRoll + detection > stealthRoll + stealth)
                    detectedOutcomes++;
            }
        }

        return detectedOutcomes / 400m;
    }

    private static PerkDetail BuildPerkWithout2daLookup(
        object definition,
        string methodName,
        PerkType perkType)
    {
        var definitionType = definition.GetType();
        definitionType
            .GetMethod(methodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(definition, null);

        var builder = definitionType
            .GetField("_builder", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(definition);

        var perks = (Dictionary<PerkType, PerkDetail>)typeof(PerkBuilder)
            .GetField("_perks", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(builder)!;

        return perks[perkType];
    }
}
