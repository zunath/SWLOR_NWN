using System;

namespace SWLOR.Game.Server.Enumeration
{
    public enum Starport
    {
        [Starport("6D9E3CFE-70B1-4B77-8166-10C4F5B0DA9D", "Viscara", "Veles Starport", 20, 400, "VISCARA_LANDING")]
        Viscara = 1,
        [Starport("E38D63C5-D595-4DC8-873A-35151229CD6F", "Tatooine", "Mos Eisley Starport", 5, 400, "TATOOINE_LANDING")]
        Tatooine = 2,
        [Starport("498EA709-7638-4A6E-9BE0-6F223EBA6C4A", "Mon Cala", "Dac City Starport", 50, 400, "MON_CALA_LANDING")]
        MonCala = 3,
        [Starport("4A882E34-437E-4300-ACE4-D43428F2CFE0", "Hutlar", "Hutlar Outpost Starport", 50, 400, "HUTLAR_LANDING")]
        Hutlar = 4
    }

    public class StarportAttribute : Attribute
    {
        public Guid StarportID { get; set; }
        public string PlanetName { get; set; }
        public string Name { get; set; }
        public int CustomsDC { get; set; }
        public int Cost { get; set; }
        public string WaypointTag { get; set; }

        public StarportAttribute(string starportID, string planetName, string name, int customsDC, int cost, string waypointTag)
        {
            StarportID = new Guid(starportID);
            PlanetName = planetName;
            Name = name;
            CustomsDC = customsDC;
            Cost = cost;
            WaypointTag = waypointTag;
        }
    }
}
