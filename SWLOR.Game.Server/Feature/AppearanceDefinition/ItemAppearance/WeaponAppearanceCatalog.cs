using System.Collections.Generic;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance
{
    /// <summary>
    /// The weapon appearance definition of each base item the appearance editor can restyle. The container builds it once, with
    /// the definitions, so no boot step fills it (ServiceRegistrations).
    /// </summary>
    public class WeaponAppearanceCatalog : IWeaponAppearanceCatalog
    {
        public IReadOnlyDictionary<BaseItem, WeaponAppearanceBaseDefinition> Definitions { get; }

        public WeaponAppearanceCatalog()
        {
            Definitions = Build();
        }

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
