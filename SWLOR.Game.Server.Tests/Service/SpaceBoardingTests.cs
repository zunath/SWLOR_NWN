using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
namespace SWLOR.Game.Server.Tests.Service;
public class SpaceBoardingTests
{
    private static readonly DateTime Now=new(2026,10,6,12,0,0,DateTimeKind.Utc);
    private static SpaceContract Contract()=>new(){Id="mission",State=SpaceContractState.Active,ShipsByPlayer=new(){["operator"]="ship",["other"]="ship2"},BoardingPlayers=new(){"operator","other"},BoardingEndsAt=Now.AddSeconds(90)};
    [Test]
    public void ThreeUniqueConsoles_RecoverExactlyTheAuthoredCargoAndSurviveReplay()
    {
        var c=Contract();var p=SpaceActivityCatalog.Default.Profiles["boarding"];var ship=new ShipStatus {CargoCapacity=36};
        for(var i=0;i<3;i++)SpaceBoardingRecovery.GrantConsole(c,p,ship,i,"operator",Now).Should().BeTrue();
        c.Boarded.Should().BeTrue();ShipCargo.Occupied(ship).Should().Be(36);ShipCargo.Amount(ship,"prec_assembly").Should().Be(33);ShipCargo.Amount(ship,"ore_tilarium").Should().Be(3);
        (ShipCargo.Amount(ship,"prec_assembly")*72+ShipCargo.Amount(ship,"ore_tilarium")*8).Should().Be(2400);
        c=JsonConvert.DeserializeObject<SpaceContract>(JsonConvert.SerializeObject(c))!;ship=JsonConvert.DeserializeObject<ShipStatus>(JsonConvert.SerializeObject(ship))!;
        SpaceBoardingRecovery.GrantConsole(c,p,ship,0,"operator",Now).Should().BeFalse();ShipCargo.Occupied(ship).Should().Be(36);
        c.Contributions.Participants["operator"].Points[SkillType.ShipSystems].Should().Be(18);
    }
    [Test]
    public void CapacityExpiryAndWrongOwner_CannotMintOrRedirectConsoleCargo()
    {
        var c=Contract();var p=SpaceActivityCatalog.Default.Profiles["boarding"];var ship=new ShipStatus {CargoCapacity=11};
        var full=()=>SpaceBoardingRecovery.GrantConsole(c,p,ship,0,"operator",Now);full.Should().Throw<InvalidOperationException>();ship.Cargo.Should().BeEmpty();c.BoardingConsoles.Should().BeEmpty();
        ship.CargoCapacity=36;c.BoardingConsoleOperators[0]="operator";
        var wrong=()=>SpaceBoardingRecovery.GrantConsole(c,p,ship,0,"other",Now);wrong.Should().Throw<InvalidOperationException>();
        var expired=()=>SpaceBoardingRecovery.GrantConsole(c,p,ship,0,"operator",Now.AddSeconds(90));expired.Should().Throw<InvalidOperationException>();ship.Cargo.Should().BeEmpty();
    }
}
