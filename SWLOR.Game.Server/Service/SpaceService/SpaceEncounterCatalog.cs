using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service.SpaceService
{
    public sealed record SpaceEncounterProfile
    {
        public string Id { get; init; }
        public string Name { get; init; }
        public int Hull { get; init; }
        public int Shield { get; init; }
        public int Resistance { get; init; }
        [JsonProperty("shield_resistance")] public int ShieldResistance { get; init; }
        public double Speed { get; init; }
        public double Signature { get; init; }
        public int Gunnery { get; init; }
        public int Piloting { get; init; }
        public int Agility { get; init; }
        public double Damage { get; init; }
        public double Cycle { get; init; }
        public int Capacitor { get; init; }
        public double Regen { get; init; }
        public double Accuracy { get; init; }
        [JsonProperty("capacitor_pool")] public int CapacitorPool { get; init; }
        public int Weapons { get; init; }
        public double Tracking { get; init; }
        public double Resolution { get; init; }
        public double Range { get; init; }
        [JsonProperty("control_seconds")] public double ControlSeconds { get; init; }
        [JsonProperty("preparation_seconds")] public double PreparationSeconds { get; init; }
        public string Ammunition { get; init; }
        [JsonProperty("ammunition_per_weapon")] public int AmmunitionPerWeapon { get; init; }
        [JsonProperty("reward_credit")] public int RewardCredits { get; init; }
        [JsonProperty("xp_pool")] public int Experience { get; init; }
        [JsonProperty("exposed_seconds")] public double ExposedSeconds { get; init; }
        [JsonProperty("bulk_reserve")] public int BulkReserve { get; init; }
        [JsonProperty("component_attempts")] public int ComponentAttempts { get; init; }
    }
    public sealed record SpaceEncounterBinding(string Tag, string Ship, string Name, string Profile);
    public sealed class SpaceEncounterCatalog
    {
        private sealed record Data(List<SpaceEncounterProfile> Encounters, List<SpaceEncounterBinding> Bindings);
        private static readonly Lazy<SpaceEncounterCatalog> _default = new(() =>
        {
            using var stream = typeof(SpaceEncounterCatalog).Assembly.GetManifestResourceStream("SWLOR.SpaceEncounters.json");
            if (stream == null) throw new InvalidDataException("Missing space encounter definitions.");
            using var reader = new StreamReader(stream);
            return Load(reader.ReadToEnd());
        });
        public static SpaceEncounterCatalog Default => _default.Value;
        public IReadOnlyDictionary<string, SpaceEncounterProfile> Profiles { get; }
        public IReadOnlyDictionary<string, SpaceEncounterBinding> Bindings { get; }
        private SpaceEncounterCatalog(Data data)
        {
            if (data?.Encounters?.Count != 8 || data.Bindings?.Count != 51)
                throw new InvalidDataException("Incomplete space encounter definitions.");
            Profiles = new ReadOnlyDictionary<string, SpaceEncounterProfile>(data.Encounters.ToDictionary(x => x.Id));
            Bindings = new ReadOnlyDictionary<string, SpaceEncounterBinding>(data.Bindings.ToDictionary(x => x.Tag));
            foreach (var profile in Profiles.Values)
                if (string.IsNullOrWhiteSpace(profile.Id) || profile.Hull <= 0 || profile.Shield < 0 || profile.CapacitorPool <= 0 ||
                    profile.Weapons is < 1 or > 5 || profile.Gunnery is < 0 or > 50 || profile.Piloting is < 0 or > 50 || profile.Agility is < 10 or > 26 ||
                    !double.IsFinite(profile.Damage) || profile.Damage <= 0 || !double.IsFinite(profile.Cycle) || profile.Cycle < 1 ||
                    !double.IsFinite(profile.Range) || profile.Range <= 0 || !double.IsFinite(profile.Speed) || profile.Speed <= 0 ||
                    profile.ControlSeconds is < 0 or > 3 || profile.Resistance is < 0 or > 60 || profile.ShieldResistance is < 0 or > 60 ||
                    profile.AmmunitionPerWeapon < 0 || profile.BulkReserve <= 0 || profile.ComponentAttempts <= 0)
                    throw new InvalidDataException("Invalid encounter profile: " + profile.Id);
            if (Bindings.Values.Any(x => !Profiles.ContainsKey(x.Profile) || string.IsNullOrWhiteSpace(x.Ship)))
                throw new InvalidDataException("Encounter binding references an unknown profile.");
        }
        public static SpaceEncounterCatalog Load(string json) => new(JsonConvert.DeserializeObject<Data>(json));
        public ShipStatus CreateStatus(SpaceEncounterBinding binding, string flightId)
        {
            if (string.IsNullOrWhiteSpace(flightId)) throw new ArgumentException("Encounter requires a unique spawn identity.", nameof(flightId));
            var profile = Profiles[binding.Profile];
            var status = new ShipStatus
            {
                ItemTag = binding.Ship, EncounterProfile = profile.Id, FlightId = flightId,
                FittingVersion = ShipFittingConversion.CurrentVersion,
                Hull = profile.Hull, MaxHull = profile.Hull, Shield = profile.Shield, MaxShield = profile.Shield,
                Capacitor = profile.CapacitorPool, MaxCapacitor = profile.CapacitorPool, CapacitorRecovery = profile.Regen,
                HullResistance = profile.Resistance, ShieldResistance = profile.ShieldResistance,
                BaseSpeed = profile.Speed, Speed = profile.Speed, Signature = profile.Signature,
                CargoCapacity = profile.Weapons * profile.AmmunitionPerWeapon,
                CapitalShip = profile.Id == "fleet_objective",
                FittingBonuses = new() { [StatType.ShipAccuracy] = profile.Accuracy }
            };
            for (var slot = 1; slot <= profile.Weapons; slot++)
            {
                status.HighPowerModules[slot] = new() { ItemInstanceId = flightId + "/weapon/" + slot, Design = "npc_weapon", ItemTag = "npc_weapon", Condition = 100 };
                status.ActiveModules.Add(slot);
            }
            if (profile.Ammunition != null && profile.AmmunitionPerWeapon > 0)
                ShipCargo.Add(status, profile.Ammunition, profile.AmmunitionPerWeapon * profile.Weapons, "encounter-supply");
            return status;
        }
        public ShipModuleProfile Weapon(string profileId)
        {
            var profile = Profiles[profileId];
            return new ShipModuleProfile
            {
                Id = "npc_weapon", Name = profile.Name + " weapon", ShortName = "Encounter Gun",
                ItemTag = "npc_weapon", ItemResref = null, Action = ShipModuleAction.Weapon,
                Family = profile.Ammunition == null ? "Thermal" : "Ordnance", Slot = ShipFittingSlot.High,
                Output = profile.Damage, Cycle = profile.Cycle, Capacitor = profile.Capacitor,
                Range = profile.Range, Tracking = profile.Tracking, Resolution = profile.Resolution,
                ShieldMultiplier = 1, HullMultiplier = 1, PreparationSeconds = profile.PreparationSeconds,
                Ammunition = profile.Ammunition, OnHitWeaponLockSeconds = profile.ControlSeconds,
                OperatorSkill = SkillType.Gunnery, DiscountStat = StatType.ShipWeaponCapacitorDiscount,
                Modifiers = Array.Empty<ShipStatModifier>()
            };
        }
        public ShipModuleVariant Variant(string profileId)
        {
            var profile = Profiles[profileId];
            return new() { Design = "npc_weapon", Calibration = "Standard", Output = profile.Damage,
                Cycle = profile.Cycle, Capacitor = profile.Capacitor, CapacitorMultiplier = 1,
                Range = profile.Range, Tracking = profile.Tracking };
        }
    }
}
