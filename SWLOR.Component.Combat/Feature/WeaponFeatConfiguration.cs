using Microsoft.Extensions.DependencyInjection;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Service;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Module;

namespace SWLOR.Component.Combat.Feature
{
    public class WeaponFeatConfiguration
    {
        private readonly IServiceProvider _serviceProvider;
        
        // Lazy-loaded services to break circular dependencies
        private IItemService ItemService => _serviceProvider.GetRequiredService<IItemService>();

        public WeaponFeatConfiguration(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// When the module loads, set all of the weapon-related feat and item configurations.
        /// </summary>
        [ScriptHandler<OnModuleLoad>]
        public void ConfigureWeaponFeats()
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
        
        private void ConfigureVibroblades()
        {
            foreach (var itemType in ItemService.VibrobladeBaseItemTypes)
            {
                WeaponPlugin.SetWeaponFocusFeat(itemType, FeatType.WeaponFocusVibroblades);
                WeaponPlugin.SetWeaponSpecializationFeat(itemType, FeatType.WeaponSpecializationVibroblades);
                WeaponPlugin.SetWeaponImprovedCriticalFeat(itemType, FeatType.ImprovedCriticalVibroblades);
            }
        }

        private void ConfigureFinesseVibroblades()
        {
            foreach (var itemType in ItemService.FinesseVibrobladeBaseItemTypes)
            {
                WeaponPlugin.SetWeaponFocusFeat(itemType, FeatType.WeaponFocusFinesseVibroblades);
                WeaponPlugin.SetWeaponSpecializationFeat(itemType, FeatType.WeaponSpecializationFinesseVibroblades);
                WeaponPlugin.SetWeaponImprovedCriticalFeat(itemType, FeatType.ImprovedCriticalFinesseVibroblades);
            }
        }

        private void ConfigureLightsabers()
        {
            foreach (var itemType in ItemService.LightsaberBaseItemTypes)
            {
                WeaponPlugin.SetWeaponFocusFeat(itemType, FeatType.WeaponFocusLightsabers);
                WeaponPlugin.SetWeaponSpecializationFeat(itemType, FeatType.WeaponSpecializationLightsabers);
                WeaponPlugin.SetWeaponImprovedCriticalFeat(itemType, FeatType.ImprovedCriticalLightsabers);
            }
        }

        private void ConfigureHeavyVibroblades()
        {
            foreach (var itemType in ItemService.HeavyVibrobladeBaseItemTypes)
            {
                WeaponPlugin.SetWeaponFocusFeat(itemType, FeatType.WeaponFocusHeavyVibroblades);
                WeaponPlugin.SetWeaponSpecializationFeat(itemType, FeatType.WeaponSpecializationHeavyVibroblades);
                WeaponPlugin.SetWeaponImprovedCriticalFeat(itemType, FeatType.ImprovedCriticalHeavyVibroblades);
            }
        }

        private void ConfigurePolearms()
        {
            foreach (var itemType in ItemService.PolearmBaseItemTypes)
            {
                WeaponPlugin.SetWeaponFocusFeat(itemType, FeatType.WeaponFocusPolearms);
                WeaponPlugin.SetWeaponSpecializationFeat(itemType, FeatType.WeaponSpecializationPolearms);
                WeaponPlugin.SetWeaponImprovedCriticalFeat(itemType, FeatType.ImprovedCriticalPolearms);
            }
        }

        private void ConfigureTwinBlades()
        {
            foreach (var itemType in ItemService.TwinBladeBaseItemTypes)
            {
                WeaponPlugin.SetWeaponFocusFeat(itemType, FeatType.WeaponFocusTwinBlades);
                WeaponPlugin.SetWeaponSpecializationFeat(itemType, FeatType.WeaponSpecializationTwinBlades);
                WeaponPlugin.SetWeaponImprovedCriticalFeat(itemType, FeatType.ImprovedCriticalTwinBlades);
            }
        }

        private void ConfigureSaberstaffs()
        {
            foreach (var itemType in ItemService.SaberstaffBaseItemTypes)
            {
                WeaponPlugin.SetWeaponFocusFeat(itemType, FeatType.WeaponFocusSaberstaffs);
                WeaponPlugin.SetWeaponSpecializationFeat(itemType, FeatType.WeaponSpecializationSaberstaffs);
                WeaponPlugin.SetWeaponImprovedCriticalFeat(itemType, FeatType.ImprovedCriticalSaberstaffs);
            }
        }

        private void ConfigureKatars()
        {
            foreach (var itemType in ItemService.KatarBaseItemTypes)
            {
                WeaponPlugin.SetWeaponFocusFeat(itemType, FeatType.WeaponFocusKatars);
                WeaponPlugin.SetWeaponSpecializationFeat(itemType, FeatType.WeaponSpecializationKatars);
                WeaponPlugin.SetWeaponImprovedCriticalFeat(itemType, FeatType.ImprovedCriticalKatars);
            }
        }

        private void ConfigureStaves()
        {
            foreach (var itemType in ItemService.StaffBaseItemTypes)
            {
                WeaponPlugin.SetWeaponFocusFeat(itemType, FeatType.WeaponFocusStaves);
                WeaponPlugin.SetWeaponSpecializationFeat(itemType, FeatType.WeaponSpecializationStaves);
                WeaponPlugin.SetWeaponImprovedCriticalFeat(itemType, FeatType.ImprovedCriticalStaff);
            }
        }
        private void ConfigurePistols()
        {
            foreach (var itemType in ItemService.PistolBaseItemTypes)
            {
                WeaponPlugin.SetWeaponFocusFeat(itemType, FeatType.WeaponFocusPistol);
                WeaponPlugin.SetWeaponSpecializationFeat(itemType, FeatType.WeaponSpecializationPistol);
                WeaponPlugin.SetWeaponImprovedCriticalFeat(itemType, FeatType.ImprovedCriticalPistol);
            }
        }

        private void ConfigureThrowingWeapons()
        {
            foreach (var itemType in ItemService.ThrowingWeaponBaseItemTypes)
            {
                WeaponPlugin.SetWeaponFocusFeat(itemType, FeatType.WeaponFocusThrowingWeapons);
                WeaponPlugin.SetWeaponSpecializationFeat(itemType, FeatType.WeaponSpecializationThrowingWeapons);
                WeaponPlugin.SetWeaponImprovedCriticalFeat(itemType, FeatType.ImprovedCriticalThrowingWeapons);
            }
        }

        private void ConfigureRifles()
        {
            foreach (var itemType in ItemService.RifleBaseItemTypes)
            {
                WeaponPlugin.SetWeaponFocusFeat(itemType, FeatType.WeaponFocusRifles);
                WeaponPlugin.SetWeaponSpecializationFeat(itemType, FeatType.WeaponSpecializationRifles);
                WeaponPlugin.SetWeaponImprovedCriticalFeat(itemType, FeatType.ImprovedCriticalRifles);
            }
        }
    }
}
