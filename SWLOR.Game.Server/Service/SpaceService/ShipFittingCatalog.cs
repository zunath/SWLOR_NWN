using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.SpaceService
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ShipMount { Any, Compact, Standard, Heavy, Industrial, Ordnance }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum ShipFittingSlot { High, Low }

    [Flags, JsonConverter(typeof(StringEnumConverter))]
    public enum ShipQualityDimension
    {
        None = 0, Output = 1, Tracking = 2, Range = 4,
        RecoveryFraction = 8, ActivationCost = 16, CycleDuration = 32
    }

    public sealed record ShipHullProfile
    {
        [JsonProperty("id")] public string Id { get; init; }
        [JsonProperty("name")] public string Name { get; init; }
        [JsonProperty("role")] public string Role { get; init; }
        [JsonProperty("piloting")] public int PilotingRank { get; init; }
        [JsonProperty("high")] public int HighSlots { get; init; }
        [JsonProperty("low")] public int LowSlots { get; init; }
        [JsonProperty("power")] public int Power { get; init; }
        [JsonProperty("hull")] public int Hull { get; init; }
        [JsonProperty("shield")] public int Shield { get; init; }
        [JsonProperty("capacitor")] public int Capacitor { get; init; }
        [JsonProperty("cap_regen")] public double CapacitorRecovery { get; init; }
        [JsonProperty("shield_regen")] public double ShieldRecovery { get; init; }
        [JsonProperty("speed")] public double Speed { get; init; }
        [JsonProperty("signature")] public double Signature { get; init; }
        [JsonProperty("cargo")] public int Cargo { get; init; }
        [JsonProperty("resistance")] public int Resistance { get; init; }
        [JsonProperty("value")] public int ReferenceValue { get; init; }
        [JsonProperty("mount", Required = Required.Always)] public string Mounts { get; init; }

        public bool Allows(ShipMount mount) =>
            mount == ShipMount.Any ||
            Mounts.Split('/').Contains(mount.ToString(), StringComparer.Ordinal) ||
            (mount == ShipMount.Ordnance && Mounts.Split('/').Contains(nameof(ShipMount.Heavy)));
    }

    public sealed record ShipModuleProfile
    {
        [JsonProperty("id")] public string Id { get; init; }
        [JsonProperty("name")] public string Name { get; init; }
        [JsonProperty("family")] public string Family { get; init; }
        [JsonProperty("slot", Required = Required.Always)] public ShipFittingSlot Slot { get; init; }
        [JsonProperty("mount", Required = Required.Always)] public ShipMount Mount { get; init; }
        [JsonProperty("power")] public int Power { get; init; }
        [JsonProperty("output")] public double Output { get; init; }
        [JsonProperty("cycle")] public double Cycle { get; init; }
        [JsonProperty("capacitor")] public int Capacitor { get; init; }
        [JsonProperty("range")] public double Range { get; init; }
        [JsonProperty("tracking")] public double Tracking { get; init; }
        [JsonProperty("resolution")] public double Resolution { get; init; }
        [JsonProperty("engineering")] public int EngineeringRank { get; init; }
        [JsonProperty("value")] public int ReferenceValue { get; init; }
        [JsonProperty("effect")] public string Effect { get; init; }
        [JsonProperty("recovery_fraction")] public double RecoveryFraction { get; init; }
        [JsonProperty("operator_skill", Required = Required.Always), JsonConverter(typeof(StringEnumConverter))]
        public SkillType OperatorSkill { get; init; }
        [JsonProperty("operator_rank", Required = Required.Always)] public int OperatorRank { get; init; }
        [JsonProperty("max_fitted", Required = Required.Always)] public int MaxFitted { get; init; }
        [JsonProperty("quality_dimensions", Required = Required.Always)] public ShipQualityDimension QualityDimensions { get; init; }
    }

    public sealed record ShipModuleVariant
    {
        [JsonProperty("design")] public string Design { get; init; }
        [JsonProperty("capacitor_multiplier", Required = Required.Always)] public double CapacitorMultiplier { get; init; }
        [JsonProperty("calibration")] public string Calibration { get; init; }
        [JsonProperty("power")] public int Power { get; init; }
        [JsonProperty("output")] public double Output { get; init; }
        [JsonProperty("cycle")] public double Cycle { get; init; }
        [JsonProperty("capacitor")] public int Capacitor { get; init; }
        [JsonProperty("range")] public double Range { get; init; }
        [JsonProperty("tracking")] public double Tracking { get; init; }
        [JsonProperty("recovery_fraction")] public double RecoveryFraction { get; init; }
        [JsonProperty("engineering")] public int EngineeringRank { get; init; }
    }

    public sealed record ShipConfigurationProfile
    {
        [JsonProperty("id")] public string Id { get; init; }
        [JsonProperty("name")] public string Name { get; init; }
        [JsonProperty("power")] public int Power { get; init; }
        [JsonProperty("benefit")] public string Benefit { get; init; }
        [JsonProperty("drawback")] public string Drawback { get; init; }
    }

    public sealed class ShipFittingCatalog
    {
        private sealed class Data
        {
            public List<ShipHullProfile> Hulls { get; set; }
            public List<ShipModuleProfile> Modules { get; set; }
            public List<ShipModuleVariant> Variants { get; set; }
            public List<ShipConfigurationProfile> Configurations { get; set; }
        }

        private static readonly Lazy<ShipFittingCatalog> _default = new(() =>
        {
            using var stream = typeof(ShipFittingCatalog).Assembly.GetManifestResourceStream("SWLOR.ShipFitting.json");
            if (stream == null) throw new InvalidDataException("Missing embedded ship fitting definitions.");
            using var reader = new StreamReader(stream);
            return Load(reader.ReadToEnd());
        });

        public static ShipFittingCatalog Default => _default.Value;
        public IReadOnlyDictionary<string, ShipHullProfile> Hulls { get; }
        public IReadOnlyDictionary<string, ShipModuleProfile> Modules { get; }
        public IReadOnlyDictionary<string, ShipConfigurationProfile> Configurations { get; }
        public IReadOnlyDictionary<(string Design, string Calibration), ShipModuleVariant> Variants { get; }

        private ShipFittingCatalog(Data data)
        {
            if (data?.Hulls == null || data.Modules == null || data.Variants == null || data.Configurations == null)
                throw new InvalidDataException("Incomplete ship fitting definitions.");
            Hulls = new ReadOnlyDictionary<string, ShipHullProfile>(data.Hulls.ToDictionary(x => x.Id, StringComparer.Ordinal));
            Modules = new ReadOnlyDictionary<string, ShipModuleProfile>(data.Modules.ToDictionary(x => x.Id, StringComparer.Ordinal));
            Configurations = new ReadOnlyDictionary<string, ShipConfigurationProfile>(data.Configurations.ToDictionary(x => x.Id, StringComparer.Ordinal));
            Variants = new ReadOnlyDictionary<(string, string), ShipModuleVariant>(data.Variants.ToDictionary(x => (x.Design, x.Calibration)));
            Validate();
        }

        public static ShipFittingCatalog Load(string json) => new(JsonConvert.DeserializeObject<Data>(json));
        public ShipModuleVariant GetVariant(string design, string calibration = "Standard") =>
            Variants.TryGetValue((design, calibration), out var variant) ? variant :
                throw new ArgumentException($"Unsupported module calibration: {design}/{calibration}.");

        private void Validate()
        {
            if (Hulls.Count == 0 || Modules.Count == 0) throw new InvalidDataException("Empty ship fitting catalogue.");
            foreach (var hull in Hulls.Values)
            {
                if (string.IsNullOrWhiteSpace(hull.Name) || string.IsNullOrWhiteSpace(hull.Role) ||
                    hull.PilotingRank < 0 || hull.PilotingRank > 50 || hull.Power <= 0 || hull.Hull <= 0 ||
                    hull.Shield < 0 || hull.Capacitor <= 0 || hull.HighSlots < 0 || hull.LowSlots < 0 ||
                    hull.Cargo < 0 || !Positive(hull.Speed) || !Positive(hull.Signature) ||
                    !NonNegative(hull.CapacitorRecovery) || !NonNegative(hull.ShieldRecovery) ||
                    string.IsNullOrWhiteSpace(hull.Mounts) ||
                    hull.Mounts.Split('/').Any(x => !Enum.TryParse<ShipMount>(x, out var mount) || !Enum.IsDefined(mount) || mount == ShipMount.Any))
                    throw new InvalidDataException($"Invalid hull profile: {hull.Id}.");
            }
            var operatorSkills = new[] { SkillType.Piloting, SkillType.Gunnery, SkillType.ShipSystems, SkillType.Astrometrics, SkillType.SpaceIndustry };
            foreach (var module in Modules.Values)
            {
                if (!Enum.IsDefined(module.Mount) || !Enum.IsDefined(module.Slot) ||
                    !NonNegative(module.RecoveryFraction) || module.RecoveryFraction > 0.95 ||
                    !operatorSkills.Contains(module.OperatorSkill) || module.OperatorRank < 0 || module.OperatorRank > 50 ||
                    module.Power <= 0 || module.Capacitor < 0 || module.MaxFitted < 0 ||
                    !NonNegative(module.Output) || !NonNegative(module.Cycle) || !NonNegative(module.Range) ||
                    !NonNegative(module.Tracking) || !NonNegative(module.Resolution) ||
                    module.EngineeringRank < 0 || module.EngineeringRank > 50)
                    throw new InvalidDataException($"Invalid module profile: {module.Id}.");
                GetVariant(module.Id);
            }
            foreach (var variant in Variants.Values)
            {
                if (!Modules.TryGetValue(variant.Design, out var module) || string.IsNullOrWhiteSpace(variant.Calibration) ||
                    !Positive(variant.CapacitorMultiplier) || variant.Power <= 0 || variant.Capacitor < 0 ||
                    (module.Capacitor > 0 && variant.Capacitor < Math.Ceiling(module.Capacitor * 0.75)) ||
                    !NonNegative(variant.Output) || !NonNegative(variant.Cycle) || variant.Cycle < module.Cycle * 0.85 ||
                    !NonNegative(variant.Range) || !NonNegative(variant.Tracking) ||
                    !NonNegative(variant.RecoveryFraction) || variant.RecoveryFraction > 0.95 ||
                    variant.EngineeringRank < 0 || variant.EngineeringRank > 50)
                    throw new InvalidDataException($"Invalid module variant: {variant.Design}/{variant.Calibration}.");
            }
            if (Configurations.Values.Any(x => x.Power <= 0)) throw new InvalidDataException("Invalid configuration fitting power.");
        }

        private static bool NonNegative(double value) => double.IsFinite(value) && value >= 0;
        private static bool Positive(double value) => double.IsFinite(value) && value > 0;
    }
}
