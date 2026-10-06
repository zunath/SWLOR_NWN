using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public sealed class ShipCockpitViewModel : GuiViewModelBase<ShipCockpitViewModel,ShipCockpitPayload>, IGuiRefreshable<ShipCockpitRefreshEvent>
    {
        public const string ContentElement="ship-cockpit-content";
        public const string MainContentPartial="ship-cockpit-main";
        private string _shipId;
        private readonly List<ShipTechniqueProfile> _perks=new();
        private readonly List<string> _modules=new();
        private string _message;
        private string _lastSignature;
        public string Summary { get=>Get<string>();set=>Set(value); }
        public string PreparedSummary { get=>Get<string>();set=>Set(value); }
        public string StatusText { get=>Get<string>();set=>Set(value); }
        public string EffectsText { get=>Get<string>();set=>Set(value); }
        public string ConstituentText { get=>Get<string>();set=>Set(value); }
        public string Bank1Text { get=>Get<string>();set=>Set(value); }
        public string Bank2Text { get=>Get<string>();set=>Set(value); }
        public bool Docked { get=>Get<bool>();set=>Set(value); }
        public bool InFlight { get=>Get<bool>();set=>Set(value); }
        public GuiBindingList<string> PerkRows { get=>Get<GuiBindingList<string>>();set=>Set(value); }
        public GuiBindingList<string> PerkIcons { get=>Get<GuiBindingList<string>>();set=>Set(value); }
        public GuiBindingList<string> PerkDescriptions { get=>Get<GuiBindingList<string>>();set=>Set(value); }
        public GuiBindingList<string> PerkUseText { get=>Get<GuiBindingList<string>>();set=>Set(value); }
        public GuiBindingList<string> PerkPrepareText { get=>Get<GuiBindingList<string>>();set=>Set(value); }
        public GuiBindingList<bool> CanUsePerk { get=>Get<GuiBindingList<bool>>();set=>Set(value); }
        public GuiBindingList<string> ModuleRows { get=>Get<GuiBindingList<string>>();set=>Set(value); }
        public GuiBindingList<string> ModuleDescriptions { get=>Get<GuiBindingList<string>>();set=>Set(value); }
        public GuiBindingList<string> ModuleBank1 { get=>Get<GuiBindingList<string>>();set=>Set(value); }
        public GuiBindingList<string> ModuleBank2 { get=>Get<GuiBindingList<string>>();set=>Set(value); }
        protected override void Initialize(ShipCockpitPayload payload){_shipId=payload.ShipId;Update();ChangePartialView(ContentElement,MainContentPartial);}
        protected override void OnModalClosedRestore()=>ChangePartialView(ContentElement,MainContentPartial);
        public void Refresh(ShipCockpitRefreshEvent payload)=>Update();
        public Action OnRefresh()=>()=>{_lastSignature=null;Update();};
        private void Run(Action action)
        {
            try { action();_message=null; }
            catch (InvalidOperationException ex){_message=ex.Message;}
            _lastSignature=null;Update();
        }
        public Action OnSelectBank(int bank)=>()=>Run(()=>{var p=DB.Get<Player>(GetObjectUUID(Player));p.ShipOperations.SelectedBank=bank;DB.Set(p);});
        public Action OnFireBank(int bank)=>()=>Run(()=>Space.ActivateShipBank(Player,bank));
        public Action OnUsePerk()=>()=>Run(()=>
        {
            var index=NuiGetEventArrayIndex();if(index<0||index>=_perks.Count)return;var p=_perks[index];
            if (!InFlight) throw new InvalidOperationException("Pilot the ship to activate operating techniques.");
            Space.ActivateShipTechnique(Player,p,Perk.GetPerkLevel(Player,p.Perk),OBJECT_INVALID);
        });
        public Action OnPreparePerk()=>()=>Run(()=>
        {
            var index=NuiGetEventArrayIndex();if(index<0||index>=_perks.Count)return;var p=_perks[index];var state=DB.Get<Player>(GetObjectUUID(Player)).ShipOperations;
            var prepared=state.Prepared.ToList();var mode=state.SelectedMode;
            if(p.Kind==ShipPerkKind.Mode) mode=mode==p.Key?null:p.Key;
            else if(!prepared.Remove(p.Key))prepared.Add(p.Key);
            Space.PrepareShipOperations(Player,_shipId,prepared,mode);
        });
        public Action OnAssignBank(int bank)=>()=>Run(()=>
        {
            var index=NuiGetEventArrayIndex();if(index<0||index>=_modules.Count)return;
            var ship=ShipEquipmentTransfers.RequireDock(Player,_shipId);var ids=(ship.Status.BankModules.GetValueOrDefault(bank)??new()).ToList();
            if(!ids.Remove(_modules[index]))ids.Add(_modules[index]);ShipBanks.Prepare(ship.Status,bank,ids,DateTime.UtcNow);DB.Set(ship);
        });
        public Action OnSelectTool()=>()=>Run(()=>
        {
            var index=NuiGetEventArrayIndex();if(index<0||index>=_modules.Count)return;
            var record=DB.Get<Player>(GetObjectUUID(Player));record.ShipOperations.SelectedToolId=_modules[index];DB.Set(record);
        });
        public Action OnSelectConstituent()=>()=>Run(()=>
        {
            if(!InFlight)throw new InvalidOperationException("Select a surveyed resource site while piloting.");
            var (target,_)=Space.GetCurrentTarget(Player);var site=Space.GetIndustrySite(target);var p=DB.Get<Player>(GetObjectUUID(Player));
            if(site==null||!site.SurveyedBy.Contains(p.Id))throw new InvalidOperationException("Survey the selected site first.");
            var choices=site.Reserves.Where(x=>x.Key.StartsWith("ore_",StringComparison.Ordinal)&&x.Value>0).Select(x=>x.Key).OrderBy(x=>x).ToList();
            if(choices.Count==0)throw new InvalidOperationException("No constituent remains.");
            p.ShipOperations.SelectedConstituent=choices[(choices.IndexOf(p.ShipOperations.SelectedConstituent)+1)%choices.Count];DB.Set(p);
        });
        private void Update()
        {
            var player=DB.Get<Player>(GetObjectUUID(Player));var state=player.ShipOperations;var ship=DB.Get<PlayerShip>(_shipId);var now=DateTime.UtcNow;
            InFlight=Space.IsPlayerInSpaceMode(Player)&&player.ActiveShipId==_shipId;Docked=!Space.IsPlayerInSpaceMode(Player);
            var timers=state.ReadyAt>now||state.Cooldowns.Values.Any(x=>x>now)||ship?.Status.TemporaryAdjustments.Any(x=>x.ExpiresAt>now)==true;
            var signature=Newtonsoft.Json.JsonConvert.SerializeObject(new{player.Perks,State=state,InFlight,Clock=timers?now.Ticks/TimeSpan.TicksPerSecond:0,
                Pools=ship==null?null:new[]{ShipResources.Available(ship.Status,ShipResource.Hull),ShipResources.Available(ship.Status,ShipResource.Shield),ShipResources.Available(ship.Status,ShipResource.Capacitor),ship.Status.CargoCapacity,ShipCargo.Occupied(ship.Status)},
                Modules=ship==null?null:ShipFittedStats.Modules(ship.Status).Select(x=>new{x.ItemInstanceId,x.Design,x.Calibration,x.Condition,x.Quality,x.QualityDimension}),Banks=ship?.Status.BankModules,Effects=ship?.Status.TemporaryAdjustments});
            if(signature==_lastSignature)return;_lastSignature=signature;
            _perks.Clear();_modules.Clear();var rows=new GuiBindingList<string>();var icons=new GuiBindingList<string>();var descriptions=new GuiBindingList<string>();var uses=new GuiBindingList<string>();var prepare=new GuiBindingList<string>();var canUse=new GuiBindingList<bool>();
            var modules=new GuiBindingList<string>();var moduleDescriptions=new GuiBindingList<string>();var bank1=new GuiBindingList<string>();var bank2=new GuiBindingList<string>();
            Summary=ship==null?"Ship unavailable":$"HL {ship.Status.Hull}/{ship.Status.MaxHull}   SH {ship.Status.Shield}/{ship.Status.MaxShield}   CAP {ShipResources.Available(ship.Status,ShipResource.Capacitor):0.#}/{ship.Status.MaxCapacitor}   Cargo {ShipCargo.Occupied(ship.Status):0.#}/{ship.Status.CargoCapacity:0}";
            var mode=ShipTechniqueCatalog.Default.Profiles.FirstOrDefault(x=>x.Kind==ShipPerkKind.Mode&&x.Key==state.SelectedMode);
            PreparedSummary=$"Prepared {state.Prepared.Count}/4 (one capstone)   Mode: {mode?.Name??"None"}";
            Bank1Text=(state.SelectedBank==1?"Selected ":"")+"Bank 1";Bank2Text=(state.SelectedBank==2?"Selected ":"")+"Bank 2";
            ConstituentText="Select constituent: "+(state.SelectedConstituent?.Replace("ore_","")??"None");
            foreach(var group in ShipTechniqueCatalog.Default.Profiles.Where(x=>x.Kind!=ShipPerkKind.Trait).GroupBy(x=>x.Key))
            {
                var rank=player.Perks.GetValueOrDefault(group.First().Perk);var p=group.FirstOrDefault(x=>x.Rank==rank);if(p==null)continue;
                var ready=state.Cooldowns.GetValueOrDefault(p.Recast.ToString());var seconds=Math.Max(0,(ready-now).TotalSeconds);
                rows.Add(p.Name+" · "+p.Kind+" · "+p.Capacitor+" CAP");icons.Add(p.Icon);descriptions.Add(p.Description);
                uses.Add(seconds>0?$"{seconds:0}s":"Activate");prepare.Add(p.Kind==ShipPerkKind.Mode?(state.SelectedMode==p.Key?"Selected":"Select"):(state.Prepared.Contains(p.Key)?"Prepared":"Prepare"));
                canUse.Add(InFlight&&seconds<=0&&(p.Kind==ShipPerkKind.Mode||state.Prepared.Contains(p.Key))&&now>=state.ReadyAt);_perks.Add(p);
            }
            if(ship!=null)
            {
                foreach(var fitted in ShipFittedStats.Modules(ship.Status))
                {
                    var profile=ShipFittingCatalog.Default.Modules[fitted.Design];if(profile.Action==ShipModuleAction.Passive)continue;
                    var operation=ShipOperations.Resolve(ship.Status,withCondition(),temporary:ShipTemporaryStats.Current(ship.Status,now,fitted.ItemInstanceId));
                    modules.Add(profile.Name+" · "+fitted.Calibration+" · "+fitted.Condition+"%");
                    moduleDescriptions.Add($"{operation.Output:0.##} output · {operation.CapacitorCost} CAP · {operation.Variant.Cycle:0.##}s · {operation.Variant.Range:0.#}m · quality {fitted.Quality} ({fitted.QualityDimension})");
                    bank1.Add((ship.Status.BankModules.GetValueOrDefault(1)?.Contains(fitted.ItemInstanceId)==true?"In ":"Add ")+"Bank 1");bank2.Add((ship.Status.BankModules.GetValueOrDefault(2)?.Contains(fitted.ItemInstanceId)==true?"In ":"Add ")+"Bank 2");_modules.Add(fitted.ItemInstanceId);
                    ShipStatus.ShipStatusModule withCondition()=>new(){Design=fitted.Design,Calibration=fitted.Calibration,Quality=fitted.Quality,QualityDimension=fitted.QualityDimension,Condition=100};
                }
                EffectsText=string.Join("; ",ship.Status.TemporaryAdjustments.Where(x=>x.ExpiresAt>now&&!string.IsNullOrEmpty(x.Label)).GroupBy(x=>x.Family).Select(g=>$"{g.First().Label} {(g.Max(x=>x.ExpiresAt)-now).TotalSeconds:0}s"));
            }
            else EffectsText="";
            StatusText=_message??(Docked?"Prepare at this ship's dock. Each change needs 5 seconds. Compatible banks hold 1–4 modules.":"Select a ship or resource target. Techniques boost fitted hardware; every operation still pays capacitor and supplies.");
            PerkRows=rows;PerkIcons=icons;PerkDescriptions=descriptions;PerkUseText=uses;PerkPrepareText=prepare;CanUsePerk=canUse;ModuleRows=modules;ModuleDescriptions=moduleDescriptions;ModuleBank1=bank1;ModuleBank2=bank2;
        }
    }
}
