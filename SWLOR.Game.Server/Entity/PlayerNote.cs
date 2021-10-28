namespace SWLOR.Game.Server.Entity
{
    public class PlayerNote: EntityBase
    {
        [Indexed]
        public string PlayerId { get; set; }
        [Indexed]
        public string Name { get; set; }
        public string Text { get; set; }
    }
}
