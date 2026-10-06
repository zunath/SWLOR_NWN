using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service.SpaceService
{
    // Operator cooldowns intentionally survive docking, refitting, preparation and ship changes.
    public sealed class ShipOperatingState
    {
        public List<string> Prepared { get; set; } = new();
        public string SelectedMode { get; set; }
        public int SelectedBank { get; set; } = 1;
        public string SelectedConstituent { get; set; }
        public string SelectedToolId { get; set; }
        public Dictionary<string, DateTime> Cooldowns { get; set; } = new();
        public DateTime ReadyAt { get; set; }
        public DateTime CommittedUntil { get; set; }
        public string PendingTechniqueId { get; set; }
    }
    public sealed record ShipTechniqueContext(bool InSpace, bool SameArea, bool Hostile, bool Allied, bool Site,
        bool Anomaly, bool Surveyed, bool Exposed, double Distance, double TargetSignature, bool PaidLeg, bool ChannelActive);

    public static class ShipTechniquePolicy
    {
        public static string Validate(ShipStatus status, ShipOperatingState state, ShipTechniqueProfile profile,
            ShipTechniqueContext context, DateTime now, IEnumerable<ShipStatus.ShipStatusModule> hardware)
        {
            if (!context.InSpace || status == null || status.Hull <= 0 || status.FittingVersion != ShipFittingConversion.CurrentVersion) return "Pilot an operational ship to use this technique.";
            if (now < state.ReadyAt || now < status.RefitReadyAt || status.PendingInventoryTransfers.Count > 0 || status.PendingCargoTransfers.Count > 0) return "Operating preparation is still settling.";
            if (context.ChannelActive || state.CommittedUntil > now) return "Finish the committed operation first.";
            if (ShipTemporaryStats.Current(status,now).GetValueOrDefault(StatType.ShipActivationLock)>0) return "Module activation is disrupted.";
            if (profile.Kind != ShipPerkKind.Mode && !state.Prepared.Contains(profile.Key)) return "Prepare this technique at a dock first.";
            if (state.Cooldowns.GetValueOrDefault(profile.Recast.ToString()) > now || status.GlobalRecast > now) return "This operating action is not ready.";
            if (ShipResources.Available(status,ShipResource.Capacitor)+1e-9 < Cost(status,profile,now)) return "Insufficient capacitor for the technique.";
            if (profile.RequiresPaidLeg && !context.PaidLeg) return "Commit to a paid transit leg first.";
            if (profile.Actions.Count > 0 && !hardware.Any()) return "Fit compatible, operational hardware and select its bank first.";
            if (profile.Selection && string.IsNullOrEmpty(state.SelectedConstituent)) return "Select a surveyed constituent in the cockpit first.";
            if (profile.Target != ShipTechniqueTarget.Self && (!context.SameArea || !double.IsFinite(context.Distance) || context.Distance < 0 || (profile.Range>0 && context.Distance>profile.Range))) return "Select a target within the technique's range.";
            if (profile.Target is ShipTechniqueTarget.Pursuit or ShipTechniqueTarget.Hostile or ShipTechniqueTarget.ExposedHostile)
                if (!context.Hostile) return "Select a hostile ship.";
            if (profile.Target==ShipTechniqueTarget.ExposedHostile && !context.Exposed) return "The target has no exposed system.";
            if (profile.Target==ShipTechniqueTarget.Allied && !context.Allied) return "Select another allied ship.";
            if (profile.Target is ShipTechniqueTarget.Site or ShipTechniqueTarget.Anomaly or ShipTechniqueTarget.SurveyedSite)
                if (!context.Site) return "Select a finite resource or salvage site.";
            if (profile.Target==ShipTechniqueTarget.Anomaly && !context.Anomaly) return "Select an authored anomaly.";
            if (profile.Target==ShipTechniqueTarget.SurveyedSite && !context.Surveyed) return "Survey this site before selecting its constituent.";
            if (profile.MinimumSignature>context.TargetSignature) return $"Target signature must be at least {profile.MinimumSignature:0}.";
            return null;
        }
        public static int Cost(ShipStatus status, ShipTechniqueProfile profile, DateTime now)
        {
            if (profile.Capacitor==0) return 0;
            var discount=ShipFittedStats.Bonus(status,StatType.ShipCapacitorDiscount);
            if (profile.Effects.Any(x=>x.Stat is StatType.ShipSpeed or StatType.ShipSoftControlImmunity)) discount+=ShipFittedStats.Bonus(status,StatType.ShipPropulsionCapacitorDiscount);
            var temporary=ShipTemporaryStats.Current(status,now);
            discount+=temporary.GetValueOrDefault(StatType.ShipCapacitorDiscount);
            return ShipModuleTuning.CapacitorCost(profile.Capacitor,1,discount,0);
        }
        public static void Pay(ShipStatus status, ShipOperatingState state, ShipTechniqueProfile profile, DateTime now)
        {
            var cost=Cost(status,profile,now);
            if (ShipResources.Available(status,ShipResource.Capacitor)+1e-9 < cost) throw new InvalidOperationException("Insufficient capacitor.");
            ShipResources.SpendPrecise(status,ShipResource.Capacitor,cost);
            state.Cooldowns[profile.Recast.ToString()]=now.AddSeconds(profile.Cooldown);
            status.GlobalRecast=now.AddSeconds(1);
        }
        public static void Prepare(ShipOperatingState state, IEnumerable<ShipTechniqueProfile> prepared, ShipTechniqueProfile mode, DateTime now)
        {
            var list=prepared.ToList();
            if (list.Count>4 || list.Select(x=>x.Key).Distinct().Count()!=list.Count || list.Any(x=>x.Kind is not (ShipPerkKind.Technique or ShipPerkKind.Capstone)) || list.Count(x=>x.Kind==ShipPerkKind.Capstone)>1)
                throw new InvalidOperationException("Prepare up to four techniques, including at most one capstone.");
            if (mode!=null && mode.Kind!=ShipPerkKind.Mode) throw new InvalidOperationException("Select one purchased operating mode.");
            if (state.CommittedUntil>now) throw new InvalidOperationException("Finish the committed operation first.");
            state.Prepared=list.Select(x=>x.Key).ToList();state.SelectedMode=mode?.Key;state.ReadyAt=now.AddSeconds(5);
        }
    }
}
