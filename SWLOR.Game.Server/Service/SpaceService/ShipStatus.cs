using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.SpaceService
{
    public class ShipStatus
    {
        public class ShipStatusModule
        {
            public string ItemInstanceId { get; set; }
            public string ItemTag { get; set; }
            public string SerializedItem { get; set; }
            public DateTime RecastTime { get; set; }
            public DateTime RecastStartedAt { get; set; }
            public int ModuleBonus { get; set; }

            /// <summary>The component traits the module item carried when it was installed (plan section 14). Null when none.</summary>
            public List<ShipComponentTraitType> Traits { get; set; }

            /// <summary>
            /// RequiresSkill ranks on the module item when it was installed. Null when it had none.
            /// </summary>
            public Dictionary<SkillType, int> RequiredSkills { get; set; }

            /// <summary>The numbers a preset NPC ship fires this weapon with. Null for player ships and other modules.</summary>
            public ShipNpcWeapon NpcShot { get; set; }
        }

        /// <summary>
        /// A toggled module that is on.
        /// </summary>
        public class ShipActiveModule
        {
            /// <summary>The hotbar feat slot number (1-30): high 1-10, low 11-20, mid 21-30.</summary>
            public int Slot { get; set; }

            /// <summary>When the module's current cycle ends and it fires again.</summary>
            public DateTime NextCycleTime { get; set; }

            /// <summary>The object a support module locked onto when it was turned on.</summary>
            public uint TargetId { get; set; } = OBJECT_INVALID;
        }

        public string ItemTag { get; set; }

        /// <summary>The pilot block of a preset space NPC ship. Null for player ships and legacy NPC ships.</summary>
        public ShipNpcPilot NpcPilot { get; set; }

        /// <summary>Whether this is a preset space NPC ship (its values come from the preset, not from a level term).</summary>
        [JsonIgnore]
        public bool IsPresetNpc => NpcPilot != null;

        /// <summary>
        /// RequiresSkill ranks on the ship's deed item when it was registered. Null when it had none.
        /// </summary>
        public Dictionary<SkillType, int> RequiredSkills { get; set; }

        /// <summary>
        /// The traits the ship's deed inherited from the components it was crafted from (plan section 14), copied when
        /// the ship was registered. Null when it had none; older records load null.
        /// </summary>
        public List<ShipComponentTraitType> HullTraits { get; set; }
        public int Shield { get; set; }
        public int Hull { get; set; }
        public int Capacitor { get; set; }
        public int MaxShield{ get; set; }
        public int MaxHull { get; set; }
        public int MaxCapacitor { get; set; }
        public int ShieldCycle { get; set; }

        /// <summary>
        /// Superseded by the WIL-based shield regen (SpaceCombatMath.ShieldRegenPerSecond). Kept because saved data
        /// carries it; nothing reads it.
        /// </summary>
        public int ShieldRechargeRate { get; set; }

        // The six per-type damage and defense fields are superseded by Attack and Defense. Saved data still carries
        // them (a later migration removes them); combat no longer reads them.
        public int EMDamage { get; set; }
        public int ThermalDamage { get; set; }
        public int ExplosiveDamage { get; set; }
        public int Accuracy { get; set; }
        public int Evasion { get; set; }
        public int ThermalDefense { get; set; }
        public int ExplosiveDefense { get; set; }
        public int EMDefense { get; set; }

        /// <summary>The ship's Attack, added to every weapon module's own Attack.</summary>
        public int Attack { get; set; }

        /// <summary>The ship's Defense, added to the pilot's VIT.</summary>
        public int Defense { get; set; }

        /// <summary>The hull size. Saved data without it reads as Small.</summary>
        public ShipSize Size { get; set; } = ShipSize.Small;

        /// <summary>Percent bonus to the ship's damage by type (Thermal, EM, Explosive).</summary>
        public Dictionary<CombatDamageType, int> DamageBonusPercent { get; set; } = new();

        /// <summary>Percent bonus to shield regen.</summary>
        public int ShieldRegenBonusPercent { get; set; }

        /// <summary>The hull's capacitor regen per second, before the pilot's WIL is added.</summary>
        public double CapacitorRegenPerSecond { get; set; } = 1.0;

        /// <summary>Critical rate bonus from the hull, added to every shot's critical rate (SpaceCombatMath.CriticalRateModifier).</summary>
        public int CriticalBonus { get; set; }

        /// <summary>The movement multiplier while piloting this ship (1.0 is today's speed).</summary>
        public double SpeedMultiplier { get; set; } = 1.0;

        /// <summary>Where the Engineer routes power (plan section 10). Saved data without it reads as Balanced.</summary>
        public ShipPowerRouting PowerRouting { get; set; }

        /// <summary>When the routing last changed; it can change again <see cref="PowerRoutingRules.ChangeCooldownSeconds"/> later.</summary>
        public DateTime PowerRoutingChangedAt { get; set; }

        /// <summary>
        /// How much larger the routing's gain is, from the Engineering rank of the Engineer who set it (W11n,
        /// <see cref="ShipSeatSkillRules.EngineeringRoutingPercent"/>). Saved data without it reads as 0; it returns to 0
        /// with the routing.
        /// </summary>
        public int PowerRoutingBonusPercent { get; set; }

        /// <summary>A Shield Hardener keeps this resistance bonus to every type until this time.</summary>
        public DateTime HardenedUntil { get; set; }

        /// <summary>The resistance bonus a Shield Hardener grants while hardened.</summary>
        public int HardenedAmount { get; set; }

        /// <summary>Accuracy lost to an enemy E-War Array (a negative number) until <see cref="AccuracyPenaltyUntil"/>.</summary>
        public int AccuracyPenalty { get; set; }
        public DateTime AccuracyPenaltyUntil { get; set; }

        /// <summary>
        /// An enemy Interdictor holds the ship until this time: no starlane or signature jump, docking, rift crossing,
        /// Rift Key or cloak, and its Afterburner shuts down (W11k). Saved data without it reads as never held.
        /// </summary>
        public DateTime InterdictedUntil { get; set; }

        /// <summary>Percent more damage the ship takes from an enemy Target Painter until <see cref="PaintedUntil"/>.</summary>
        public int PaintedPercent { get; set; }
        public DateTime PaintedUntil { get; set; }

        /// <summary>Whether an Interdictor holds the ship right now.</summary>
        public bool IsInterdicted(DateTime now) => now < InterdictedUntil;

        /// <summary>The extra damage taken from a Target Painter right now, in percent (0 when unpainted).</summary>
        public int GetPaintPercent(DateTime now) => now < PaintedUntil ? PaintedPercent : 0;

        /// <summary>Percent added to max shield by an allied Bulwark Shield Generator until <see cref="ShieldAuraUntil"/>.</summary>
        public int ShieldAuraPercent { get; set; }
        public DateTime ShieldAuraUntil { get; set; }

        /// <summary>
        /// Keeps the Shield Hardener bonus for one more cycle. With two hardeners running, the stronger bonus counts.
        /// </summary>
        public void Harden(int amount, double cycleSeconds, DateTime now)
        {
            HardenedAmount = HardenedUntil > now ? Math.Max(HardenedAmount, amount) : amount;
            var until = now.AddSeconds(cycleSeconds + 0.5);
            if (until > HardenedUntil)
                HardenedUntil = until;
        }

        /// <summary>The Accuracy lost to an E-War Array right now (0 or negative).</summary>
        public int GetAccuracyPenalty(DateTime now) => now < AccuracyPenaltyUntil ? AccuracyPenalty : 0;

        /// <summary>Max shield including any Bulwark Shield Generator bonus that is still active.</summary>
        public int GetEffectiveMaxShield(DateTime now) =>
            now < ShieldAuraUntil ? MaxShield + (int)Math.Round(MaxShield * ShieldAuraPercent / 100.0) : MaxShield;

        /// <summary>
        /// Fractional shield and capacitor regen carried between ticks. Persisted, because a player's status is
        /// reloaded from the database every tick.
        /// </summary>
        public double ShieldRegenRemainder { get; set; }
        public double CapacitorRegenRemainder { get; set; }

        /// <summary>"On your next weapon hit" effects waiting for a landed weapon shot.</summary>
        public List<SpaceShotRider> PendingRiders { get; set; } = new();

        public int Industrial { get; set; }
        public bool CapitalShip { get; set; }
        public DateTime GlobalRecast { get; set; }

        /// <summary>
        /// Equipped high-powered modules
        /// </summary>
        public Dictionary<int, ShipStatusModule> HighPowerModules { get; set; }

        /// <summary>
        /// Equipped low-powered modules
        /// </summary>
        public Dictionary<int, ShipStatusModule> LowPowerModules { get; set; }

        /// <summary>
        /// Equipped mid-powered modules
        /// </summary>
        public Dictionary<int, ShipStatusModule> MidPowerModules { get; set; }

        /// <summary>
        /// Equipped configuration module
        /// </summary>
        public Dictionary<int, ShipStatusModule> ConfigurationModules { get; set; }

        /// <summary>
        /// The toggled modules that are currently on, for players and ships alike (plan section 8).
        /// Saved data that has an empty list still loads.
        /// </summary>
        public List<ShipActiveModule> ActiveModules { get; set; }

        /// <summary>
        /// Resistance percentages by damage type.
        /// </summary>
        public Dictionary<ResistanceType, int> Resistances { get; set; }

        /// <summary>
        /// Crew station tags keyed by hotbar feat slot number (1-30). Used for crew stations (plan section 10).
        /// </summary>
        public Dictionary<int, ShipSlotTag> SlotTags { get; set; }

        /// <summary>The fitted weapon override of a preset NPC ship for a module tag, or null when it has none.</summary>
        public ShipNpcWeapon FindNpcShot(string moduleTag)
        {
            return HighPowerModules.Values
                .Concat(MidPowerModules.Values)
                .Concat(LowPowerModules.Values)
                .FirstOrDefault(x => x.ItemTag == moduleTag && x.NpcShot != null)?.NpcShot;
        }

        public ShipStatus()
        {
            HighPowerModules = new Dictionary<int, ShipStatusModule>();
            LowPowerModules = new Dictionary<int, ShipStatusModule>();
            MidPowerModules = new Dictionary<int, ShipStatusModule>();
            ConfigurationModules = new Dictionary<int, ShipStatusModule>();
            ActiveModules = new List<ShipActiveModule>();
            Resistances = new Dictionary<ResistanceType, int>();
            SlotTags = new Dictionary<int, ShipSlotTag>();
        }
    }
}
