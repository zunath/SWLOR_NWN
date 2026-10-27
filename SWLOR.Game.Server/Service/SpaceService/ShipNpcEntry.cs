namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// The space NPC ship preset of one creature tag. Rows come from the generated RevampNpcShipCatalog.
    /// Defense excludes the pilot's VIT and Evasion excludes AGI x 3, which the shot math adds.
    /// </summary>
    public class ShipNpcEntry
    {
        public string CreatureTag { get; set; }
        public int Level { get; set; }
        public string Difficulty { get; set; }
        public string Role { get; set; }
        public ShipSize Size { get; set; }
        public int Hull { get; set; }
        public int Shield { get; set; }
        public double ShieldRegenPerSecond { get; set; }
        public int Attack { get; set; }
        public int Accuracy { get; set; }
        public int Defense { get; set; }
        public int Evasion { get; set; }
        public int ResistAll { get; set; }
        public int Agility { get; set; }
        public int Perception { get; set; }
        public int Vitality { get; set; }
        public int Willpower { get; set; }
        public int Might { get; set; }
        public ShipNpcWeapon[] Weapons { get; set; } = new ShipNpcWeapon[0];
    }
}
