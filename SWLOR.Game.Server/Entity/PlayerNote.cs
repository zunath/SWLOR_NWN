namespace SWLOR.Game.Server.Entity
{
    public class PlayerNote: EntityBase
    {
        [Indexed]
        public string PlayerId { get; set; }
        [Indexed]
        public string Name { get; set; }
        public string Text { get; set; }

        [Indexed]
        public bool IsDMNote { get; set; }
        [Indexed]
        public string DMCreatorName { get; set; }
        [Indexed]
        public string DMCreatorCDKey { get; set; }

        public PlayerNote()
        {
            IsDMNote = false;
            DMCreatorName = string.Empty;
            DMCreatorCDKey = string.Empty;
        }
    }
}
