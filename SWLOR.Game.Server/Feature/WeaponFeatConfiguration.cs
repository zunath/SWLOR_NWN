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
                Weapon.SetWeaponFocusFeat(itemType, Feat.WeaponFocusVibroblades);
                Weapon.SetWeaponSpecializationFeat(itemType, Feat.WeaponSpecializationVibroblades);
                Weapon.SetWeaponImprovedCriticalFeat(itemType, Feat.ImprovedCriticalVibroblades);
            }
        }

        private static void ConfigureFinesseVibroblades()
        {
            foreach (var itemType in Item.FinesseVibrobladeBaseItemTypes)
            {
                Weapon.SetWeaponFocusFeat(itemType, Feat.WeaponFocusFinesseVibroblades);
                Weapon.SetWeaponSpecializationFeat(itemType, Feat.WeaponSpecializationFinesseVibroblades);
                Weapon.SetWeaponImprovedCriticalFeat(itemType, Feat.ImprovedCriticalFinesseVibroblades);
            }
        }

        private static void ConfigureLightsabers()
        {
            foreach (var itemType in Item.LightsaberBaseItemTypes)
            {
                Weapon.SetWeaponFocusFeat(itemType, Feat.WeaponFocusLightsabers);
                Weapon.SetWeaponSpecializationFeat(itemType, Feat.WeaponSpecializationLightsabers);
                Weapon.SetWeaponImprovedCriticalFeat(itemType, Feat.ImprovedCriticalLightsabers);
            }
        }

        private static void ConfigureHeavyVibroblades()
        {
            foreach (var itemType in Item.HeavyVibrobladeBaseItemTypes)
            {
                Weapon.SetWeaponFocusFeat(itemType, Feat.WeaponFocusHeavyVibroblades);
                Weapon.SetWeaponSpecializationFeat(itemType, Feat.WeaponSpecializationHeavyVibroblades);
                Weapon.SetWeaponImprovedCriticalFeat(itemType, Feat.ImprovedCriticalHeavyVibroblades);
            }
        }

        private static void ConfigurePolearms()
        {
            foreach (var itemType in Item.PolearmBaseItemTypes)
            {
                Weapon.SetWeaponFocusFeat(itemType, Feat.WeaponFocusPolearms);
                Weapon.SetWeaponSpecializationFeat(itemType, Feat.WeaponSpecializationPolearms);
                Weapon.SetWeaponImprovedCriticalFeat(itemType, Feat.ImprovedCriticalPolearms);
            }
        }

        private static void ConfigureTwinBlades()
        {
            foreach (var itemType in Item.TwinBladeBaseItemTypes)
            {
                Weapon.SetWeaponFocusFeat(itemType, Feat.WeaponFocusTwinBlades);
                Weapon.SetWeaponSpecializationFeat(itemType, Feat.WeaponSpecializationTwinBlades);
                Weapon.SetWeaponImprovedCriticalFeat(itemType, Feat.ImprovedCriticalTwinBlades);
            }
        }

        private static void ConfigureSaberstaffs()
        {
            foreach (var itemType in Item.SaberstaffBaseItemTypes)
            {
                Weapon.SetWeaponFocusFeat(itemType, Feat.WeaponFocusSaberstaffs);
                Weapon.SetWeaponSpecializationFeat(itemType, Feat.WeaponSpecializationSaberstaffs);
                Weapon.SetWeaponImprovedCriticalFeat(itemType, Feat.ImprovedCriticalSaberstaffs);
            }
        }

        private static void ConfigureKnuckles()
        {
            foreach (var itemType in Item.KnucklesBaseItemTypes)
            {
                Weapon.SetWeaponFocusFeat(itemType, Feat.WeaponFocusKnuckles);
                Weapon.SetWeaponSpecializationFeat(itemType, Feat.WeaponSpecializationKnuckles);
                Weapon.SetWeaponImprovedCriticalFeat(itemType, Feat.ImprovedCriticalKnuckles);
            }
        }

        private static void ConfigureStaves()
        {
            foreach (var itemType in Item.StaffBaseItemTypes)
            {
                Weapon.SetWeaponFocusFeat(itemType, Feat.WeaponFocusStaves);
                Weapon.SetWeaponSpecializationFeat(itemType, Feat.WeaponSpecializationStaves);
                Weapon.SetWeaponImprovedCriticalFeat(itemType, Feat.ImprovedCriticalStaff);
            }
        }
        private static void ConfigurePistols()
        {
            foreach (var itemType in Item.PistolBaseItemTypes)
            {
                Weapon.SetWeaponFocusFeat(itemType, Feat.WeaponFocusPistol);
                Weapon.SetWeaponSpecializationFeat(itemType, Feat.WeaponSpecializationPistol);
                Weapon.SetWeaponImprovedCriticalFeat(itemType, Feat.ImprovedCriticalPistol);
            }
        }

        private static void ConfigureThrowingWeapons()
        {
            foreach (var itemType in Item.ThrowingWeaponBaseItemTypes)
            {
                Weapon.SetWeaponFocusFeat(itemType, Feat.WeaponFocusThrowingWeapons);
                Weapon.SetWeaponSpecializationFeat(itemType, Feat.WeaponSpecializationThrowingWeapons);
                Weapon.SetWeaponImprovedCriticalFeat(itemType, Feat.ImprovedCriticalThrowingWeapons);
            }
        }

        private static void ConfigureCannons()
        {
            foreach (var itemType in Item.CannonBaseItemTypes)
            {
                Weapon.SetWeaponFocusFeat(itemType, Feat.WeaponFocusCannons);
                Weapon.SetWeaponSpecializationFeat(itemType, Feat.WeaponSpecializationCannons);
                Weapon.SetWeaponImprovedCriticalFeat(itemType, Feat.ImprovedCriticalCannons);
            }
        }

        private static void ConfigureRifles()
        {
            foreach (var itemType in Item.RifleBaseItemTypes)
            {
                Weapon.SetWeaponFocusFeat(itemType, Feat.WeaponFocusRifles);
                Weapon.SetWeaponSpecializationFeat(itemType, Feat.WeaponSpecializationRifles);
                Weapon.SetWeaponImprovedCriticalFeat(itemType, Feat.ImprovedCriticalRifles);
            }
        }
    }
}
