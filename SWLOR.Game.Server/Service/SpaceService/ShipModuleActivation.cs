using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service.SpaceService
{
    public sealed record ShipActivationContext(bool InSpace, bool SameArea, bool HasTarget, bool Hostile,
        bool Allied, bool Site, bool SelfTarget, double Distance, int OperatorRank);
    public sealed record ShipModuleActivation(string Id, string FlightId, string OperatorId, string TargetId,
        string ModuleId, DateTime StartedAt, DateTime CompletesAt);

    public static class ShipModuleActivationPolicy
    {
        public static string Validate(ShipStatus status, ShipStatus.ShipStatusModule fitted, ShipModuleOperation operation,
            ShipActivationContext context, DateTime now, IReadOnlyDictionary<StatType, double> temporary = null)
        {
            double Temp(StatType stat) => temporary?.GetValueOrDefault(stat) ?? 0;
            var action = operation.Profile.Action;
            if (!context.InSpace || status.FittingVersion != ShipFittingConversion.CurrentVersion || status.Hull <= 0) return "You must pilot an operational ship.";
            if (status.PendingInventoryTransfers.Count > 0 || status.PendingCargoTransfers.Count > 0 || now < status.RefitReadyAt) return "The ship's refit is still settling.";
            if (fitted.Condition <= 0) return "That module needs servicing at a dock.";
            if (context.OperatorRank < operation.Profile.OperatorRank) return $"Requires {operation.Profile.OperatorRank} {operation.Profile.OperatorSkill} ranks.";
            if (action == ShipModuleAction.Passive) return "That fitting operates automatically.";
            if (Temp(StatType.ShipActivationLock) > 0 || (action == ShipModuleAction.Weapon && Temp(StatType.ShipWeaponLock) > 0)) return "The ship cannot activate that module yet.";
            if (status.GlobalRecast > now || fitted.RecastTime > now) return "That module is not ready.";
            if (ShipResources.Available(status, ShipResource.Capacitor) + 1e-9 < operation.CapacitorCost) return $"Requires {operation.CapacitorCost} capacitor.";
            var self = action is ShipModuleAction.SelfShieldRepair or ShipModuleAction.SelfHullRepair or ShipModuleAction.FuelInjection or ShipModuleAction.Countermeasures or ShipModuleAction.Compression or ShipModuleAction.RepairField;
            if (!self)
            {
                if (!context.HasTarget || !context.SameArea) return "Select a target in this space area.";
                if (!double.IsFinite(context.Distance) || context.Distance < 0 || context.Distance > operation.Variant.Range) return $"Target is outside {operation.Variant.Range:0.#}m range.";
                if (action is ShipModuleAction.Weapon or ShipModuleAction.Interference)
                {
                    if (!context.Hostile || context.SelfTarget) return "Select a hostile ship.";
                }
                else if (action is ShipModuleAction.ShieldRepair or ShipModuleAction.HullRepair or ShipModuleAction.CapacitorTransfer)
                {
                    if (!context.Allied || context.SelfTarget) return "Select another allied ship.";
                }
                else if (!context.Site) return "Select a deposit, anomaly, or salvage site.";
            }
            var supply = Supply(operation.Profile);
            if (supply != null && ShipCargo.Amount(status, supply) < 1) return $"Load {supply} into the ship's hold at a dock first.";
            return null;
        }

        public static string Supply(ShipModuleProfile profile) => profile.Ammunition ?? (profile.Action switch
        {
            ShipModuleAction.FuelInjection => "injector_charge",
            ShipModuleAction.Compression => "ore_pack_charge",
            _ => null
        });

        public static void Pay(ShipStatus status, ShipStatus.ShipStatusModule fitted, ShipModuleOperation operation, DateTime now)
        {
            if (ShipResources.Available(status, ShipResource.Capacitor) + 1e-9 < operation.CapacitorCost) throw new InvalidOperationException("Insufficient capacitor.");
            var supply = Supply(operation.Profile);
            if (supply != null && ShipCargo.Amount(status, supply) < 1) throw new InvalidOperationException("Insufficient loaded supplies.");
            if (supply != null) ShipCargo.Consume(status, supply, 1);
            ShipResources.SpendPrecise(status, ShipResource.Capacitor, operation.CapacitorCost);
            fitted.RecastTime = now.AddSeconds(operation.Variant.Cycle);
            status.GlobalRecast = now.AddSeconds(1);
        }
    }
}
