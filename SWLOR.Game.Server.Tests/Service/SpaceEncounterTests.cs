using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.SpaceObjectDefinition;
using SWLOR.Game.Server.Feature.ShipDefinition;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Tests.Service;

public class SpaceEncounterTests
{
    private static readonly DateTime Now = new(2026, 10, 6, 12, 0, 0, DateTimeKind.Utc);
    [Test]
    public void Catalog_UsesApprovedRolesAndPreservesEveryAuthoredIdentity()
    {
        var catalog = SpaceEncounterCatalog.Default;
        catalog.Profiles.Should().HaveCount(8);
        catalog.Bindings.Should().HaveCount(51);
        var ships = new NPCShipDefinition().BuildShips();
        var objects = new NPCSpaceObjectDefinition().BuildSpaceObjects();
        foreach (var binding in catalog.Bindings.Values)
        {
            var profile = catalog.Profiles[binding.Profile];
            objects[binding.Tag].ShipItemTag.Should().Be(binding.Ship);
            objects[binding.Tag].EncounterProfile.Should().Be(profile.Id);
            ships[binding.Ship].MaxHull.Should().Be(profile.Hull);
            ships[binding.Ship].MaxShield.Should().Be(profile.Shield);
            profile.ShieldResistance.Should().Be(0);
            if (profile.Id.Contains("interceptor")) profile.Agility.Should().Be(26);
        }
        ships.Should().HaveCount(51);
    }
    [Test]
    public void VirtualWeapons_UseExactOutputAndSpendFiniteCapacitorAndAmmunition()
    {
        var catalog = SpaceEncounterCatalog.Default;
        var bomber = catalog.Bindings.Values.First(x => x.Profile == "advanced_bomber");
        var status = catalog.CreateStatus(bomber, "spawn-one");
        var weapon = status.HighPowerModules[1];
        var operation = ShipOperations.Resolve(status, weapon);
        operation.Output.Should().Be(55);
        operation.CapacitorCost.Should().Be(8);
        operation.Profile.PreparationSeconds.Should().Be(2);
        ShipCargo.Amount(status, "heavy_missile").Should().Be(40);
        var context = new ShipActivationContext(true, true, true, true, false, false, false, 30, 40);
        ShipModuleActivationPolicy.Validate(status, weapon, operation, context, Now).Should().BeNull();
        ShipModuleActivationPolicy.Pay(status, weapon, operation, Now);
        status.Capacitor.Should().Be(142);
        ShipCargo.Amount(status, "heavy_missile").Should().Be(39);
        weapon.RecastTime.Should().Be(Now.AddSeconds(10));
        status.GlobalRecast.Should().Be(Now.AddSeconds(1));
        var saved = JsonConvert.DeserializeObject<ShipStatus>(JsonConvert.SerializeObject(status))!;
        saved.HighPowerModules[1].ItemInstanceId.Should().Be(weapon.ItemInstanceId);
        ShipCargo.Amount(saved, "heavy_missile").Should().Be(39);
        ShipModuleActivationPolicy.Validate(saved, saved.HighPowerModules[1], operation, context, Now).Should().NotBeNull();
    }
    [Test]
    public void DamageContribution_CannotExceedFiniteEncounterHitPoints()
    {
        var ledger = new SpaceContributionLedger { RemainingDamage = 140 };
        ledger.CreditDamage("first", 100).Should().Be(100);
        ledger.CreditDamage("second", 100).Should().Be(40);
        ledger.CreditDamage("first", 1000).Should().Be(0);
        ledger.RemainingDamage.Should().Be(0);
        ledger.Participants.Values.Sum(x => x.Points.GetValueOrDefault(SkillType.Gunnery)).Should().Be(140);
        var shares = ledger.Allocate(325, SkillType.Gunnery);
        shares.Values.Sum().Should().Be(325);
        shares["first"].Should().Be(232);
        shares["second"].Should().Be(93);
        ledger.Allocate(50, SkillType.ShipSystems).Should().BeEmpty("unused support XP remains unearned");
    }
    [Test]
    public void SupportContribution_ConsumesOnlyActualHostileDamageDebt()
    {
        var debt = new SpaceHostileDamageDebt { Hull = 30, Shield = 20 };
        debt.Recover(ShipResource.Hull, 100).Should().Be(30);
        debt.Recover(ShipResource.Hull, 100).Should().Be(0);
        debt.Recover(ShipResource.Shield, 5).Should().Be(5);
        debt.Recover(ShipResource.Capacitor, 100).Should().Be(0);
        var ledger = new SpaceContributionLedger();
        ledger.CreditEnergy("support", "idle", 20, 16);
        ledger.Participants.Should().BeEmpty();
        ledger.Credit("active", SkillType.Piloting, 10);
        ledger.CreditEnergy("active", "active", 20, 16);
        ledger.Participants["active"].EnergyPoints.Should().Be(0);
        ledger.CreditEnergy("support", "active", 20, 16);
        ledger.Participants["support"].EnergyPoints.Should().Be(16);
        ledger.Allocate(5, SkillType.ShipSystems, true).Values.Sum().Should().Be(5);
    }
    [Test]
    public void Wreck_ReservesBelongToParticipantsUntilPublicRecoveryAndNeverRegenerate()
    {
        var profile = SpaceEncounterCatalog.Default.Profiles["advanced_bomber"];
        var site = SpaceWorkClaims.NewWreck("wreck-one", "space", profile, new[] { "participant" }, Now);
        site.Reserves["bulk"].Should().Be(36);
        site.Reserves["components"].Should().Be(3);
        site.ExclusiveUntil.Should().Be(Now.AddSeconds(120));
        site.ExpiresAt.Should().Be(Now.AddSeconds(600));
        site.RespawnsAt.Should().Be(DateTime.MaxValue);
        site.DifficultComponent.Should().BeTrue();
        var ship = new ShipStatus { FittingVersion = ShipFittingConversion.CurrentVersion, Hull = 100, MaxHull = 100 };
        var fitted = new ShipStatus.ShipStatusModule { Design = "recovery_arm" };
        var operation = ShipOperations.Resolve(ship, fitted);
        SpaceWorkClaims.Validate(site, "outsider", "ship", operation, 100, Now).Should().Contain("reserved");
        SpaceWorkClaims.Validate(site, "outsider", "ship", operation, 100, Now.AddSeconds(120)).Should().BeNull();
        var claim = SpaceWorkClaims.Reserve(site, "participant", "ship", "flight", "module", operation, 100, Now);
        SpaceWorkClaims.Complete(site, claim.Id, claim.CompletesAt, .5).Should().BeTrue();
        site.Reserves["bulk"].Should().Be(30);
        SpaceWorkClaims.Complete(site, claim.Id, claim.CompletesAt, .5).Should().BeFalse();
    }
    [Test]
    public void ActiveClock_SharesCombatAndIndustryCeilingWithoutAdvancingForIdleTime()
    {
        var ledger = new SpaceExperienceLedger();
        ledger.Touch(Now, Now.AddSeconds(5));
        ledger.Touch(Now.AddSeconds(1), Now.AddSeconds(6));
        ledger.ActiveClock.Should().Be(6);
        ledger.ClaimOperating(17900).Should().Be(17900);
        ledger.Claim("flight", ShipModuleAction.Extraction, 300, Now.AddHours(1), Now.AddHours(1).AddSeconds(10)).Should().Be(100);
        ledger.ActiveClock.Should().Be(16);
        ledger.ClaimOperating(500).Should().Be(0);
        ledger.Touch(Now.AddHours(1).AddSeconds(10), Now.AddHours(2).AddSeconds(10));
        ledger.ClaimOperating(500).Should().Be(500);
    }
}
