using System;
using System.Linq;

namespace SWLOR.Game.Server.Service.SpaceService
{
    public sealed record ShipCargoLot(string Resref, double Quantity, bool Compressed = false, string ContractId = null)
    {
        public double Occupancy => Quantity * (Compressed ? .5 : 1);
    }

    public static class ShipCargo
    {
        public static string SupplyIdentity(string resref) => resref switch
        {
            "ship_missile" => "light_missile", "acm_ammo" => "assault_missile", "proton_bomb" => "ship_bomb", "ship_fuelcapsule" => "injector_charge", _ => resref
        };
        public static double Occupied(ShipStatus status) => status.Cargo.Values.Sum(x => x.Occupancy);
        public static double Available(ShipStatus status) => Math.Max(0, status.CargoCapacity - Occupied(status) - status.PendingCargoTransfers.Values.Where(x => x.Direction == ShipCargoTransferDirection.Load).Sum(x => x.Quantity));
        public static double Amount(ShipStatus status, string resref) => status.Cargo.Values.Where(x => SupplyIdentity(x.Resref) == SupplyIdentity(resref) && x.ContractId == null).Sum(x => x.Quantity);
        public static bool CanAdd(ShipStatus status, double quantity, bool compressed = false) =>
            double.IsFinite(quantity) && quantity >= 0 && quantity * (compressed ? .5 : 1) <= Available(status) + 1e-9;

        public static void Add(ShipStatus status, string resref, double quantity, string receipt, bool compressed = false, string contractId = null)
        {
            if (string.IsNullOrWhiteSpace(resref) || string.IsNullOrWhiteSpace(receipt) || !double.IsFinite(quantity) || quantity <= 0)
                throw new ArgumentException("Cargo needs a resource, positive quantity, and stable receipt.");
            var expected = new ShipCargoLot(resref, quantity, compressed, contractId);
            if (status.Cargo.TryGetValue(receipt, out var existing))
            {
                if (existing != expected) throw new InvalidOperationException("A cargo receipt cannot change its contents.");
                return;
            }
            if (!CanAdd(status, quantity, compressed)) throw new InvalidOperationException("The cargo hold is full. Unload at a dock before collecting more cargo.");
            status.Cargo.Add(receipt, expected);
        }

        public static void Consume(ShipStatus status, string resref, double quantity)
        {
            if (!double.IsFinite(quantity) || quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
            if (Amount(status, resref) + 1e-9 < quantity) throw new InvalidOperationException($"Load {resref} into the ship's hold at a dock first.");
            foreach (var key in status.Cargo.Where(x => SupplyIdentity(x.Value.Resref) == SupplyIdentity(resref) && x.Value.ContractId == null).Select(x => x.Key).OrderBy(x => x, StringComparer.Ordinal).ToArray())
            {
                var lot = status.Cargo[key];
                var removed = Math.Min(quantity, lot.Quantity);
                quantity -= removed;
                if (lot.Quantity - removed <= 1e-9) status.Cargo.Remove(key);
                else status.Cargo[key] = lot with { Quantity = lot.Quantity - removed };
                if (quantity <= 1e-9) break;
            }
        }
    }
}
