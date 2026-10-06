using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.SpaceService
{
    public sealed record ShipDockPayment(string Id, string PlayerId, int Credits);
    public sealed record ShipDefeatSettlement(int RecoveryCredits, IReadOnlyDictionary<string, int> LostCargo, bool Applied);
    public static class ShipRecovery
    {
        public static int HullRecoveryPrice(ShipStatus status, ShipFittingCatalog catalog = null) => Math.Clamp(
            (int)Math.Ceiling((catalog ?? ShipFittingCatalog.Default).Hulls[status.ItemTag].ReferenceValue * .03), 60, 1800);
        public static int ModuleServicePrice(ShipStatus.ShipStatusModule module, ShipFittingCatalog catalog = null)
        {
            catalog ??= ShipFittingCatalog.Default;
            if (module.Condition >= 100) return 0;
            var value = catalog.Modules.TryGetValue(module.Design, out var profile) ? profile.ReferenceValue : 350;
            return Math.Max(10, (int)Math.Ceiling(value * .01 * (100 - Math.Clamp(module.Condition, 0, 100))));
        }
        public static int DockPrice(ShipStatus status, ShipFittingCatalog catalog = null)
        {
            var hull = status.OutstandingRecoveryCredits > 0 ? status.OutstandingRecoveryCredits :
                ShipResources.Available(status, ShipResource.Hull) < status.MaxHull ? HullRecoveryPrice(status, catalog) : 0;
            return checked(hull + ShipFittedStats.Modules(status).Concat(status.ConfigurationModules.Values).Sum(x => ModuleServicePrice(x, catalog)));
        }
        public static ShipDefeatSettlement Defeat(ShipStatus status, ShipFittingCatalog catalog = null)
        {
            if (string.IsNullOrEmpty(status.FlightId)) throw new InvalidOperationException("Defeat needs a flight identity.");
            if (status.LastDefeatFlightId == status.FlightId) return new(0, new Dictionary<string, int>(), false);
            var loss = new Dictionary<string, int>();
            var protection = (double)status.ProtectedCargo;
            // Protect and round once per commodity, regardless of how collection split it into lots.
            foreach (var group in status.Cargo.Where(x => x.Value.ContractId == null &&
                (x.Value.Resref.StartsWith("ore_", StringComparison.Ordinal) || x.Value.Resref.StartsWith("ref_", StringComparison.Ordinal) || x.Value.Resref is "elec_recover" or "elec_ruined"))
                .GroupBy(x => x.Value.Resref).OrderBy(x => x.Key, StringComparer.Ordinal))
            {
                var quantity = group.Sum(x => x.Value.Quantity);
                var occupied = group.Sum(x => x.Value.Occupancy);
                var protectedQuantity = occupied <= protection ? quantity : quantity * protection / occupied;
                protection = Math.Max(0, protection - occupied);
                var removed = (int)Math.Floor(.2 * Math.Max(0, quantity - protectedQuantity) + 1e-9);
                if (removed > 0) { ShipCargo.Consume(status, group.Key, removed); loss[group.Key] = removed; }
            }
            foreach (var fitted in ShipFittedStats.Modules(status).Concat(status.ConfigurationModules.Values)) fitted.Condition = Math.Max(0, fitted.Condition - 20);
            var price = HullRecoveryPrice(status, catalog);
            status.OutstandingRecoveryCredits = checked(status.OutstandingRecoveryCredits + price);
            status.LastDefeatFlightId = status.FlightId;
            status.PendingModuleActivations.Clear(); status.TemporaryAdjustments.Clear();
            status.Hull = 1; status.Shield = 0; status.Capacitor = 0;
            status.FractionalResourceDeficits.Clear();
            status.ResourceDeficits = ShipResourceDeficits.Capture(status);
            return new(price, loss, true);
        }
        public static void CompleteDockService(ShipStatus status)
        {
            foreach (var module in ShipFittedStats.Modules(status).Concat(status.ConfigurationModules.Values)) module.Condition = 100;
            status.OutstandingRecoveryCredits = 0;
            // Fitting is recalculated by the dock operator before filling its current pools.
            status.Hull = status.MaxHull; status.Shield = status.MaxShield; status.Capacitor = status.MaxCapacitor;
            status.FractionalResourceDeficits.Clear(); status.ResourceDeficits = ShipResourceDeficits.Capture(status);
            status.PendingDockPayment = null;
        }
    }
}
