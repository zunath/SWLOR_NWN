using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class ShipTechniqueEngineTests
    {
        public static bool Supports(FeatType feat)=>ShipTechniqueCatalog.Default.Profiles.Any(x=>x.Feat==feat&&x.Kind!=ShipPerkKind.Trait);
        public static async Task RunCaseAsync(EngineTestContext ctx,FeatType feat)
        {
            var profile=ShipTechniqueCatalog.Default.Get(feat);
            using var actor=await PlayerAbilityFixture.CreateAsync(ctx);
            using var ally=await PlayerAbilityFixture.CreateAsync(ctx,2f);
            var shipId="engine-ship/"+actor.Id;var allyId="engine-ship/"+ally.Id;string siteId=null;
            var hull=ShipFittingCatalog.Default.Hulls.Values.OrderByDescending(x=>x.Power).First();
            ShipStatus NewShip()=>new(){ItemTag=hull.Id,FittingVersion=ShipFittingConversion.CurrentVersion,FlightId=Guid.NewGuid().ToString()};
            var status=NewShip();var other=NewShip();
            var modules=ShipFittingCatalog.Default.Modules.Values.Where(x=>x.Action!=ShipModuleAction.Passive&&profile.Allows(x)).ToArray();
            if(profile.Actions.Count>0)ctx.Assert(modules.Length>0,"technique has real compatible hardware");
            var module=modules.FirstOrDefault();
            if(module!=null){var fitted=new ShipStatus.ShipStatusModule{Design=module.Id,Calibration="Standard",Condition=100,ItemInstanceId=Guid.NewGuid().ToString()};status.HighPowerModules[1]=fitted;status.BankModules[1]=new(){fitted.ItemInstanceId};}
            actor.Update(p=>{foreach(var skill in new[]{SkillType.Piloting,SkillType.Gunnery,SkillType.ShipSystems,SkillType.Astrometrics,SkillType.SpaceIndustry})p.Skills[skill].Rank=50;p.Perks[profile.Perk]=profile.Rank;p.ActiveShipId=shipId;p.ShipOperations.Prepared.Add(profile.Key);p.ShipOperations.SelectedToolId=status.HighPowerModules.GetValueOrDefault(1)?.ItemInstanceId;});
            ally.Update(p=>p.ActiveShipId=allyId);
            ShipFittedStats.Recompute(status,Space.GetOperatingSkills(actor.Creature),Space.GetShipStatAdjustments(actor.Creature));ShipFittedStats.Recompute(other);
            status.CommittedLegId="paid-test-leg";other.Signature=Math.Max(other.Signature,profile.MinimumSignature);
            var target=ally.Creature;
            SpaceSite site=null;
            try
            {
                if(profile.Target is ShipTechniqueTarget.Site or ShipTechniqueTarget.SurveyedSite or ShipTechniqueTarget.Anomaly)
                {
                    siteId="engine-site/"+actor.Id;
                    if(profile.DifficultComponent||module?.Action is ShipModuleAction.IntactSalvage or ShipModuleAction.BulkSalvage)
                        site=SpaceWorkClaims.NewWreck(siteId,GetResRef(ctx.Arena),SpaceEncounterCatalog.Default.Profiles["fleet_objective"],new[]{actor.Id},DateTime.UtcNow);
                    else
                    {
                        var deposit=SpaceIndustryCatalog.Default.Deposits.First(x=>profile.Target==ShipTechniqueTarget.Anomaly?x.Hardness>=100:x.Hardness<90);
                        site=SpaceWorkClaims.NewDeposit(siteId,GetResRef(ctx.Arena),0,deposit,DateTime.UtcNow);
                    }
                    site.SurveyedBy.Add(actor.Id);site.DifficultComponent=true;DB.Set(site);
                    target=CreateObject(ObjectType.Placeable,"space_wreck",ctx.GetArenaLocation(2f));ctx.Track(target);SetLocalString(target,"SPACE_SITE_ID",siteId);
                    var selected=site.Reserves.Keys.First();actor.Update(p=>p.ShipOperations.SelectedConstituent=selected);
                }
                if(profile.Target is ShipTechniqueTarget.Hostile or ShipTechniqueTarget.ExposedHostile or ShipTechniqueTarget.Pursuit)
                {SetIsTemporaryEnemy(actor.Creature,ally.Creature);SetIsTemporaryEnemy(ally.Creature,actor.Creature);}
                if(profile.Target==ShipTechniqueTarget.ExposedHostile)ShipTemporaryStats.Add(other,StatType.ShipExposedSystems,1,60,"test exposed",DateTime.UtcNow);
                DB.Set(new PlayerShip{Id=shipId,OwnerPlayerId=actor.Id,Status=status});DB.Set(new PlayerShip{Id=allyId,OwnerPlayerId=ally.Id,Status=other});
                SetLocalObject(actor.Creature,"SPACE_TARGET",target);
                var supply=module==null?null:ShipModuleActivationPolicy.Supply(module);
                if(supply!=null)ShipCargo.Add(status,supply,10,"test supply");
                DB.Set(new PlayerShip{Id=shipId,OwnerPlayerId=actor.Id,Status=status});
                var cost=ShipTechniquePolicy.Cost(status,profile,DateTime.UtcNow);var before=ShipResources.Available(status,ShipResource.Capacitor);
                var accepted=false;await ctx.ExecuteInCreatureContextAsync(actor.Creature,()=>accepted=UsePerkFeat.TryUseAbility(actor.Creature,target,feat,GetLocation(target)));
                ctx.Assert(accepted,$"{profile.Name}: native feat activation accepted");
                var saved=DB.Get<Player>(actor.Id);ctx.Assert(saved.ShipOperations.Cooldowns.GetValueOrDefault(profile.Recast.ToString())>DateTime.UtcNow,"personal recast committed");
                ctx.Assert(ShipResources.Available(DB.Get<PlayerShip>(shipId).Status,ShipResource.Capacitor)<=before-cost+1e-6,"technique capacitor paid before effects");
                ctx.Assert(!UsePerkFeat.TryUseAbility(actor.Creature,target,feat,GetLocation(target)),"immediate duplicate activation rejected");
                if(profile.Preparation>0)await ctx.DelaySecondsAsync((float)profile.Preparation+.2f);else await ctx.WaitFrameAsync();
                var live=DB.Get<PlayerShip>(shipId).Status;var state=DB.Get<Player>(actor.Id).ShipOperations;
                if(profile.Kind==ShipPerkKind.Mode)ctx.AssertEqual(profile.Key,state.SelectedMode,"selected mode persisted");
                else
                {
                    foreach(var effect in profile.Effects.Where(x=>profile.Channel<=0||!x.Once))
                    {var recipient=effect.Scope==ShipEffectScope.Target?DB.Get<PlayerShip>(allyId).Status:live;ctx.Assert(recipient.TemporaryAdjustments.Any(x=>x.Family==profile.Key&&x.Stat==effect.Stat),"declared scoped effect committed: "+effect.Stat);}
                    if(profile.Channel>0)ctx.Assert(DB.Get<SpaceSite>(siteId).Claims.Values.Any(x=>x.PlayerId==actor.Id&&x.ModuleId==state.SelectedToolId),"paid channel reserved exactly one finite claim");
                }
            }
            finally
            {DB.Delete<PlayerShip>(shipId);DB.Delete<PlayerShip>(allyId);if(siteId!=null)DB.Delete<SpaceSite>(siteId);}
        }
    }
}
