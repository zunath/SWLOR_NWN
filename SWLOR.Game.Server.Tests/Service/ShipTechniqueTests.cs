using System.Reflection;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Tests.Service;

public class ShipTechniqueTests
{
    private static readonly DateTime Now=new(2026,10,6,12,0,0,DateTimeKind.Utc);
    private static ShipStatus Status()=>new(){FittingVersion=ShipFittingConversion.CurrentVersion,Hull=100,MaxHull=100,Capacitor=100,MaxCapacitor=100};
    private static ShipTechniqueProfile Profile(string name,int rank=1)=>ShipTechniqueCatalog.Default.Profiles.Single(x=>x.Key==name&&x.Rank==rank);
    [Test]
    public void NativeModuleControls_PreserveAllThirtyFeatAndSpellRows()
    {
        var root=new DirectoryInfo(AppContext.BaseDirectory);
        while(root!=null&&!Directory.Exists(Path.Combine(root.FullName,"SWLOR_Haks")))root=root.Parent;
        root.Should().NotBeNull();
        foreach(var table in new[]{("feat.2da",1687),("spells.2da",851)})
        {
            var rows=File.ReadAllLines(Path.Combine(root!.FullName,"SWLOR_Haks","sw_2da",table.Item1)).Skip(3).Where(x=>!string.IsNullOrWhiteSpace(x)).Select(x=>x.Split((char[])null,StringSplitOptions.RemoveEmptyEntries)).ToDictionary(x=>int.Parse(x[0]),x=>x[1]);
            for(var index=0;index<30;index++)rows[table.Item2+index].Should().Be("ShipModule"+(index+1));
        }
    }
    [Test]
    public void Catalog_PreservesAllTenFortyPointSpecializationsAndDistinctAbilityRanks()
    {
        var catalog=ShipTechniqueCatalog.Default;
        catalog.Profiles.GroupBy(x=>x.Style).Should().HaveCount(10);
        foreach(var style in catalog.Profiles.GroupBy(x=>x.Style))
        {
            style.Sum(x=>x.Price).Should().Be(40);
            style.Count().Should().Be(17);
            style.Count(x=>x.Kind==ShipPerkKind.Capstone).Should().Be(1);
        }
        var definitions=typeof(IAbilityListDefinition).Assembly.GetTypes().Where(x=>typeof(IAbilityListDefinition).IsAssignableFrom(x)&&!x.IsInterface&&!x.IsAbstract&&x.Namespace=="SWLOR.Game.Server.Feature.AbilityDefinition.Ships").ToArray();
        definitions.Should().HaveCount(50);
        var abilities=definitions.SelectMany(x=>((IAbilityListDefinition)Activator.CreateInstance(x)!).BuildAbilities()).ToList();
        abilities.Should().HaveCount(110);
        abilities.Select(x=>x.Key).Distinct().Should().HaveCount(110);
        foreach(var (_,ability) in abilities)
        {
            ability.ShipTechnique.Should().NotBeNull();
            ability.CanBeUsedInSpace.Should().BeTrue();
            ability.AbilityLevel.Should().Be(ability.ShipTechnique.Rank);
            ability.EffectiveLevelPerkType.Should().Be(ability.ShipTechnique.Perk);
        }
    }
    [Test]
    public void Preparation_RejectsTwoCapstonesAndDoesNotResetPersistentFamilyCooldown()
    {
        var state=new ShipOperatingState();var status=Status();var ace=Profile("ShipAceManeuver");
        ShipTechniquePolicy.Prepare(state,new[]{ace},Profile("ShipPursuit"),Now);
        ShipTechniquePolicy.Pay(status,state,ace,Now);
        var saved=JsonConvert.DeserializeObject<ShipOperatingState>(JsonConvert.SerializeObject(state))!;
        ShipTechniquePolicy.Prepare(saved,new[]{Profile("ShipDamageControl")},Profile("ShipCruise"),Now.AddSeconds(6));
        saved.Cooldowns[ace.Recast.ToString()].Should().Be(Now.AddSeconds(120));
        saved.ReadyAt.Should().Be(Now.AddSeconds(11));
        Action invalid=()=>ShipTechniquePolicy.Prepare(saved,new[]{ace,Profile("ShipDamageControl")},null!,Now.AddSeconds(12));
        invalid.Should().Throw<InvalidOperationException>();
        saved.Prepared.Should().Equal("ShipDamageControl");
    }
    [Test]
    public void NextPaidEffects_AreScopedAndConsumedTogetherAfterPayment()
    {
        var status=Status();var module=new ShipStatus.ShipStatusModule(){ItemInstanceId="launcher",Design="rapid_missile"};
        ShipTemporaryStats.Add(status,StatType.ShipOrdnanceOutput,.3,10,"salvo",Now,new[]{"launcher"},"enemy",once:true);
        ShipTemporaryStats.Add(status,StatType.ShipAmmunitionDemand,.5,10,"salvo",Now,new[]{"launcher"},"enemy",once:true);
        ShipTemporaryStats.Current(status,Now).GetValueOrDefault(StatType.ShipOrdnanceOutput).Should().Be(0);
        ShipTemporaryStats.Current(status,Now,"launcher","other").GetValueOrDefault(StatType.ShipOrdnanceOutput).Should().Be(0);
        var operation=ShipOperations.Resolve(status,module,temporarySources:ShipTemporaryStats.Sources(status,Now,"launcher","enemy"));
        operation.SupplyQuantity.Should().Be(2);
        operation.Output.Should().BeApproximately(operation.Profile.Output*1.3,1e-9);
        ShipTemporaryStats.ConsumePaidOperation(status,"wrong","enemy",Now);
        status.TemporaryAdjustments.Should().HaveCount(2);
        ShipTemporaryStats.ConsumePaidOperation(status,"launcher","enemy",Now);
        status.TemporaryAdjustments.Should().BeEmpty();
        operation.SupplyQuantity.Should().Be(2,"the paid operation retains its complete snapshot");
    }
    [Test]
    public void BankPayment_IsAtomicForAllAmmoAndCapacitorCosts()
    {
        var status=Status();var first=new ShipStatus.ShipStatusModule(){ItemInstanceId="one",Design="rapid_missile"};var second=new ShipStatus.ShipStatusModule(){ItemInstanceId="two",Design="rapid_missile"};
        var operation=ShipOperations.Resolve(status,first);
        status.Cargo["loaded"]=new("light_missile",1);
        var modules=new[]{(first,operation),(second,operation)};
        Action insufficient=()=>ShipBanks.PayVolley(status,modules,Now);
        insufficient.Should().Throw<InvalidOperationException>();
        status.Capacitor.Should().Be(100);first.RecastTime.Should().Be(default(DateTime));ShipCargo.Amount(status,"light_missile").Should().Be(1);
        status.Cargo["loaded"]=new("light_missile",2);
        ShipBanks.PayVolley(status,modules,Now);
        status.Capacitor.Should().Be(100-2*operation.CapacitorCost);
        ShipCargo.Amount(status,"light_missile").Should().Be(0);
        first.RecastTime.Should().Be(second.RecastTime);
        status.GlobalRecast.Should().Be(Now.AddSeconds(1));
    }
    [Test]
    public void EscapeImmunity_RemovesSoftControlSlowsButKeepsPreparationPenalties()
    {
        var status=Status();status.Speed=1;status.BaseSpeed=1;
        ShipTemporaryStats.Add(status,StatType.ShipSpeed,-.15,10,"enemy slow",Now,softControl:true);
        ShipTemporaryStats.Add(status,StatType.ShipSpeed,-.20,10,"torpedo preparation",Now);
        ShipTemporaryStats.Add(status,StatType.ShipSoftControlImmunity,1,5,"escape",Now);
        ShipOperations.MovementSpeed(status,Now).Should().BeApproximately(.8,1e-9);
        ShipOperations.MovementSpeed(status,Now.AddSeconds(6)).Should().BeApproximately(.65,1e-9);
    }
    [Test]
    public void SelfRecoveryMode_DoesNotImproveAlliedProjectors()
    {
        var status=Status();status.FittingBonuses[StatType.ShipSelfRecoveryOutput]=.1;
        var self=ShipOperations.Resolve(status,new(){Design="hull_repair"});
        var allied=ShipOperations.Resolve(status,new(){Design="repair_projector"});
        self.Output.Should().BeApproximately(self.Profile.Output*1.1,1e-9);
        allied.Output.Should().Be(allied.Profile.Output);
    }
    [Test]
    public void TargetInformation_OnlyBenefitsEligibleAlliesOfTheDeclaringSource()
    {
        var target=Status();
        ShipTemporaryStats.Add(target,StatType.ShipIncomingAccuracy,.05,10,"analysis",Now,benefactorId:"caster");
        ShipTemporaryStats.Current(target,Now).GetValueOrDefault(StatType.ShipIncomingAccuracy).Should().Be(0);
        ShipTemporaryStats.Current(target,Now,eligibleBenefactor:id=>id=="caster").GetValueOrDefault(StatType.ShipIncomingAccuracy).Should().Be(.05);
        ShipTemporaryStats.Current(target,Now,eligibleBenefactor:_=>false).GetValueOrDefault(StatType.ShipIncomingAccuracy).Should().Be(0);
    }
}
