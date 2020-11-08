using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using Item = SWLOR.Game.Server.Service.Item;

namespace SWLOR.Game.Server.Feature.TraitDefinition
{
    public class WeaponTraitDefinition
    {
        /// <summary>
        /// When the module loads, set all of the weapon-related feat and item configurations.
        /// </summary>
        [NWNEventHandler("mod_load")]
        public static void ConfigureWeaponFeats()
        {
            Vibroblades();
            FinesseVibroblades();
            Lightsabers();
            HeavyVibroblades();
            Polearms();
            TwinBlades();
            Saberstaffs();
            Knuckles();
            Staves();
            Pistols();
            ThrowingWeapons();
            Cannons();
            Rifles();
        }

        private static void Vibroblades()
        {
            foreach (var itemType in Item.VibrobladeBaseItemTypes)
            {
                Weapon.SetWeaponFocusFeat(itemType, Feat.WeaponFocusVibroblades);
                Weapon.SetWeaponSpecializationFeat(itemType, Feat.WeaponSpecializationVibroblades);
                Weapon.SetWeaponImprovedCriticalFeat(itemType, Feat.ImprovedCriticalVibroblades);
            }
        }

        private static void FinesseVibroblades()
        {
            foreach (var itemType in Item.FinesseVibrobladeBaseItemTypes)
            {
                Weapon.SetWeaponFocusFeat(itemType, Feat.WeaponFocusFinesseVibroblades);
                Weapon.SetWeaponSpecializationFeat(itemType, Feat.WeaponSpecializationFinesseVibroblades);
                Weapon.SetWeaponImprovedCriticalFeat(itemType, Feat.ImprovedCriticalFinesseVibroblades);
            }
        }

        private static void Lightsabers()
        {
            foreach (var itemType in Item.LightsaberBaseItemTypes)
            {
                Weapon.SetWeaponFocusFeat(itemType, Feat.WeaponFocusLightsabers);
                Weapon.SetWeaponSpecializationFeat(itemType, Feat.WeaponSpecializationLightsabers);
                Weapon.SetWeaponImprovedCriticalFeat(itemType, Feat.ImprovedCriticalLightsabers);
            }
        }

        private static void HeavyVibroblades()
        {
            foreach (var itemType in Item.HeavyVibrobladeBaseItemTypes)
            {
                Weapon.SetWeaponFocusFeat(itemType, Feat.WeaponFocusFinesseVibroblades);
                Weapon.SetWeaponSpecializationFeat(itemType, Feat.WeaponSpecializationFinesseVibroblades);
                Weapon.SetWeaponImprovedCriticalFeat(itemType, Feat.ImprovedCriticalFinesseVibroblades);
            }
        }

        private static void Polearms()
        {
            foreach (var itemType in Item.PolearmBaseItemTypes)
            {
                Weapon.SetWeaponFocusFeat(itemType, Feat.WeaponFocusPolearms);
                Weapon.SetWeaponSpecializationFeat(itemType, Feat.WeaponSpecializationPolearms);
                Weapon.SetWeaponImprovedCriticalFeat(itemType, Feat.ImprovedCriticalPolearms);
            }
        }

        private static void TwinBlades()
        {
            foreach (var itemType in Item.TwinBladeBaseItemTypes)
            {
                Weapon.SetWeaponFocusFeat(itemType, Feat.WeaponFocusTwinBlades);
                Weapon.SetWeaponSpecializationFeat(itemType, Feat.WeaponSpecializationTwinBlades);
                Weapon.SetWeaponImprovedCriticalFeat(itemType, Feat.ImprovedCriticalTwinBlades);
            }
        }

        private static void Saberstaffs()
        {
            foreach (var itemType in Item.SaberstaffBaseItemTypes)
            {
                Weapon.SetWeaponFocusFeat(itemType, Feat.WeaponFocusSaberstaffs);
                Weapon.SetWeaponSpecializationFeat(itemType, Feat.WeaponSpecializationSaberstaffs);
                Weapon.SetWeaponImprovedCriticalFeat(itemType, Feat.ImprovedCriticalSaberstaffs);
            }
        }

        private static void Knuckles()
        {
            foreach (var itemType in Item.KnucklesBaseItemTypes)
            {
                Weapon.SetWeaponFocusFeat(itemType, Feat.WeaponFocusKnuckles);
                Weapon.SetWeaponSpecializationFeat(itemType, Feat.WeaponSpecializationKnuckles);
                Weapon.SetWeaponImprovedCriticalFeat(itemType, Feat.ImprovedCriticalKnuckles);
            }
        }

        private static void Staves()
        {
            foreach (var itemType in Item.StaffBaseItemTypes)
            {
                Weapon.SetWeaponFocusFeat(itemType, Feat.WeaponFocusStaves);
                Weapon.SetWeaponSpecializationFeat(itemType, Feat.WeaponSpecializationStaves);
                Weapon.SetWeaponImprovedCriticalFeat(itemType, Feat.ImprovedCriticalStaff);
            }
        }
        private static void Pistols()
        {
            foreach (var itemType in Item.PistolBaseItemTypes)
            {
                Weapon.SetWeaponFocusFeat(itemType, Feat.WeaponFocusPistol);
                Weapon.SetWeaponSpecializationFeat(itemType, Feat.WeaponSpecializationPistol);
                Weapon.SetWeaponImprovedCriticalFeat(itemType, Feat.ImprovedCriticalPistol);
            }
        }

        private static void ThrowingWeapons()
        {
            foreach (var itemType in Item.ThrowingWeaponBaseItemTypes)
            {
                Weapon.SetWeaponFocusFeat(itemType, Feat.WeaponFocusThrowingWeapons);
                Weapon.SetWeaponSpecializationFeat(itemType, Feat.WeaponSpecializationThrowingWeapons);
                Weapon.SetWeaponImprovedCriticalFeat(itemType, Feat.ImprovedCriticalThrowingWeapons);
            }
        }

        private static void Cannons()
        {
            foreach (var itemType in Item.CannonBaseItemTypes)
            {
                Weapon.SetWeaponFocusFeat(itemType, Feat.WeaponFocusCannons);
                Weapon.SetWeaponSpecializationFeat(itemType, Feat.WeaponSpecializationCannons);
                Weapon.SetWeaponImprovedCriticalFeat(itemType, Feat.ImprovedCriticalCannons);
            }
        }

        private static void Rifles()
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
