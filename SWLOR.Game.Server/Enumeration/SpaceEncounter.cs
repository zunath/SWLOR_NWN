using System;

namespace SWLOR.Game.Server.Enumeration
{
    public enum SpaceEncounter
    {
        // Need to figure out how to better name these. Source data didn't have a name for them.

        [SpaceEncounter(Planet.Viscara, SpaceEncounterType.Customs, 15, 10, 52)]
        Encounter1 = 1,
        [SpaceEncounter(Planet.Viscara, SpaceEncounterType.Customs, 4, 20, 53)]
        Encounter2 = 2,
        [SpaceEncounter(Planet.Viscara, SpaceEncounterType.AsteroidField, 1, 5, 0)]
        Encounter3 = 3,
        [SpaceEncounter(Planet.Viscara, SpaceEncounterType.Salvage, 20, 15, 51)]
        Encounter4 = 4,
        [SpaceEncounter(Planet.Tatooine, SpaceEncounterType.Pirates, 20, 10, 52)]
        Encounter5 = 5,
        [SpaceEncounter(Planet.Tatooine, SpaceEncounterType.Pirates, 5, 20, 52)]
        Encounter6 = 6,
        [SpaceEncounter(Planet.Tatooine, SpaceEncounterType.Pirates, 1, 30, 53)]
        Encounter7 = 7,
        [SpaceEncounter(Planet.Tatooine, SpaceEncounterType.AsteroidField, 1, 15, 0)]
        Encounter8 = 8,
        [SpaceEncounter(Planet.Tatooine, SpaceEncounterType.Salvage, 15, 15, 51)]
        Encounter9 = 9,
        [SpaceEncounter(Planet.Tatooine, SpaceEncounterType.Salvage, 5, 25, 51)]
        Encounter10 = 10,
        [SpaceEncounter(Planet.Tatooine, SpaceEncounterType.Salvage, 3, 35, 51)]
        Encounter11 = 11,
        [SpaceEncounter(Planet.MonCala, SpaceEncounterType.Customs, 15, 40, 52)]
        Encounter12 = 12,
        [SpaceEncounter(Planet.MonCala, SpaceEncounterType.AsteroidField, 1, 40, 0)]
        Encounter13 = 13,
        [SpaceEncounter(Planet.MonCala, SpaceEncounterType.Salvage, 15, 45, 0)]
        Encounter14 = 14,
        [SpaceEncounter(Planet.Hutlar, SpaceEncounterType.Customs, 15, 40, 52)]
        Encounter15 = 15,
        [SpaceEncounter(Planet.Hutlar, SpaceEncounterType.AsteroidField, 1, 40, 0)]
        Encounter16 = 16,
        [SpaceEncounter(Planet.Hutlar, SpaceEncounterType.Salvage, 15, 45, 0)]
        Encounter17 = 17,
    }

    public class SpaceEncounterAttribute : Attribute
    {
        public Planet Planet { get; set; }
        public SpaceEncounterType Type { get; set; }
        public int Chance { get; set; }
        public int Difficulty { get; set; }
        public int LootTableID { get; set; }

        public SpaceEncounterAttribute(Planet planet, SpaceEncounterType type, int chance, int difficulty, int lootTableID)
        {
            Planet = planet;
            Type = type;
            Chance = chance;
            Difficulty = difficulty;
            LootTableID = lootTableID;
        }
    }
}
