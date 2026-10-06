using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWNX;
namespace SWLOR.Game.Server.Service
{
    public static class ShipSupply
    {
        public const string BoundOwner="SHIP_BOUND_PLAYER";
        private const string Delivered="SHIP_TRADE_OUTPUT";
        private const string Consumed="SHIP_TRADE_CONSUMED";
        private const string Paid="SHIP_TRADE_PAID";
        public static void RequireServiceDock(uint player)
        {
            if(!GetIsPC(player)||Space.IsPlayerInSpaceMode(player)||GetIsDead(player))throw new InvalidOperationException("Visit a planetary dock for ship supplies.");
            var area=GetArea(player);var points=Space.GetDockPointsByPlanet(Planet.GetPlanetType(area));
            if(!points.Values.Any(x=>x.IsNPC && GetAreaFromLocation(x.Location)==area && GetDistanceBetweenLocations(GetLocation(player),x.Location)<=30))
                throw new InvalidOperationException("Visit the dock's supply desk within 30m of an NPC landing point.");
        }
        private static uint Find(uint player,string id,string variable=null)
        {
            for(var item=GetFirstItemInInventory(player);GetIsObjectValid(item);item=GetNextItemInInventory(player))
                if(variable==null?GetObjectUUID(item)==id:GetLocalString(item,variable)==id)return item;
            return OBJECT_INVALID;
        }
        public static void Buy(uint player,string resref,int quantity=1)
        {
            RequireServiceDock(player);var record=DB.Get<Player>(GetObjectUUID(player));
            if(record.SpaceEconomy.Pending!=null)throw new InvalidOperationException("Finish the pending supply transaction first.");
            var item=SpaceEconomyCatalog.Default.Items.GetValueOrDefault(resref);
            if(item?.Vendor!=true||quantity<1||quantity>20||(!item.Ammunition&&quantity!=1))throw new InvalidOperationException("Choose a stocked Standard fitting or supply.");
            var discount=Stat.GetStatAdjustment(player,StatType.ShipAmmunitionDiscount)/10000d;
            var credits=SpaceEconomyCatalog.SupplyPrice(item,quantity,discount);
            if(GetGold(player)<credits)throw new InvalidOperationException("Not enough credits.");
            record.SpaceEconomy.Pending=new(){Id=Guid.NewGuid().ToString(),Resref=resref,Quantity=quantity,Credits=credits,Purchase=true,Day=DateTime.UtcNow};DB.Set(record);RecoverTrade(player);
        }
        public static void SellOre(uint player,string resref,int requested=20)
        {
            RequireServiceDock(player);var record=DB.Get<Player>(GetObjectUUID(player));
            if(record.SpaceEconomy.Pending!=null)throw new InvalidOperationException("Finish the pending supply transaction first.");
            if(!SpaceEconomyCatalog.Default.OreBids.TryGetValue(resref,out var bid))throw new InvalidOperationException("Choose a listed ore commission.");
            uint item=OBJECT_INVALID;
            for(var candidate=GetFirstItemInInventory(player);GetIsObjectValid(candidate);candidate=GetNextItemInInventory(player))
                if(GetResRef(candidate)==resref && !GetPlotFlag(candidate)){item=candidate;break;}
            if(!GetIsObjectValid(item))throw new InvalidOperationException("Unload this ore into your inventory first.");
            var now=DateTime.UtcNow;var quantity=Math.Min(Math.Min(Math.Clamp(requested,1,1000),GetItemStackSize(item)),record.SpaceEconomy.OreAvailable(now));
            if(quantity<=0)throw new InvalidOperationException("Your 1,000-unit ore commission allowance is used for today.");
            BeginSale(player,item,quantity,checked(quantity*bid),true);
        }
        public static void BeginSale(uint player,uint item,int quantity,int credits,bool ore=false)
        {
            var record=DB.Get<Player>(GetObjectUUID(player));
            if(record.SpaceEconomy.Pending!=null)throw new InvalidOperationException("Finish the pending supply transaction first.");
            if(GetItemPossessor(item)!=player||GetPlotFlag(item)||!string.IsNullOrEmpty(GetLocalString(item,BoundOwner))||quantity<=0||quantity>GetItemStackSize(item))throw new InvalidOperationException("Choose unbound equipment in your inventory.");
            SetLocalString(item,"SHIP_TRADE_RESERVED",GetObjectUUID(item));SetPlotFlag(item,true);
            ExportSingleCharacter(player);
            record.SpaceEconomy.Pending=new(){Id=Guid.NewGuid().ToString(),Resref=GetResRef(item),InputId=GetObjectUUID(item),InitialQuantity=GetItemStackSize(item),Quantity=quantity,Credits=credits,OreCommission=ore,Day=DateTime.UtcNow};DB.Set(record);RecoverTrade(player);
        }
        public static void ClaimMaterials(uint player)
        {
            RequireServiceDock(player);var record=DB.Get<Player>(GetObjectUUID(player));
            if(record.SpaceEconomy.Pending!=null)throw new InvalidOperationException("Finish the pending supply transaction first.");
            var component=record.SpaceEconomy.MaterialRecovery.FirstOrDefault(x=>x.Value>=1);
            if(component.Key==null)throw new InvalidOperationException("No whole verified materials are ready. Fractional recovery remains credited.");
            record.SpaceEconomy.Pending=new(){Id=Guid.NewGuid().ToString(),Resref=component.Key,Quantity=Math.Min(20,(int)Math.Floor(component.Value)),Purchase=true,MaterialRecovery=true,Day=DateTime.UtcNow};DB.Set(record);RecoverTrade(player);
        }
        public static void RecoverTrade(uint player)
        {
            var record=DB.Get<Player>(GetObjectUUID(player));var trade=record.SpaceEconomy.Pending;if(trade==null)return;
            if(GetLocalString(player,Paid)!=trade.Id)
            {
                if(trade.Purchase)
                {
                    if(GetGold(player)<trade.Credits)throw new InvalidOperationException("The pending purchase needs its recorded payment.");
                    if(!GetIsObjectValid(Find(player,trade.Id,Delivered)))
                    {
                        var output=CreateItemOnObject(trade.Resref,player,trade.Quantity);
                        if(!GetIsObjectValid(output)||GetItemPossessor(output)!=player){if(GetIsObjectValid(output))DestroyObject(output);throw new InvalidOperationException("Make room for the supplies in your inventory.");}
                        SetLocalString(output,Delivered,trade.Id);
                    }
                    TakeGoldFromCreature(trade.Credits,player,true);SetLocalString(player,Paid,trade.Id);ExportSingleCharacter(player);
                }
                else
                {
                    var input=Find(player,trade.InputId);
                    if(GetIsObjectValid(input)&&GetResRef(input)!=trade.Resref)throw new InvalidOperationException("The reserved sale item changed.");
                    var source=ShipTradePolicy.Source(trade,GetIsObjectValid(input)?GetItemStackSize(input):0);
                    if(source==ShipSaleSourceState.Invalid)throw new InvalidOperationException("Restore the reserved sale quantity to your inventory.");
                    if(source==ShipSaleSourceState.Untouched)
                    {
                        var remaining=trade.InitialQuantity-trade.Quantity;
                        if(remaining>0)SetItemStackSize(input,remaining);else DestroyObject(input);
                        var identity=record.Id;
                        DelayCommand(.1f,()=>
                        {
                            if(!GetIsObjectValid(player)||GetObjectUUID(player)!=identity)return;
                            var saved=Find(player,trade.InputId);
                            if(ShipTradePolicy.Source(trade,GetIsObjectValid(saved)?GetItemStackSize(saved):0)!=ShipSaleSourceState.Consumed)return;
                            if(GetIsObjectValid(saved)){DeleteLocalString(saved,"SHIP_TRADE_RESERVED");SetPlotFlag(saved,false);}
                            SetLocalString(player,Consumed,trade.Id);ExportSingleCharacter(player);CompleteSale(player,trade.Id);
                        });
                        return;
                    }
                    if(GetIsObjectValid(input)){DeleteLocalString(input,"SHIP_TRADE_RESERVED");SetPlotFlag(input,false);}
                    SetLocalString(player,Consumed,trade.Id);ExportSingleCharacter(player);CompleteSale(player,trade.Id);
                }
            }
            FinishTrade(player,trade.Id);
        }
        private static void CompleteSale(uint player,string id)
        {
            var record=DB.Get<Player>(GetObjectUUID(player));var trade=record.SpaceEconomy.Pending;if(trade?.Id!=id)return;
            if(GetLocalString(player,Paid)!=id){GiveGoldToCreature(player,trade.Credits);SetLocalString(player,Paid,id);ExportSingleCharacter(player);}
            FinishTrade(player,id);
        }
        private static void FinishTrade(uint player,string id)
        {
            var record=DB.Get<Player>(GetObjectUUID(player));var trade=record.SpaceEconomy.Pending;if(trade?.Id!=id||GetLocalString(player,Paid)!=id)return;
            if(trade.OreCommission)record.SpaceEconomy.Day(trade.Day).Ore+=trade.Quantity;
            if(trade.MaterialRecovery)record.SpaceEconomy.MaterialRecovery[trade.Resref]-=trade.Quantity;
            record.SpaceEconomy.Pending=null;DB.Set(record);
            SendMessageToPC(player,trade.Purchase?$"Bought {trade.Quantity} {(SpaceEconomyCatalog.Default.Items.GetValueOrDefault(trade.Resref)?.Name??Cache.GetItemNameByResref(trade.Resref))} for {trade.Credits} credits.":$"Sold {trade.Quantity} items for {trade.Credits} credits.");
        }
        public static void ClaimStarter(uint player,bool industrial)
        {
            RequireServiceDock(player);var record=DB.Get<Player>(GetObjectUUID(player));var economy=record.SpaceEconomy;
            if(economy.ClaimedStarterRevision==economy.StarterRevision)throw new InvalidOperationException("The starter grant for this rebuild has already been claimed.");
            if(economy.StarterChoice==null){economy.StarterChoice=industrial?"industry":"escort";DB.Set(record);}
            RecoverStarter(player);
        }
        private static void RecoverStarter(uint player)
        {
            var record=DB.Get<Player>(GetObjectUUID(player));var economy=record.SpaceEconomy;
            if(economy.StarterChoice==null||economy.ClaimedStarterRevision==economy.StarterRevision)return;
            var industrial=economy.StarterChoice=="industry";
            var deed=Space.GetShipDetailByItemTag(industrial?"ShipDeedLightFreighter":"ShipDeedLightEscort").ItemResref;
            var catalog=ShipFittingCatalog.Default;
            var resources=new[]{deed,catalog.GetVariant(industrial?"precision_cutter":"tracking_laser").ItemResref,catalog.GetVariant(industrial?"survey_scanner":"tracking_laser").ItemResref,catalog.GetVariant("hull_repair").ItemResref};
            for(var i=0;i<resources.Length;i++)
            {
                var id=$"starter/{record.Id}/{economy.StarterRevision}/{i}";
                if(economy.StarterOutputs.Contains(id))continue;
                if(!GetIsObjectValid(Find(player,id,Delivered)))
                {
                    var item=CreateItemOnObject(resources[i],player);
                    if(!GetIsObjectValid(item)||GetItemPossessor(item)!=player){if(GetIsObjectValid(item))DestroyObject(item);throw new InvalidOperationException("Make room for your starter equipment.");}
                    SetLocalString(item,Delivered,id);SetLocalString(item,BoundOwner,record.Id);SetPlotFlag(item,true);
                }
                ExportSingleCharacter(player);economy.StarterOutputs.Add(id);DB.Set(record);
            }
            economy.ServiceVoucher+=100;economy.ClaimedStarterRevision=economy.StarterRevision;economy.StarterChoice=null;DB.Set(record);
            SendMessageToPC(player,"Starter deed and fittings delivered. Register at this dock, then fit your equipment. Your 100-credit voucher pays dock service; /spacejobs offers routes and objectives.");
        }
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void RecoverOnLogin()
        {
            var player=GetEnteringObject();if(!GetIsPC(player)||GetIsDM(player))return;
            try{RecoverTrade(player);RecoverStarter(player);}catch(InvalidOperationException ex){SendMessageToPC(player,ex.Message);}
        }
    }
}
