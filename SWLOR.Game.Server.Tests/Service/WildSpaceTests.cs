using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.LootTableDefinition;
using SWLOR.Game.Server.Feature.ShipRewardDefinition;
using SWLOR.Game.Server.Feature.SpaceObjectDefinition;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SlicingService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Wild Space (plan section 23, W12c): the environments, the rift network, the Rift Raiders, Wild Space loot and the
/// tier 5.5 content step. The areas and their flags are in ShipAccessRulesTests.
/// </summary>
public class WildSpaceTests
{
    private static readonly DateTime Now = new(2026, 10, 27, 12, 0, 0, DateTimeKind.Utc);

    private static Func<int, int> Rolls(params int[] values)
    {
        var index = 0;
        return max => Math.Min(values[Math.Min(index++, values.Length - 1)], max - 1);
    }

    // Environments

    [Test]
    public void EveryEnvironment_ChangesStatsAndMarksItsPenalties()
    {
        foreach (var system in WildSpaceTuning.Systems)
        {
            var effects = WildSpaceRules.Effects(system.Environment, false);
            effects.Should().NotBeEmpty();
            foreach (var effect in effects)
            {
                var beneficial = SWLOR.Game.Server.Service.Stat.IsBeneficialStatAdjustment(effect.Stat, effect.Amount);
                effect.Penalty.Should().Be(!beneficial, $"{system.Environment} {effect.Stat} {effect.Amount}");
            }

            effects.Should().Contain(x => x.Penalty, "each environment is a trade-off");
            effects.Should().Contain(x => !x.Penalty, "each environment is a trade-off");
        }
    }

    [Test]
    public void Environments_FollowThePlanTable()
    {
        static IEnumerable<StatType> Stats(WildSpaceEnvironment env) => WildSpaceRules.Effects(env, false).Select(x => x.Stat);

        Stats(WildSpaceEnvironment.Pulsar).Should().BeEquivalentTo(new[] { StatType.SpaceShieldDamageTakenPercentAdjustment, StatType.SpaceCapacitorRegenPercentAdjustment });
        Stats(WildSpaceEnvironment.BlackHole).Should().BeEquivalentTo(new[] { StatType.MovementSpeedPercentAdjustment, StatType.SpaceAccuracyPercentAdjustment });
        Stats(WildSpaceEnvironment.IonStorm).Should().BeEquivalentTo(new[] { StatType.SpaceEmDamagePercentAdjustment, StatType.SpaceShieldDamageTakenPercentAdjustment });
        Stats(WildSpaceEnvironment.Magnetar).Should().BeEquivalentTo(new[] { StatType.DamageDealtPercentAdjustment, StatType.EvasionPercentAdjustment });
        Stats(WildSpaceEnvironment.RedGiant).Should().BeEquivalentTo(new[] { StatType.SpaceAbilityDamagePercentAdjustment, StatType.SpaceCapacitorCostPercentAdjustment });
        Stats(WildSpaceEnvironment.ShipGraveyard).Should().Contain(new[] { StatType.SpaceSalvageYieldBonus, StatType.SpaceScanStrengthPercent });

        WildSpaceRules.Effects(WildSpaceEnvironment.Pulsar, false).Single(x => x.Stat == StatType.SpaceShieldDamageTakenPercentAdjustment).Amount
            .Should().BeNegative("stronger shields take less shield damage");
        WildSpaceRules.Effects(WildSpaceEnvironment.ShipGraveyard, false).Single(x => x.Stat == StatType.SpaceScanStrengthPercent).Amount
            .Should().BeNegative("weaker scanning");
    }

    [Test]
    public void NewEnvironmentStats_AreNumberedFromTwelveEighty()
    {
        ((int)StatType.SpaceShieldDamageTakenPercentAdjustment).Should().Be(1280);
        ((int)StatType.SpaceCapacitorRegenPercentAdjustment).Should().Be(1281);
        ((int)StatType.SpaceEmDamagePercentAdjustment).Should().Be(1282);
        ((int)StatType.SpaceAbilityDamagePercentAdjustment).Should().Be(1283);
        ((int)StatType.SpaceCapacitorCostPercentAdjustment).Should().Be(1284);
    }

    [Test]
    public void TheRaiderProwler_IgnoresPenaltiesAndScansWildSpaceFaster()
    {
        foreach (var env in WildSpaceTuning.Effects.Keys)
            WildSpaceRules.Effects(env, true).Should().OnlyContain(x => !x.Penalty).And.HaveCount(WildSpaceRules.Effects(env, false).Count(x => !x.Penalty));

        WildSpaceRules.IsProwler("sd_raiderprowler").Should().BeTrue();
        WildSpaceRules.HullScanBonusPercent("sd_raiderprowler", true).Should().Be(50);
        WildSpaceRules.HullScanBonusPercent("sd_raiderprowler", false).Should().Be(0);
        WildSpaceRules.HullScanBonusPercent("sd_chisshyperion", true).Should().Be(0);
    }

    [Test]
    public void EnvironmentStats_ChangeShipCombat()
    {
        WildSpaceRules.AdjustShieldDamageMultiplier(1.0, -12).Should().BeApproximately(0.88, 1e-9);
        WildSpaceRules.AdjustShieldDamageMultiplier(1.5, 15).Should().BeApproximately(1.725, 1e-9);
        WildSpaceRules.AdjustShieldDamageMultiplier(0, 0).Should().Be(1.0);
        WildSpaceRules.AdjustShieldDamageMultiplier(1.0, -500).Should().Be(0.1);

        WildSpaceRules.AdjustCapacitorRegen(4.0, -25).Should().Be(3.0);
        WildSpaceRules.AdjustCapacitorRegen(4.0, -200).Should().Be(0);

        WildSpaceRules.EnvironmentDamagePercent(CombatDamageType.EM, false, 25, 20).Should().Be(25);
        WildSpaceRules.EnvironmentDamagePercent(CombatDamageType.Thermal, false, 25, 20).Should().Be(0);
        WildSpaceRules.EnvironmentDamagePercent(CombatDamageType.Thermal, true, 25, 20).Should().Be(20);
        WildSpaceRules.EnvironmentDamagePercent(CombatDamageType.EM, true, 25, 20).Should().Be(45);

        WildSpaceRules.AdjustCapacitorCost(10, 20).Should().Be(12);
        WildSpaceRules.AdjustCapacitorCost(1, 20).Should().Be(1);
        WildSpaceRules.AdjustCapacitorCost(0, 20).Should().Be(0);
        WildSpaceRules.AdjustCapacitorCost(5, -90).Should().Be(1, "a module that costs capacitor always costs some");
    }

    [Test]
    public void Wiring_EnvironmentsGoOnAndOffWithTheArea()
    {
        var wild = ReadServerSource("Service", "SpaceWildSpace.cs");
        wild.Should().Contain("[NWNEventHandler(ScriptName.OnAreaEnter)]\n        public static void ApplyWildSpaceEnvironmentOnEnter()");
        wild.Should().Contain("[NWNEventHandler(ScriptName.OnAreaExit)]\n        public static void RemoveWildSpaceEnvironmentOnExit()");
        wild.Should().Contain("TemporaryStatModifier.Add(ship, effect.Stat, effect.Amount, WildSpaceTuning.EnvironmentSeconds, WildSpaceEnvironmentGroup)");
        wild.Should().Contain("TemporaryStatModifier.Consume(ship, stat, WildSpaceEnvironmentGroup)");
        wild.Should().Contain("Stat.ApplyCreatureMovementRate(ship)", "the Black Hole's speed reaches the movement rate");
        wild.Should().Contain("RemoveWildSpaceEnvironment(ship);\n            var ignorePenalties = IsProwlerShip(ship);", "entering again replaces, never stacks");

        var space = ReadServerSource("Service", "Space.cs");
        space.Should().Contain("Stat.GetStatAdjustment(target, StatType.SpaceShieldDamageTakenPercentAdjustment)");
        space.Should().Contain("Stat.GetStatAdjustment(ship, StatType.SpaceCapacitorRegenPercentAdjustment)");
        space.Should().Contain("Stat.GetStatAdjustment(source, StatType.SpaceEmDamagePercentAdjustment)");
        space.Should().Contain("Stat.GetStatAdjustment(source, StatType.SpaceAbilityDamagePercentAdjustment)");
        ReadServerSource("Service", "SpacePowerRouting.cs").Should().Contain("StatType.SpaceCapacitorCostPercentAdjustment");
        ReadServerSource("Service", "SpaceScanning.cs").Should().Contain("Stat.GetStatAdjustment(ship, StatType.SpaceScanStrengthPercent)");
    }

    [Test]
    public void TheEntryMessage_NamesTheSystemTheEnvironmentAndTheStakes()
    {
        var message = WildSpaceRules.EntryMessage("Wild Space - Vesh Pulsar", WildSpaceEnvironment.Pulsar, false);
        message.Should().Contain("Tier 5.5").And.Contain("Pulsar").And.Contain("-12% shield damage taken").And.Contain("-25% capacitor regen")
            .And.Contain("lose its modules");
        WildSpaceRules.EntryMessage("Wild Space - Vesh Pulsar", WildSpaceEnvironment.Pulsar, true).Should().NotContain("capacitor regen");
    }

    // Tier 5.5

    [Test]
    public void TierFiveAndAHalf_IsTierFiveContentAtTierFiveAndAHalfSignal()
    {
        WildSpaceRules.ContentTier(55).Should().Be(5);
        WildSpaceRules.TierLabel(5, true).Should().Be("5.5");
        WildSpaceRules.TierLabel(5, false).Should().Be("5");
        ScanningRules.SignalRequired(SignatureType.CombatSite, 5, true).Should().BeApproximately(240 * 5.5 * 0.8, 1e-9);
        ScanningRules.SignalRequired(SignatureType.CombatSite, 5).Should().BeApproximately(240 * 5 * 0.8, 1e-9);

        var site = SignatureSiteRules.Create("s", "space_wspulsar", 5, SignatureType.CombatSite, 1, 2, 0, Now, wildSpace: true);
        site.WildSpace.Should().BeTrue();
        ScanningRules.RowName(site, 60, false).Should().Be("Combat Site (Tier 5.5)");
        SignatureSiteRules.CombatSquad(5, true).Should().OnlyContain(x => WildSpaceTuning.RiftRaiderTags.Contains(x));
        SignatureSiteRules.CacheLootTable(5, true).Should().Be("SPACE_ELITE_WS");
        SignatureSiteRules.CacheLootTable(5).Should().Be("SPACE_ELITE_T5");
    }

    // Rifts

    [Test]
    public void ARift_LivesFourToTwentyFourHours()
    {
        RiftRules.RollLifetimeSeconds(_ => 0).Should().Be(4 * 3600);
        RiftRules.RollLifetimeSeconds(max => max - 1).Should().Be(24 * 3600);
        var random = new System.Random(3);
        for (var i = 0; i < 500; i++)
            RiftRules.RollLifetimeSeconds(random.Next).Should().BeInRange(4 * 3600, 24 * 3600);
    }

    [Test]
    public void SizeClasses_CapTheHullAndOnlyLargeTakesCapitals()
    {
        RiftRules.RollSize(_ => 0).Should().Be(RiftSizeClass.Small);
        RiftRules.RollSize(max => max - 1).Should().Be(RiftSizeClass.Large);
        RiftRules.RollSize(_ => 50).Should().Be(RiftSizeClass.Medium);

        RiftRules.Admits(RiftSizeClass.Small, ShipSize.Medium).Should().BeTrue();
        RiftRules.Admits(RiftSizeClass.Small, ShipSize.Large).Should().BeFalse();
        RiftRules.Admits(RiftSizeClass.Medium, ShipSize.Large).Should().BeTrue();
        RiftRules.Admits(RiftSizeClass.Medium, ShipSize.Capital).Should().BeFalse();
        RiftRules.Admits(RiftSizeClass.Large, ShipSize.Capital).Should().BeTrue();
        RiftRules.Admits(RiftSizeClass.Large, ShipSize.Invalid).Should().BeFalse();
        foreach (var size in new[] { RiftSizeClass.Small, RiftSizeClass.Medium })
            RiftRules.Admits(size, ShipSize.Capital).Should().BeFalse();
    }

    [Test]
    public void EveryPassage_SpendsMassByHullSize_AndASpentRiftCollapsesAfterTheWarning()
    {
        var rift = RiftRules.Create("r", "space_wspulsar", "viscaraorbit", RiftSizeClass.Small, Now, 10 * 3600);
        rift.MassRemaining.Should().Be(30);
        RiftRules.PassageMass(ShipSize.Small).Should().BeLessThan(RiftRules.PassageMass(ShipSize.Medium));
        RiftRules.PassageMass(ShipSize.Large).Should().BeLessThan(RiftRules.PassageMass(ShipSize.Capital));

        RiftRules.GetPassageRefusal(rift, ShipSize.Large, Now).Should().Be(RiftPassageRefusal.HullTooLarge);
        for (var i = 0; i < 14; i++)
        {
            RiftRules.GetPassageRefusal(rift, ShipSize.Medium, Now).Should().Be(RiftPassageRefusal.None);
            RiftRules.SpendPassage(rift, ShipSize.Medium, Now);
        }

        rift.MassRemaining.Should().Be(2);
        rift.CollapsesAt.Should().Be(Now.AddHours(10), "mass is left");
        RiftRules.SpendPassage(rift, ShipSize.Small, Now);
        RiftRules.GetPassageRefusal(rift, ShipSize.Medium, Now).Should().Be(RiftPassageRefusal.MassSpent);
        RiftRules.GetPassageRefusal(rift, ShipSize.Small, Now).Should().Be(RiftPassageRefusal.None);
        RiftRules.SpendPassage(rift, ShipSize.Small, Now);

        rift.MassRemaining.Should().Be(0);
        rift.CollapsesAt.Should().Be(Now.AddSeconds(WildSpaceTuning.WarningSeconds), "a spent rift collapses after the warning");
        RiftRules.IsWarningDue(rift, Now).Should().BeTrue();
        RiftRules.IsCollapseDue(rift, Now).Should().BeFalse();
        RiftRules.IsCollapseDue(rift, Now.AddSeconds(WildSpaceTuning.WarningSeconds)).Should().BeTrue();
        RiftRules.GetPassageRefusal(rift, ShipSize.Small, Now.AddSeconds(WildSpaceTuning.WarningSeconds)).Should().Be(RiftPassageRefusal.Collapsed);
    }

    [Test]
    public void TheCollapseWarning_ComesTwoMinutesBeforeTheEndOfALifetime()
    {
        var rift = RiftRules.Create("r", "space_wspulsar", "viscaraorbit", RiftSizeClass.Large, Now, 4 * 3600);
        WildSpaceTuning.WarningSeconds.Should().Be(120);
        RiftRules.IsWarningDue(rift, Now.AddHours(4).AddSeconds(-121)).Should().BeFalse();
        RiftRules.IsWarningDue(rift, Now.AddHours(4).AddSeconds(-120)).Should().BeTrue();
        rift.WarningSent = true;
        RiftRules.IsWarningDue(rift, Now.AddHours(4).AddSeconds(-60)).Should().BeFalse("the warning goes out once");
        RiftRules.IsCollapseDue(rift, Now.AddHours(4)).Should().BeTrue();
    }

    [Test]
    public void AShipIsThrownOut_OnlyWhenNoOpenRiftItsHullFitsLeavesItsSystem()
    {
        var small = RiftRules.Create("a", "space_wspulsar", "viscaraorbit", RiftSizeClass.Small, Now, 3600);
        var large = RiftRules.Create("b", "space_wsmagnetar", "space_wspulsar", RiftSizeClass.Large, Now, 3600);
        var rifts = new List<RiftConnection> { small, large };

        RiftRules.HasExit("space_wspulsar", ShipSize.Capital, rifts, Now).Should().BeTrue("the Large rift reaches the Pulsar");
        large.Collapsed = true;
        RiftRules.HasExit("space_wspulsar", ShipSize.Capital, rifts, Now).Should().BeFalse("a Capital hull cannot use the Small rift");
        RiftRules.HasExit("space_wspulsar", ShipSize.Medium, rifts, Now).Should().BeTrue();
        RiftRules.HasExit("space_wsmagnetar", ShipSize.Small, rifts, Now).Should().BeFalse();
        RiftRules.HasExit("space_wspulsar", ShipSize.Small, rifts, Now.AddHours(2)).Should().BeFalse("every rift has closed");

        WildSpaceTuning.EjectHullPercent.Should().Be(25);
        RiftRules.HullAfterEjection(400, 400).Should().Be(300);
        RiftRules.HullAfterEjection(50, 400).Should().Be(1, "the collapse damages but never destroys");
        RiftRules.HullAfterEjection(399, 401).Should().Be(298);
    }

    [Test]
    public void Restart_SeedsEveryWildSpaceSystemAtOnce_AndLaterGapsWait()
    {
        var none = new Dictionary<string, int>();
        RiftRules.SystemsShort(Array.Empty<RiftConnection>(), none, Now)
            .Should().BeEquivalentTo(WildSpaceTuning.Systems.Select(x => x.AreaResref), "a fresh network after a restart is empty");
        RiftRules.ReplacementTime(false, Now).Should().Be(Now, "the first pass opens every rift at once");
        RiftRules.ReplacementTime(true, Now).Should().Be(Now.AddSeconds(WildSpaceTuning.ReplacementSeconds));

        var full = WildSpaceTuning.Systems
            .SelectMany(x => Enumerable.Range(0, WildSpaceTuning.ConnectionsPerSystem)
                .Select(i => RiftRules.Create($"{x.AreaResref}{i}", x.AreaResref, "viscaraorbit", RiftSizeClass.Small, Now, 3600)))
            .ToList();
        RiftRules.SystemsShort(full, none, Now).Should().BeEmpty();
        full[0].Collapsed = true;
        RiftRules.SystemsShort(full, none, Now).Should().BeEquivalentTo(new[] { full[0].AreaA });
        RiftRules.SystemsShort(full, new Dictionary<string, int> { [full[0].AreaA] = 1 }, Now).Should().BeEmpty("a replacement is on its way");

        // Rift state is in memory; ships in space lose their position at boot and relaunch from their dock.
        ReadServerSource("Service", "Property.cs").Should().Contain("property.Positions.Remove(PropertyLocationType.CurrentPosition);");
        ReadServerSource("Service", "SpaceRifts.cs").Should().Contain("_pendingRifts.Add((system, RiftRules.ReplacementTime(_riftNetworkSeeded, now)));");
    }

    [Test]
    public void ANewRift_LeadsToAKnownOrbitOrAnotherWildSpaceSystemShortOfRifts()
    {
        var open = WildSpaceTuning.Systems.ToDictionary(x => x.AreaResref, _ => 2);
        RiftRules.PickDestination("space_wspulsar", open, Rolls(0, 3)).Should().Be(WildSpaceTuning.KnownOrbits[3]);
        RiftRules.PickDestination("space_wspulsar", open, Rolls(99, 0)).Should().BeOneOf(WildSpaceTuning.KnownOrbits, "no system has room");
        open["space_wsredgiant"] = 1;
        RiftRules.PickDestination("space_wspulsar", open, Rolls(99, 0)).Should().Be("space_wsredgiant");
        open["space_wspulsar"] = 0;
        RiftRules.PickDestination("space_wspulsar", open, Rolls(99, 0)).Should().NotBe("space_wspulsar", "a rift never loops back");
        WildSpaceTuning.KnownOrbits.Should().HaveCount(10).And.OnlyContain(x => !WildSpaceRules.IsWildSpaceSystem(x));
    }

    [Test]
    public void ARiftEnd_IsAHiddenTierFiveAndAHalfSignatureThatLastsWithItsRift()
    {
        var rift = RiftRules.Create("r", "space_wspulsar", "viscaraorbit", RiftSizeClass.Medium, Now, 4 * 3600);
        var end = SignatureSiteRules.CreateRiftEnd("e", "viscaraorbit", rift.Id, 1, 2, 0, Now, rift.CollapsesAt,
            RiftRules.Describe(rift, "Vesh Pulsar"));
        end.Type.Should().Be(SignatureType.Rift);
        end.WildSpace.Should().BeTrue();
        end.SignalRequired.Should().BeApproximately(240 * 5.5 * 1.5, 1e-9);
        end.IsRevealed.Should().BeFalse("nothing shows until it is scanned");

        end.RevealedAt = Now;
        SignatureSiteRules.IsOver(end, Now.AddHours(3)).Should().BeFalse("only the rift network closes a rift end");
        ScanningRules.RowTooltip(end, 80, 320, 320).Should().Contain("Medium rift (up to Large hulls) to Vesh Pulsar");
        ScanningRules.RowTooltip(end, 60, 320, 320).Should().NotContain("Vesh Pulsar", "the destination shows from the threat level");

        SignatureSiteRules.PickType(5, _ => 0).Should().NotBe(SignatureType.Rift);
        ScanningTuning.Weights[SignatureType.Rift].Should().Be(0, "the rift network places rifts, never the site spawner");
    }

    [Test]
    public void Wiring_JumpToSignatureCrossesALocatedRift()
    {
        var jump = ReadServerSource("Service", "SpaceSignatureJump.cs");
        jump.Should().Contain("if (signature.Type == SignatureType.Rift)\n            {\n                CrossRift(player, signature);");
        jump.Should().Contain("RiftRules.GetPassageRefusal(FindRift(signature.RiftId), status.Size, DateTime.UtcNow)");

        var rifts = ReadServerSource("Service", "SpaceRifts.cs");
        rifts.Should().Contain("RiftRules.SpendPassage(rift, status.Size, now);");
        rifts.Should().Contain("if (!CanFlownShipEnterArea(pilot, destination))");
        rifts.Should().Contain("farEnd.Signal[GetObjectUUID(pilot)] = farEnd.SignalRequired;", "the crossing ship can come back");
        rifts.Should().Contain("EjectStrandedShips(end, now);");
        rifts.Should().Contain("RiftRules.HullAfterEjection(status.Hull, status.MaxHull)");

        var scanning = ReadServerSource("Service", "SpaceScanning.cs");
        scanning.Should().Contain("if (signature.Type == SignatureType.Rift)\n                    continue;", "the site spawner leaves rift ends alone");
    }

    // Rift Raiders

    [Test]
    public void RiftRaiders_FlyEveryRoleAtLevelFiftyTwo()
    {
        var presets = RevampNpcShipCatalog.Ships.Where(x => WildSpaceRules.IsRiftRaider(x.CreatureTag)).ToList();
        presets.Select(x => x.CreatureTag).Should().BeEquivalentTo(WildSpaceTuning.RiftRaiderTags);
        presets.Should().OnlyContain(x => x.Level == 52);
        presets.Select(x => x.Role).Should().Contain(new[]
        {
            "Gunship", "Logistics", "Electronic Warfare", "Interceptor", "Bomber", "Sniper", "Carrier", "Minelayer", "Fighter",
        });
        presets.Single(x => x.CreatureTag == "rrcarrier").Difficulty.Should().Be("Boss");

        // Level 52 sits between the T5 and T6 presets of the same role.
        foreach (var preset in presets.Where(x => x.CreatureTag != "rrcarrier"))
        {
            var hull = preset.CreatureTag[2..];
            var t5 = RevampNpcShipCatalog.Ships.FirstOrDefault(x => x.CreatureTag == $"t5{hull}" && x.Difficulty == preset.Difficulty);
            if (t5 != null && t5.Level < 52)
                (preset.Hull + preset.Shield).Should().BeGreaterThanOrEqualTo(t5.Hull + t5.Shield, preset.CreatureTag);
        }

        ShipNpcRoleRules.FighterTag("rrcarrier").Should().Be("rrfighter", "the Rift Raider carrier launches Rift Raider fighters");
        ShipManufacturerRules.WreckManufacturer("rrgunship").Should().Be(ShipManufacturer.RiftRaider);
        ShipManufacturerRules.Component(ShipManufacturer.RiftRaider).Should().NotBeNull("Rift Raider wrecks yield Raider Refit parts");

        var registered = new NPCSpaceObjectDefinition().BuildSpaceObjects();
        registered.Should().ContainKeys(WildSpaceTuning.RiftRaiderTags);
    }

    [Test]
    public void RiftRaiderBlueprints_UseTheLevelFiftyTwoSkinAndTheWildSpaceTables()
    {
        var tables = new SpaceLootTableDefinition().BuildLootTables();
        using var skin = JsonDocument.Parse(File.ReadAllText(Path.Combine(RepositoryRoot, "Module", "uti", "rrskin.uti.json")));
        skin.RootElement.GetProperty("PropertiesList").GetProperty("value")[0].GetProperty("CostValue").GetProperty("value").GetInt32().Should().Be(52);

        foreach (var tag in WildSpaceTuning.RiftRaiderTags)
        {
            using var utc = JsonDocument.Parse(File.ReadAllText(Path.Combine(RepositoryRoot, "Module", "utc", $"{tag}.utc.json")));
            var root = utc.RootElement;
            root.GetProperty("Tag").GetProperty("value").GetString().Should().Be(tag);
            root.GetProperty("FactionID").GetProperty("value").GetInt32().Should().Be(1, "Rift Raiders are hostile");
            root.GetProperty("Equip_ItemList").GetProperty("value").EnumerateArray()
                .Select(x => x.GetProperty("EquippedRes").GetProperty("value").GetString()).Should().Contain("rrskin");
            var loot = root.GetProperty("VarTable").GetProperty("value").EnumerateArray()
                .Where(x => x.GetProperty("Name").GetProperty("value").GetString()!.StartsWith("LOOT_TABLE_"))
                .Select(x => x.GetProperty("Value").GetProperty("value").GetString()!.Split(',')[0].Trim())
                .ToList();
            loot.Should().NotBeEmpty().And.OnlyContain(x => x.EndsWith("_WS") || x.EndsWith("_WS_RARES"));
            tables.Should().ContainKeys(loot);
        }
    }

    [Test]
    public void StolenCargo_SpillsSometimes_AndSomeOfItIsContraband()
    {
        WildSpaceRules.RollStolenCargo(_ => 99).Should().BeEmpty();
        var spill = WildSpaceRules.RollStolenCargo(Rolls(0, 0, 1));
        spill.Should().ContainSingle();
        spill.Single().Key.Should().Be(CommodityType.Moonspice);
        spill.Single().Value.Should().Be(2);

        WildSpaceTuning.StolenCargo.Should().Contain(x => x.Commodity == CommodityType.Moonspice, "spice runners carry contraband");
        var random = new System.Random(7);
        var spills = 0;
        for (var i = 0; i < 2000; i++)
        {
            foreach (var (commodity, units) in WildSpaceRules.RollStolenCargo(random.Next))
            {
                spills++;
                var entry = WildSpaceTuning.StolenCargo.Single(x => x.Commodity == commodity);
                units.Should().BeInRange(entry.MinimumUnits, entry.MaximumUnits);
            }
        }

        spills.Should().BeInRange(300, 500, "about a fifth of kills");
        WildSpaceRules.IsRiftRaider("rrgunship").Should().BeTrue();
        WildSpaceRules.IsRiftRaider("t5gunship").Should().BeFalse();
        ReadServerSource("Service", "SpaceWildSpace.cs").Should().Contain("Trade.SpillUnits(cargo, GetLocation(creature), kept)");
    }

    // Loot

    [Test]
    public void WildSpaceTables_HoldTheWildSpaceLoot()
    {
        var tables = new SpaceLootTableDefinition().BuildLootTables();
        IEnumerable<string> Items(string name) => tables[name].Select(x => x.Resref);

        Items("SPACE_BASIC_WS").Should().Contain("aurodium");
        Items("SPACE_BASIC_WS_RARES").Should().Contain(new[] { "thor_crys", "proto_tech", WildSpaceTuning.RiftKeyResref });
        Items("SPACE_ELITE_WS").Should().Contain(new[] { "aurodium", "thor_crys", "proto_tech", WildSpaceTuning.RiftKeyResref });
        Items("SPACE_ELITE_WS").Should().Contain(RevampRewardCatalog.ShipSchematics.Where(x => x.WildSpaceOnly).Select(x => x.Resref));
        Items("SPACE_BOSS_WS").Should().Contain(RevampRewardCatalog.ModuleSchematics.Select(x => x.Resref), "the named rank-50 recipe unlocks");
        Items("SPACE_BOSS_WS").Should().Contain(RevampRewardCatalog.HullDesigns.Where(x => x.WildSpaceOnly).Select(x => x.Resref));
        tables["SPACE_BASIC_WS_RARES"].Single(x => x.Resref == WildSpaceTuning.RiftKeyResref).Weight
            .Should().BeLessThanOrEqualTo(tables["SPACE_BASIC_WS_RARES"].Min(x => x.Weight), "the Rift Key drops at a low rate");

        foreach (var resref in tables.Where(x => x.Key.Contains("_WS")).SelectMany(x => x.Value).Select(x => x.Resref).Where(x => x != "nw_it_gold001").Distinct())
            File.Exists(Path.Combine(RepositoryRoot, "Module", "uti", $"{resref}.uti.json")).Should().BeTrue(resref);
    }

    [Test]
    public void TheRiftKey_IsAnObtainableItemOnAReusedModel()
    {
        using var key = JsonDocument.Parse(File.ReadAllText(Path.Combine(RepositoryRoot, "Module", "uti", "rift_key_t55.uti.json")));
        var root = key.RootElement;
        root.GetProperty("LocalizedName").GetProperty("value").GetProperty("0").GetString().Should().Be("Rift Key (Tier 5.5)");
        root.TryGetProperty("VarTable", out _).Should().BeFalse("it drops from the Rift Raiders, so it is not NO_ECONOMY");
        root.GetProperty("ModelPart1").GetProperty("value").GetInt32().Should().Be(197, "the keycard model, reused");

        File.ReadAllText(Path.Combine(RepositoryRoot, "Module", "uti", "proto_tech.uti.json")).Should().NotContain("NO_ECONOMY",
            "Prototype Tech drops in Wild Space");
    }

    [Test]
    public void WildSpaceWrecks_GiveWildSpaceSchematicsModuleSchematicsAndPrototypeTech()
    {
        ShipRareFindRules.Designs(5).Should().NotContain(x => x.WildSpaceOnly);
        ShipRareFindRules.Designs(5, true).Should().Contain(x => x.WildSpaceOnly);
        ShipRareFindRules.ShipSchematics(5, true).Should().Contain(x => x.WildSpaceOnly);

        // Elite: design 20, ship 5, torpedo 0, then module 5 and Prototype Tech 10.
        ShipRareFindRules.Roll(5, WreckDifficulty.Elite, Rolls(26, 0), true).Should().ContainSingle()
            .Which.Should().Be(RevampRewardCatalog.ModuleSchematics[0].Resref);
        ShipRareFindRules.Roll(5, WreckDifficulty.Elite, Rolls(31), true).Should().Equal(RevampRewardCatalog.PrototypeTechResref);
        ShipRareFindRules.Roll(5, WreckDifficulty.Elite, Rolls(31), false).Should().BeEmpty("outside Wild Space it is the table's item");
        // Then the Rift Key (W15b): elite 5 after every other share; the Wild Space key in Wild Space.
        ShipRareFindRules.Roll(5, WreckDifficulty.Elite, Rolls(41), true).Should().Equal(WildSpaceTuning.RiftKeyResref);
        ShipRareFindRules.Roll(5, WreckDifficulty.Elite, Rolls(45), true).Should().BeEmpty();

        ShipSalvageRules.RareFindTable(5, WreckDifficulty.Elite, true).Should().Be("SPACE_ELITE_WS");
        ShipSalvageRules.RareFindTable(5, WreckDifficulty.Boss, true).Should().Be("SPACE_BOSS_WS");
        ShipSalvageRules.RareFindTable(5, WreckDifficulty.Boss).Should().Be("SPACE_BOSS_T5");
        WildSpaceTuning.RareTraitBonusPercent.Should().BePositive();

        var salvage = File.ReadAllText(Path.Combine(RepositoryRoot, "SWLOR.Game.Server", "Feature", "ShipModuleDefinition", "ShipSalvageActions.cs"));
        salvage.Should().Contain("ShipRareFindRules.Roll(wreck.Tier, wreck.Difficulty, Random.Next, wreck.WildSpace)");
        salvage.Should().Contain("(wreck.WildSpace ? WildSpaceTuning.RareTraitBonusPercent : 0)");
        ReadServerSource("Service", "SpaceWrecks.cs").Should().Contain("WildSpace = IsWildSpace(GetArea(creature)),");
    }

    [Test]
    public void WildSpaceSlicing_GivesTheWildSpaceOnlySchematics()
    {
        var wildOnly = RevampRewardCatalog.HullDesigns.Where(x => x.WildSpaceOnly).Select(x => x.Resref).ToList();
        SlicingReward.GetSchematicPool(SpaceSlicingRewardKind.DesignSchematic, 5).Should().NotContain(wildOnly);
        SlicingReward.GetSchematicPool(SpaceSlicingRewardKind.DesignSchematic, 5, true).Should().Contain(wildOnly);
        SlicingReward.GetSchematicPool(SpaceSlicingRewardKind.ShipSchematic, 5, true)
            .Should().Contain(RevampRewardCatalog.ShipSchematics.Where(x => x.WildSpaceOnly).Select(x => x.Resref));
        ReadServerSource("Service", "SpaceSlicing.cs").Should().Contain("SlicingReward.RollSpace(source, tier, max => Random.Next(max), AgentIntelAvailable, wildSpace)");
    }

    // PvP and achievements

    [Test]
    public void APvpDeathInWildSpace_DropsModules_AndEnteringGrantsTheAchievement()
    {
        SpaceDeathRules.ModulesDrop(true, true).Should().BeTrue();
        SpaceDeathRules.ModulesDrop(true, false).Should().BeFalse("NPC kills never drop modules");
        ReadServerSource("Service", "SpaceDeath.cs").Should().Contain("SpaceDeathRules.ModulesDrop(IsWildSpace(GetArea(creature)), pvpStakes)");
        ReadServerSource("Service", "SpaceDeath.cs").Should().Contain("public const string WildSpaceVariable = \"WILD_SPACE\";");

        // Achievement 215 keys on the same WILD_SPACE area local the six systems carry (ShipAccessRulesTests).
        ((int)SWLOR.Game.Server.Service.AchievementService.AchievementType.SpaceWildSpace).Should().Be(215);
        ReadServerSource("Service", "SpaceAchievements.cs")
            .Should().Contain("!IsWildSpace(OBJECT_SELF))\n                return;\n\n            Achievement.GiveAchievement(player, AchievementType.SpaceWildSpace);");
    }

    private static string RepositoryRoot
    {
        get
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                directory = directory.Parent;
            return directory!.FullName;
        }
    }

    private static string ReadServerSource(params string[] path) =>
        File.ReadAllText(Path.Combine(new[] { RepositoryRoot, "SWLOR.Game.Server" }.Concat(path).ToArray())).Replace("\r\n", "\n");
}
