using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Service.SpaceService;
namespace SWLOR.Game.Server.Tests.Service;
public class SpaceEconomyTests
{
    [Test] public void VendorStocks_RespectStandardAccessAndAmmunitionSavings()
    {
        var data=SpaceEconomyCatalog.Default;
        data.Items.Values.Where(x=>x.Vendor&&!x.Ammunition).Should().OnlyContain(x=>x.Engineering<=20 && !x.Name.Contains(" ("));
        var missile=data.Items["light_missile"];
        SpaceEconomyCatalog.SupplyPrice(missile,10,.09).Should().Be(37);
        var module=data.Items.Values.First(x=>x.Vendor&&!x.Ammunition);
        SpaceEconomyCatalog.SupplyPrice(module,1,.25).Should().Be(module.Reference);
        data.Items.Values.Should().OnlyContain(x=>x.Resale<=Math.Floor(x.Reference*.25));
    }
    [Test] public void OreQuota_IsSharedAcrossTypesAndResetsByUtcDay()
    {
        var ledger=new SpaceEconomyLedger();var now=new DateTime(2026,10,6,23,59,0,DateTimeKind.Utc);
        ledger.Day(now).Ore=900;ledger.OreAvailable(now).Should().Be(100);
        ledger.Day(now).Ore+=100;ledger.OreAvailable(now).Should().Be(0);
        ledger.OreAvailable(now.AddMinutes(2)).Should().Be(1000);
    }
    [Test] public void Reputation_UsesCompletionReceiptsAndOneDailyCapAfterReload()
    {
        var ledger=new SpaceEconomyLedger();var now=DateTime.UtcNow;
        for(var i=0;i<7;i++)ledger.AwardReputation("fleet/"+i,20,now);
        ledger.Reputation.Should().Be(100);
        ledger=JsonConvert.DeserializeObject<SpaceEconomyLedger>(JsonConvert.SerializeObject(ledger))!;
        ledger.AwardReputation("fleet/0",20,now.AddDays(1)).Should().Be(0);
        ledger.AwardReputation("new",15,now.AddDays(1)).Should().Be(15);
    }
    [Test] public void AuthoredRewardsAndSupplyOutputs_HaveNativeBlueprints()
    {
        var outputs=SpaceEconomyCatalog.Default.Items.Keys.ToHashSet();
        var now=DateTime.UtcNow;
        foreach(var profile in SpaceIndustryCatalog.Default.Deposits)
        {var site=SpaceWorkClaims.NewDeposit("site","area",0,profile,now);if(site.HiddenResearchComponent!=null)outputs.Add(site.HiddenResearchComponent);}
        foreach(var profile in SpaceEncounterCatalog.Default.Profiles.Values)
        {var site=SpaceWorkClaims.NewWreck("wreck","area",profile,new[]{"player"},now);if(site.HiddenResearchComponent!=null)outputs.Add(site.HiddenResearchComponent);}
        foreach(var resref in outputs)File.Exists(Path.Combine(ShipFittingTests.Root(),"Module","uti",resref+".uti.json")).Should().BeTrue(resref+" must be deliverable");
    }
    [Test] public void SaleSourceRecovery_DistinguishesDeletionFromAnUnconsumedSavedItem()
    {
        var trade=new SpaceTradeTransaction {Quantity=10,InitialQuantity=10};
        ShipTradePolicy.Source(trade,10).Should().Be(ShipSaleSourceState.Untouched);
        ShipTradePolicy.Source(trade,0).Should().Be(ShipSaleSourceState.Consumed);
        ShipTradePolicy.Source(trade,5).Should().Be(ShipSaleSourceState.Invalid);
        trade.InitialQuantity=30;
        ShipTradePolicy.Source(trade,20).Should().Be(ShipSaleSourceState.Consumed);
        ShipTradePolicy.Source(trade,0).Should().Be(ShipSaleSourceState.Invalid);
    }
}
