using Microsoft.Extensions.DependencyInjection;
using SWLOR.NWN.API.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Component.Combat.Feature
{
    public class WeaponFeatConfiguration
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IWeaponPluginService _weaponPlugin;
        
        // Lazy-loaded services to break circular dependencies
        private IItemService ItemService => _serviceProvider.GetRequiredService<IItemService>();

        public WeaponFeatConfiguration(
            IServiceProvider serviceProvider,
            IWeaponPluginService weaponPlugin,
            IEventAggregator eventAggregator)
        {
            _serviceProvider = serviceProvider;
            _weaponPlugin = weaponPlugin;

            // Subscribe to events
            eventAggregator.Subscribe<OnModuleLoad>(e => ConfigureWeaponFeats());
        }

        /// <summary>
        /// When the module loads, set all of the weapon-related feat and item configurations.
        /// </summary>
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
                _weaponPlugin.SetWeaponFocusFeat(itemType, FeatType.WeaponFocusVibroblades);
                _weaponPlugin.SetWeaponSpecializationFeat(itemType, FeatType.WeaponSpecializationVibroblades);
                _weaponPlugin.SetWeaponImprovedCriticalFeat(itemType, FeatType.ImprovedCriticalVibroblades);
            }
        }

        private void ConfigureFinesseVibroblades()
        {
            foreach (var itemType in ItemService.FinesseVibrobladeBaseItemTypes)
            {
                _weaponPlugin.SetWeaponFocusFeat(itemType, FeatType.WeaponFocusFinesseVibroblades);
                _weaponPlugin.SetWeaponSpecializationFeat(itemType, FeatType.WeaponSpecializationFinesseVibroblades);
                _weaponPlugin.SetWeaponImprovedCriticalFeat(itemType, FeatType.ImprovedCriticalFinesseVibroblades);
            }
        }

        private void ConfigureLightsabers()
        {
            foreach (var itemType in ItemService.LightsaberBaseItemTypes)
            {
                _weaponPlugin.SetWeaponFocusFeat(itemType, FeatType.WeaponFocusLightsabers);
                _weaponPlugin.SetWeaponSpecializationFeat(itemType, FeatType.WeaponSpecializationLightsabers);
                _weaponPlugin.SetWeaponImprovedCriticalFeat(itemType, FeatType.ImprovedCriticalLightsabers);
            }
        }

        private void ConfigureHeavyVibroblades()
        {
            foreach (var itemType in ItemService.HeavyVibrobladeBaseItemTypes)
            {
                _weaponPlugin.SetWeaponFocusFeat(itemType, FeatType.WeaponFocusHeavyVibroblades);
                _weaponPlugin.SetWeaponSpecializationFeat(itemType, FeatType.WeaponSpecializationHeavyVibroblades);
                _weaponPlugin.SetWeaponImprovedCriticalFeat(itemType, FeatType.ImprovedCriticalHeavyVibroblades);
            }
        }

        private void ConfigurePolearms()
        {
            foreach (var itemType in ItemService.PolearmBaseItemTypes)
            {
                _weaponPlugin.SetWeaponFocusFeat(itemType, FeatType.WeaponFocusPolearms);
                _weaponPlugin.SetWeaponSpecializationFeat(itemType, FeatType.WeaponSpecializationPolearms);
                _weaponPlugin.SetWeaponImprovedCriticalFeat(itemType, FeatType.ImprovedCriticalPolearms);
            }
        }

        private void ConfigureTwinBlades()
        {
            foreach (var itemType in ItemService.TwinBladeBaseItemTypes)
            {
                _weaponPlugin.SetWeaponFocusFeat(itemType, FeatType.WeaponFocusTwinBlades);
                _weaponPlugin.SetWeaponSpecializationFeat(itemType, FeatType.WeaponSpecializationTwinBlades);
                _weaponPlugin.SetWeaponImprovedCriticalFeat(itemType, FeatType.ImprovedCriticalTwinBlades);
            }
        }

        private void ConfigureSaberstaffs()
        {
            foreach (var itemType in ItemService.SaberstaffBaseItemTypes)
            {
                _weaponPlugin.SetWeaponFocusFeat(itemType, FeatType.WeaponFocusSaberstaffs);
                _weaponPlugin.SetWeaponSpecializationFeat(itemType, FeatType.WeaponSpecializationSaberstaffs);
                _weaponPlugin.SetWeaponImprovedCriticalFeat(itemType, FeatType.ImprovedCriticalSaberstaffs);
            }
        }

        private void ConfigureKatars()
        {
            foreach (var itemType in ItemService.KatarBaseItemTypes)
            {
                _weaponPlugin.SetWeaponFocusFeat(itemType, FeatType.WeaponFocusKatars);
                _weaponPlugin.SetWeaponSpecializationFeat(itemType, FeatType.WeaponSpecializationKatars);
                _weaponPlugin.SetWeaponImprovedCriticalFeat(itemType, FeatType.ImprovedCriticalKatars);
            }
        }

        private void ConfigureStaves()
        {
            foreach (var itemType in ItemService.StaffBaseItemTypes)
            {
                _weaponPlugin.SetWeaponFocusFeat(itemType, FeatType.WeaponFocusStaves);
                _weaponPlugin.SetWeaponSpecializationFeat(itemType, FeatType.WeaponSpecializationStaves);
                _weaponPlugin.SetWeaponImprovedCriticalFeat(itemType, FeatType.ImprovedCriticalStaff);
            }
        }
        private void ConfigurePistols()
        {
            foreach (var itemType in ItemService.PistolBaseItemTypes)
            {
                _weaponPlugin.SetWeaponFocusFeat(itemType, FeatType.WeaponFocusPistol);
                _weaponPlugin.SetWeaponSpecializationFeat(itemType, FeatType.WeaponSpecializationPistol);
                _weaponPlugin.SetWeaponImprovedCriticalFeat(itemType, FeatType.ImprovedCriticalPistol);
            }
        }

        private void ConfigureThrowingWeapons()
        {
            foreach (var itemType in ItemService.ThrowingWeaponBaseItemTypes)
            {
                _weaponPlugin.SetWeaponFocusFeat(itemType, FeatType.WeaponFocusThrowingWeapons);
                _weaponPlugin.SetWeaponSpecializationFeat(itemType, FeatType.WeaponSpecializationThrowingWeapons);
                _weaponPlugin.SetWeaponImprovedCriticalFeat(itemType, FeatType.ImprovedCriticalThrowingWeapons);
            }
        }

        private void ConfigureRifles()
        {
            foreach (var itemType in ItemService.RifleBaseItemTypes)
            {
                _weaponPlugin.SetWeaponFocusFeat(itemType, FeatType.WeaponFocusRifles);
                _weaponPlugin.SetWeaponSpecializationFeat(itemType, FeatType.WeaponSpecializationRifles);
                _weaponPlugin.SetWeaponImprovedCriticalFeat(itemType, FeatType.ImprovedCriticalRifles);
            }
        }
    }
}
