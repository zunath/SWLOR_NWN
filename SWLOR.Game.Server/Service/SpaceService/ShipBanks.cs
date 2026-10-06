using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.SpaceService
{
    public static class ShipBanks
    {
        public static IReadOnlyList<ShipStatus.ShipStatusModule> Modules(ShipStatus status,int bank)
        {
            if (bank is < 1 or > 2) throw new ArgumentOutOfRangeException(nameof(bank));
            var ids=status.BankModules.GetValueOrDefault(bank) ?? new();
            return ids.Select(id=>ShipFittedStats.Modules(status).FirstOrDefault(x=>x.ItemInstanceId==id)).Where(x=>x!=null).ToList();
        }
        public static void Prepare(ShipStatus status,int bank,IEnumerable<string> moduleIds,DateTime now)
        {
            if (bank is < 1 or > 2) throw new ArgumentOutOfRangeException(nameof(bank));
            var ids=moduleIds.ToList();
            if (ids.Count>4 || ids.Distinct().Count()!=ids.Count) throw new InvalidOperationException("A bank holds up to four distinct compatible modules.");
            var fitted=ShipFittedStats.Modules(status).ToDictionary(x=>x.ItemInstanceId);
            if (ids.Any(id=>!fitted.ContainsKey(id))) throw new InvalidOperationException("Only installed modules can be banked.");
            var profiles=ids.Select(id=>ShipFittingCatalog.Default.Modules[fitted[id].Design]).ToList();
            if (profiles.Any(p=>p.Action==ShipModuleAction.Passive || p.Action is ShipModuleAction.Survey or ShipModuleAction.Extraction or ShipModuleAction.BulkSalvage or ShipModuleAction.IntactSalvage or ShipModuleAction.Compression) ||
                profiles.Select(p=>p.Action).Distinct().Count()>1)
                throw new InvalidOperationException("Bank modules must share one firing or support operation.");
            status.BankModules[bank]=ids;status.RefitReadyAt=now.AddSeconds(5);
        }
        public static void PayVolley(ShipStatus status,IReadOnlyList<(ShipStatus.ShipStatusModule Fitted,ShipModuleOperation Operation)> modules,DateTime now)
        {
            if (modules.Count is < 1 or > 4 || modules.Select(x=>x.Fitted.ItemInstanceId).Distinct().Count()!=modules.Count)
                throw new InvalidOperationException("Select a bank containing one to four distinct modules.");
            if (ShipResources.Available(status,ShipResource.Capacitor)+1e-9<modules.Sum(x=>x.Operation.CapacitorCost)) throw new InvalidOperationException("Insufficient capacitor for the entire bank.");
            foreach (var group in modules.Where(x=>ShipModuleActivationPolicy.Supply(x.Operation.Profile)!=null).GroupBy(x=>ShipModuleActivationPolicy.Supply(x.Operation.Profile)))
                if (ShipCargo.Amount(status,group.Key)<group.Sum(x=>x.Operation.SupplyQuantity)) throw new InvalidOperationException("Insufficient loaded ammunition for the entire bank.");
            foreach (var module in modules) ShipModuleActivationPolicy.Pay(status,module.Fitted,module.Operation,now);
        }
    }
}
