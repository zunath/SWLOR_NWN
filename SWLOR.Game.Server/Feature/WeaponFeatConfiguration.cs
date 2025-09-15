using SWLOR.Game.Server.Core;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using Item = SWLOR.Game.Server.Service.Item;

namespace SWLOR.Game.Server.Feature
{
    public class WeaponFeatConfiguration
    {
        /// <summary>
        /// When the module loads, set all of the weapon-related feat and item configurations.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void ConfigureWeaponFeats()
        {
            // Weapon Focus, Specialization, Improved Critical
            ConfigureVibroblades();
            ConfigureFinesseVibroblades();
            ConfigureLightsabers();
            ConfigureHeavyVibroblades();
            ConfigurePolearms();
            ConfigureTwinBlades();
            ConfigureSaberstaffs();
            ConfigureKatars();
            ConfigureStaves();
            ConfigurePistols();
            ConfigureThrowingWeapons();
            ConfigureRifles();
        }
        
        private static void ConfigureVibroblades()
        {
            foreach (var itemType in Item.VibrobladeBaseItemTypes)
            {
                WeaponPlugin.SetWeaponFocusFeat(itemType, FeatType.WeaponFocusVibroblades);
                WeaponPlugin.SetWeaponSpecializationFeat(itemType, FeatType.WeaponSpecializationVibroblades);
                WeaponPlugin.SetWeaponImprovedCriticalFeat(itemType, FeatType.ImprovedCriticalVibroblades);
            }
        }

        private static void ConfigureFinesseVibroblades()
        {
            foreach (var itemType in Item.FinesseVibrobladeBaseItemTypes)
            {
                WeaponPlugin.SetWeaponFocusFeat(itemType, FeatType.WeaponFocusFinesseVibroblades);
                WeaponPlugin.SetWeaponSpecializationFeat(itemType, FeatType.WeaponSpecializationFinesseVibroblades);
                WeaponPlugin.SetWeaponImprovedCriticalFeat(itemType, FeatType.ImprovedCriticalFinesseVibroblades);
            }
        }

        private static void ConfigureLightsabers()
        {
            foreach (var itemType in Item.LightsaberBaseItemTypes)
            {
                WeaponPlugin.SetWeaponFocusFeat(itemType, FeatType.WeaponFocusLightsabers);
                WeaponPlugin.SetWeaponSpecializationFeat(itemType, FeatType.WeaponSpecializationLightsabers);
                WeaponPlugin.SetWeaponImprovedCriticalFeat(itemType, FeatType.ImprovedCriticalLightsabers);
            }
        }

        private static void ConfigureHeavyVibroblades()
        {
            foreach (var itemType in Item.HeavyVibrobladeBaseItemTypes)
            {
                WeaponPlugin.SetWeaponFocusFeat(itemType, FeatType.WeaponFocusHeavyVibroblades);
                WeaponPlugin.SetWeaponSpecializationFeat(itemType, FeatType.WeaponSpecializationHeavyVibroblades);
                WeaponPlugin.SetWeaponImprovedCriticalFeat(itemType, FeatType.ImprovedCriticalHeavyVibroblades);
            }
        }

        private static void ConfigurePolearms()
        {
            foreach (var itemType in Item.PolearmBaseItemTypes)
            {
                WeaponPlugin.SetWeaponFocusFeat(itemType, FeatType.WeaponFocusPolearms);
                WeaponPlugin.SetWeaponSpecializationFeat(itemType, FeatType.WeaponSpecializationPolearms);
                WeaponPlugin.SetWeaponImprovedCriticalFeat(itemType, FeatType.ImprovedCriticalPolearms);
            }
        }

        private static void ConfigureTwinBlades()
        {
            foreach (var itemType in Item.TwinBladeBaseItemTypes)
            {
                WeaponPlugin.SetWeaponFocusFeat(itemType, FeatType.WeaponFocusTwinBlades);
                WeaponPlugin.SetWeaponSpecializationFeat(itemType, FeatType.WeaponSpecializationTwinBlades);
                WeaponPlugin.SetWeaponImprovedCriticalFeat(itemType, FeatType.ImprovedCriticalTwinBlades);
            }
        }

        private static void ConfigureSaberstaffs()
        {
            foreach (var itemType in Item.SaberstaffBaseItemTypes)
            {
                WeaponPlugin.SetWeaponFocusFeat(itemType, FeatType.WeaponFocusSaberstaffs);
                WeaponPlugin.SetWeaponSpecializationFeat(itemType, FeatType.WeaponSpecializationSaberstaffs);
                WeaponPlugin.SetWeaponImprovedCriticalFeat(itemType, FeatType.ImprovedCriticalSaberstaffs);
            }
        }

        private static void ConfigureKatars()
        {
            foreach (var itemType in Item.KatarBaseItemTypes)
            {
                WeaponPlugin.SetWeaponFocusFeat(itemType, FeatType.WeaponFocusKatars);
                WeaponPlugin.SetWeaponSpecializationFeat(itemType, FeatType.WeaponSpecializationKatars);
                WeaponPlugin.SetWeaponImprovedCriticalFeat(itemType, FeatType.ImprovedCriticalKatars);
            }
        }

        private static void ConfigureStaves()
        {
            foreach (var itemType in Item.StaffBaseItemTypes)
            {
                WeaponPlugin.SetWeaponFocusFeat(itemType, FeatType.WeaponFocusStaves);
                WeaponPlugin.SetWeaponSpecializationFeat(itemType, FeatType.WeaponSpecializationStaves);
                WeaponPlugin.SetWeaponImprovedCriticalFeat(itemType, FeatType.ImprovedCriticalStaff);
            }
        }
        private static void ConfigurePistols()
        {
            foreach (var itemType in Item.PistolBaseItemTypes)
            {
                WeaponPlugin.SetWeaponFocusFeat(itemType, FeatType.WeaponFocusPistol);
                WeaponPlugin.SetWeaponSpecializationFeat(itemType, FeatType.WeaponSpecializationPistol);
                WeaponPlugin.SetWeaponImprovedCriticalFeat(itemType, FeatType.ImprovedCriticalPistol);
            }
        }

        private static void ConfigureThrowingWeapons()
        {
            foreach (var itemType in Item.ThrowingWeaponBaseItemTypes)
            {
                WeaponPlugin.SetWeaponFocusFeat(itemType, FeatType.WeaponFocusThrowingWeapons);
                WeaponPlugin.SetWeaponSpecializationFeat(itemType, FeatType.WeaponSpecializationThrowingWeapons);
                WeaponPlugin.SetWeaponImprovedCriticalFeat(itemType, FeatType.ImprovedCriticalThrowingWeapons);
            }
        }

        private static void ConfigureRifles()
        {
            foreach (var itemType in Item.RifleBaseItemTypes)
            {
                WeaponPlugin.SetWeaponFocusFeat(itemType, FeatType.WeaponFocusRifles);
                WeaponPlugin.SetWeaponSpecializationFeat(itemType, FeatType.WeaponSpecializationRifles);
                WeaponPlugin.SetWeaponImprovedCriticalFeat(itemType, FeatType.ImprovedCriticalRifles);
            }
        }
    }
}
