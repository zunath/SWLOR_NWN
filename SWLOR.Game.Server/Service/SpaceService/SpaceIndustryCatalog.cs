using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace SWLOR.Game.Server.Service.SpaceService
{
    public sealed record SpaceDepositProfile
    {
        public string Name { get; init; }
        public int Ships { get; init; }
        public int Reserve { get; init; }
        public Dictionary<string, double> Composition { get; init; }
        public int Hardness { get; init; }
        public int Stability { get; init; }
        [JsonProperty("hazard_seconds")] public int HazardSeconds { get; init; }
        [JsonProperty("hazard_damage")] public int HazardDamage { get; init; }
        [JsonProperty("scan_seconds")] public int ScanSeconds { get; init; }
        public int Lifetime { get; init; }
        public int Respawn { get; init; }
    }
    public sealed record SpaceCommodityProfile(string Ore, string Metal, int Cargo, int Bid, int Hardness, int Resolution);

    public sealed class SpaceIndustryCatalog
    {
        private sealed record Data(List<SpaceDepositProfile> Deposits, List<SpaceCommodityProfile> Resources);
        private static readonly Lazy<SpaceIndustryCatalog> _default = new(() =>
        {
            using var stream = typeof(SpaceIndustryCatalog).Assembly.GetManifestResourceStream("SWLOR.SpaceIndustry.json");
            if (stream == null) throw new InvalidDataException("Missing space industry definitions.");
            using var reader = new StreamReader(stream);
            return Load(reader.ReadToEnd());
        });
        public static SpaceIndustryCatalog Default => _default.Value;
        public IReadOnlyList<SpaceDepositProfile> Deposits { get; }
        public IReadOnlyDictionary<string, SpaceCommodityProfile> Commodities { get; }
        private SpaceIndustryCatalog(Data data)
        {
            if (data?.Deposits == null || data.Resources == null || data.Deposits.Count != 6 || data.Resources.Count != 6)
                throw new InvalidDataException("Incomplete finite space industry definitions.");
            Deposits = data.Deposits.AsReadOnly();
            Commodities = new ReadOnlyDictionary<string, SpaceCommodityProfile>(data.Resources.ToDictionary(x => x.Ore));
            foreach (var deposit in Deposits)
                if (deposit.Reserve <= 0 || deposit.Ships <= 0 || deposit.Hardness < 0 || deposit.Lifetime <= 0 || deposit.Respawn <= 0 ||
                    deposit.Composition == null || deposit.Composition.Any(x => !Commodities.ContainsKey(x.Key) || !double.IsFinite(x.Value) || x.Value <= 0) ||
                    Math.Abs(deposit.Composition.Values.Sum() - 1) > 1e-9)
                    throw new InvalidDataException("Invalid deposit: " + deposit.Name);
        }
        public static SpaceIndustryCatalog Load(string json) => new(JsonConvert.DeserializeObject<Data>(json));
        public static bool IsRegionTable(string table) => table?.StartsWith("SPACE_RESOURCES_", StringComparison.Ordinal) == true;
    }
}
