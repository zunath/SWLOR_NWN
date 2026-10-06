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
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class SpaceRecastEngineTests
    {
        /// <summary>Exercises paid module activation, independent timing, reconnect progress, legacy records, and expiry.</summary>
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

        /// <summary>Checks the native mining cycle and its shared paid capacitor cost and recast deadline.</summary>
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

        /// <summary>Checks source-only rendering, refresh retention, expiry, and explicit artwork preference in the native scheduler.</summary>
        [EngineTest("Source-only cooldown textures survive refresh and prefer explicit artwork", Category = "SpaceRecast", TimeoutSeconds = 15f)]
        public static async Task SourceTextureCooldown(EngineTestContext ctx)
        {
            using var actor = await PlayerAbilityFixture.CreateAsync(ctx);
            var startedAt = DateTime.UtcNow;
            var endsAt = startedAt.AddSeconds(6);
            try
            {
                Recast.ApplyRecastDelay(actor.Creature, RecastGroup.ShipModule1, startedAt, endsAt, "iit_ess_020");
                var visual = Visual(actor.Id, RecastGroup.ShipModule1);
                ctx.Assert(visual != null, "a source texture alone creates a native cooldown visual");
                ctx.AssertEqual("iit_ess_020", Value<IReadOnlyList<string>>(visual, "SourceTextures").Single(), "the supplied source remains the override anchor");
                ctx.AssertEqual<string>(null, Value<string>(visual, "IconTexture"), "source-only rendering derives frames from the anchor");
                await ctx.DelaySecondsAsync(1.1f);
                Recast.ReduceRecastDelay(actor.Creature, RecastGroup.ShipModule1, 2);
                visual = Visual(actor.Id, RecastGroup.ShipModule1);
                ctx.Assert(visual != null, "refresh retains a source-only visual for an uncached module group");
                ctx.AssertEqual(startedAt, Value<DateTime>(visual, "StartedAt"), "refresh retains the original start");
                ctx.AssertEqual(endsAt.AddSeconds(-2), Value<DateTime>(visual, "EndsAt"), "refresh uses the reduced deadline");
                ctx.AssertEqual("iit_ess_020", Value<IReadOnlyList<string>>(visual, "SourceTextures").Single(), "refresh retains the custom anchor");
                ctx.Assert(Value<int>(visual, "Stage") > 0, "refresh resumes elapsed progress");
                await ctx.WaitUntilAsync(() => Visual(actor.Id, RecastGroup.ShipModule1) == null, 5, "source-only recharge expires through the regular scheduler");

                startedAt = DateTime.UtcNow;
                Recast.ApplyRecastDelay(actor.Creature, RecastGroup.ShipModule1, startedAt, startedAt.AddSeconds(10), "ife_sm1", "iit_ess_084");
                visual = Visual(actor.Id, RecastGroup.ShipModule1);
                ctx.Assert(visual != null, "separate fitted artwork still creates a visual");
                ctx.AssertEqual("iit_ess_084", Value<string>(visual, "IconTexture"), "explicit artwork takes precedence over the slot anchor");
                ctx.AssertEqual("ife_sm1", Value<IReadOnlyList<string>>(visual, "SourceTextures").Single(), "explicit artwork preserves its distinct anchor");
            }
            finally { AbilityCooldownVisual.ClearAllRecastDelays(actor.Creature); }
        }

        /// <summary>Runs ship death cleanup using a native pilot fixture and preserves a regular ability deadline.</summary>
        [EngineTest("Ship destruction clears module recasts and preserves regular ability cooldowns", Category = "SpaceRecast", TimeoutSeconds = 15f)]
        public static async Task ShipDestructionRecasts(EngineTestContext ctx)
        {
            using var actor = await PlayerAbilityFixture.CreateAsync(ctx);
            var shipId = "engine-destroyed-recast/" + actor.Id;
            var propertyId = "engine-destroyed-property/" + actor.Id;
            actor.Update(p =>
            {
                p.ActiveShipId = shipId;
                p.OriginalAppearanceType = GetAppearanceType(actor.Creature);
                p.SerializedHotBar = CreaturePlugin.SerializeQuickbar(actor.Creature);
            });
            DB.Set(new WorldProperty { Id = propertyId, OwnerPlayerId = actor.Id });
            DB.Set(new PlayerShip { Id = shipId, OwnerPlayerId = actor.Id, PropertyId = propertyId, Status = new ShipStatus() });
            var startedAt = DateTime.UtcNow;
            var endsAt = startedAt.AddMinutes(1);
            try
            {
                Recast.ApplyRecastDelay(actor.Creature, RecastGroup.ShipModule1, startedAt, endsAt, "ife_sm1", "iit_ess_020");
                Recast.ApplyRecastDelay(actor.Creature, RecastGroup.ShipModule11, startedAt, endsAt, "ife_sm11", "iit_ess_084");
                Recast.ApplyRecastDelay(actor.Creature, RecastGroup.Flash, startedAt, endsAt);
                ctx.Assert(Visual(actor.Id, RecastGroup.ShipModule1) != null && Visual(actor.Id, RecastGroup.ShipModule11) != null, "both high and low slot visuals exist before destruction");
                ctx.Assert(Visual(actor.Id, RecastGroup.Flash) != null, "a regular ability visual exists before destruction");
                // Headless fixtures do not dispatch connected-client death events; call their shared cleanup path.
                await ctx.ExecuteInCreatureContextAsync(actor.Creature, () => Space.ApplyDeath(actor.Creature));
                await ctx.WaitUntilAsync(() => DB.Get<Player>(actor.Id).ActiveShipId == Guid.Empty.ToString(), 5, "ship death cleanup exits space mode");
                var record = DB.Get<Player>(actor.Id);
                ctx.Assert(!record.RecastTimes.ContainsKey(RecastGroup.ShipModule1) && !record.RecastTimes.ContainsKey(RecastGroup.ShipModule11), "destruction removes persisted ship-slot timers");
                ctx.Assert(Visual(actor.Id, RecastGroup.ShipModule1) == null && Visual(actor.Id, RecastGroup.ShipModule11) == null, "destruction removes both ship-slot visuals");
                ctx.AssertEqual(endsAt, record.RecastTimes[RecastGroup.Flash], "destruction preserves the regular ability deadline");
                ctx.Assert(Visual(actor.Id, RecastGroup.Flash) != null, "destruction preserves the regular ability visual");
            }
            finally
            {
                AbilityCooldownVisual.ClearAllRecastDelays(actor.Creature);
                DB.Delete<PlayerShip>(shipId);
                DB.Delete<WorldProperty>(propertyId);
            }
        }

        /// <summary>Reads a live renderer entry to assert native scheduler behavior without requiring a connected client.</summary>
        private static object Visual(string playerId,RecastGroup group)
        {
            var all=(IDictionary)typeof(AbilityCooldownVisual).GetField("_activeVisuals",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);
            return all.Contains(playerId)&&((IDictionary)all[playerId]).Contains(group)?((IDictionary)all[playerId])[group]:null;
        }
        /// <summary>Reads a renderer state property for native lifecycle assertions.</summary>
        private static T Value<T>(object state,string property)=>(T)state.GetType().GetProperty(property).GetValue(state);
    }
}
