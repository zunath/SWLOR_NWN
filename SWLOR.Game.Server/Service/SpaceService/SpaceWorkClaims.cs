using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service.SpaceService
{
    public enum SpaceSiteKind { Deposit, Anomaly, Wreck }
    public enum SpaceWorkState { Reserved, Completed }
    public sealed record SpaceWorkClaim
    {
        public string Id { get; init; }
        public string Generation { get; init; }
        public string PlayerId { get; init; }
        public string ShipId { get; init; }
        public string FlightId { get; init; }
        public string ModuleId { get; init; }
        public ShipModuleAction Action { get; init; }
        public DateTime StartedAt { get; init; }
        public DateTime CompletesAt { get; init; }
        public double Range { get; init; }
        public Dictionary<string, double> Allocations { get; init; } = new();
        public Dictionary<string, double> Cargo { get; init; } = new();
        public double Recovery { get; init; }
        public double IntactChance { get; init; }
        public double Resolution { get; init; }
        public int StabilityCost { get; init; }
        public SpaceWorkState State { get; set; }
        public bool CargoSettled { get; set; }
        public bool XPSettled { get; set; }
        public int BaseXP { get; set; }
        public double DiscoveryChance { get; init; }
        public double ReservedCargo => (Action == ShipModuleAction.Survey ? 0 : Action == ShipModuleAction.IntactSalvage ? 1 : Allocations.Values.Sum() * Recovery) + (DiscoveryChance>0?1:0);
    }

    public static class SpaceWorkClaims
    {
        public static SpaceSite NewDeposit(string id, string area, int slot, SpaceDepositProfile profile, DateTime now)
        {
            var reserves = profile.Composition.ToDictionary(x => x.Key, x => x.Value * profile.Reserve);
            return new SpaceSite { Id = id, AreaResref = area, Slot = slot, Generation = Guid.NewGuid().ToString(), Profile = profile.Name,
                Kind = profile.Hardness >= 100 ? SpaceSiteKind.Anomaly : SpaceSiteKind.Deposit, Hardness = profile.Hardness,
                MaximumShips = profile.Ships, Stability = profile.Stability, Reserves = reserves, InitialReserves = new(reserves),
                ExpiresAt = now.AddSeconds(profile.Lifetime), RespawnsAt = now.AddSeconds(profile.Lifetime + profile.Respawn),
                NextHazardAt = profile.HazardSeconds > 0 ? now.AddSeconds(profile.HazardSeconds) : DateTime.MaxValue,
                HiddenResearchComponent = profile.Hardness >= 100 ? "space_precise" : null };
        }

        public static SpaceSite NewWreck(string id, string area, SpaceEncounterProfile profile, IEnumerable<string> participants, DateTime now)
        {
            var reserves = new Dictionary<string, double> { ["bulk"] = profile.BulkReserve, ["components"] = profile.ComponentAttempts };
            return new() { Id = id, AreaResref = area, Generation = Guid.NewGuid().ToString(), Kind = SpaceSiteKind.Wreck,
                Profile = profile.Name + " wreck", Blueprint = "space_wreck", MaximumShips = 4, Stability = 100,
                Reserves = reserves, InitialReserves = new(reserves), Participants = participants.ToHashSet(),
                ExclusiveUntil = now.AddSeconds(120), ExpiresAt = now.AddSeconds(600), RespawnsAt = DateTime.MaxValue,
                NextHazardAt = DateTime.MaxValue, DifficultComponent = profile.Gunnery >= 35,
                HiddenResearchComponent = profile.Gunnery >= 35 ? "space_precise" : null };
        }

        public static string Validate(SpaceSite site, string playerId, string shipId, ShipModuleOperation operation,
            double freeCargo, DateTime now, IReadOnlyDictionary<StatType, double> bonuses = null, IReadOnlyDictionary<StatType, double> temporary = null)
        {
            if (site == null || now >= site.ExpiresAt) return "That site is no longer available.";
            var action = operation.Profile.Action;
            if (site.Kind == SpaceSiteKind.Wreck && site.ExclusiveUntil > now && !site.Participants.Contains(playerId)) return "The wreck remains reserved for its encounter participants.";
            if (site.Claims.Values.Where(x => x.State == SpaceWorkState.Reserved && x.ShipId != shipId).Select(x => x.ShipId).Distinct().Count() >= site.MaximumShips) return "All working positions are occupied.";
            if (action == ShipModuleAction.Survey)
            {
                var resolution = operation.Output + (bonuses?.GetValueOrDefault(StatType.ShipScannerResolution) ?? 0) + (temporary?.GetValueOrDefault(StatType.ShipScannerResolution) ?? 0);
                var requiredResolution=site.InitialReserves.Keys.Where(x=>SpaceIndustryCatalog.Default.Commodities.ContainsKey(x)).Select(x=>SpaceIndustryCatalog.Default.Commodities[x].Resolution).DefaultIfEmpty(0).Max();
                if (resolution < requiredResolution) return $"This site requires scanner resolution {requiredResolution}.";
                return site.Claims.Values.Any(x => x.PlayerId == playerId && x.Action == action && x.State == SpaceWorkState.Reserved) ? "A scan is already underway." : null;
            }
            if (action == ShipModuleAction.Extraction)
            {
                if (site.Kind == SpaceSiteKind.Wreck) return "Use salvage equipment on this wreck.";
                if (now < site.VentsUntil) return "The deposit is venting. Extraction resumes after the warning clears.";
                var hardness = operation.Profile.HardnessLimit + (bonuses?.GetValueOrDefault(StatType.ShipExtractionHardness) ?? 0) + (temporary?.GetValueOrDefault(StatType.ShipExtractionHardness) ?? 0);
                if (site.Hardness > hardness) return $"This deposit needs hardness capacity {site.Hardness}.";
                if (site.Hardness >= 90 && !site.SurveyedBy.Contains(playerId)) return "Survey the deep deposit before extracting.";
            }
            else if (action is ShipModuleAction.BulkSalvage or ShipModuleAction.IntactSalvage)
            {
                if (site.Kind != SpaceSiteKind.Wreck) return "Use salvage equipment on a wreck.";
            }
            else return "That module cannot work this site.";
            var available = AvailableReserves(site, action);
            if (available <= 1e-9) return "The relevant reserve is exhausted.";
            var recovery = Recovery(operation, bonuses, temporary);
            var required = action == ShipModuleAction.IntactSalvage ? 1 : Math.Min(available, Removal(operation, bonuses, temporary)) * recovery;
            return required > freeCargo + 1e-9 ? "The ship does not have room for this recovery cycle." : null;
        }

        public static double Recovery(ShipModuleOperation operation, IReadOnlyDictionary<StatType, double> bonuses, IReadOnlyDictionary<StatType, double> temporary) =>
            Math.Clamp(operation.Variant.RecoveryFraction + (bonuses?.GetValueOrDefault(StatType.ShipResourceRecovery) ?? 0) + (temporary?.GetValueOrDefault(StatType.ShipResourceRecovery) ?? 0)
                + (operation.Profile.Action==ShipModuleAction.BulkSalvage ? (bonuses?.GetValueOrDefault(StatType.ShipSalvageRecovery)??0)+(temporary?.GetValueOrDefault(StatType.ShipSalvageRecovery)??0) : 0), 0, .95);
        public static double Removal(ShipModuleOperation operation, IReadOnlyDictionary<StatType, double> bonuses, IReadOnlyDictionary<StatType, double> temporary) =>
            ShipModuleTuning.Output(operation.Profile.Output, operation.Variant.Output,
                Math.Max(0, bonuses?.GetValueOrDefault(StatType.ShipReserveRemoval) ?? 0),
                Math.Max(0, -(bonuses?.GetValueOrDefault(StatType.ShipReserveRemoval) ?? 0)) + Math.Max(0, -(temporary?.GetValueOrDefault(StatType.ShipReserveRemoval) ?? 0)),
                new[] { Math.Max(0, temporary?.GetValueOrDefault(StatType.ShipReserveRemoval) ?? 0) });
        private static double AvailableReserves(SpaceSite site, ShipModuleAction action) => site.Reserves.Where(x =>
            action == ShipModuleAction.IntactSalvage ? x.Key == "components" : action == ShipModuleAction.BulkSalvage ? x.Key == "bulk" : x.Key.StartsWith("ore_", StringComparison.Ordinal)).Sum(x => x.Value);

        public static SpaceWorkClaim Reserve(SpaceSite site, string playerId, string shipId, string flightId, string moduleId,
            ShipModuleOperation operation, double freeCargo, DateTime now, IReadOnlyDictionary<StatType, double> bonuses = null,
            IReadOnlyDictionary<StatType, double> temporary = null, double? channelSeconds = null, string selectedConstituent = null)
        {
            if (site == null) throw new InvalidOperationException("Select a finite deposit or salvage site.");
            if (site.Claims.Values.Any(x => x.State == SpaceWorkState.Reserved && x.ShipId == shipId && x.ModuleId == moduleId))
                throw new InvalidOperationException("That tool already has a pending claim.");
            var error = Validate(site, playerId, shipId, operation, freeCargo, now, bonuses, temporary);
            if (error != null) throw new InvalidOperationException(error);
            var action = operation.Profile.Action;
            if (!site.DiscoveryDrawn && !string.IsNullOrWhiteSpace(site.HiddenResearchComponent) && (temporary?.GetValueOrDefault(StatType.ShipDiscoveryChance)??0)>0 && freeCargo < (action==ShipModuleAction.Survey?0:action==ShipModuleAction.IntactSalvage?1:Math.Min(AvailableReserves(site,action),Removal(operation,bonuses,temporary))*Recovery(operation,bonuses,temporary))+1-1e-9)
                throw new InvalidOperationException("Reserve one cargo unit for possible research recovery.");
            var allocation = new Dictionary<string, double>();
            if (action != ShipModuleAction.Survey)
            {
                var available = AvailableReserves(site, action);
                var removed = Math.Min(available, action == ShipModuleAction.IntactSalvage ? 1 : Removal(operation, bonuses, temporary));
                var selection=Math.Clamp(temporary?.GetValueOrDefault(StatType.ShipSelectedRecovery)??0,0,1);
                if (selection>0 && action==ShipModuleAction.Extraction)
                {
                    if (!site.SurveyedBy.Contains(playerId) || string.IsNullOrEmpty(selectedConstituent) || !site.Reserves.ContainsKey(selectedConstituent) || !selectedConstituent.StartsWith("ore_",StringComparison.Ordinal))
                        throw new InvalidOperationException("Select a constituent present in this surveyed site.");
                    var selected=Math.Min(site.Reserves[selectedConstituent],removed*selection);
                    var others=available-site.Reserves[selectedConstituent];
                    if (removed-selected>others) selected=Math.Min(site.Reserves[selectedConstituent],removed-others);
                    allocation[selectedConstituent]=selected;
                    foreach (var key in site.Reserves.Keys.Where(k=>k!=selectedConstituent && k.StartsWith("ore_",StringComparison.Ordinal)).ToArray())
                        allocation[key]=others>0?(removed-selected)*site.Reserves[key]/others:0;
                    foreach (var (key,quantity) in allocation) site.Reserves[key]=Math.Max(0,site.Reserves[key]-quantity);
                }
                else foreach (var key in site.Reserves.Keys.Where(k => action == ShipModuleAction.IntactSalvage ? k == "components" : action == ShipModuleAction.BulkSalvage ? k == "bulk" : k.StartsWith("ore_", StringComparison.Ordinal)).ToArray())
                {
                    var quantity = removed * site.Reserves[key] / available;
                    allocation[key] = quantity;
                    site.Reserves[key] = Math.Max(0, site.Reserves[key] - quantity);
                }
            }
            var claim = new SpaceWorkClaim { Id = Guid.NewGuid().ToString(), Generation = site.Generation, PlayerId = playerId, ShipId = shipId,
                FlightId = flightId, ModuleId = moduleId, Action = action, StartedAt = now, CompletesAt = now.AddSeconds(channelSeconds ?? operation.Variant.Cycle),
                Range = operation.Variant.Range, Allocations = allocation, Recovery = Recovery(operation, bonuses, temporary),
                Resolution = operation.Output + (bonuses?.GetValueOrDefault(StatType.ShipScannerResolution) ?? 0) + (temporary?.GetValueOrDefault(StatType.ShipScannerResolution) ?? 0),
                IntactChance = Math.Clamp(.35 + (bonuses?.GetValueOrDefault(StatType.ShipIntactSalvageChance) ?? 0) + (temporary?.GetValueOrDefault(StatType.ShipIntactSalvageChance) ?? 0), 0, .55),
                StabilityCost = operation.Profile.MovementLock || operation.Profile.WorkingSpeedPenalty > 0 ? 2 : 1,
                DiscoveryChance = !site.DiscoveryDrawn && !string.IsNullOrWhiteSpace(site.HiddenResearchComponent) ? Math.Clamp(temporary?.GetValueOrDefault(StatType.ShipDiscoveryChance)??0,0,.05) : 0 };
            site.Claims.Add(claim.Id, claim);
            return claim;
        }

        public static bool Cancel(SpaceSite site, string claimId)
        {
            if (!site.Claims.TryGetValue(claimId, out var claim) || claim.State != SpaceWorkState.Reserved) return false;
            if (claim.Generation == site.Generation)
                foreach (var (resource, quantity) in claim.Allocations)
                    site.Reserves[resource] = Math.Min(site.InitialReserves[resource], site.Reserves.GetValueOrDefault(resource) + quantity);
            site.Claims.Remove(claimId);
            return true;
        }

        public static bool Complete(SpaceSite site, string claimId, DateTime now, double roll, double discoveryRoll = 1)
        {
            if (!double.IsFinite(roll) || roll < 0 || roll >= 1) throw new ArgumentOutOfRangeException(nameof(roll));
            if (!double.IsFinite(discoveryRoll)||discoveryRoll<0||discoveryRoll>1) throw new ArgumentOutOfRangeException(nameof(discoveryRoll));
            if (!site.Claims.TryGetValue(claimId, out var claim) || claim.State != SpaceWorkState.Reserved || now < claim.CompletesAt) return false;
            if (claim.Generation != site.Generation || now >= site.ExpiresAt || now < site.VentsUntil)
            { Cancel(site, claimId); return false; }
            if (claim.Action == ShipModuleAction.Survey)
            {
                if (site.SurveyedBy.Add(claim.PlayerId)) claim.BaseXP = 150;
                site.SurveyResolution[claim.PlayerId] = Math.Max(site.SurveyResolution.GetValueOrDefault(claim.PlayerId), claim.Resolution);
            }
            else if (claim.Action == ShipModuleAction.IntactSalvage)
            {
                if (roll < claim.IntactChance) claim.Cargo.Add("elec_recover", 1);
                claim.BaseXP = 3;
            }
            else
            {
                foreach (var (resource, quantity) in claim.Allocations)
                    claim.Cargo.Add(resource == "bulk" ? "elec_ruined" : resource, quantity * claim.Recovery);
                claim.BaseXP = (int)Math.Floor(3 * claim.Allocations.Values.Sum() + 1e-8);
                if (claim.Action == ShipModuleAction.Extraction)
                {
                    site.Stability = Math.Max(0, site.Stability - claim.StabilityCost);
                    if (site.Stability <= 20) site.VentsUntil = now.AddSeconds(10);
                }
            }
            if (claim.DiscoveryChance>0 && !site.DiscoveryDrawn && !string.IsNullOrWhiteSpace(site.HiddenResearchComponent))
            {
                site.DiscoveryDrawn=true;
                if (discoveryRoll<claim.DiscoveryChance) claim.Cargo[site.HiddenResearchComponent]=1;
            }
            claim.State = SpaceWorkState.Completed;
            return true;
        }

        public static void SettleCargo(ShipStatus status, SpaceWorkClaim claim)
        {
            if (claim.State != SpaceWorkState.Completed) throw new InvalidOperationException("Only completed claims settle.");
            if (!status.SettledSiteClaims.Add(claim.Id)) return;
            foreach (var (resref, quantity) in claim.Cargo.Where(x => x.Value > 0))
                status.Cargo.Add(claim.Id + "/" + resref, new ShipCargoLot(resref, quantity));
        }
    }
}
