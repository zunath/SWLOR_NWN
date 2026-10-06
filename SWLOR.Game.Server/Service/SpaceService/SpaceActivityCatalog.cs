using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using SWLOR.Game.Server.Service.SkillService;
namespace SWLOR.Game.Server.Service.SpaceService
{
    public sealed record SpaceActivityProfile
    {
        public string Id { get; init; }
        public string Name { get; init; }
        public string Region { get; init; }
        public string Objective { get; init; }
        public int Party { get; init; }
        public int Credits { get; init; }
        public int XP { get; init; }
        public double Piloting { get; init; }
        public double Gunnery { get; init; }
        public double Systems { get; init; }
        public double Astro { get; init; }
        public double Industry { get; init; }
        public int Legs { get; init; }
        public List<string> Kills { get; init; } = new();
        public int Ore { get; init; }
        public int Reserve { get; init; }
        public Dictionary<string,double> Composition { get; init; } = new();
        public int Surveys { get; init; }
        public int Wrecks { get; init; }
        public int Bulk { get; init; }
        public int Attempts { get; init; }
        public int Freight { get; init; }
        public int Rescue { get; init; }
        public bool Boarding { get; init; }
        [JsonProperty("boarding_target")] public string BoardingTarget {get;init;}
        [JsonProperty("boarding_consoles")] public int BoardingConsoles {get;init;}
        [JsonProperty("boarding_console_seconds")] public int BoardingConsoleSeconds {get;init;}
        [JsonProperty("boarding_seconds")] public int BoardingSeconds {get;init;}
        [JsonProperty("boarding_preparation")] public int BoardingPreparation {get;init;}
        [JsonProperty("boarding_range")] public int BoardingRange {get;init;}
        [JsonProperty("boarding_hull_fraction")] public double BoardingHullFraction {get;init;}
        [JsonProperty("boarding_precision_per_console")] public int BoardingPrecisionPerConsole {get;init;}
        [JsonProperty("boarding_ore_per_console")] public int BoardingOrePerConsole {get;init;}
        public int Reputation { get; init; }
        [JsonProperty("minimum_seconds")] public int MinimumSeconds { get; init; }
        [JsonProperty("expiry_seconds")] public int ExpirySeconds { get; init; }
        [JsonProperty("leg_interval")] public int LegInterval { get; init; }
        [JsonProperty("leg_radius")] public int LegRadius { get; init; }
        [JsonProperty("route_minimum_distance")] public int MinimumRouteDistance { get; init; }
        [JsonProperty("load_seconds")] public int LoadingSeconds { get; init; }
        [JsonProperty("unload_seconds")] public int UnloadingSeconds { get; init; }
        [JsonProperty("freight_deposit")] public int FreightDeposit { get; init; }
        [JsonProperty("navigation_fee")] public int NavigationFee { get; init; }
        public IReadOnlyDictionary<SkillType,int> SkillPools => new Dictionary<SkillType,int>
        { [SkillType.Piloting]=(int)Math.Floor(XP*Piloting),[SkillType.Gunnery]=(int)Math.Floor(XP*Gunnery),
          [SkillType.ShipSystems]=(int)Math.Floor(XP*Systems),[SkillType.Astrometrics]=(int)Math.Floor(XP*Astro),[SkillType.SpaceIndustry]=(int)Math.Floor(XP*Industry) };
    }
    public sealed class SpaceActivityCatalog
    {
        private static readonly Lazy<SpaceActivityCatalog> _default=new(()=>
        {
            using var stream=typeof(SpaceActivityCatalog).Assembly.GetManifestResourceStream("SWLOR.SpaceActivities.json")??throw new InvalidDataException("Missing space contracts.");
            using var reader=new StreamReader(stream);return Load(reader.ReadToEnd());
        });
        public static SpaceActivityCatalog Default=>_default.Value;
        public IReadOnlyDictionary<string,SpaceActivityProfile> Profiles { get; }
        private SpaceActivityCatalog(List<SpaceActivityProfile> rows)
        {
            if(rows?.Count!=11)throw new InvalidDataException("Expected eleven space activities.");
            foreach(var row in rows)
                if(string.IsNullOrWhiteSpace(row.Id)||row.Party<1||row.Party>4||row.MinimumSeconds<=0||row.ExpirySeconds<=row.MinimumSeconds||row.Legs!=3||row.LegInterval*row.Legs!=row.MinimumSeconds||row.LegRadius<=0||row.MinimumRouteDistance<=row.LegRadius||row.Credits<0||row.XP<=0||row.SkillPools.Values.Sum()>row.XP||row.Kills.Any(x=>!SpaceEncounterCatalog.Default.Profiles.ContainsKey(x))||row.Composition.Values.Sum()>1.0000001)
                    throw new InvalidDataException("Invalid space contract: "+row.Id);
            Profiles=new ReadOnlyDictionary<string,SpaceActivityProfile>(rows.ToDictionary(x=>x.Id));
        }
        public static SpaceActivityCatalog Load(string json)=>new(JsonConvert.DeserializeObject<List<SpaceActivityProfile>>(json));
    }
}
