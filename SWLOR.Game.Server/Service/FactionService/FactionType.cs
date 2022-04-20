using System;

namespace SWLOR.Game.Server.Service.FactionService
{
    public enum FactionType
    {
        [Faction("Invalid", "")]
        Invalid = 0,
        [Faction("Republic Military", "")]
        RepublicMilitary = 1,
        [Faction("Jedi Order", "")]
        Jedi = 2,
        [Faction("Sith Military", "")]
        SithMilitary = 3,
        [Faction("Sith Order", "")]
        Sith = 4,
        [Faction("Hutt Cartel", "")]
        HuttCartel = 5,
        [Faction("Smugglers", "")]
        Smuggler = 6,
        [Faction("Czerka Corporation", "")]
        Czerka = 7,
        [Faction("Mandalorian", "")]
        Mandalorian = 8
    }

    public class FactionAttribute : Attribute
    {
        public string Name { get; set; }
        public string Description { get; set; }

        public FactionAttribute(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }
}
