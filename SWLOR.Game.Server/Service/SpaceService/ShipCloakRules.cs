namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// Decision rules for cloaking (plan section 32): which hulls fit a Cloaking Device, the role bonus, the cargo rule,
    /// what a cloaked ship may do, the decloak lock and who sees a cloaked ship. Pure logic with no engine calls.
    /// </summary>
    public static class ShipCloakRules
    {
        public const string FitMessage = "A Cloaking Device fits only Small hulls and Blockade Runners.";
        public const string CargoMessage = "Only Blockade Runners can cloak with cargo aboard.";
        public const string HullMessage = "This hull cannot cloak.";

        /// <summary>Whether a hull accepts a Cloaking Device: every Small hull and the Blockade Runner.</summary>
        public static bool CanFit(ShipSize size, ShipRole role) =>
            size == ShipSize.Small || role == ShipRole.BlockadeRunner;

        /// <summary>Interceptors, Bombers and Explorers cloak faster and lose less speed.</summary>
        public static bool HasRoleBonus(ShipRole role) =>
            role == ShipRole.Interceptor || role == ShipRole.Bomber || role == ShipRole.Explorer;

        /// <summary>Whether a hull may cloak with cargo aboard.</summary>
        public static bool CanCloakWithCargo(ShipRole role) => role == ShipRole.BlockadeRunner;

        /// <summary>Seconds from the press until the ship vanishes.</summary>
        public static double GetEngageSeconds(ShipRole role, string hullTag)
        {
            if (hullTag == CloakTuning.PirateFreighterTag)
                return CloakTuning.PirateFreighterEngageSeconds;

            return HasRoleBonus(role) ? CloakTuning.RoleEngageSeconds : CloakTuning.EngageSeconds;
        }

        /// <summary>The share of its speed a cloaking or cloaked ship keeps.</summary>
        public static double GetSpeedMultiplier(ShipRole role) =>
            (HasRoleBonus(role) ? CloakTuning.RoleSpeedPercent : CloakTuning.SpeedPercent) / 100.0;

        /// <summary>The reveal range of a hull's cloak: the device's, or the Barracuda's shorter one.</summary>
        public static int GetRevealMeters(int deviceRevealMeters, string hullTag)
        {
            var meters = deviceRevealMeters > 0 ? deviceRevealMeters : CloakTuning.RevealBaseMeters;
            return hullTag == CloakTuning.BarracudaTag ? Math.Min(meters, CloakTuning.BarracudaRevealMeters) : meters;
        }

        /// <summary>The profile a ship cloaks with.</summary>
        public static ShipCloakProfile BuildProfile(ShipRole role, string hullTag, int deviceRevealMeters) => new()
        {
            EngageSeconds = GetEngageSeconds(role, hullTag),
            SpeedMultiplier = GetSpeedMultiplier(role),
            RevealMeters = GetRevealMeters(deviceRevealMeters, hullTag),
        };

        /// <summary>Why a ship cannot cloak (or keep its cloak), or an empty string.</summary>
        public static string GetStartError(ShipSize size, ShipRole role, bool hasCargo)
        {
            if (!CanFit(size, role))
                return HullMessage;

            return hasCargo && !CanCloakWithCargo(role) ? CargoMessage : string.Empty;
        }

        /// <summary>The phase of a cloak started at a time.</summary>
        public static ShipCloakPhase GetPhase(DateTime startedAt, double engageSeconds, DateTime now) =>
            now >= startedAt.AddSeconds(engageSeconds) ? ShipCloakPhase.Cloaked : ShipCloakPhase.Engaging;

        /// <summary>Whole seconds until a cloaking ship vanishes, 0 once it has.</summary>
        public static int GetEngageSecondsLeft(DateTime startedAt, double engageSeconds, DateTime now) =>
            Math.Max(0, (int)Math.Ceiling((startedAt.AddSeconds(engageSeconds) - now).TotalSeconds));

        /// <summary>When firing unlocks after a ship decloaks at a time.</summary>
        public static DateTime GetFiringUnlockTime(DateTime decloakedAt) => decloakedAt.AddSeconds(CloakTuning.DecloakLockSeconds);

        /// <summary>Whole seconds of the decloak lock left, 0 when firing is open.</summary>
        public static int GetLockSecondsLeft(DateTime? unlocksAt, DateTime now) =>
            unlocksAt == null ? 0 : Math.Max(0, (int)Math.Ceiling((unlocksAt.Value - now).TotalSeconds));

        /// <summary>
        /// Why a module cannot be used, or an empty string. While cloaking or cloaked only the Cloaking Device itself
        /// answers; after decloaking a weapon stays locked until the lock ends.
        /// </summary>
        public static string GetModuleBlock(ShipCloakPhase phase, int lockSecondsLeft, bool isCloakingDevice, bool isWeapon)
        {
            if (isCloakingDevice)
                return string.Empty;

            if (phase == ShipCloakPhase.Engaging)
                return "You cannot use modules while your ship is cloaking.";

            if (phase == ShipCloakPhase.Cloaked)
                return "You cannot use modules while your ship is cloaked.";

            return isWeapon ? GetFiringLockMessage(lockSecondsLeft) : string.Empty;
        }

        /// <summary>Why an ability cannot be used, or an empty string. Abilities are shut off while cloaking and cloaked.</summary>
        public static string GetAbilityBlock(ShipCloakPhase phase)
        {
            return phase == ShipCloakPhase.Off
                ? string.Empty
                : phase == ShipCloakPhase.Engaging
                    ? "You cannot use abilities while your ship is cloaking."
                    : "You cannot use abilities while your ship is cloaked.";
        }

        /// <summary>The message for a weapon or weapon ability stopped by the decloak lock, empty once it is open.</summary>
        public static string GetFiringLockMessage(int lockSecondsLeft) =>
            lockSecondsLeft > 0
                ? $"Your weapons are locked for {lockSecondsLeft} more second{(lockSecondsLeft == 1 ? string.Empty : "s")} after decloaking."
                : string.Empty;

        /// <summary>Whether a ship in this phase cannot fire a weapon.</summary>
        public static bool IsFiringLocked(ShipCloakPhase phase, int lockSecondsLeft) =>
            phase != ShipCloakPhase.Off || lockSecondsLeft > 0;

        /// <summary>
        /// Whether an observer cannot see a cloaked ship. The ship's own pilot, party and crew, and anyone a Sensor
        /// Sweep revealed it to, see it; so does any ship within the reveal range. Only a finished cloak hides.
        /// </summary>
        public static bool IsHidden(ShipCloakPhase phase, bool isAlly, double distance, int revealMeters, bool isPainted) =>
            phase == ShipCloakPhase.Cloaked && !isAlly && !isPainted && distance > revealMeters;

        /// <summary>A cloaked ship slips past a pirate ambush roll; one still cloaking does not.</summary>
        public static bool DodgesAmbush(ShipCloakPhase phase) => phase == ShipCloakPhase.Cloaked;

        /// <summary>The speed of a ship under its cloak.</summary>
        public static double ApplySpeed(double speed, double cloakMultiplier) => speed * cloakMultiplier;

        /// <summary>The Ship HUD's Cloak line, or null when the ship has no Cloaking Device.</summary>
        public static string GetStatusText(bool hasDevice, ShipCloakPhase phase, int engageSecondsLeft, int lockSecondsLeft)
        {
            if (!hasDevice)
                return null;

            return phase switch
            {
                ShipCloakPhase.Engaging => $"Cloaking ({engageSecondsLeft} s)",
                ShipCloakPhase.Cloaked => "Cloaked",
                _ => lockSecondsLeft > 0 ? $"Weapons locked ({lockSecondsLeft} s)" : "Off",
            };
        }
    }
}
