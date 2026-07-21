namespace SWLOR.Game.Server.Entity
{
    public class PlayerNoteCategory: EntityBase
    {
        [Indexed]
        public string PlayerId { get; set; }
        [Indexed]
        public string Name { get; set; }

        public PlayerNoteCategory()
        {
            PlayerId = string.Empty;
            Name = string.Empty;
        }
    }
}
