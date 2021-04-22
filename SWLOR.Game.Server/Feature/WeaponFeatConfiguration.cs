using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using Item = SWLOR.Game.Server.Service.Item;

namespace SWLOR.Game.Server.Feature
{
    public class WeaponFeatConfiguration
    {
        /// <summary>
        /// When the module loads, set all of the weapon-related feat and item configurations.
        /// </summary>
        [NWNEventHandler("mod_load")]
        public static void ConfigureWeaponFeats()
        {
            ConfigureMartialArts();

            // Weapon Focus, Specialization, Improved Critical
            ConfigureVibroblades();
            ConfigureFinesseVibroblades();
            ConfigureLightsabers();
            ConfigureHeavyVibroblades();
            ConfigurePolearms();
            ConfigureTwinBlades();
            ConfigureSaberstaffs();
            ConfigureKnuckles();
            ConfigureStaves();
            ConfigurePistols();
            ConfigureThrowingWeapons();
            ConfigureCannons();
            ConfigureRifles();
        }

        private static void ConfigureMartialArts()
        {
            Weapon.SetWeaponIsMonkWeapon(BaseItem.Club);
            Weapon.SetWeaponIsMonkWeapon(BaseItem.QuarterStaff);
            Weapon.SetWeaponIsMonkWeapon(BaseItem.Knuckles);
            Weapon.SetWeaponIsMonkWeapon(BaseItem.LightMace);
        }

        private static void ConfigureVibroblades()
        {
            foreach (var itemType in Item.VibrobladeBaseItemTypes)
            {
                Weapon.SetWeaponFocusFeat(itemType, FeatType.WeaponFocusVibroblades);
                Weapon.SetWeaponSpecializationFeat(itemType, FeatType.WeaponSpecializationVibroblades);
                Weapon.SetWeaponImprovedCriticalFeat(itemType, FeatType.ImprovedCriticalVibroblades);
            }
        }

        private static void ConfigureFinesseVibroblades()
        {
            foreach (var itemType in Item.FinesseVibrobladeBaseItemTypes)
            {
                Weapon.SetWeaponFocusFeat(itemType, FeatType.WeaponFocusFinesseVibroblades);
                Weapon.SetWeaponSpecializationFeat(itemType, FeatType.WeaponSpecializationFinesseVibroblades);
                Weapon.SetWeaponImprovedCriticalFeat(itemType, FeatType.ImprovedCriticalFinesseVibroblades);
            }
        }

        private static void ConfigureLightsabers()
        {
            foreach (var itemType in Item.LightsaberBaseItemTypes)
            {
                Weapon.SetWeaponFocusFeat(itemType, FeatType.WeaponFocusLightsabers);
                Weapon.SetWeaponSpecializationFeat(itemType, FeatType.WeaponSpecializationLightsabers);
                Weapon.SetWeaponImprovedCriticalFeat(itemType, FeatType.ImprovedCriticalLightsabers);
            }
        }

        private static void ConfigureHeavyVibroblades()
        {
            foreach (var itemType in Item.HeavyVibrobladeBaseItemTypes)
            {
                Weapon.SetWeaponFocusFeat(itemType, FeatType.WeaponFocusHeavyVibroblades);
                Weapon.SetWeaponSpecializationFeat(itemType, FeatType.WeaponSpecializationHeavyVibroblades);
                Weapon.SetWeaponImprovedCriticalFeat(itemType, FeatType.ImprovedCriticalHeavyVibroblades);
            }
        }

        private static void ConfigurePolearms()
        {
            foreach (var itemType in Item.PolearmBaseItemTypes)
            {
                Weapon.SetWeaponFocusFeat(itemType, FeatType.WeaponFocusPolearms);
                Weapon.SetWeaponSpecializationFeat(itemType, FeatType.WeaponSpecializationPolearms);
                Weapon.SetWeaponImprovedCriticalFeat(itemType, FeatType.ImprovedCriticalPolearms);
            }
        }

        private static void ConfigureTwinBlades()
        {
            foreach (var itemType in Item.TwinBladeBaseItemTypes)
            {
                Weapon.SetWeaponFocusFeat(itemType, FeatType.WeaponFocusTwinBlades);
                Weapon.SetWeaponSpecializationFeat(itemType, FeatType.WeaponSpecializationTwinBlades);
                Weapon.SetWeaponImprovedCriticalFeat(itemType, FeatType.ImprovedCriticalTwinBlades);
            }
        }

        private static void ConfigureSaberstaffs()
        {
            foreach (var itemType in Item.SaberstaffBaseItemTypes)
            {
                Weapon.SetWeaponFocusFeat(itemType, FeatType.WeaponFocusSaberstaffs);
                Weapon.SetWeaponSpecializationFeat(itemType, FeatType.WeaponSpecializationSaberstaffs);
                Weapon.SetWeaponImprovedCriticalFeat(itemType, FeatType.ImprovedCriticalSaberstaffs);
            }
        }

        private static void ConfigureKnuckles()
        {
            foreach (var itemType in Item.KnucklesBaseItemTypes)
            {
                Weapon.SetWeaponFocusFeat(itemType, FeatType.WeaponFocusKnuckles);
                Weapon.SetWeaponSpecializationFeat(itemType, FeatType.WeaponSpecializationKnuckles);
                Weapon.SetWeaponImprovedCriticalFeat(itemType, FeatType.ImprovedCriticalKnuckles);
            }
        }

        private static void ConfigureStaves()
        {
            foreach (var itemType in Item.StaffBaseItemTypes)
            {
                Weapon.SetWeaponFocusFeat(itemType, FeatType.WeaponFocusStaves);
                Weapon.SetWeaponSpecializationFeat(itemType, FeatType.WeaponSpecializationStaves);
                Weapon.SetWeaponImprovedCriticalFeat(itemType, FeatType.ImprovedCriticalStaff);
            }
        }
        private static void ConfigurePistols()
        {
            foreach (var itemType in Item.PistolBaseItemTypes)
            {
                Weapon.SetWeaponFocusFeat(itemType, FeatType.WeaponFocusPistol);
                Weapon.SetWeaponSpecializationFeat(itemType, FeatType.WeaponSpecializationPistol);
                Weapon.SetWeaponImprovedCriticalFeat(itemType, FeatType.ImprovedCriticalPistol);
            }
        }

        private static void ConfigureThrowingWeapons()
        {
            foreach (var itemType in Item.ThrowingWeaponBaseItemTypes)
            {
                Weapon.SetWeaponFocusFeat(itemType, FeatType.WeaponFocusThrowingWeapons);
                Weapon.SetWeaponSpecializationFeat(itemType, FeatType.WeaponSpecializationThrowingWeapons);
                Weapon.SetWeaponImprovedCriticalFeat(itemType, FeatType.ImprovedCriticalThrowingWeapons);
            }
        }

        private static void ConfigureCannons()
        {
            foreach (var itemType in Item.CannonBaseItemTypes)
            {
                Weapon.SetWeaponFocusFeat(itemType, FeatType.WeaponFocusCannons);
                Weapon.SetWeaponSpecializationFeat(itemType, FeatType.WeaponSpecializationCannons);
                Weapon.SetWeaponImprovedCriticalFeat(itemType, FeatType.ImprovedCriticalCannons);
            }
        }

        private static void ConfigureRifles()
        {
            foreach (var itemType in Item.RifleBaseItemTypes)
            {
                Weapon.SetWeaponFocusFeat(itemType, FeatType.WeaponFocusRifles);
                Weapon.SetWeaponSpecializationFeat(itemType, FeatType.WeaponSpecializationRifles);
                Weapon.SetWeaponImprovedCriticalFeat(itemType, FeatType.ImprovedCriticalRifles);
            }
        }
    }
}
