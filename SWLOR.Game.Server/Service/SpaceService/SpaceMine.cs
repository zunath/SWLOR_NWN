namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>A mine a Minelayer dropped: it detonates on the first enemy ship that comes close.</summary>
    public class SpaceMine
    {
        public uint Mine { get; set; }

        /// <summary>The minelayer. Its mines go when it does, and the detonation is its shot.</summary>
        public uint Owner { get; set; }

        public DateTime LaidAt { get; set; }

        public int Dmg { get; set; }

        /// <summary>A standalone minefield mine (W11o): no minelayer, no lifetime; it fires as its tier's minelayer.</summary>
        public bool Field { get; set; }

        /// <summary>A field mine's tier.</summary>
        public int Tier { get; set; }

        /// <summary>A field mine laid in a Wild Space system: it hits as the Rift Raider Minelayer.</summary>
        public bool WildSpace { get; set; }
    }
}