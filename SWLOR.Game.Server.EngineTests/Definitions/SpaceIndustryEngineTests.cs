using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class SpaceIndustryEngineTests
    {
        [EngineTest("Finite paid extraction settles once and releases reserves after an area change", Category="SpacePersistence", TimeoutSeconds=40f)]
        public static async Task ExtractionSettlementAndCancellation(EngineTestContext ctx)
        {
            using var actor=await PlayerAbilityFixture.CreateAsync(ctx);
            var shipId="engine-industry/"+actor.Id;var siteId="engine-deposit/"+actor.Id;
            var status=new ShipStatus{ItemTag=ShipFittingCatalog.Default.Hulls.Values.OrderByDescending(x=>x.Power).First().Id,FittingVersion=ShipFittingConversion.CurrentVersion,FlightId=Guid.NewGuid().ToString()};
            var tool=new ShipStatus.ShipStatusModule{Design="precision_cutter",Condition=100,ItemInstanceId=Guid.NewGuid().ToString()};status.HighPowerModules[1]=tool;
            actor.Update(p=>{p.ActiveShipId=shipId;p.Skills[SkillType.SpaceIndustry].Rank=20;p.ShipOperations.SelectedToolId=tool.ItemInstanceId;});
            ShipFittedStats.Recompute(status,Space.GetOperatingSkills(actor.Creature),Space.GetShipStatAdjustments(actor.Creature));
            var supply=ShipModuleActivationPolicy.Supply(ShipFittingCatalog.Default.Modules[tool.Design]);if(supply!=null)ShipCargo.Add(status,supply,10,"test supply");
            var site=SpaceWorkClaims.NewDeposit(siteId,GetResRef(ctx.Arena),0,SpaceIndustryCatalog.Default.Deposits[0],DateTime.UtcNow);site.SurveyedBy.Add(actor.Id);
            var obj=CreateObject(ObjectType.Placeable,site.Blueprint,ctx.GetArenaLocation(2f));ctx.Track(obj);ctx.Assert(GetIsObjectValid(obj),"finite deposit has a native target");SetLocalString(obj,"SPACE_SITE_ID",siteId);SetLocalObject(actor.Creature,"SPACE_TARGET",obj);
            var pilots=(HashSet<uint>)typeof(Space).GetField("_playersInSpace",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);
            var pools=(Dictionary<uint,List<string>>)typeof(Space).GetField("_sitePools",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);
            var objects=(Dictionary<string,uint>)typeof(Space).GetField("_siteObjects",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);
            var previous=pools.GetValueOrDefault(ctx.Arena);pools[ctx.Arena]=new(){siteId};objects[siteId]=obj;pilots.Add(actor.Creature);
            try
            {
                DB.Set(new PlayerShip{Id=shipId,OwnerPlayerId=actor.Id,Status=status});DB.Set(site);
                var accepted=false;await ctx.ExecuteInCreatureContextAsync(actor.Creature,()=>accepted=Space.ActivateFittedModule(actor.Creature,tool.ItemInstanceId));ctx.Assert(accepted,"native cutter pays and reserves a finite channel");
                ctx.AssertEqual(1,DB.Get<SpaceSite>(siteId).Claims.Count,"one paid claim");
                await ctx.WaitUntilAsync(()=>DB.Get<SpaceSite>(siteId).Claims.Count==0,16,"native extraction completes and acknowledges cargo and XP");
                double Ore()=>DB.Get<PlayerShip>(shipId).Status.Cargo.Values.Where(x=>x.Resref.StartsWith("ore_",StringComparison.Ordinal)).Sum(x=>x.Quantity);
                ctx.Assert(Math.Abs(Ore()-4.92)<1e-6,"six reserve units at82% recovery produce4.92 cargo units");
                var reserves=DB.Get<SpaceSite>(siteId).Reserves.Values.Sum();ctx.Assert(Math.Abs(reserves-(site.InitialReserves.Values.Sum()-6))<1e-6,"only six reserve units consumed");
                var process=typeof(Space).GetMethod("ProcessIndustrySites",BindingFlags.NonPublic|BindingFlags.Static);process.Invoke(null,null);process.Invoke(null,null);
                ctx.Assert(Math.Abs(Ore()-4.92)<1e-6,"settlement replay creates no duplicate cargo");
                ctx.AssertEqual(0,DB.Get<PlayerShip>(shipId).Status.PaidWorkClaims.Count,"acknowledged claim releases its ownership guard");
                await ctx.DelaySecondsAsync(.3f);await ctx.ExecuteInCreatureContextAsync(actor.Creature,()=>accepted=Space.ActivateFittedModule(actor.Creature,tool.ItemInstanceId));ctx.Assert(accepted,"next paid cycle can begin after the real hardware cooldown");
                var otherArea=ctx.CreateInstancedArea(GetResRef(ctx.Arena));await ctx.ExecuteInCreatureContextAsync(actor.Creature,()=>actor.MoveToArea(otherArea,GetPosition(actor.Creature)));
                await ctx.WaitUntilAsync(()=>GetArea(actor.Creature)==otherArea,3,"operator leaves the deposit area");
                await ctx.WaitUntilAsync(()=>DB.Get<SpaceSite>(siteId).Claims.Count==0,3,"invalidated native claim is cancelled");
                ctx.Assert(Math.Abs(DB.Get<SpaceSite>(siteId).Reserves.Values.Sum()-reserves)<1e-6,"cancelled work returns its exact reserved materials");ctx.Assert(Math.Abs(Ore()-4.92)<1e-6,"cancelled work grants no cargo");ctx.AssertEqual(0,DB.Get<PlayerShip>(shipId).Status.PaidWorkClaims.Count,"cancelled claim releases ownership guard");
            }
            finally
            {
                pilots.Remove(actor.Creature);objects.Remove(siteId);if(previous==null)pools.Remove(ctx.Arena);else pools[ctx.Arena]=previous;
                DB.Delete<PlayerShip>(shipId);DB.Delete<SpaceSite>(siteId);
            }
        }
    }
}
