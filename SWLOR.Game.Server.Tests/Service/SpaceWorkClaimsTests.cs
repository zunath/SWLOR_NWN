using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Tests.Service;

public class SpaceWorkClaimsTests
{
    private static readonly DateTime Now = new(2026, 10, 6, 0, 0, 0, DateTimeKind.Utc);
    private static SpaceSite Site(int profile = 1) => SpaceWorkClaims.NewDeposit("site", "orbit", 0, SpaceIndustryCatalog.Default.Deposits[profile], Now);
    private static ShipModuleOperation Operation(string design) => ShipOperations.Resolve(new(), new() { Design = design, ItemInstanceId = design });
    private static SpaceWorkClaim Reserve(SpaceSite site, string module = "precision_cutter", string ship = "ship", double cargo = 1000) =>
        SpaceWorkClaims.Reserve(site, "operator", ship, "flight", module, Operation(module), cargo, Now);

    [Test]
    public void MixedReservesAndCargo_AreConservedThroughRestartSettlementAndReplay()
    {
        var site = Site();
        var claim = Reserve(site);
        site.Reserves["ore_tilarium"].Should().BeApproximately(245 - 4.2, 1e-9);
        site.Reserves["ore_currian"].Should().BeApproximately(105 - 1.8, 1e-9);
        site = JsonConvert.DeserializeObject<SpaceSite>(JsonConvert.SerializeObject(site))!;
        SpaceWorkClaims.Complete(site, claim.Id, Now.AddSeconds(12), .5).Should().BeTrue();
        claim = site.Claims[claim.Id];
        claim.Cargo.Values.Sum().Should().BeApproximately(4.8, 1e-9);
        claim.BaseXP.Should().Be(18);
        var ship = new ShipStatus { CargoCapacity = 100 };
        SpaceWorkClaims.SettleCargo(ship, claim);
        ship = JsonConvert.DeserializeObject<ShipStatus>(JsonConvert.SerializeObject(ship))!;
        SpaceWorkClaims.SettleCargo(ship, claim);
        ShipCargo.Occupied(ship).Should().BeApproximately(4.8, 1e-9);
        SpaceWorkClaims.Complete(site, claim.Id, Now.AddSeconds(24), .1).Should().BeFalse();
        SpaceWorkClaims.Cancel(site, claim.Id).Should().BeFalse();
    }

    [Test]
    public void CancellationAndExpiry_ReturnReservedConstituentsOnce()
    {
        var site = Site(); var claim = Reserve(site);
        SpaceWorkClaims.Cancel(site, claim.Id).Should().BeTrue();
        SpaceWorkClaims.Cancel(site, claim.Id).Should().BeFalse();
        site.Reserves.Should().BeEquivalentTo(site.InitialReserves);
        var expired = Reserve(site);
        SpaceWorkClaims.Complete(site, expired.Id, site.ExpiresAt, .5).Should().BeFalse();
        site.Reserves.Should().BeEquivalentTo(site.InitialReserves);
    }

    [Test]
    public void CapacityAndHardness_RejectBeforeReservingAnything()
    {
        var site = Site();
        var tooFull = () => Reserve(site, cargo: 4);
        tooFull.Should().Throw<InvalidOperationException>().WithMessage("*room*");
        site.Reserves.Should().BeEquivalentTo(site.InitialReserves);
        var deep = Site(4);
        var weakTool = () => Reserve(deep);
        weakTool.Should().Throw<InvalidOperationException>().WithMessage("*hardness*");
        deep.Reserves.Should().BeEquivalentTo(deep.InitialReserves);
        var unsurveyed = () => Reserve(deep, "compact_drill");
        unsurveyed.Should().Throw<InvalidOperationException>().WithMessage("*Survey*");
    }

    [Test]
    public void WorkingPositions_CountShipsAndPreventDuplicateToolClaims()
    {
        var site = Site();
        Reserve(site, ship: "one"); Reserve(site, ship: "two"); Reserve(site, ship: "three");
        var fourth = () => Reserve(site, ship: "four");
        fourth.Should().Throw<InvalidOperationException>().WithMessage("*positions*");
        var duplicate = () => Reserve(site, ship: "one");
        duplicate.Should().Throw<InvalidOperationException>().WithMessage("*pending*");
    }

    [Test]
    public void LastPartialCycle_CannotCreateAnExhaustedRareConstituent()
    {
        var site = Site(); site.Reserves["ore_tilarium"] = 2; site.Reserves["ore_currian"] = 0;
        var claim = Reserve(site);
        claim.Allocations.Values.Sum().Should().Be(2);
        SpaceWorkClaims.Complete(site, claim.Id, claim.CompletesAt, .5);
        claim.Cargo["ore_currian"].Should().Be(0);
        claim.Cargo["ore_tilarium"].Should().BeApproximately(1.6, 1e-9);
        site.Reserves.Values.Sum().Should().Be(0);
    }

    [Test]
    public void Rescanning_RevealsInformationWithoutRepeatingDiscoveryXP()
    {
        var site = Site(0); var first = Reserve(site, "survey_scanner");
        SpaceWorkClaims.Complete(site, first.Id, first.CompletesAt, .5);
        first.BaseXP.Should().Be(150);
        var second = SpaceWorkClaims.Reserve(site, "operator", "ship", "flight", "scanner2", Operation("survey_scanner"), 0, first.CompletesAt);
        SpaceWorkClaims.Complete(site, second.Id, second.CompletesAt, .5);
        second.BaseXP.Should().Be(0);
        site.Reserves.Should().BeEquivalentTo(site.InitialReserves);
    }

    [Test]
    public void ComponentAttempt_ConsumesFiniteReserveOnSuccessAndFailure()
    {
        var site = Site(); site.Kind = SpaceSiteKind.Wreck; site.Reserves = new() { ["components"] = 2, ["bulk"] = 18 }; site.InitialReserves = new(site.Reserves);
        var failed = Reserve(site, "electronics_kit");
        SpaceWorkClaims.Complete(site, failed.Id, failed.CompletesAt, .9);
        failed.Cargo.Should().BeEmpty(); site.Reserves["components"].Should().Be(1);
        var succeeded = SpaceWorkClaims.Reserve(site, "operator", "ship", "flight", "kit2", Operation("electronics_kit"), 10, failed.CompletesAt);
        SpaceWorkClaims.Complete(site, succeeded.Id, succeeded.CompletesAt, .1);
        succeeded.Cargo["elec_recover"].Should().Be(1); site.Reserves["components"].Should().Be(0);
    }

    [Test]
    public void StabilityVent_PausesOtherClaimsWithoutDuplicatingReserve()
    {
        var site = Site(); site.Stability = 21;
        var first = Reserve(site); var second = SpaceWorkClaims.Reserve(site, "operator", "ship2", "flight2", "cutter2", Operation("precision_cutter"), 100, Now);
        SpaceWorkClaims.Complete(site, first.Id, first.CompletesAt, .5).Should().BeTrue();
        site.Stability.Should().Be(20); site.VentsUntil.Should().Be(first.CompletesAt.AddSeconds(10));
        SpaceWorkClaims.Complete(site, second.Id, second.CompletesAt, .5).Should().BeFalse();
        site.Reserves.Values.Sum().Should().BeApproximately(344, 1e-9);
    }

    [Test]
    public void RecoveryAndIntactBonuses_RespectTheirSeparateCaps()
    {
        var operation = Operation("precision_cutter");
        SpaceWorkClaims.Recovery(operation, new Dictionary<StatType, double> { [StatType.ShipResourceRecovery] = .3 }, null).Should().Be(.95);
        var site = Site(); site.Kind = SpaceSiteKind.Wreck; site.Reserves = new() { ["components"] = 2 }; site.InitialReserves = new(site.Reserves);
        var claim = SpaceWorkClaims.Reserve(site, "operator", "ship", "flight", "kit", Operation("electronics_kit"), 10, Now,
            new Dictionary<StatType, double> { [StatType.ShipIntactSalvageChance] = 1 });
        claim.IntactChance.Should().Be(.55);
    }
    [Test]
    public void SelectiveRecovery_ConsumesActualRareConstituentsAndCannotCreateAnExhaustedOne()
    {
        var site=Site();site.SurveyedBy.Add("operator");
        var temporary=new Dictionary<StatType,double>{[StatType.ShipSelectedRecovery]=.8};
        var first=SpaceWorkClaims.Reserve(site,"operator","ship","flight","one",Operation("precision_cutter"),100,Now,temporary:temporary,selectedConstituent:"ore_currian");
        first.Allocations["ore_currian"].Should().BeApproximately(4.8,1e-9);
        first.Allocations.Values.Sum().Should().BeApproximately(6,1e-9);
        SpaceWorkClaims.Cancel(site,first.Id);
        site.Reserves["ore_currian"]=2;
        var scarce=SpaceWorkClaims.Reserve(site,"operator","ship","flight","two",Operation("precision_cutter"),100,Now,temporary:temporary,selectedConstituent:"ore_currian");
        scarce.Allocations["ore_currian"].Should().Be(2);
        site.Reserves["ore_currian"].Should().Be(0);
        var exhausted=SpaceWorkClaims.Reserve(site,"operator","ship","flight","three",Operation("precision_cutter"),100,Now,temporary:temporary,selectedConstituent:"ore_currian");
        exhausted.Allocations["ore_currian"].Should().Be(0);
        exhausted.Allocations.Values.Sum().Should().BeApproximately(6,1e-9);
    }
    [Test]
    public void AuthoredDiscovery_HasOneDrawPerObjectAcrossRepeatedScansAndRestart()
    {
        var site=Site(0);site.HiddenResearchComponent="authored_component";
        var temporary=new Dictionary<StatType,double>{[StatType.ShipDiscoveryChance]=.05};
        var first=SpaceWorkClaims.Reserve(site,"operator","ship","flight","scanner",Operation("survey_scanner"),1,Now,temporary:temporary,channelSeconds:15);
        first.ReservedCargo.Should().Be(1);
        SpaceWorkClaims.Complete(site,first.Id,Now.AddSeconds(14),.5,.01).Should().BeFalse();
        SpaceWorkClaims.Complete(site,first.Id,Now.AddSeconds(15),.5,.01).Should().BeTrue();
        first.Cargo["authored_component"].Should().Be(1);
        site=JsonConvert.DeserializeObject<SpaceSite>(JsonConvert.SerializeObject(site))!;
        var second=SpaceWorkClaims.Reserve(site,"operator","ship","flight","scanner",Operation("survey_scanner"),0,Now.AddSeconds(16),temporary:temporary);
        second.ReservedCargo.Should().Be(0);
        SpaceWorkClaims.Complete(site,second.Id,Now.AddSeconds(30),.5,.01).Should().BeTrue();
        second.Cargo.Should().BeEmpty();second.BaseXP.Should().Be(0);
    }
}
