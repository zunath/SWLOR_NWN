using System;

namespace SWLOR.Game.Server.Enumeration
{
    public enum SpaceEncounter
    {
        // Need to figure out how to better name these. Source data didn't have a name for them.

        [SpaceEncounter(Planet.Viscara, SpaceEncounterType.Customs, 15, 10, LootTable.StarshipPiratesBasic)]
        Encounter1 = 1,
        [SpaceEncounter(Planet.Viscara, SpaceEncounterType.Customs, 4, 20, LootTable.StarshipPiratesRare)]
        Encounter2 = 2,
        [SpaceEncounter(Planet.Viscara, SpaceEncounterType.AsteroidField, 1, 5, LootTable.Invalid)]
        Encounter3 = 3,
        [SpaceEncounter(Planet.Viscara, SpaceEncounterType.Salvage, 20, 15, LootTable.SpaceBasicLoot)]
        Encounter4 = 4,
        [SpaceEncounter(Planet.Tatooine, SpaceEncounterType.Pirates, 20, 10, LootTable.StarshipPiratesBasic)]
        Encounter5 = 5,
        [SpaceEncounter(Planet.Tatooine, SpaceEncounterType.Pirates, 5, 20, LootTable.StarshipPiratesBasic)]
        Encounter6 = 6,
        [SpaceEncounter(Planet.Tatooine, SpaceEncounterType.Pirates, 1, 30, LootTable.StarshipPiratesRare)]
        Encounter7 = 7,
        [SpaceEncounter(Planet.Tatooine, SpaceEncounterType.AsteroidField, 1, 15, LootTable.Invalid)]
        Encounter8 = 8,
        [SpaceEncounter(Planet.Tatooine, SpaceEncounterType.Salvage, 15, 15, LootTable.SpaceBasicLoot)]
        Encounter9 = 9,
        [SpaceEncounter(Planet.Tatooine, SpaceEncounterType.Salvage, 5, 25, LootTable.SpaceBasicLoot)]
        Encounter10 = 10,
        [SpaceEncounter(Planet.Tatooine, SpaceEncounterType.Salvage, 3, 35, LootTable.SpaceBasicLoot)]
        Encounter11 = 11,
        [SpaceEncounter(Planet.MonCala, SpaceEncounterType.Customs, 15, 40, LootTable.StarshipPiratesBasic)]
        Encounter12 = 12,
        [SpaceEncounter(Planet.MonCala, SpaceEncounterType.AsteroidField, 1, 40, LootTable.Invalid)]
        Encounter13 = 13,
        [SpaceEncounter(Planet.MonCala, SpaceEncounterType.Salvage, 15, 45, LootTable.Invalid)]
        Encounter14 = 14,
        [SpaceEncounter(Planet.Hutlar, SpaceEncounterType.Customs, 15, 40, LootTable.StarshipPiratesBasic)]
        Encounter15 = 15,
        [SpaceEncounter(Planet.Hutlar, SpaceEncounterType.AsteroidField, 1, 40, LootTable.Invalid)]
        Encounter16 = 16,
        [SpaceEncounter(Planet.Hutlar, SpaceEncounterType.Salvage, 15, 45, LootTable.Invalid)]
        Encounter17 = 17,
    }

    public class SpaceEncounterAttribute : Attribute
    {
        public Planet Planet { get; set; }
        public SpaceEncounterType Type { get; set; }
        public int Chance { get; set; }
        public int Difficulty { get; set; }
        public LootTable LootTable { get; set; }

        public SpaceEncounterAttribute(Planet planet, SpaceEncounterType type, int chance, int difficulty, LootTable lootTable)
        {
            Planet = planet;
            Type = type;
            Chance = chance;
            Difficulty = difficulty;
            LootTable = lootTable;
        }
    }
}
