using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class SpaceRecastEngineTests
    {
        [EngineTest("Existing ship modules use independent regular recasts and resume elapsed recharge",Category="SpaceRecast",TimeoutSeconds=30f)]
        public static async Task ModuleRecastLifecycle(EngineTestContext ctx)
        {
            using var actor=await PlayerAbilityFixture.CreateAsync(ctx);
            var shipId="engine-recast/"+actor.Id;
            var status=new ShipStatus { Hull=80,MaxHull=100,Capacitor=0,MaxCapacitor=100 };
            status.HighPowerModules[1]=new ShipStatus.ShipStatusModule { ItemTag="hull_rep_b" };
            status.HighPowerModules[2]=new ShipStatus.ShipStatusModule { ItemTag="hull_rep_b" };
            status.LowPowerModules[1]=new ShipStatus.ShipStatusModule { ItemTag="shld_boost_b",RecastTime=DateTime.UtcNow.AddMinutes(1) };
            actor.Update(p=>p.ActiveShipId=shipId);
            DB.Set(new PlayerShip { Id=shipId,OwnerPlayerId=actor.Id,Status=status });
            SetLocalObject(actor.Creature,"SPACE_TARGET",actor.Creature);
            try
            {
                await ctx.ExecuteInCreatureContextAsync(actor.Creature,()=>Space.ActivateShipModule(actor.Creature,FeatType.ShipModule1));
                ctx.Assert(!DB.Get<Player>(actor.Id).RecastTimes.ContainsKey(RecastGroup.ShipModule1),"insufficient capacitor starts no UI recast");
                status.Capacitor=100;DB.Set(new PlayerShip { Id=shipId,OwnerPlayerId=actor.Id,Status=status });
                await ctx.ExecuteInCreatureContextAsync(actor.Creature,()=>Space.ActivateShipModule(actor.Creature,FeatType.ShipModule1));
                var paid=DB.Get<PlayerShip>(shipId).Status.HighPowerModules[1];
                var deadline=paid.RecastTime;var start=paid.RecastStartedAt;
                ctx.Assert(deadline>DateTime.UtcNow,"existing hull repair activates");
                ctx.AssertEqual(deadline,DB.Get<Player>(actor.Id).RecastTimes[RecastGroup.ShipModule1],"regular recast stores exact hardware deadline");
                ctx.Assert(!DB.Get<Player>(actor.Id).RecastTimes.ContainsKey(RecastGroup.ShipModule2),"identical idle module has no timer");
                var visual=Visual(actor.Id,RecastGroup.ShipModule1);
                ctx.AssertEqual("iit_ess_020",Value<string>(visual,"IconTexture"),"regular renderer preserves fitted repair artwork");
                ctx.AssertEqual("ife_sm1",Value<IReadOnlyList<string>>(visual,"SourceTextures").Single(),"recharge overrides the first slot anchor");
                ctx.AssertEqual(0,Value<int>(visual,"Stage"),"recharge begins empty");
                var capacitor=DB.Get<PlayerShip>(shipId).Status.Capacitor;
                await ctx.ExecuteInCreatureContextAsync(actor.Creature,()=>Space.ActivateShipModule(actor.Creature,FeatType.ShipModule1));
                ctx.AssertEqual(capacitor,DB.Get<PlayerShip>(shipId).Status.Capacitor,"denied reuse spends nothing");
                ctx.AssertEqual(deadline,DB.Get<Player>(actor.Id).RecastTimes[RecastGroup.ShipModule1],"denied reuse cannot extend cooldown");
                await ctx.DelaySecondsAsync(2.1f);
                await ctx.ExecuteInCreatureContextAsync(actor.Creature,()=>Space.ActivateShipModule(actor.Creature,FeatType.ShipModule2));
                ctx.Assert(DB.Get<Player>(actor.Id).RecastTimes[RecastGroup.ShipModule2]>deadline,"identical second slot has independent timing");
                AbilityCooldownVisual.ClearAllRecastDelays(actor.Creature);
                Space.RestoreShipModuleRecasts(actor.Creature);
                visual=Visual(actor.Id,RecastGroup.ShipModule1);
                ctx.AssertEqual(start,Value<DateTime>(visual,"StartedAt"),"restore retains the original start");
                ctx.AssertEqual(deadline,Value<DateTime>(visual,"EndsAt"),"restore never restarts hardware");
                ctx.Assert(Value<int>(visual,"Stage")>0,"green progress resumes after reconnect");
                ctx.Assert(!DB.Get<Player>(actor.Id).RecastTimes.ContainsKey(RecastGroup.ShipModule11),"passive low-slot module has no recharge display");
                var ship=DB.Get<PlayerShip>(shipId);ship.Status.HighPowerModules[1].RecastStartedAt=default;DB.Set(ship);
                Space.RestoreShipModuleRecasts(actor.Creature);
                ctx.AssertEqual(start,Value<DateTime>(Visual(actor.Id,RecastGroup.ShipModule1),"StartedAt"),"old persisted records infer elapsed progress from their unchanged deadline");
                await ctx.WaitUntilAsync(()=>Visual(actor.Id,RecastGroup.ShipModule1)==null,14,"regular scheduler expires first module");
                await ctx.WaitUntilAsync(()=>Visual(actor.Id,RecastGroup.ShipModule2)==null,4,"regular scheduler expires second module");
                actor.Update(p=>{p.ActiveShipId=Guid.Empty.ToString();p.RecastTimes[RecastGroup.ShipModule1]=DateTime.UtcNow.AddMinutes(1);p.RecastTimes[RecastGroup.Flash]=DateTime.UtcNow.AddMinutes(1);});
                Space.RestoreShipModuleRecasts(actor.Creature);
                ctx.Assert(!DB.Get<Player>(actor.Id).RecastTimes.ContainsKey(RecastGroup.ShipModule1),"leaving a ship clears stale slot timers");
                ctx.Assert(DB.Get<Player>(actor.Id).RecastTimes.ContainsKey(RecastGroup.Flash),"regular ability cooldown survives ship changes");
            }
            finally { AbilityCooldownVisual.ClearAllRecastDelays(actor.Creature); DB.Delete<PlayerShip>(shipId); }
        }

        [EngineTest("Existing mining module pays one cooldown and displays the standard recharge",Category="SpaceRecast",TimeoutSeconds=20f)]
        public static async Task MiningRecast(EngineTestContext ctx)
        {
            using var actor=await PlayerAbilityFixture.CreateAsync(ctx);
            var shipId="engine-mining-recast/"+actor.Id;
            var status=new ShipStatus { Hull=100,MaxHull=100,Capacitor=100,MaxCapacitor=100 };
            status.HighPowerModules[1]=new ShipStatus.ShipStatusModule { ItemTag="min_laser_b" };
            actor.Update(p=>p.ActiveShipId=shipId);
            DB.Set(new PlayerShip { Id=shipId,OwnerPlayerId=actor.Id,Status=status });
            var asteroid=CreateObject(ObjectType.Placeable,"spc_asteroid_til",ctx.GetArenaLocation(2f));ctx.Track(asteroid);
            ctx.Assert(GetIsObjectValid(asteroid),"native asteroid exists");
            SetLocalInt(asteroid,"ASTEROID_REMAINING_UNITS",10);
            SetLocalObject(actor.Creature,"SPACE_TARGET",asteroid);
            try
            {
                await ctx.ExecuteInCreatureContextAsync(actor.Creature,()=>Space.ActivateShipModule(actor.Creature,FeatType.ShipModule1));
                var paid=DB.Get<PlayerShip>(shipId).Status;
                ctx.AssertEqual(95,paid.Capacitor,"existing mining capacitor cost is unchanged");
                ctx.AssertEqual(paid.HighPowerModules[1].RecastTime,DB.Get<Player>(actor.Id).RecastTimes[RecastGroup.ShipModule1],"mining and regular UI share the exact paid deadline");
                ctx.AssertEqual("iit_ess_084",Value<string>(Visual(actor.Id,RecastGroup.ShipModule1),"IconTexture"),"mining recharge uses mining artwork");
                await ctx.WaitUntilAsync(()=>GetLocalInt(asteroid,"ASTEROID_REMAINING_UNITS")<10 && Visual(actor.Id,RecastGroup.ShipModule1)==null,9,"native extraction and regular recharge complete");
                ctx.Assert(Visual(actor.Id,RecastGroup.ShipModule1)==null,"regular recharge finishes alongside the mining cycle");
            }
            finally { AbilityCooldownVisual.ClearAllRecastDelays(actor.Creature); DB.Delete<PlayerShip>(shipId); }
        }

        private static object Visual(string playerId,RecastGroup group)
        {
            var all=(IDictionary)typeof(AbilityCooldownVisual).GetField("_activeVisuals",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);
            return all.Contains(playerId)&&((IDictionary)all[playerId]).Contains(group)?((IDictionary)all[playerId])[group]:null;
        }
        private static T Value<T>(object state,string property)=>(T)state.GetType().GetProperty(property).GetValue(state);
    }
}
