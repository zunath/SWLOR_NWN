using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
namespace SWLOR.Game.Server.Tests.Service;
public class SpaceContractTests
{
    private static readonly DateTime Now=new(2026,10,6,12,0,0,DateTimeKind.Utc);
    private static SpaceContract Contract(string profile)=>new() { Id="mission",Profile=profile,State=SpaceContractState.Active,StartedAt=Now,ExpiresAt=Now.AddHours(2),ShipsByPlayer=new() {["pilot"]="ship"} };
    [Test]
    public void Catalog_PreservesAllElevenApprovedSharedWalletsAndTripBudgets()
    {
        var catalog=SpaceActivityCatalog.Default;catalog.Profiles.Should().HaveCount(11);
        catalog.Profiles["fleet"].Credits.Should().Be(14000);catalog.Profiles["fleet"].XP.Should().Be(30000);catalog.Profiles["fleet"].Party.Should().Be(4);
        catalog.Profiles["patrol"].MinimumSeconds.Should().Be(900);catalog.Profiles["prospecting"].Ore.Should().Be(200);
        catalog.Profiles["bulk"].Ore.Should().Be(600);catalog.Profiles["salvage"].Bulk.Should().Be(144);catalog.Profiles["salvage"].Attempts.Should().Be(12);
        catalog.Profiles["delivery"].Freight.Should().Be(60);catalog.Profiles["freight"].Freight.Should().Be(120);
        foreach(var p in catalog.Profiles.Values){p.SkillPools.Values.Sum().Should().BeLessThanOrEqualTo(p.XP);(p.LegInterval*p.Legs).Should().Be(p.MinimumSeconds);}
    }
    [Test]
    public void Navigation_RequiresActualArrivalAndCadenceAndCannotRepeat()
    {
        var p=SpaceActivityCatalog.Default.Profiles["patrol"];var c=Contract(p.Id);
        SpaceContractPolicy.Arrive(c,p,"pilot",0,Now.AddSeconds(299)).Should().BeFalse();
        SpaceContractPolicy.Arrive(c,p,"outsider",0,Now.AddSeconds(300)).Should().BeFalse();
        SpaceContractPolicy.Arrive(c,p,"pilot",11,Now.AddSeconds(300)).Should().BeFalse();
        SpaceContractPolicy.Arrive(c,p,"pilot",10,Now.AddSeconds(300)).Should().BeTrue();
        SpaceContractPolicy.Arrive(c,p,"pilot",0,Now.AddSeconds(300)).Should().BeFalse();
        SpaceContractPolicy.CanFinish(c,p,Now.AddHours(1)).Should().BeFalse();
        c.Kills.UnionWith(new[]{"a","b","c"});
        SpaceContractPolicy.Arrive(c,p,"pilot",0,Now.AddSeconds(600)).Should().BeTrue();
        SpaceContractPolicy.Arrive(c,p,"pilot",0,Now.AddSeconds(900)).Should().BeTrue();
        SpaceContractPolicy.CanFinish(c,p,Now.AddSeconds(900)).Should().BeTrue();
        SpaceContractPolicy.CanFinish(c,p,c.ExpiresAt).Should().BeFalse();
        c.Contributions.Participants["pilot"].Points[SkillType.Piloting].Should().Be(3);
    }
    [Test]
    public void MissionWork_CreditsOnlyItsAssignedFiniteSiteAndStableClaimOnce()
    {
        var c=Contract("prospecting");c.Sites.Add("site");
        var site=new SpaceSite {Id="site",ActivityId=c.Id};
        var claim=new SpaceWorkClaim {Id="work",PlayerId="pilot",ShipId="ship",ActivityId=c.Id,State=SpaceWorkState.Completed,Action=ShipModuleAction.Extraction,Allocations=new(){["ore_tilarium"]=10},Cargo=new(){["ore_tilarium"]=7.5}};
        SpaceContractPolicy.CreditWork(c,new SpaceSite {Id="elsewhere"},claim).Should().BeFalse();
        SpaceContractPolicy.CreditWork(c,site,claim).Should().BeTrue();
        var restored=JsonConvert.DeserializeObject<SpaceContract>(JsonConvert.SerializeObject(c))!;
        SpaceContractPolicy.CreditWork(restored,site,claim).Should().BeFalse();restored.RecoveredOre.Should().Be(7.5);
        restored.Contributions.Participants["pilot"].Points[SkillType.SpaceIndustry].Should().Be(10);
    }
    [Test]
    public void RewardPools_AreSharedOnlyAmongActualContributorsAndEnergyStaysBounded()
    {
        var p=SpaceActivityCatalog.Default.Profiles["fleet"];var c=Contract(p.Id);c.ShipsByPlayer["gunner"]="ship2";c.ShipsByPlayer["support"]="ship3";c.ShipsByPlayer["idle"]="ship4";
        c.Contributions.Credit("pilot",SkillType.Piloting,3);c.Contributions.Credit("gunner",SkillType.Gunnery,200);
        c.Contributions.CreditEnergy("support","gunner",20,16);
        var xp=SpaceContractPolicy.Experience(c,p);
        xp["pilot"][SkillType.Piloting].Should().Be(4500);xp["gunner"][SkillType.Gunnery].Should().Be(12000);
        xp["support"][SkillType.ShipSystems].Should().Be(750);xp["idle"].Values.Sum().Should().Be(0);
        xp.Values.Sum(x=>x.Values.Sum()).Should().Be(17250);c.Contributions.Allocate(p.Credits).Values.Sum().Should().Be(14000);
    }
    [Test]
    public void SpawnedEncounter_CannotBePaidTwiceOrImportOutsiderContribution()
    {
        var c=Contract("patrol");c.EncounterObjectives["spawn"]="mission/enemy/0";
        var e=new SpaceEncounter {Id="spawn",ActivityId=c.Id,Completed=true};e.Contributions.Credit("pilot",SkillType.Gunnery,140);e.Contributions.Credit("outsider",SkillType.Gunnery,10000);
        SpaceContractPolicy.CreditEncounter(c,e).Should().BeTrue();SpaceContractPolicy.CreditEncounter(c,e).Should().BeFalse();
        c.Contributions.Participants.Should().ContainKey("pilot").And.NotContainKey("outsider");c.Kills.Should().HaveCount(1);
    }
    [Test]
    public void EmployerCargo_IsUnavailableToAmmoAndNormalConsumptionOrSale()
    {
        var ship=new ShipStatus {CargoCapacity=180};ShipCargo.Add(ship,"light_missile",120,"freight",contractId:"mission");
        ShipCargo.Available(ship).Should().Be(60);ShipCargo.Amount(ship,"light_missile").Should().Be(0);
        var consume=()=>ShipCargo.Consume(ship,"light_missile",1);consume.Should().Throw<InvalidOperationException>();
        SpaceContractPolicy.FreightCharge(100,1).Should().Be(45);SpaceContractPolicy.FreightCharge(1,.09).Should().Be(2);
    }
}
