using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatService;

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
            public int ModuleBonus { get; set; }
            public string Design { get; set; }
            public string Calibration { get; set; } = "Standard";
            public ShipQualityDimension QualityDimension { get; set; }
            public int Quality { get; set; }
            public int Condition { get; set; } = 100;
            public string OriginalSerializedItem { get; set; }
            public string BoundPlayerId {get;set;}
        }

        public string LastDefeatFlightId { get; set; }
        public int OutstandingRecoveryCredits { get; set; }
        public ShipDockPayment PendingDockPayment { get; set; }
        public Dictionary<string, SpaceHostileDamageDebt> HostileDamageDebt { get; set; } = new();
        public string EncounterProfile { get; set; }
        public int NextEncounterWeapon { get; set; }
        public string FlightId { get; set; }
        public string CommittedLegId { get; set; }
        public string OperatorBuildSignature { get; set; }
        public Dictionary<string, ShipCargoTransfer> PendingCargoTransfers { get; set; } = new();
        public Dictionary<string, ShipCargoLot> Cargo { get; set; } = new();
        public HashSet<string> PaidWorkClaims { get; set; } = new();
        public HashSet<string> SettledSiteClaims { get; set; } = new();
        public Dictionary<int, List<string>> BankModules { get; set; } = new();
        public Dictionary<string, ShipModuleActivation> PendingModuleActivations { get; set; } = new();
        public Dictionary<StatType, double> FittingBonuses { get; set; } = new();
        public Dictionary<StatType, double> FittingPenalties { get; set; } = new();
        public Dictionary<ShipResource, double> FractionalResourceDeficits { get; set; } = new();
        public DateTime LastHostileActivity { get; set; }
        public List<ShipTemporaryAdjustment> TemporaryAdjustments { get; set; } = new();
        public Dictionary<string, ShipControlWindow> ControlWindows { get; set; } = new();
        public List<ShipRecoveryReceipt> ExternalRecoveryReceipts { get; set; } = new();
        public int ProtectedCargo { get; set; }
        public DateTime RefitReadyAt { get; set; }
        public Dictionary<string, string> LegacyEquipmentAudit { get; set; } = new();
        public Dictionary<string, ShipInventoryTransfer> PendingInventoryTransfers { get; set; } = new();
        public string ActiveContractId { get; set; }
        public int FittingVersion { get; set; }
        public ShipResourceDeficits ResourceDeficits { get; set; }
        public Dictionary<string, ShipStatusModule> RefitRecovery { get; set; } = new();
        public Dictionary<string, string> RefitRecoveryReasons { get; set; } = new();
        public string ConfigurationDesign { get; set; }
        public int FittingPowerUsed { get; set; }
        public double CargoCapacity { get; set; }
        public double BaseSpeed { get; set; }
        public double Speed { get; set; }
        public double Signature { get; set; }
        public double HullResistance { get; set; }
        public double ShieldResistance { get; set; }
        public double CapacitorRecovery { get; set; }
        public double OutOfCombatShieldRecovery { get; set; }
        public string ItemTag { get; set; }
        public int Shield { get; set; }
        public int Hull { get; set; }
        public int Capacitor { get; set; }
        public int MaxShield{ get; set; }
        public int MaxHull { get; set; }
        public int MaxCapacitor { get; set; }
        public int ShieldCycle { get; set; }
        public int ShieldRechargeRate { get; set; }
        public int EMDamage { get; set; }
        public int ThermalDamage { get; set; }
        public int ExplosiveDamage { get; set; }
        public int Accuracy { get; set; }
        public int Evasion { get; set; }
        public int ThermalDefense { get; set; }
        public int ExplosiveDefense { get; set; }
        public int EMDefense { get; set; }
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
        /// Equipped configuration module
        /// </summary>
        public Dictionary<int, ShipStatusModule> ConfigurationModules { get; set; }

        /// <summary>
        /// A collection of ship modules, by feat, which can be activated.
        /// This is primarily used by ship AI but is also available for player ships.
        /// </summary>
        public HashSet<int> ActiveModules { get; set; }

        public ShipStatus()
        {
            HighPowerModules = new Dictionary<int, ShipStatusModule>();
            LowPowerModules = new Dictionary<int, ShipStatusModule>();
            ConfigurationModules = new Dictionary<int, ShipStatusModule>();
            ActiveModules = new HashSet<int>();
        }
    }
}
