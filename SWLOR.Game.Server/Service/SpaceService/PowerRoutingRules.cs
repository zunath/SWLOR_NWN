using System.Collections.Generic;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// Power routing (plan section 10): the Engineer shifts power between weapons, shields and engines. Each mode
    /// raises one subsystem and lowers the other two. The numbers are a sidegrade with no net gain in the balance
    /// model (<c>tools/SpaceRevampBalance/checks.py</c>, section L): no mode shortens a kill, so a crewed ship
    /// stays at or below N separate ships, and the model's job score is level with Balanced in Shields mode and
    /// lower in the others, whose gains (capacitor and speed) the fights do not measure.
    /// </summary>
    public static class PowerRoutingRules
    {
        /// <summary>Seconds between changes of the routing.</summary>
        public const int ChangeCooldownSeconds = 6;

        /// <summary>Weapons mode: weapon modules use this much less capacitor (percent).</summary>
        public const int WeaponsCapacitorSavingPercent = 25;

        /// <summary>Shields and Engines modes: weapon damage falls by this much (percent).</summary>
        public const int WeaponDamagePenaltyPercent = 5;

        /// <summary>Shields mode: shield regen rises by this much (percent).</summary>
        public const int ShieldRegenBonusPercent = 15;

        /// <summary>Weapons and Engines modes: shield regen falls by this much (percent).</summary>
        public const int ShieldRegenPenaltyPercent = 10;

        /// <summary>Engines mode: speed rises by this much (percent).</summary>
        public const int SpeedBonusPercent = 20;

        /// <summary>Weapons and Shields modes: speed falls by this much (percent).</summary>
        public const int SpeedPenaltyPercent = 10;

        /// <summary>Percent change to weapon modules' capacitor use.</summary>
        public static int WeaponCapacitorPercent(ShipPowerRouting mode) =>
            mode == ShipPowerRouting.Weapons ? -WeaponsCapacitorSavingPercent : 0;

        /// <summary>Percent change to the ship's weapon damage.</summary>
        public static int WeaponDamagePercent(ShipPowerRouting mode) =>
            mode is ShipPowerRouting.Shields or ShipPowerRouting.Engines ? -WeaponDamagePenaltyPercent : 0;

        /// <summary>Percent change to the ship's shield regen.</summary>
        public static int ShieldRegenPercent(ShipPowerRouting mode) => mode switch
        {
            ShipPowerRouting.Shields => ShieldRegenBonusPercent,
            ShipPowerRouting.Weapons or ShipPowerRouting.Engines => -ShieldRegenPenaltyPercent,
            _ => 0,
        };

        /// <summary>Percent change to the ship's speed.</summary>
        public static int SpeedPercent(ShipPowerRouting mode) => mode switch
        {
            ShipPowerRouting.Engines => SpeedBonusPercent,
            ShipPowerRouting.Weapons or ShipPowerRouting.Shields => -SpeedPenaltyPercent,
            _ => 0,
        };

        /// <summary>
        /// A module's capacitor use under the routing: weapon modules only, rounded, at least 1 when it costs any.
        /// </summary>
        public static int ApplyCapacitorCost(int cost, ShipPowerRouting mode, bool isWeapon)
        {
            var percent = isWeapon ? WeaponCapacitorPercent(mode) : 0;
            if (cost <= 0 || percent == 0)
                return cost;

            return Math.Max(1, (int)Math.Round(cost * (1 + percent / 100.0), MidpointRounding.AwayFromZero));
        }

        /// <summary>Shield regen per second under the routing.</summary>
        public static double ApplyShieldRegen(double regenPerSecond, ShipPowerRouting mode) =>
            regenPerSecond * (1 + ShieldRegenPercent(mode) / 100.0);

        /// <summary>The ship's speed multiplier under the routing.</summary>
        public static double ApplySpeed(double speedMultiplier, ShipPowerRouting mode) =>
            speedMultiplier * (1 + SpeedPercent(mode) / 100.0);

        /// <summary>Whether a change is allowed now: the routing differs and the cooldown has passed.</summary>
        public static bool CanChange(ShipPowerRouting current, ShipPowerRouting requested, DateTime changedAt, DateTime now) =>
            current != requested && now >= changedAt.AddSeconds(ChangeCooldownSeconds);

        /// <summary>Whole seconds left before the routing can change again.</summary>
        public static int SecondsUntilChange(DateTime changedAt, DateTime now) =>
            Math.Max(0, (int)Math.Ceiling((changedAt.AddSeconds(ChangeCooldownSeconds) - now).TotalSeconds));

        /// <summary>The routing's name.</summary>
        public static string GetName(ShipPowerRouting mode) => mode switch
        {
            ShipPowerRouting.Weapons => "Weapons",
            ShipPowerRouting.Shields => "Shields",
            ShipPowerRouting.Engines => "Engines",
            _ => "Balanced",
        };

        /// <summary>The routing's effect, as the HUD and the Engineer's message show it.</summary>
        public static string GetEffect(ShipPowerRouting mode) => mode switch
        {
            ShipPowerRouting.Weapons =>
                $"weapons use {WeaponsCapacitorSavingPercent}% less capacitor; shield regen -{ShieldRegenPenaltyPercent}%, speed -{SpeedPenaltyPercent}%",
            ShipPowerRouting.Shields =>
                $"shield regen +{ShieldRegenBonusPercent}%; weapon damage -{WeaponDamagePenaltyPercent}%, speed -{SpeedPenaltyPercent}%",
            ShipPowerRouting.Engines =>
                $"speed +{SpeedBonusPercent}%; weapon damage -{WeaponDamagePenaltyPercent}%, shield regen -{ShieldRegenPenaltyPercent}%",
            _ => "weapons, shields and engines work normally",
        };

        /// <summary>The Engineer station feat for each routing.</summary>
        public static FeatType GetFeat(ShipPowerRouting mode) => mode switch
        {
            ShipPowerRouting.Weapons => FeatType.ShipPowerWeapons,
            ShipPowerRouting.Shields => FeatType.ShipPowerShields,
            ShipPowerRouting.Engines => FeatType.ShipPowerEngines,
            _ => FeatType.ShipPowerBalanced,
        };

        /// <summary>The routing a feat selects, or false for any other feat.</summary>
        public static bool TryGetMode(FeatType feat, out ShipPowerRouting mode)
        {
            switch (feat)
            {
                case FeatType.ShipPowerBalanced:
                    mode = ShipPowerRouting.Balanced;
                    return true;
                case FeatType.ShipPowerWeapons:
                    mode = ShipPowerRouting.Weapons;
                    return true;
                case FeatType.ShipPowerShields:
                    mode = ShipPowerRouting.Shields;
                    return true;
                case FeatType.ShipPowerEngines:
                    mode = ShipPowerRouting.Engines;
                    return true;
                default:
                    mode = ShipPowerRouting.Balanced;
                    return false;
            }
        }

        /// <summary>The four routing feats, Balanced first.</summary>
        public static IReadOnlyList<FeatType> Feats { get; } = new[]
        {
            FeatType.ShipPowerBalanced, FeatType.ShipPowerWeapons, FeatType.ShipPowerShields, FeatType.ShipPowerEngines,
        };
    }
}
