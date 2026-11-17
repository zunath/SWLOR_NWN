namespace SWLOR.Game.Server.Domain.Creatures
{
    /// <summary>What placing a spawn produced: the object and whether it was a rare spawn.</summary>
    public class SpawnResult
    {
        public uint SpawnObject { get; set; }
        public bool IsRare { get; set; }
    }
}
