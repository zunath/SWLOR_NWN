namespace SWLOR.Game.Server.Entity
{
    public class AreaNote: EntityBase
    {
        [Indexed]
        public string AreaResrefId { get; set; }
        public string PublicText { get; set; }
        public string PrivateText { get; set; }

        public AreaNote()
        {
            PublicText = string.Empty;
            PrivateText = string.Empty;
        }
    }
}
