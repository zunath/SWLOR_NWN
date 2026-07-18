namespace SWLOR.Game.Server.Service.MasteryService
{
    // Note: labels mirror mastery-catalog.json's "category" field so seed/import tooling
    // and the catalog UI stay in sync. See MasteryCatalogSeed for the JSON -> enum mapping.
    public enum MasteryCategoryType
    {
        [MasteryCategory("CRAFT Agriculture")]
        CraftAgriculture = 0,

        [MasteryCategory("Armor")]
        Armor = 1,

        [MasteryCategory("Beast Mastery")]
        BeastMastery = 2,

        [MasteryCategory("Devices")]
        Devices = 3,

        [MasteryCategory("CRAFT Engineering")]
        CraftEngineering = 4,

        [MasteryCategory("CRAFT Fabrication")]
        CraftFabrication = 5,

        [MasteryCategory("First Aid")]
        FirstAid = 6,

        [MasteryCategory("FORCE Universal")]
        ForceUniversal = 7,

        [MasteryCategory("FORCE Light Side")]
        ForceLightSide = 8,

        [MasteryCategory("FORCE Dark Side")]
        ForceDarkSide = 9,

        [MasteryCategory("Gathering")]
        Gathering = 10,

        [MasteryCategory("Leadership")]
        Leadership = 11,

        [MasteryCategory("Martial Arts")]
        MartialArts = 12,

        [MasteryCategory("COMBAT Lightsaber")]
        CombatLightsaber = 13,

        [MasteryCategory("Piloting")]
        Piloting = 14,

        [MasteryCategory("CRAFT Smithing")]
        CraftSmithing = 15,

        [MasteryCategory("General")]
        General = 16,

        [MasteryCategory("Ranged")]
        Ranged = 17,

        [MasteryCategory("COMBAT Weapons")]
        CombatWeapons = 18
    }

    public class MasteryCategoryAttribute : Attribute
    {
        public string Label { get; set; }

        public MasteryCategoryAttribute(string label)
        {
            Label = label;
        }
    }
}
