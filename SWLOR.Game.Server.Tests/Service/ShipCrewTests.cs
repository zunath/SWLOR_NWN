using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
namespace SWLOR.Game.Server.Tests.Service;
public class ShipCrewTests
{
    [Test]
    public void Stations_ReplaceOnlyTheirSkillsAndNeverAddPassengerRanks()
    {
        var skills=new[]{SkillType.Piloting,SkillType.Gunnery,SkillType.ShipSystems,SkillType.Astrometrics,SkillType.SpaceIndustry};
        var pilot=skills.ToDictionary(x=>x,x=>50);
        var result=ShipCrewPolicy.Skills(pilot,new Dictionary<ShipCrewStation,IReadOnlyDictionary<SkillType,int>>
        {[ShipCrewStation.Weapons]=skills.ToDictionary(x=>x,x=>10),[ShipCrewStation.SurveyIndustry]=skills.ToDictionary(x=>x,x=>20)});
        result[SkillType.Piloting].Should().Be(50);result[SkillType.Gunnery].Should().Be(10);
        result[SkillType.ShipSystems].Should().Be(50);result[SkillType.Astrometrics].Should().Be(20);result[SkillType.SpaceIndustry].Should().Be(20);
        ShipCrewPolicy.Skills(pilot,new Dictionary<ShipCrewStation,IReadOnlyDictionary<SkillType,int>>())[SkillType.Gunnery].Should().Be(50);
    }
    [Test]
    public void Authority_UsesOneResponsibleOperatorWithPilotFallbackForInactiveStations()
    {
        var active=new Dictionary<ShipCrewStation,string>{[ShipCrewStation.Weapons]="gunner"};
        ShipCrewPolicy.CanOperate("pilot","pilot",SkillType.Gunnery,active).Should().BeFalse();
        ShipCrewPolicy.CanOperate("gunner","pilot",SkillType.Gunnery,active).Should().BeTrue();
        ShipCrewPolicy.CanOperate("gunner","pilot",SkillType.ShipSystems,active).Should().BeFalse();
        ShipCrewPolicy.CanOperate("pilot","pilot",SkillType.ShipSystems,active).Should().BeTrue();
        ShipCrewPolicy.CanOperate("passenger","pilot",SkillType.Piloting,active).Should().BeFalse();
        active.Clear();ShipCrewPolicy.CanOperate("pilot","pilot",SkillType.Gunnery,active).Should().BeTrue();
    }
    [Test]
    public void Assignment_RejectsDuplicateRolesAndOccupiedStations()
    {
        var ship=new ShipStatus();ShipCrewPolicy.Assign(ship,ShipCrewStation.Weapons,"gunner");
        var duplicate=()=>ShipCrewPolicy.Assign(ship,ShipCrewStation.Systems,"gunner");duplicate.Should().Throw<InvalidOperationException>();
        var takeover=()=>ShipCrewPolicy.Assign(ship,ShipCrewStation.Weapons,"intruder");takeover.Should().Throw<InvalidOperationException>();
        ShipCrewPolicy.Assign(ship,ShipCrewStation.Systems,"support");ShipCrewPolicy.Assign(ship,ShipCrewStation.SurveyIndustry,"miner");ship.Crew.Should().HaveCount(3);
    }
    [Test]
    public void ContractCrew_ShareOneMissionWalletWithoutIncreasingRequiredShipsOrIdleXP()
    {
        var contract=new SpaceContract{Id="crew-test",State=SpaceContractState.Active,ShipsByPlayer=new(){["pilot"]="ship"},CrewByPlayer=new(){["gunner"]="ship",["miner"]="ship",["idle"]="ship"}};
        var site=new SpaceSite{Id="site",ActivityId=contract.Id};contract.Sites.Add(site.Id);
        var claim=new SpaceWorkClaim{Id="work",PlayerId="miner",ShipId="ship",State=SpaceWorkState.Completed,Action=ShipModuleAction.Extraction,Allocations=new(){["ore_tilarium"]=10},Cargo=new(){["ore_tilarium"]=8}};
        SpaceContractPolicy.CreditWork(contract,site,claim).Should().BeTrue();
        var encounter=new SpaceEncounter{Id="enemy",ActivityId=contract.Id,Completed=true};contract.EncounterObjectives[encounter.Id]="objective";
        encounter.Contributions.CreditDamage("gunner",100);SpaceContractPolicy.CreditEncounter(contract,encounter).Should().BeTrue();
        var profile=SpaceActivityCatalog.Default.Profiles["prospecting"];var xp=SpaceContractPolicy.Experience(contract,profile);
        contract.ShipsByPlayer.Should().HaveCount(1);xp["idle"].Values.Sum().Should().Be(0);
        xp["miner"][SkillType.SpaceIndustry].Should().BeGreaterThan(0);xp.Values.Sum(x=>x.Values.Sum()).Should().BeLessThanOrEqualTo(profile.XP);
        contract.Contributions.Allocate(profile.Credits).Values.Sum().Should().BeLessThanOrEqualTo(profile.Credits);
    }
}
