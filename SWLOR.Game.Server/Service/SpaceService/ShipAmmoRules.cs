using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.ShipRewardDefinition;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// Launcher ammo and its variants (plan sections 8 and 14) as pure functions: which item a launcher spends, and what
    /// a variant does to the shot it fires.
    /// </summary>
    public static class ShipAmmoRules
    {
        /// <summary>The creature local naming the variant a pilot loaded into a launcher family's launchers.</summary>
        public const string LoadedVariablePrefix = "SHIP_LOADED_AMMO_";

        private static readonly Dictionary<string, ShipAmmoVariant> _variants =
            RevampRewardCatalog.AmmoVariants.ToDictionary(x => x.Resref);

        /// <summary>The variant an ammo resref is, or null for plain ammo and other items.</summary>
        public static ShipAmmoVariant Variant(string resref) =>
            resref != null && _variants.TryGetValue(resref, out var variant) ? variant : null;

        /// <summary>The variants a launcher family fires, in catalog order.</summary>
        public static List<ShipAmmoVariant> VariantsFor(ShipModuleFamily launcher) =>
            RevampRewardCatalog.AmmoVariants.Where(x => x.Launcher == launcher).ToList();

        public static string LoadedVariable(ShipModuleFamily launcher) => LoadedVariablePrefix + (int)launcher;

        /// <summary>Every item a launcher with this plain ammo can fire: the plain ammo first, then its variants.</summary>
        public static List<string> AmmoFor(ShipModuleFamily launcher, string plainResref)
        {
            var ammo = new List<string>();
            if (!string.IsNullOrWhiteSpace(plainResref))
                ammo.Add(plainResref);

            ammo.AddRange(VariantsFor(launcher).Select(x => x.Resref));
            return ammo;
        }

        /// <summary>
        /// The ammo a launcher spends next: the loaded variant while the pilot carries it, else the plain ammo, else
        /// the first variant the pilot carries. Null when the pilot carries none.
        /// </summary>
        public static string Pick(ShipModuleFamily launcher, string plainResref, string loadedResref, Func<string, bool> carries)
        {
            if (!string.IsNullOrWhiteSpace(loadedResref) && Variant(loadedResref)?.Launcher == launcher && carries(loadedResref))
                return loadedResref;

            return AmmoFor(launcher, plainResref).FirstOrDefault(carries);
        }

        /// <summary>
        /// The shot as the ammo makes it: a variant changes the damage type, scales DMG (never below 1), adds Accuracy
        /// and sets how hard the shot hits shields. Plain ammo leaves the shot as it is.
        /// </summary>
        public static SpaceWeaponShot Apply(SpaceWeaponShot shot, ShipAmmoVariant variant)
        {
            if (shot == null || variant == null)
                return shot;

            var fired = shot.WithBonusDmg(0);
            fired.DamageType = variant.DamageType;
            fired.Dmg = Math.Max(1, (int)Math.Round(shot.Dmg * variant.DmgPercent / 100.0, MidpointRounding.AwayFromZero));
            fired.Accuracy = shot.Accuracy + variant.Accuracy;
            fired.ShieldDamageMultiplier = variant.ShieldDamageMultiplier > 0 ? variant.ShieldDamageMultiplier : 1.0;
            return fired;
        }

        /// <summary>The damage of one burn tick: the variant's percent of the shot's DMG, at least 1.</summary>
        public static int BurnDamage(int shotDmg, ShipAmmoVariant variant) =>
            variant == null || variant.Rider != ShipAmmoRider.Burn
                ? 0
                : Math.Max(1, (int)Math.Round(shotDmg * variant.RiderPercent / 100.0, MidpointRounding.AwayFromZero));

        /// <summary>How many burn ticks a landed hit sets off.</summary>
        public static int BurnTicks(ShipAmmoVariant variant) =>
            variant == null || variant.Rider != ShipAmmoRider.Burn
                ? 0
                : Math.Max(1, variant.RiderSeconds / Math.Max(1, RevampRewardCatalog.BurnTickSeconds));

        /// <summary>Whether the landed hit slows the target: a Slow variant once it has damaged the hull.</summary>
        public static bool Slows(ShipAmmoVariant variant, int hullDamage) =>
            variant != null && variant.Rider == ShipAmmoRider.Slow && hullDamage > 0;
    }
}
