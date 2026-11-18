using System.Collections.Generic;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance
{
    /// <summary>The weapon appearance definition of each base item the appearance editor can restyle.</summary>
    public static class WeaponAppearanceCatalog
    {
        public static readonly IReadOnlyDictionary<BaseItem, WeaponAppearanceBaseDefinition> Definitions = Build();

        /// <summary>Builds the definitions now, as the boot handler did when it filled the editor's dictionary.</summary>
        public static void EnsureLoaded() =>
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(WeaponAppearanceCatalog).TypeHandle);

        private static Dictionary<BaseItem, WeaponAppearanceBaseDefinition> Build()
        {
            var pistolAppearance = new PistolAppearanceDefinition();
            return new Dictionary<BaseItem, WeaponAppearanceBaseDefinition>
            {
                [BaseItem.Dagger] = new DaggerAppearanceDefinition(),
                [BaseItem.Electroblade] = new ElectrobladeAppearanceDefinition(),
                [BaseItem.GreatSword] = new GreatSwordAppearanceDefinition(),
                [BaseItem.Katar] = new KatarAppearanceDefinition(),
                [BaseItem.PowerGlove] = new PowerGloveAppearanceDefinition(),
                [BaseItem.LargeShield] = new LargeShieldAppearanceDefinition(),
                [BaseItem.Lightsaber] = new LightsaberAppearanceDefinition(),
                [BaseItem.Saberstaff] = new SaberstaffAppearanceDefinition(),
                [BaseItem.Longsword] = new LongswordAppearanceDefinition(),
                [BaseItem.Pistol] = pistolAppearance,
                [BaseItem.Sling] = pistolAppearance,
                [BaseItem.LegacyPistol] = pistolAppearance,
                [BaseItem.QuarterStaff] = new QuarterstaffAppearanceDefinition(),
                [BaseItem.Rifle] = new RifleAppearanceDefinition(),
                [BaseItem.Shuriken] = new ShurikenAppearanceDefinition(),
                [BaseItem.ShortSpear] = new SpearAppearanceDefinition(),
                [BaseItem.TwoBladedSword] = new TwinBladeAppearanceDefinition(),
                [BaseItem.TwinElectroBlade] = new TwinElectrobladeAppearanceDefinition()
            };
        }
    }
}
