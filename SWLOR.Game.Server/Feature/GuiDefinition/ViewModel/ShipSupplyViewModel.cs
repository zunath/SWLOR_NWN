using System.Collections.Generic;
using System;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.SpaceService;
namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public sealed class ShipSupplyViewModel : GuiViewModelBase<ShipSupplyViewModel,ShipSupplyPayload>
    {
        public const string ContentElement="ship-supply-content",MainContentPartial="ship-supply-main";
        private string[] _stock,_ores;
        public string Summary {get=>Get<string>();set=>Set(value);}
        public string Message {get=>Get<string>();set=>Set(value);}
        public GuiBindingList<string> Stock {get=>Get<GuiBindingList<string>>();set=>Set(value);}
        public GuiBindingList<string> Ore {get=>Get<GuiBindingList<string>>();set=>Set(value);}
        protected override void Initialize(ShipSupplyPayload payload)
        {
            Stock=new();Ore=new();Message="";
            _stock=SpaceEconomyCatalog.Default.Items.Values.Where(x=>x.Vendor).OrderBy(x=>x.Engineering).ThenBy(x=>x.Name).Select(x=>x.Resref).ToArray();
            _ores=SpaceEconomyCatalog.Default.OreBids.Keys.ToArray();Refresh();ChangePartialView(ContentElement,MainContentPartial);
        }
        protected override void OnModalClosedRestore()=>ChangePartialView(ContentElement,MainContentPartial);
        private void Refresh()
        {
            var record=DB.Get<Player>(GetObjectUUID(Player));var economy=record.SpaceEconomy;
            Summary=$"Standard fittings and supplies. Unload cargo before ore commissions.\nOre allowance: {economy.OreAvailable(DateTime.UtcNow)}/1,000 today | Reputation: {economy.Reputation} ({economy.Day(DateTime.UtcNow).Reputation}/100 today)\nService voucher: {economy.ServiceVoucher}cr | Starter grant: {(economy.ClaimedStarterRevision==economy.StarterRevision?"claimed":"available")}\nRegister your deed, install fittings, launch and use /spacejobs for a timed route and objective.";
            Stock=new();Ore=new();
            var discount=Space.GetShipStatAdjustments(Player).GetValueOrDefault(Service.StatService.StatType.ShipAmmunitionDiscount)/10000d;
            foreach(var id in _stock){var p=SpaceEconomyCatalog.Default.Items[id];var quantity=p.Ammunition?10:1;Stock.Add($"{p.Name} Ã—{quantity} | {SpaceEconomyCatalog.SupplyPrice(p,quantity,discount)}cr");}
            foreach(var id in _ores)Ore.Add($"{Cache.GetItemNameByResref(id)} | {SpaceEconomyCatalog.Default.OreBids[id]}cr/unit | sell up to 20");
        }
        private void Run(Action action){try{action();Message="";}catch(InvalidOperationException ex){Message=ex.Message;}Refresh();}
        public Action OnBuy()=>()=>Run(()=>{var i=NuiGetEventArrayIndex();if(i>=0&&i<_stock.Length){var p=SpaceEconomyCatalog.Default.Items[_stock[i]];ShipSupply.Buy(Player,p.Resref,p.Ammunition?10:1);}});
        public Action OnSellOre()=>()=>Run(()=>{var i=NuiGetEventArrayIndex();if(i>=0&&i<_ores.Length)ShipSupply.SellOre(Player,_ores[i]);});
        public Action OnStarterEscort()=>()=>ShowModal("Claim a bound Light Escort deed, two tracking lasers, one hull repairer and 100cr service voucher?",()=>Run(()=>ShipSupply.ClaimStarter(Player,false)));
        public Action OnStarterIndustry()=>()=>ShowModal("Claim a bound Light Freighter deed, precision cutter, survey scanner, one hull repairer and 100cr service voucher?",()=>Run(()=>ShipSupply.ClaimStarter(Player,true)));
        public Action OnRecover()=>()=>Run(()=>ShipSupply.RecoverTrade(Player));
        public Action OnMaterials()=>()=>Run(()=>ShipSupply.ClaimMaterials(Player));
        public Action OnRefresh()=>()=>Refresh();
    }
}
