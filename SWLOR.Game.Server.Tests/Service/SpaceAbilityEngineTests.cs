using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.ShipModuleDefinition;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The shared machinery the space abilities fire weapons through: which weapon fires, the next-weapon-hit rider,
/// the space radii and the combat events.
/// </summary>
public class SpaceAbilityEngineTests
{
    private static readonly DateTime Now = new(2026, 10, 27, 12, 0, 0, DateTimeKind.Utc);

    private static SpaceWeaponCandidate Weapon(
        int slot, int rank, SpaceWeaponFamily family, float range = 40f, bool usable = true) =>
        new() { Slot = slot, Rank = rank, Family = family, Range = range, Usable = usable };

    [Test]
    public void Pick_TakesTheHighestRankedUsableWeaponOfTheFamily()
    {
        var weapons = new[]
        {
            Weapon(1, 10, SpaceWeaponFamily.Energy),
            Weapon(2, 30, SpaceWeaponFamily.Energy),
            Weapon(3, 45, SpaceWeaponFamily.Launcher),
            Weapon(4, 20, SpaceWeaponFamily.Energy),
        };

        SpaceWeaponPicker.Pick(weapons, SpaceWeaponFilter.Energy, 20f).Slot.Should().Be(2);
        SpaceWeaponPicker.Pick(weapons, SpaceWeaponFilter.Launcher, 20f).Slot.Should().Be(3);
        SpaceWeaponPicker.Pick(weapons, SpaceWeaponFilter.Any, 20f).Slot.Should().Be(3);
    }

    [Test]
    public void Pick_BreaksATieWithTheLowerSlot()
    {
        var weapons = new[]
        {
            Weapon(7, 25, SpaceWeaponFamily.Energy),
            Weapon(3, 25, SpaceWeaponFamily.Energy),
        };

        SpaceWeaponPicker.Pick(weapons, SpaceWeaponFilter.Energy, 10f).Slot.Should().Be(3);
    }

    [Test]
    public void Pick_SkipsUnusableWeaponsAndWeaponsThatCannotReach()
    {
        var weapons = new[]
        {
            Weapon(1, 40, SpaceWeaponFamily.Energy, range: 40f, usable: false),
            Weapon(2, 30, SpaceWeaponFamily.Energy, range: 15f),
            Weapon(3, 10, SpaceWeaponFamily.Energy, range: 40f),
        };

        SpaceWeaponPicker.Pick(weapons, SpaceWeaponFilter.Energy, 30f).Slot.Should().Be(3);
        SpaceWeaponPicker.Pick(weapons, SpaceWeaponFilter.Energy, 10f).Slot.Should().Be(2);
        SpaceWeaponPicker.Pick(weapons, SpaceWeaponFilter.Energy, 50f).Should().BeNull();
        SpaceWeaponPicker.Pick(weapons, SpaceWeaponFilter.Launcher, 10f).Should().BeNull();
    }

    [Test]
    public void LongestRange_ReadsOnlyUsableWeaponsOfTheFamily()
    {
        var weapons = new[]
        {
            Weapon(1, 10, SpaceWeaponFamily.Energy, range: 40f),
            Weapon(2, 10, SpaceWeaponFamily.Energy, range: 60f, usable: false),
            Weapon(3, 10, SpaceWeaponFamily.Launcher, range: 50f),
        };

        SpaceWeaponPicker.LongestRange(weapons, SpaceWeaponFilter.Energy).Should().Be(40f);
        SpaceWeaponPicker.LongestRange(weapons, SpaceWeaponFilter.Any).Should().Be(50f);
        SpaceWeaponPicker.LongestRange(Array.Empty<SpaceWeaponCandidate>(), SpaceWeaponFilter.Any).Should().Be(0f);
    }

    [Test]
    public void ThePilotsAbilities_FireHelmSlotsOnly()
    {
        var status = new ShipStatus
        {
            Size = ShipSize.Large,
            SlotTags = new Dictionary<int, ShipSlotTag> { [1] = ShipSlotTag.Helm, [2] = ShipSlotTag.Gunner },
        };

        SpaceCrew.CanActingSeatFire(77001u, status, 1).Should().BeTrue();
        SpaceCrew.CanActingSeatFire(77001u, status, 2).Should().BeFalse("the pilot cannot fire station slots");
        SpaceCrew.CanActingSeatFire(77001u, new ShipStatus(), 2).Should().BeTrue("an older ship has no tags");
        SpaceCrew.EffectivenessMultiplier(0, 0).Should().Be(1.0);
        SpaceCrew.IsSeatCrewed(77001u, new ShipStatus(), 1).Should().BeFalse();
    }

    [Test]
    public void AbilityWeapons_NeedTheStationRankLikeSwitchingTheModuleOn()
    {
        var abilities = File.ReadAllText(Path.Combine(RepositoryRoot(), "SWLOR.Game.Server", "Service", "SpaceAbilities.cs"));
        var candidates = abilities[abilities.IndexOf("BuildCandidates(", StringComparison.Ordinal)..];
        candidates.Should().Contain("SpaceCrew.CanActingSeatFire(ship, status, weapon.Slot) &&")
            .And.Contain("Space.GetStationSlotEffectiveness(ship, status, weapon.Slot) > 0",
                "a crew member below the module's rank works it at 0 effectiveness and cannot switch it on");
    }

    // Riders

    [Test]
    public void Rider_MatchesOnlyBeforeItLapsesAndOnlyForItsFamily()
    {
        var rider = new SpaceShotRider { Key = "ram", Filter = SpaceWeaponFilter.Energy, ExpiresAt = Now.AddSeconds(30) };

        rider.Matches(SpaceWeaponFamily.Energy, Now).Should().BeTrue();
        rider.Matches(SpaceWeaponFamily.Launcher, Now).Should().BeFalse();
        rider.Matches(SpaceWeaponFamily.Energy, Now.AddSeconds(30)).Should().BeFalse("it lapses at the expiry time");
        new SpaceShotRider { Key = "any", ExpiresAt = Now.AddSeconds(1) }
            .Matches(SpaceWeaponFamily.Launcher, Now).Should().BeTrue();
        new SpaceShotRider { Key = "unset" }.Matches(SpaceWeaponFamily.Energy, Now).Should().BeFalse();
    }

    [Test]
    public void ShipStatus_PendingRidersSurviveTheDatabaseRoundTrip()
    {
        var status = new ShipStatus();
        status.PendingRiders.Add(new SpaceShotRider
        {
            Key = "ram",
            BonusDmg = 10,
            ExtraDamage = 24,
            ExtraEnmity = 350,
            Amount = 4,
            Filter = SpaceWeaponFilter.Launcher,
            ExpiresAt = Now,
        });

        var copy = JsonConvert.DeserializeObject<ShipStatus>(JsonConvert.SerializeObject(status));

        copy.PendingRiders.Should().ContainSingle();
        var rider = copy.PendingRiders[0];
        rider.Key.Should().Be("ram");
        rider.BonusDmg.Should().Be(10);
        rider.ExtraDamage.Should().Be(24);
        rider.ExtraEnmity.Should().Be(350);
        rider.Amount.Should().Be(4);
        rider.Filter.Should().Be(SpaceWeaponFilter.Launcher);
        rider.ExpiresAt.Should().Be(Now);
    }

    [Test]
    public void ShipStatus_SavedBeforeRidersExistedLoadsWithAnEmptyList()
    {
        var copy = JsonConvert.DeserializeObject<ShipStatus>("{\"ItemTag\":\"x\",\"Hull\":5}");

        copy.PendingRiders.Should().NotBeNull().And.BeEmpty();
    }

    [Test]
    public void WithBonusDmg_CopiesTheShotSoASharedShotKeepsItsDmg()
    {
        var shot = new SpaceWeaponShot { Dmg = 12, Rank = 5, Family = SpaceWeaponFamily.Launcher, IsAbility = true };

        var boosted = shot.WithBonusDmg(8);

        boosted.Dmg.Should().Be(20);
        boosted.Rank.Should().Be(5);
        boosted.Family.Should().Be(SpaceWeaponFamily.Launcher);
        boosted.IsAbility.Should().BeTrue();
        shot.Dmg.Should().Be(12);
    }

    [Test]
    public void AShotConsumesRidersUnlessItBelongsToAnAbility()
    {
        new SpaceWeaponShot().ConsumesRiders.Should().BeTrue("a weapon cycle is the next weapon hit");
        new SpaceFireOptions().ConsumesRiders.Should().BeFalse("an ability leaves riders for the next weapon cycle");
        new SpaceFireOptions().IsAbility.Should().BeTrue();
    }

    // Weapons

    /// <summary>
    /// Every catalog step that deals damage is a weapon FireStep knows how to fire, so a new weapon family cannot
    /// be fitted to a hull and be unusable by abilities.
    /// </summary>
    [Test]
    public void EveryDamagingCatalogStep_IsAFireableWeapon()
    {
        var damaging = RevampModuleCatalog.Steps
            .Where(x => x.Dmg > 0 && x.WeaponFamily != 0)
            .Select(x => x.Family)
            .Distinct()
            .ToList();

        damaging.Should().NotBeEmpty();
        damaging.Where(x => !ShipModuleActions.IsShipWeapon(x)).Should().BeEmpty();
    }

    [Test]
    public void EveryShipWeaponFamily_HasStepsWithAWeaponFamilyAndOnlyLaunchersNeedAmmo()
    {
        foreach (var family in Enum.GetValues<ShipModuleFamily>().Where(ShipModuleActions.IsShipWeapon))
        {
            var steps = RevampModuleCatalog.Steps.Where(x => x.Family == family).ToList();
            steps.Should().NotBeEmpty($"{family} is a weapon family");
            steps.Should().OnlyContain(x => x.WeaponFamily != 0 && x.Activation != ShipModuleActivation.Invalid);

            var ammo = ShipModuleActions.GetAmmoResref(family);
            if (ammo != null)
                steps.Should().OnlyContain(x => x.WeaponFamily == SpaceWeaponFamily.Launcher, $"{family} uses ammo");
        }
    }

    // Radii and auras

    [Test]
    public void SpaceRadii_ScaleTheGroundAreaByOneFactor()
    {
        SpaceRadii.GroundToSpaceFactor.Should().Be(4f);
        SpaceRadii.GroundAreaMeters.Should().Be(5f);
        SpaceRadii.NearShip.Should().Be(20f);
        SpaceRadii.NearTarget.Should().Be(20f);
        SpaceRadii.FromGround(3f).Should().Be(12f);
        SpaceRadii.CommandRange.Should().Be(30f);
    }

    [Test]
    public void CommandRange_AddsTheScaledGroundBonusAndAnyExtraMeters()
    {
        SpaceRadii.GetCommandRange(0).Should().Be(30f);
        SpaceRadii.GetCommandRange(2).Should().Be(38f);
        SpaceRadii.GetCommandRange(4, 5f).Should().Be(51f);
        SpaceRadii.GetCommandRange(-3, -5f).Should().Be(30f, "penalties never shrink the base range");
    }

    [TestCase(8, 1)]
    [TestCase(10, 1)]
    [TestCase(15, 2)]
    [TestCase(20, 3)]
    [TestCase(25, 4)]
    [TestCase(60, 4)]
    public void MaxAuras_FollowsTheGroundSocialRule(int social, int expected)
    {
        SpaceAuraRules.MaxAuras(social).Should().Be(expected);
    }

    [Test]
    public void Reconcile_GivesTheAuraToNewShipsAndTakesItFromShipsThatLeft()
    {
        var (add, remove) = SpaceAuraRules.Reconcile(new uint[] { 1, 2, 3 }, new uint[] { 2, 3, 4, 5 });

        add.Should().BeEquivalentTo(new uint[] { 4, 5 });
        remove.Should().BeEquivalentTo(new uint[] { 1 });
        var (noAdd, noRemove) = SpaceAuraRules.Reconcile(new uint[] { 1 }, new uint[] { 1 });
        noAdd.Should().BeEmpty();
        noRemove.Should().BeEmpty();
    }

    [Test]
    public void HasEqualOrStrongerCopy_StopsTwoCommandersReplacingEachOther()
    {
        SpaceAuraRules.HasEqualOrStrongerCopy(14, new[] { 14 }).Should().BeTrue();
        SpaceAuraRules.HasEqualOrStrongerCopy(14, new[] { 16 }).Should().BeTrue();
        SpaceAuraRules.HasEqualOrStrongerCopy(14, new[] { 12 }).Should().BeFalse();
        SpaceAuraRules.HasEqualOrStrongerCopy(14, Array.Empty<int>()).Should().BeFalse();
    }

    [Test]
    public void LostCopies_TwoCommandersOnOneShip_TheWeakerAuraTakesOverWhenTheStrongerEnds()
    {
        // Formation's status does not stack, so a ship holds one copy at a time: here, which commander's copy it holds.
        const uint ship = 7, strong = 1, weak = 2;
        var social = new Dictionary<uint, int> { [strong] = 20, [weak] = 14 };
        var held = new Dictionary<uint, uint>();
        var recipients = new Dictionary<uint, HashSet<uint>> { [strong] = new(), [weak] = new() };

        void Refresh(uint source, params uint[] inRange) => ReconcileAura(source, social, held, recipients[source], inRange);

        Refresh(weak, ship);
        held[ship].Should().Be(weak);
        Refresh(strong, ship);
        held[ship].Should().Be(strong, "the stronger commander's copy replaces the weaker one");

        Refresh(weak, ship);
        recipients[weak].Should().NotContain(ship, "the weaker aura no longer holds the ship, so it may give it again later");
        held[ship].Should().Be(strong, "and it does not replace the stronger copy meanwhile");

        // The stronger commander turns Formation off (or leaves range): its copy goes.
        foreach (var recipient in recipients[strong])
            held.Remove(recipient);
        recipients[strong].Clear();

        Refresh(weak, ship);
        held[ship].Should().Be(weak, "the ship is still inside the weaker active aura");
    }

    [Test]
    public void LostCopies_OneCommander_KeepsItsRecipientsAndFollowsTheRange()
    {
        const uint ship = 7, commander = 1;
        var social = new Dictionary<uint, int> { [commander] = 14 };
        var held = new Dictionary<uint, uint>();
        var recipients = new HashSet<uint>();

        ReconcileAura(commander, social, held, recipients, ship, commander);
        ReconcileAura(commander, social, held, recipients, ship, commander);
        recipients.Should().BeEquivalentTo(new[] { ship, commander });
        held[ship].Should().Be(commander);

        ReconcileAura(commander, social, held, recipients, commander);
        held.Should().NotContainKey(ship, "a ship that left range loses the aura");

        ReconcileAura(commander, social, held, recipients, ship, commander);
        held[ship].Should().Be(commander, "and gets it back on return");

        // Something else removed the status (a cleanse): the next refresh gives it back.
        held.Remove(ship);
        ReconcileAura(commander, social, held, recipients, ship, commander);
        held[ship].Should().Be(commander);
    }

    [Test]
    public void SpaceAuras_DropsLostCopiesBeforeItReconciles()
    {
        var source = File.ReadAllText(Path.Combine(RepositoryRoot(), "SWLOR.Game.Server", "Service", "SpaceAuras.cs")).Replace("\r\n", "\n");
        var reconcile = source[source.IndexOf("private static void Reconcile(uint source, SpaceAura aura)", StringComparison.Ordinal)..];
        var lost = reconcile.IndexOf("SpaceAuraRules.LostCopies(aura.Recipients, x => StatusEffect.HasStatusEffect(x, aura.StatusEffect, source))", StringComparison.Ordinal);
        var diff = reconcile.IndexOf("SpaceAuraRules.Reconcile(aura.Recipients, inRange)", StringComparison.Ordinal);

        lost.Should().BeGreaterThan(0);
        diff.Should().BeGreaterThan(lost, "a recipient that lost its copy must be a ship to add again in the same pass");
    }

    [Test]
    public void AuraUser_AStationUsersAuraReadsThem_AndThePilotsOwnOrAnOldAuraReadsTheShip()
    {
        const uint ship = 7, gunner = 9;
        const uint invalid = SWLOR.NWN.API.NWScript.NWScript.OBJECT_INVALID;

        SpaceAuraRules.RecordedUser(ship, gunner).Should().Be(gunner, "a crew member switched it on from a station");
        SpaceAuraRules.RecordedUser(ship, ship).Should().Be(invalid, "the pilot's own aura records no user");
        SpaceAuraRules.Commander(ship, gunner).Should().Be(gunner);
        SpaceAuraRules.Commander(ship, invalid).Should().Be(ship, "an aura with no user, the pilot's or an older one, reads the ship");
        SpaceAuraRules.Commander(ship, 0).Should().Be(ship);

        SpaceAuraRules.UserLeft(gunner, seatedOnShip: true).Should().BeFalse();
        SpaceAuraRules.UserLeft(gunner, seatedOnShip: false).Should().BeTrue("the user left the seat or logged out");
        SpaceAuraRules.UserLeft(invalid, seatedOnShip: false).Should().BeFalse("the pilot's own aura has no seat to leave");

        // The strongest-copy comparison is between commanders: a SOC 20 gunner's copy on a SOC 10 pilot's ship beats a
        // SOC 16 commander's.
        var social = new Dictionary<uint, int> { [ship] = 10, [gunner] = 20, [3] = 16 };
        SpaceAuraRules.HasEqualOrStrongerCopy(social[SpaceAuraRules.Commander(3, invalid)],
            new[] { social[SpaceAuraRules.Commander(ship, gunner)] }).Should().BeTrue();
        SpaceAuraRules.HasEqualOrStrongerCopy(social[SpaceAuraRules.Commander(ship, gunner)],
            new[] { social[SpaceAuraRules.Commander(3, invalid)] }).Should().BeFalse();
    }

    [Test]
    public void SpaceAuras_ReadTheStationUser_AndEndWithTheirSeat()
    {
        var root = RepositoryRoot();
        var auras = File.ReadAllText(Path.Combine(root, "SWLOR.Game.Server", "Service", "SpaceAuras.cs")).Replace("\r\n", "\n");

        // Switching on records the user, whose Social sets the limit.
        var apply = auras[auras.IndexOf("public static bool ApplyAura(", StringComparison.Ordinal)..];
        apply.Should().Contain("var user = SpaceCrew.GetAbilityUser(source);")
            .And.Contain("SpaceAuraRules.MaxAuras(GetAbilityScore(user, AbilityType.Social))")
            .And.Contain("User = SpaceAuraRules.RecordedUser(source, user),");

        // Range reads the commander (Leadership radius here, Command Presence and the Leadership bonus in SpaceCommand).
        auras.Should().Contain("Stat.GetStatAdjustment(SpaceCrew.GetAbilityUser(ship), StatType.LeadershipCommandRadiusBonusMeters)");
        File.ReadAllText(Path.Combine(root, "SWLOR.Game.Server", "Service", "SpaceCommand.cs"))
            .Should().Contain("Stat.GetStatAdjustment(SpaceCrew.GetAbilityUser(ship), StatType.SpaceCommandRangeBonusMeters)");

        // Each refresh sets the user's station use up again before it measures range and gives the status, so the
        // status's Social and Leadership scaling read the user; the ship stays the status source.
        var reconcile = auras[auras.IndexOf("private static void Reconcile(uint source, SpaceAura aura)", StringComparison.Ordinal)..];
        reconcile.IndexOf("using var userScope = BeginUserScope(source, aura);", StringComparison.Ordinal)
            .Should().BeGreaterThan(0)
            .And.BeLessThan(reconcile.IndexOf("GetCommandRange(source)", StringComparison.Ordinal));
        auras.Should().Contain("StatusEffect.ApplyStatusEffect(source, ship, statusEffect, 0f)")
            .And.Contain("StatusEffect.RemoveStatusEffect(ship, aura.StatusEffect, source, false);")
            .And.Contain("GetAbilityScore(GetCommander(source, aura.StatusEffect), AbilityType.Social)")
            .And.Contain("GetAbilityScore(GetCommander(effect.Source, aura.StatusEffect), AbilityType.Social)");

        // The aura ends with the user's seat: on the refresh, and at once when they leave or disconnect.
        var refresh = auras[auras.IndexOf("public static void Refresh()", StringComparison.Ordinal)..];
        refresh.IndexOf("SpaceAuraRules.UserLeft(aura.User, seated)", StringComparison.Ordinal)
            .Should().BeGreaterThan(0)
            .And.BeLessThan(refresh.IndexOf("Reconcile(source, aura);", StringComparison.Ordinal));
        var stations = File.ReadAllText(Path.Combine(root, "SWLOR.Game.Server", "Service", "SpaceCrewStations.cs")).Replace("\r\n", "\n");
        var leave = stations[stations.IndexOf("public static void LeaveCrewStation(", StringComparison.Ordinal)..];
        leave.Should().Contain("SpaceAuras.RemoveUserAuras(pilot, player);");
        var disconnect = stations[stations.IndexOf("private static void CrewMemberDisconnected(", StringComparison.Ordinal)..];
        disconnect.Should().Contain("SpaceAuras.RemoveUserAuras(assignment.Pilot, player);");
    }

    [Test]
    public void ADroidsShot_KeepsItsInvalidStatSource_SoNoPerkProcFires()
    {
        const uint ship = 7, gunner = 9;
        const uint invalid = SWLOR.NWN.API.NWScript.NWScript.OBJECT_INVALID;

        new SpaceShotEvent { Attacker = ship, Result = new ShipShotResult { StatSource = invalid } }.StatSource
            .Should().Be(invalid, "a droid has no perks, so the pilot's must not stand in");
        new SpaceShotEvent { Attacker = ship, Result = new ShipShotResult { StatSource = gunner } }.StatSource.Should().Be(gunner);
        new SpaceShotEvent { Attacker = ship, Result = new ShipShotResult { StatSource = ship } }.StatSource.Should().Be(ship);
        new SpaceShotEvent { Attacker = ship }.StatSource.Should().Be(ship, "an event with no roll reads the attacker");

        var root = RepositoryRoot();
        var sustained = File.ReadAllText(Path.Combine(root, "SWLOR.Game.Server", "Service", "SpaceSustainedFire.cs")).Replace("\r\n", "\n");
        sustained.Should().NotContain("statSource = attacker;", "droid fire must not borrow the pilot's Sustained Fire")
            .And.Contain("if (!GetIsObjectValid(statSource))\n            {\n                ClearWeapon(attacker, weaponSlot);\n                return 0;\n            }");
        var ordnance = File.ReadAllText(Path.Combine(root, "SWLOR.Game.Server", "Service", "SpaceOrdnance.cs")).Replace("\r\n", "\n");
        var landed = ordnance[ordnance.IndexOf("private static void OnShotLanded(", StringComparison.Ordinal)..];
        landed.IndexOf("if (!GetIsObjectValid(e.StatSource))", StringComparison.Ordinal)
            .Should().BeGreaterThan(0)
            .And.BeLessThan(landed.IndexOf("Stat.GetStatAdjustment(e.StatSource, StatType.SpaceLauncherSplashChance)", StringComparison.Ordinal));
    }

    /// <summary>One refresh of a commander's aura as SpaceAuras runs it, with a non-stacking status.</summary>
    private static void ReconcileAura(uint source, IReadOnlyDictionary<uint, int> social, Dictionary<uint, uint> held,
        HashSet<uint> recipients, params uint[] inRange)
    {
        foreach (var ship in SpaceAuraRules.LostCopies(recipients, x => held.TryGetValue(x, out var holder) && holder == source))
            recipients.Remove(ship);

        var (add, remove) = SpaceAuraRules.Reconcile(recipients, inRange);
        foreach (var ship in remove)
        {
            recipients.Remove(ship);
            if (held.TryGetValue(ship, out var holder) && holder == source)
                held.Remove(ship);
        }

        foreach (var ship in add)
        {
            if (held.TryGetValue(ship, out var holder) && holder != source &&
                SpaceAuraRules.HasEqualOrStrongerCopy(social[source], new[] { social[holder] }))
            {
                continue;
            }

            held[ship] = source;
            recipients.Add(ship);
        }
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        return directory!.FullName;
    }

    // Events

    [Test]
    public void CombatEvents_ReachEverySubscriberEvenWhenOneThrows()
    {
        var seen = new List<string>();
        Action<SpaceShotEvent> failing = _ => throw new InvalidOperationException("handler bug");
        Action<SpaceShotEvent> recording = e => seen.Add($"landed {e.Result.Damage}");
        SpaceCombatEvents.ShotLanded += failing;
        SpaceCombatEvents.ShotLanded += recording;
        try
        {
            SpaceCombatEvents.RaiseShotLanded(new SpaceShotEvent
            {
                Shot = new SpaceWeaponShot { Family = SpaceWeaponFamily.Launcher, IsAbility = true },
                Result = new ShipShotResult { IsHit = true, Damage = 31 },
            });
        }
        finally
        {
            SpaceCombatEvents.ShotLanded -= failing;
            SpaceCombatEvents.ShotLanded -= recording;
        }

        seen.Should().Equal("landed 31");
    }

    [Test]
    public void ShotEvent_ReportsTheFamilyAndWhetherAnAbilityFiredIt()
    {
        var abilityShot = new SpaceShotEvent { Shot = new SpaceWeaponShot { Family = SpaceWeaponFamily.Launcher, IsAbility = true } };
        var cycleShot = new SpaceShotEvent { Shot = new SpaceWeaponShot { Family = SpaceWeaponFamily.Energy } };

        abilityShot.Family.Should().Be(SpaceWeaponFamily.Launcher);
        abilityShot.FromAbility.Should().BeTrue();
        cycleShot.Family.Should().Be(SpaceWeaponFamily.Energy);
        cycleShot.FromAbility.Should().BeFalse();
    }

    [Test]
    public void RaisingWithNoSubscribersDoesNothing()
    {
        var raise = () =>
        {
            SpaceCombatEvents.RaiseShotEvaded(new SpaceShotEvent());
            SpaceCombatEvents.RaiseHullDamaged(new SpaceHullEvent());
            SpaceCombatEvents.RaiseHostileAbilityUsed(new SpaceAbilityEvent());
        };

        raise.Should().NotThrow();
    }

    [Test]
    public void FireResult_FailureCarriesTheReasonAndNeverCountsAsAHit()
    {
        var result = SpaceFireResult.Failure("Your ship has no energy weapon fitted.");

        result.Fired.Should().BeFalse();
        result.IsHit.Should().BeFalse();
        result.IsCritical.Should().BeFalse();
        result.Error.Should().Be("Your ship has no energy weapon fitted.");
    }

    [Test]
    public void FireResult_ReportsHitAndCriticalFromTheShot()
    {
        var result = new SpaceFireResult { Fired = true, Shot = new ShipShotResult { IsHit = true, IsCritical = true, Damage = 40 } };

        result.IsHit.Should().BeTrue();
        result.IsCritical.Should().BeTrue();
    }
}
