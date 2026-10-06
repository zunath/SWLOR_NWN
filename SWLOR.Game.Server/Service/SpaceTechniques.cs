using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    public static partial class Space
    {
        public static bool ActivateShipTechnique(uint player,ShipTechniqueProfile requested,int purchasedRank,uint nativeTarget)
        {
            if (!GetIsPC(player) || purchasedRank!=requested.Rank) { SendMessageToPC(player,"Use your highest purchased technique rank.");return false; }
            var status=GetShipStatus(player);var dbPlayer=DB.Get<Player>(GetObjectUUID(player));var state=dbPlayer.ShipOperations;var now=DateTime.UtcNow;
            if(status!=null)RefreshOperatingBuild(player,dbPlayer,status);
            var (target,targetStatus)=GetCurrentTarget(player);
            if (requested.Effects.Any(x=>x.Scope==ShipEffectScope.Target) && GetIsObjectValid(nativeTarget) && nativeTarget!=player)
            { target=nativeTarget;targetStatus=GetShipStatus(target); }
            var site=GetIsObjectValid(target)?GetIndustrySite(target):null;
            var hostile=GetIsObjectValid(target) && target!=player && targetStatus!=null && (GetIsEnemy(target,player)||GetIsEnemy(player,target));
            var hardware=status==null?Array.Empty<ShipStatus.ShipStatusModule>(): (requested.Bank?ShipBanks.Modules(status,state.SelectedBank):ShipFittedStats.Modules(status))
                .Where(x=>x.Condition>0 && requested.Allows(ShipFittingCatalog.Default.Modules[x.Design]) && GetOperatingSkills(player).GetValueOrDefault(ShipFittingCatalog.Default.Modules[x.Design].OperatorSkill)>=ShipFittingCatalog.Default.Modules[x.Design].OperatorRank).ToArray();
            if (requested.Bank && status!=null && ShipBanks.Modules(status,state.SelectedBank).Count!=hardware.Length)
            { SendMessageToPC(player,"Every module in the selected bank must support this technique.");return false; }
            var distance=GetIsObjectValid(target)?GetDistanceBetween(player,target):0;
            var context=new ShipTechniqueContext(IsPlayerInSpaceMode(player),GetIsObjectValid(target)&&GetArea(player)==GetArea(target),hostile,
                targetStatus!=null&&target!=player&&!hostile,site!=null,site?.Kind==SpaceSiteKind.Anomaly,site?.SurveyedBy.Contains(dbPlayer.Id)==true,
                targetStatus!=null&&ShipTemporaryStats.Current(targetStatus,now).GetValueOrDefault(StatType.ShipExposedSystems)>0,distance,targetStatus?.Signature??0,
                !string.IsNullOrEmpty(status?.CommittedLegId),status!=null&&HasCommittedShipWork(dbPlayer.ActiveShipId));
            var error=ShipTechniquePolicy.Validate(status,state,requested,context,now,hardware);
            if(error==null && GetOperatingSkills(player).GetValueOrDefault(requested.Skill)<requested.SkillRank) error="Restore the technique's required operating skill ranks first.";
            if (error==null && requested.Actions.Count>0 && requested.Target is ShipTechniqueTarget.Site or ShipTechniqueTarget.SurveyedSite or ShipTechniqueTarget.Anomaly or ShipTechniqueTarget.Allied)
                if (!hardware.Any(x=>distance<=ShipFittingCatalog.Default.GetVariant(x.Design,x.Calibration).Range)) error="The selected target is outside the fitted hardware's range.";
            if (error==null && requested.Selection && (site==null || !site.Reserves.ContainsKey(state.SelectedConstituent))) error="Choose a constituent present in the surveyed site.";
            if (error==null && requested.DifficultComponent && site?.DifficultComponent!=true) error="Select a wreck with an authored difficult component.";
            var committedTool=requested.Channel>0 ? hardware.FirstOrDefault(x=>x.ItemInstanceId==state.SelectedToolId)??hardware.FirstOrDefault() : null;
            if(error==null && requested.Channel>0) error=ValidateTechniqueChannel(player,dbPlayer,status,site,requested,committedTool,target,now);
            if (error!=null) { SendMessageToPC(player,error);return false; }
            ShipTechniquePolicy.Pay(status,state,requested,now);
            dbPlayer.RecastTimes[requested.Recast]=state.Cooldowns[requested.Recast.ToString()];
            AbilityCooldownVisual.ApplyRecastDelay(player,requested.Recast,now,dbPlayer.RecastTimes[requested.Recast]);
            if (requested.Kind==ShipPerkKind.Mode)
            {
                state.SelectedMode=state.SelectedMode==requested.Key?null:requested.Key;DB.Set(dbPlayer);
                ShipFittedStats.Recompute(status,GetOperatingSkills(player),GetShipStatAdjustments(player));PersistShipStatus(player,status);
                Stat.ApplyCreatureMovementRate(player);ExecuteScript("pc_target_upd",player);return true;
            }
            var receipt=Guid.NewGuid().ToString();var flight=status.FlightId;var targetId=GetIsObjectValid(target)?GetObjectUUID(target):null;
            state.PendingTechniqueId=receipt;state.CommittedUntil=now.AddSeconds(requested.Preparation);DB.Set(dbPlayer);PersistShipStatus(player,status);
            void Complete()
            {
                if (!GetIsObjectValid(player)||!IsPlayerInSpaceMode(player)) return;
                var saved=DB.Get<Player>(GetObjectUUID(player));var live=GetShipStatus(player);
                if (saved.ShipOperations.PendingTechniqueId!=receipt) return;
                saved.ShipOperations.PendingTechniqueId=null;saved.ShipOperations.CommittedUntil=default;DB.Set(saved);
                if (live?.FlightId!=flight || live.Hull<=0 || ShipTemporaryStats.Current(live,DateTime.UtcNow).GetValueOrDefault(StatType.ShipActivationLock)>0) return;
                if (requested.Target!=ShipTechniqueTarget.Self && (!GetIsObjectValid(target)||GetObjectUUID(target)!=targetId||GetArea(target)!=GetArea(player)||
                    (requested.Range>0&&GetDistanceBetween(player,target)>requested.Range))) return;
                var moduleIds=(committedTool!=null?new[]{committedTool}:hardware).Select(x=>x.ItemInstanceId).ToArray();
                if (requested.Actions.Count>0 && !moduleIds.All(id=>ShipFittedStats.Modules(live).Any(x=>x.ItemInstanceId==id&&x.Condition>0))) return;
                var at=DateTime.UtcNow;
                foreach (var effect in requested.Effects)
                {
                    var recipient=effect.Scope==ShipEffectScope.Target?GetShipStatus(target):live;
                    if (recipient==null||recipient.Hull<=0) continue;
                    var duration=effect.Seconds;
                    if (requested.HardControl)
                    {
                        const string family="activation disruption";
                        if (recipient.ControlWindows.TryGetValue(family,out var window)&&window.StartedAt.AddSeconds(20)>at) continue;
                        recipient.ControlWindows[family]=new(at,3);duration=Math.Min(3,duration);
                    }
                    ShipTemporaryStats.Add(recipient,effect.Stat,effect.Amount,duration,requested.Key,at,
                        effect.Scope==ShipEffectScope.Hardware?moduleIds:null,
                        effect.Scope==ShipEffectScope.Hardware&&requested.Target is ShipTechniqueTarget.Hostile or ShipTechniqueTarget.ExposedHostile or ShipTechniqueTarget.Allied?targetId:null,
                        effect.Once,icon:requested.Icon,label:requested.Name,selectedConstituent:requested.Selection?saved.ShipOperations.SelectedConstituent:null,benefactorId:effect.AlliesOnly?GetObjectUUID(player):null);
                    PersistShipStatus(effect.Scope==ShipEffectScope.Target?target:player,recipient);
                }
                if (requested.Channel>0) ShipTemporaryStats.Add(live,StatType.ShipCommittedCycleSeconds,requested.Channel,30,requested.Key,at,moduleIds,targetId,once:true,icon:requested.Icon,label:requested.Name);
                if (requested.Discovery) ShipTemporaryStats.Add(live,StatType.ShipDiscoveryChance,.05,30,requested.Key,at,moduleIds,targetId,once:true,icon:requested.Icon,label:requested.Name);
                if (requested.MovementLock) ShipTemporaryStats.Add(live,StatType.ShipCommittedMovementLock,1,30,requested.Key,at,moduleIds,targetId,once:true,icon:requested.Icon,label:requested.Name);
                PersistShipStatus(player,live);Stat.ApplyCreatureMovementRate(player);ExecuteScript("pc_target_upd",player);
                if(committedTool!=null)
                    ActivateResolvedFittedModule(player,committedTool.ItemInstanceId,sharesTechniqueCadence:true);
                else SendMessageToPC(player,requested.Name+" ready. Fitted hardware still pays its normal activation costs.");
            }
            if (requested.Preparation>0)
            {
                SendMessageToPC(player,$"Preparing {requested.Name} ({requested.Preparation:0.#}s).");
                Messaging.SendMessageNearbyToPlayers(player,observer=>$"{PlayerName.GetDisplayName(observer,player)} is preparing {requested.Name}.",60f);
                DelayCommand((float)requested.Preparation,Complete);
            }
            else Complete();
            return true;
        }
        private static string ValidateTechniqueChannel(uint pilot,Player player,ShipStatus status,SpaceSite site,ShipTechniqueProfile technique,ShipStatus.ShipStatusModule tool,uint target,DateTime now)
        {
            if(tool==null||site==null) return "Select a finite site and compatible industrial tool.";
            // Preview both payments and the finite claim on detached state. No rejected channel spends either cost.
            var preview=Newtonsoft.Json.JsonConvert.DeserializeObject<ShipStatus>(Newtonsoft.Json.JsonConvert.SerializeObject(status));
            var previewSite=Newtonsoft.Json.JsonConvert.DeserializeObject<SpaceSite>(Newtonsoft.Json.JsonConvert.SerializeObject(site));
            var fitted=ShipFittedStats.Modules(preview).Single(x=>x.ItemInstanceId==tool.ItemInstanceId);var targetId=GetObjectUUID(target);
            foreach(var effect in technique.Effects)
                ShipTemporaryStats.Add(preview,effect.Stat,effect.Amount,effect.Seconds,technique.Key,now,new[]{tool.ItemInstanceId},targetId,effect.Once);
            if(technique.Discovery) ShipTemporaryStats.Add(preview,StatType.ShipDiscoveryChance,.05,30,technique.Key,now,new[]{tool.ItemInstanceId},targetId,once:true);
            ShipResources.SpendPrecise(preview,ShipResource.Capacitor,ShipTechniquePolicy.Cost(status,technique,now));
            var temporary=ShipTemporaryStats.Current(preview,now,tool.ItemInstanceId,targetId);
            var operation=ShipOperations.Resolve(preview,fitted,GetOperatingAttribute(pilot,AbilityType.Perception),temporary,temporarySources:ShipTemporaryStats.Sources(preview,now,tool.ItemInstanceId,targetId));
            var context=new ShipActivationContext(true,GetArea(pilot)==GetArea(target),true,false,false,true,false,GetDistanceBetween(pilot,target),GetOperatingSkills(pilot).GetValueOrDefault(operation.Profile.OperatorSkill));
            var error=ShipModuleActivationPolicy.Validate(preview,fitted,operation,context,now,temporary);
            if(error!=null)return error;
            try
            {
                var bonuses=ShipFittedStats.StatUnits.Keys.ToDictionary(x=>x,x=>ShipFittedStats.Bonus(preview,x)-ShipFittedStats.Penalty(preview,x));
                SpaceWorkClaims.Reserve(previewSite,player.Id,player.ActiveShipId,status.FlightId,tool.ItemInstanceId,operation,Math.Max(0,ShipCargo.Available(status)-ReservedSiteCargo(player.ActiveShipId)),now,bonuses,temporary,technique.Channel);
            }
            catch(InvalidOperationException ex){return ex.Message;}
            return null;
        }

        [Core.NWNEventHandler(Core.ScriptName.OnModuleLoad)]
        public static void StartCockpitRefresh()
        {
            Core.Scheduler.ScheduleRepeating(()=>
            {
                foreach(var pilot in _playersInSpace.Where(GetIsObjectValid).ToArray())
                    if(Gui.IsWindowOpen(pilot,GuiService.GuiWindowType.ShipCockpit))
                        Gui.PublishRefreshEvent(pilot,new Feature.GuiDefinition.RefreshEvent.ShipCockpitRefreshEvent());
            },TimeSpan.FromMilliseconds(500));
        }

        public static bool HasCommittedShipWork(string shipId) => !string.IsNullOrEmpty(shipId) && FindSpaceSites().Any(site=>site.Claims.Values.Any(claim=>claim.ShipId==shipId&&claim.State==SpaceWorkState.Reserved));
        public static void PrepareShipOperations(uint player,string shipId,IEnumerable<string> keys,string mode)
        {
            var ship=ShipEquipmentTransfers.RequireDock(player,shipId);var record=DB.Get<Player>(GetObjectUUID(player));
            ShipTechniqueProfile Owned(string key)
            {
                var first=ShipTechniqueCatalog.Default.Profiles.FirstOrDefault(x=>x.Key==key)??throw new InvalidOperationException("Unknown operating perk.");
                return ShipTechniqueCatalog.Default.Highest(key,Perk.GetPerkLevel(player,first.Perk))??throw new InvalidOperationException("Purchase the perk first.");
            }
            ShipTechniquePolicy.Prepare(record.ShipOperations,keys.Select(Owned),string.IsNullOrEmpty(mode)?null:Owned(mode),DateTime.UtcNow);
            DB.Set(record);ship.Status.RefitReadyAt=record.ShipOperations.ReadyAt;
            ShipFittedStats.Recompute(ship.Status,GetOperatingSkills(player),GetShipStatAdjustments(player));DB.Set(ship);
        }
        public static bool ActivateShipBank(uint player,int bank)
        {
            if (!IsPlayerInSpaceMode(player)) return false;
            var status=GetShipStatus(player);RefreshOperatingBuild(player,DB.Get<Player>(GetObjectUUID(player)),status);var now=DateTime.UtcNow;var (target,targetStatus)=GetCurrentTarget(player);
            var targetId=GetIsObjectValid(target)?GetObjectUUID(target):null;
            var modules=ShipBanks.Modules(status,bank);var volley=new List<(ShipStatus.ShipStatusModule,ShipModuleOperation)>();
            foreach (var fitted in modules)
            {
                var temporary=ShipTemporaryStats.Current(status,now,fitted.ItemInstanceId,targetId);
                var operation=ShipOperations.Resolve(status,fitted,GetOperatingAttribute(player,AbilityType.Perception),temporary,temporarySources:ShipTemporaryStats.Sources(status,now,fitted.ItemInstanceId,targetId));
                var hostile=targetStatus!=null&&target!=player&&(GetIsEnemy(target,player)||GetIsEnemy(player,target));
                var self=operation.Profile.Action is ShipModuleAction.SelfHullRepair or ShipModuleAction.SelfShieldRepair or ShipModuleAction.RepairField or ShipModuleAction.Countermeasures or ShipModuleAction.FuelInjection;
                var context=new ShipActivationContext(true,self||(GetIsObjectValid(target)&&GetArea(target)==GetArea(player)),self||targetStatus!=null,hostile,!hostile&&targetStatus!=null,false,self,self?0:GetIsObjectValid(target)?GetDistanceBetween(player,target):0,GetOperatingSkills(player).GetValueOrDefault(operation.Profile.OperatorSkill));
                var error=ShipModuleActivationPolicy.Validate(status,fitted,operation,context,now,temporary);
                if (error!=null){SendMessageToPC(player,error);return false;}
                volley.Add((fitted,operation));
            }
            try { ShipBanks.PayVolley(status,volley,now); }
            catch (InvalidOperationException ex) { SendMessageToPC(player,ex.Message);return false; }
            foreach (var fitted in modules) ShipTemporaryStats.ConsumePaidOperation(status,fitted.ItemInstanceId,targetId,now);
            PersistShipStatus(player,status);
            foreach (var (fitted,operation) in volley) ActivateResolvedFittedModule(player,fitted.ItemInstanceId,operation);
            return true;
        }
    }
}
