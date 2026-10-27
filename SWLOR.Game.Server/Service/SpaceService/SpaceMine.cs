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
    }
}