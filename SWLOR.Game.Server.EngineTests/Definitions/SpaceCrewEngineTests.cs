using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class SpaceCrewEngineTests
    {
        [EngineTest("Crew interior authority replaces skills and shares capacitor and module cadence",Category="SpacePersistence",TimeoutSeconds=30f)]
        public static async Task CrewLeaseAndModuleCadence(EngineTestContext ctx)
        {
            using var pilot=await PlayerAbilityFixture.CreateAsync(ctx);using var gunner=await PlayerAbilityFixture.CreateAsync(ctx,2f);
            var shipId="engine-crew/"+pilot.Id;var propertyId="engine-property/"+pilot.Id;var permissionId="engine-permission/"+gunner.Id;
            var interior=ctx.CreateInstancedArea(GetResRef(ctx.Arena));SetLocalString(interior,"PROPERTY_ID",propertyId);
            await ctx.ExecuteInCreatureContextAsync(gunner.Creature,()=>gunner.MoveToArea(interior,GetPosition(pilot.Creature)));
            await ctx.WaitUntilAsync(()=>GetArea(gunner.Creature)==interior,3,"crew entered the separate native interior");
            var status=new ShipStatus{ItemTag=ShipFittingCatalog.Default.Hulls.Values.OrderByDescending(x=>x.Power).First().Id,FittingVersion=ShipFittingConversion.CurrentVersion,FlightId=Guid.NewGuid().ToString()};
            var weapon=new ShipStatus.ShipStatusModule{Design="tracking_laser",Condition=100,ItemInstanceId=Guid.NewGuid().ToString()};var repair=new ShipStatus.ShipStatusModule{Design="hull_repair",Condition=100,ItemInstanceId=Guid.NewGuid().ToString()};status.HighPowerModules[1]=weapon;status.HighPowerModules[2]=repair;
            pilot.Update(p=>{p.ActiveShipId=shipId;p.Skills[SkillType.Gunnery].Rank=50;p.Skills[SkillType.ShipSystems].Rank=50;});gunner.Update(p=>{p.CrewShipId=shipId;p.Skills[SkillType.Gunnery].Rank=12;});
            status.Crew[ShipCrewStation.Weapons]=gunner.Id;
            var livePilots=(HashSet<uint>)typeof(Space).GetField("_playersInSpace",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);livePilots.Add(pilot.Creature);
            try
            {
                DB.Set(new WorldProperty{Id=propertyId,OwnerPlayerId=pilot.Id,PropertyType=PropertyType.Starship});DB.Set(new WorldPropertyPermission{Id=permissionId,PropertyId=propertyId,PlayerId=gunner.Id,Permissions=new(){[PropertyPermissionType.OperateStations]=true}});
                DB.Set(new PlayerShip{Id=shipId,OwnerPlayerId=pilot.Id,PropertyId=propertyId,Status=status});ShipFittedStats.Recompute(status,Space.GetOperatingSkills(pilot.Creature),Space.GetShipStatAdjustments(pilot.Creature));DB.Set(new PlayerShip{Id=shipId,OwnerPlayerId=pilot.Id,PropertyId=propertyId,Status=status});
                var enemyStatus=new ShipStatus{ItemTag=status.ItemTag,FittingVersion=status.FittingVersion,FlightId=Guid.NewGuid().ToString()};ShipFittedStats.Recompute(enemyStatus);
                using var enemy=new ShipEnemyFixture(ctx,pilot.Creature,enemyStatus,4f);SetLocalObject(pilot.Creature,"SPACE_TARGET",enemy.Creature);
                ctx.AssertEqual(pilot.Creature,Space.GetExteriorShip(gunner.Creature),"station resolves exterior from another native area");ctx.AssertEqual(12,Space.GetOperatingSkills(pilot.Creature)[SkillType.Gunnery],"gunner replaces pilot50 instead of stacking");
                var before=ShipResources.Available(status,ShipResource.Capacitor);ctx.Assert(!Space.ActivateFittedModule(pilot.Creature,weapon.ItemInstanceId),"pilot cannot duplicate an occupied weapons station");ctx.AssertEqual(before,ShipResources.Available(DB.Get<PlayerShip>(shipId).Status,ShipResource.Capacitor),"denied station operation spends nothing");
                var accepted=false;await ctx.ExecuteInCreatureContextAsync(gunner.Creature,()=>accepted=Space.ActivateFittedModule(gunner.Creature,weapon.ItemInstanceId));ctx.Assert(accepted,"authorized interior gunner activates exterior weapon");
                var paid=DB.Get<PlayerShip>(shipId).Status;ctx.Assert(ShipResources.Available(paid,ShipResource.Capacitor)<before,"shared capacitor paid once");ctx.Assert(paid.HighPowerModules[1].RecastTime>DateTime.UtcNow,"shared hardware cooldown committed");
                ctx.Assert(!Space.ActivateFittedModule(pilot.Creature,repair.ItemInstanceId),"another station cannot bypass shared one-second cadence");
                var permission=DB.Get<WorldPropertyPermission>(permissionId);permission.Permissions.Clear();DB.Set(permission);
                ctx.AssertEqual(OBJECT_INVALID,Space.GetExteriorShip(gunner.Creature),"revoked permission ends station authority");ctx.AssertEqual(50,Space.GetOperatingSkills(pilot.Creature)[SkillType.Gunnery],"inactive station returns to pilot rank");ctx.Assert(Space.CanOperateSkill(pilot.Creature,SkillType.Gunnery),"pilot fallback resumes authority");
                ctx.Assert(DB.Get<PlayerShip>(shipId).Status.HighPowerModules[1].RecastTime>DateTime.UtcNow,"lease change does not reset hardware cooldown");
            }
            finally{livePilots.Remove(pilot.Creature);DB.Delete<PlayerShip>(shipId);DB.Delete<WorldProperty>(propertyId);DB.Delete<WorldPropertyPermission>(permissionId);}
        }
    }
}
