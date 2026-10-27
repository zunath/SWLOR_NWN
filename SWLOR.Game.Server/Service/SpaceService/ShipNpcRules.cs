using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// How a space NPC ship preset (<see cref="ShipNpcEntry"/>) becomes a ship status, and where a preset ship's
    /// shot math differs from a player's. Pure functions, no engine calls.
    /// </summary>
    public static class ShipNpcRules
    {
        /// <summary>Preset ships carry a capacitor so large that a weapon never waits on it.</summary>
        public const int Capacitor = 1000;

        public const double CapacitorRegenPerSecond = 50.0;

        /// <summary>The message when a torpedo has no valid target.</summary>
        public const string TorpedoTargetError = "Torpedoes only target Large and Capital ships.";

        /// <summary>
        /// Builds the ship status of a preset NPC. Weapons go into the High slots in order, each fitted as its
        /// module with the preset's numbers attached.
        /// </summary>
        public static ShipStatus BuildStatus(ShipNpcEntry entry, string itemTag, bool capitalShip, DateTime now)
        {
            var status = new ShipStatus
            {
                ItemTag = itemTag,
                Size = entry.Size == ShipSize.Invalid ? ShipSize.Small : entry.Size,
                CapitalShip = capitalShip,
                MaxHull = entry.Hull,
                Hull = entry.Hull,
                MaxShield = entry.Shield,
                Shield = entry.Shield,
                MaxCapacitor = Capacitor,
                Capacitor = Capacitor,
                CapacitorRegenPerSecond = CapacitorRegenPerSecond,
                Attack = entry.Attack,
                Accuracy = entry.Accuracy,
                Defense = entry.Defense,
                Evasion = entry.Evasion,
                NpcPilot = new ShipNpcPilot
                {
                    Agility = entry.Agility,
                    Perception = entry.Perception,
                    Vitality = entry.Vitality,
                    Willpower = entry.Willpower,
                    Might = entry.Might,
                    ShieldRegenPerSecond = entry.ShieldRegenPerSecond,
                },
            };

            status.Resistances[ResistanceType.Thermal] = entry.ResistAll;
            status.Resistances[ResistanceType.EM] = entry.ResistAll;
            status.Resistances[ResistanceType.Explosive] = entry.ResistAll;

            var slot = 1;
            foreach (var weapon in entry.Weapons)
            {
                status.HighPowerModules[slot++] = new ShipStatus.ShipStatusModule
                {
                    ItemTag = weapon.ModuleTag,
                    RecastTime = now,
                    NpcShot = new ShipNpcWeapon
                    {
                        ModuleTag = weapon.ModuleTag,
                        Dmg = weapon.Dmg,
                        Attack = weapon.Attack,
                        Accuracy = weapon.Accuracy,
                        Rank = weapon.Rank,
                    },
                };
            }

            return status;
        }

        /// <summary>
        /// A pilot attribute for the shot math: the preset's for a preset NPC ship, otherwise the creature's own.
        /// </summary>
        public static int PilotScore(ShipStatus status, AbilityType ability, Func<AbilityType, int> creatureScore)
        {
            return status?.NpcPilot != null ? status.NpcPilot.GetScore(ability) : creatureScore(ability);
        }

        /// <summary>
        /// Whether a ship adds the level term to its Accuracy, Evasion, Attack and Defense. Only a legacy NPC ship
        /// does: a player has none, and a preset NPC ship's values already include its level.
        /// </summary>
        public static bool UsesLevelTerm(bool playerPilot, ShipStatus status)
        {
            return !playerPilot && (status == null || !status.IsPresetNpc);
        }

        /// <summary>Shield regained per second: a preset NPC ship's own rate, otherwise the size and WIL formula.</summary>
        public static double ShieldRegenPerSecond(ShipStatus status, double maxShield, int wil)
        {
            return status.NpcPilot != null
                ? status.NpcPilot.ShieldRegenPerSecond
                : SpaceCombatMath.ShieldRegenPerSecond(maxShield, status.Size == ShipSize.Invalid ? ShipSize.Small : status.Size, wil, status.ShieldRegenBonusPercent);
        }

        /// <summary>An error message when a preset NPC ship's weapon cannot fire at a target of this size.</summary>
        public static string TargetError(ShipModuleFamily family, ShipStatus attacker, ShipSize targetSize)
        {
            if (family != ShipModuleFamily.TorpedoLauncher || attacker == null || !attacker.IsPresetNpc)
                return string.Empty;

            return targetSize == ShipSize.Large || targetSize == ShipSize.Capital ? string.Empty : TorpedoTargetError;
        }
    }
}
