using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class SpacePersistenceEngineTests
    {
        [EngineTest("Ship equipment native conversion and withdrawal preserve identity and quality",Category="SpacePersistence",TimeoutSeconds=30f)]
        public static async Task EquipmentRoundTrip(EngineTestContext ctx)
        {
            using var player=await PlayerAbilityFixture.CreateAsync(ctx);var container=ctx.SpawnCreature("civilian",2);
            var legacy=ShipFittingCatalog.Default.LegacyModules.Values.First(x=>ShipFittingCatalog.Default.Modules.TryGetValue(x.Target,out var module)&&module.QualityDimensions.HasFlag(ShipQualityDimension.Output));
            var item=CreateItemOnObject(legacy.Resref,container);ctx.Track(item);ctx.Assert(GetIsObjectValid(item),"legacy blueprint exists in packed module");
            var id=GetObjectUUID(item);AddItemProperty(DurationType.Permanent,ItemPropertyCustom(ItemPropertyType.ModuleBonus,0,80),item);
            var migration=typeof(Space).Assembly.GetType("SWLOR.Game.Server.Feature.MigrationDefinition.ShipEquipmentMigration").GetMethod("MigrateObject",BindingFlags.Public|BindingFlags.Static);
            ctx.Assert((bool)migration.Invoke(null,new object[]{item}),"legacy equipment converted in place");
            ctx.AssertEqual(id,GetObjectUUID(item),"native item identity survives conversion");
            var audit=DB.Get<ShipItemAudit>("ship-item/"+id);ctx.Assert(audit?.Converted==true&&!string.IsNullOrEmpty(audit.OriginalItem),"original serialized source retained");
            var fitted=ShipEquipment.Read(item);ctx.AssertEqual(100,fitted.Quality,"legacy enhancement caps at100 quality");ctx.AssertEqual(legacy.Target,fitted.Design,"horizontal replacement design");
            fitted.RecastTime=DateTime.UtcNow.AddSeconds(40);fitted.Condition=37;fitted.BoundPlayerId=player.Id;
            DestroyObject(item);await ctx.WaitFrameAsync();uint withdrawn=OBJECT_INVALID;
            await ctx.ExecuteInCreatureContextAsync(player.Creature,()=>withdrawn=ShipEquipment.CreateForWithdrawal(fitted,player.Creature));ctx.Track(withdrawn);
            var restored=ShipEquipment.Read(withdrawn);ctx.AssertEqual(id,restored.ItemInstanceId,"withdrawal retains exact inventory identity");ctx.AssertEqual(100,restored.Quality,"withdrawal retains one refinement");ctx.AssertEqual(37,restored.Condition,"condition retained");ctx.AssertEqual(fitted.RecastTime,restored.RecastTime,"hardware cooldown retained");ctx.AssertEqual(player.Id,restored.BoundPlayerId,"starter ownership retained");
            ctx.Assert(!(bool)migration.Invoke(null,new object[]{withdrawn}),"converted equipment does not remigrate");DB.Delete<ShipItemAudit>(audit.Id);
        }
        [EngineTest("Space starter grant resumes per-output acknowledgements without duplicate voucher",Category="SpacePersistence",TimeoutSeconds=20f)]
        public static async Task StarterReplay(EngineTestContext ctx)
        {
            using var actor=await PlayerAbilityFixture.CreateAsync(ctx);actor.Update(p=>p.SpaceEconomy.StarterChoice="industry");
            var recover=typeof(ShipSupply).GetMethod("RecoverStarter",BindingFlags.NonPublic|BindingFlags.Static);
            await ctx.ExecuteInCreatureContextAsync(actor.Creature,()=>recover.Invoke(null,new object[]{actor.Creature}));
            var saved=DB.Get<Player>(actor.Id);ctx.AssertEqual(4,saved.SpaceEconomy.StarterOutputs.Count,"four acknowledged starter outputs");ctx.AssertEqual(100,saved.SpaceEconomy.ServiceVoucher,"single100-credit voucher");
            var count=0;for(var item=GetFirstItemInInventory(actor.Creature);GetIsObjectValid(item);item=GetNextItemInInventory(actor.Creature))if(GetLocalString(item,ShipSupply.BoundOwner)==actor.Id){count++;ctx.Assert(GetPlotFlag(item),"starter item cannot be sold");}
            ctx.AssertEqual(4,count,"four bound native outputs");
            await ctx.ExecuteInCreatureContextAsync(actor.Creature,()=>recover.Invoke(null,new object[]{actor.Creature}));ctx.AssertEqual(100,DB.Get<Player>(actor.Id).SpaceEconomy.ServiceVoucher,"recovery does not repeat voucher");
        }
        [EngineTest("Ship research delivers a prepared output once after interrupted acknowledgement",Category="SpacePersistence",TimeoutSeconds=20f)]
        public static async Task ResearchReplay(EngineTestContext ctx)
        {
            using var actor=await PlayerAbilityFixture.CreateAsync(ctx);var item=CreateItemOnObject("blueprint",actor.Creature);ctx.Track(item);
            var recipe=Craft.GetAllRecipes().First(x=>x.Value.IsShipEquipment).Key;
            var details=Craft.GetBlueprintDetails(item);details.Recipe=recipe;details.Level=4;details.LicensedRuns=7;Craft.SetBlueprintDetails(item,details);
            var job=new ResearchJob{Id="engine-research/"+actor.Id,PlayerId=actor.Id,Recipe=recipe,IsShipResearch=true,InputSettled=true,Success=false,OutputPrepared=true,DateCompleted=DateTime.UtcNow.AddMinutes(-1)};
            SetLocalString(item,"SHIP_RESEARCH_OUTPUT",job.Id);job.SerializedOutput=ObjectPlugin.Serialize(item);DB.Set(job);
            await ctx.ExecuteInCreatureContextAsync(actor.Creature,()=>ShipResearch.Deliver(actor.Creature,job));
            var count=0;for(var candidate=GetFirstItemInInventory(actor.Creature);GetIsObjectValid(candidate);candidate=GetNextItemInInventory(actor.Creature))if(GetLocalString(candidate,"SHIP_RESEARCH_OUTPUT")==job.Id)count++;
            ctx.AssertEqual(1,count,"durably delivered blueprint reused instead of duplicated");ctx.Assert(DB.Get<ResearchJob>(job.Id)==null,"settled research entitlement removed");ctx.AssertEqual(4,Craft.GetBlueprintDetails(item).Level,"failed upgrade retains previous level");ctx.AssertEqual(7,Craft.GetBlueprintDetails(item).LicensedRuns,"failed upgrade retains runs");
        }
    }
}
