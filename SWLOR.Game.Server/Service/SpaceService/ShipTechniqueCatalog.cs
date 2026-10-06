using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.SpaceService
{
    [JsonConverter(typeof(StringEnumConverter))] public enum ShipPerkKind { Technique, Trait, Mode, Capstone }
    [JsonConverter(typeof(StringEnumConverter))] public enum ShipTechniqueTarget { Self, Pursuit, Hostile, ExposedHostile, Allied, Site, Anomaly, SurveyedSite }
    [JsonConverter(typeof(StringEnumConverter))] public enum ShipEffectScope { Self, Hardware, Target }
    public sealed record ShipTechniqueEffect
    {
        [JsonProperty("stat"), JsonConverter(typeof(StringEnumConverter))] public StatType Stat { get; init; }
        [JsonProperty("amount")] public double Amount { get; init; }
        [JsonProperty("seconds")] public double Seconds { get; init; }
        [JsonProperty("scope")] public ShipEffectScope Scope { get; init; }
        [JsonProperty("once")] public bool Once { get; init; }
        [JsonProperty("allies_only")] public bool AlliesOnly { get; init; }
    }
    public sealed record ShipTechniqueProfile
    {
        [JsonProperty("key")] public string Key { get; init; }
        [JsonProperty("name")] public string Name { get; init; }
        [JsonProperty("kind")] public ShipPerkKind Kind { get; init; }
        [JsonProperty("skill"), JsonConverter(typeof(StringEnumConverter))] public SkillType Skill { get; init; }
        [JsonProperty("style")] public string Style { get; init; }
        [JsonProperty("rank")] public int Rank { get; init; }
        [JsonProperty("skill_rank")] public int SkillRank { get; init; }
        [JsonProperty("price")] public int Price { get; init; }
        [JsonProperty("capacitor")] public int Capacitor { get; init; }
        [JsonProperty("cooldown")] public double Cooldown { get; init; }
        [JsonProperty("description")] public string Description { get; init; }
        [JsonProperty("target")] public ShipTechniqueTarget Target { get; init; }
        [JsonProperty("range")] public double Range { get; init; }
        [JsonProperty("actions")] public IReadOnlyList<ShipModuleAction> Actions { get; init; }
        [JsonProperty("designs")] public IReadOnlyList<string> Designs { get; init; }
        [JsonProperty("effects")] public IReadOnlyList<ShipTechniqueEffect> Effects { get; init; }
        [JsonProperty("stats")] public IReadOnlyDictionary<StatType, double> Stats { get; init; }
        [JsonProperty("bank")] public bool Bank { get; init; }
        [JsonProperty("ordnance")] public bool Ordnance { get; init; }
        [JsonProperty("preparation")] public double Preparation { get; init; }
        [JsonProperty("channel")] public double Channel { get; init; }
        [JsonProperty("discovery")] public bool Discovery { get; init; }
        [JsonProperty("min_signature")] public double MinimumSignature { get; init; }
        [JsonProperty("paid_leg")] public bool RequiresPaidLeg { get; init; }
        [JsonProperty("hard_control")] public bool HardControl { get; init; }
        [JsonProperty("movement_lock")] public bool MovementLock { get; init; }
        [JsonProperty("selection")] public bool Selection { get; init; }
        [JsonProperty("difficult_component")] public bool DifficultComponent { get; init; }
        [JsonProperty("perk"), JsonConverter(typeof(StringEnumConverter))] public PerkType Perk { get; init; }
        [JsonProperty("feat"), JsonConverter(typeof(StringEnumConverter))] public FeatType Feat { get; init; }
        [JsonProperty("recast"), JsonConverter(typeof(StringEnumConverter))] public RecastGroup Recast { get; init; }
        [JsonProperty("icon")] public string Icon { get; init; }

        public bool Allows(ShipModuleProfile module) => (Actions.Count == 0 || Actions.Contains(module.Action)) &&
            (Designs.Count == 0 || Designs.Contains(module.Id)) && (!Ordnance || module.Family == "Ordnance");
    }
    public sealed class ShipTechniqueCatalog
    {
        public static ShipTechniqueCatalog Default { get; } = Load();
        public IReadOnlyList<ShipTechniqueProfile> Profiles { get; }
        private readonly IReadOnlyDictionary<FeatType, ShipTechniqueProfile> _active;
        private ShipTechniqueCatalog(List<ShipTechniqueProfile> profiles)
        {
            foreach (var p in profiles)
            {
                if (p.Rank < 1 || p.Rank > 3 || p.SkillRank < 0 || p.SkillRank > 50 || p.Price < 1 || p.Capacitor < 0 ||
                    !double.IsFinite(p.Cooldown) || p.Cooldown < 0 || !double.IsFinite(p.Range) || p.Range < 0 ||
                    p.Actions == null || p.Designs == null || p.Effects == null || p.Stats == null || string.IsNullOrWhiteSpace(p.Key) ||
                    p.Effects.Any(x => !ShipFittedStats.StatUnits.ContainsKey(x.Stat) || !double.IsFinite(x.Amount) || !double.IsFinite(x.Seconds) || x.Seconds <= 0) ||
                    p.Stats.Any(x => !ShipFittedStats.StatUnits.ContainsKey(x.Key) || !double.IsFinite(x.Value)) ||
                    p.Designs.Any(x => !ShipFittingCatalog.Default.Modules.ContainsKey(x)))
                    throw new InvalidDataException("Invalid operating perk metadata: " + p.Name);
            }
            Profiles = profiles.Select(p => p with { Actions = Array.AsReadOnly(p.Actions.ToArray()), Designs = Array.AsReadOnly(p.Designs.ToArray()),
                Effects = Array.AsReadOnly(p.Effects.ToArray()), Stats = new ReadOnlyDictionary<StatType,double>(p.Stats.ToDictionary(x=>x.Key,x=>x.Value)) }).ToList().AsReadOnly();
            _active = new ReadOnlyDictionary<FeatType,ShipTechniqueProfile>(Profiles.Where(x => x.Kind != ShipPerkKind.Trait).ToDictionary(x=>x.Feat));
            if (Profiles.Count != 170 || Profiles.GroupBy(x=>x.Key).Count()!=70 || _active.Count!=110)
                throw new InvalidDataException("The operating perk catalog is incomplete.");
        }
        public ShipTechniqueProfile Get(FeatType feat) => _active[feat];
        public ShipTechniqueProfile Highest(string key, int rank) => Profiles.FirstOrDefault(x=>x.Key==key && x.Rank==rank);
        private static ShipTechniqueCatalog Load()
        {
            using var stream = typeof(ShipTechniqueCatalog).Assembly.GetManifestResourceStream("SWLOR.ShipTechniques.json");
            using var reader = new StreamReader(stream ?? throw new InvalidOperationException("Ship techniques are missing."));
            return new(JsonConvert.DeserializeObject<List<ShipTechniqueProfile>>(reader.ReadToEnd()));
        }
    }
}
