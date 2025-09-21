using SWLOR.Shared.Abstractions;

namespace SWLOR.Game.Server.Entity
{
    public class AreaNote: EntityBase
    {
        [Indexed]
        public string AreaResref { get; set; }
        public string PublicText { get; set; }
        public string PrivateText { get; set; }

        public AreaNote()
        {
            PublicText = string.Empty;
            PrivateText = string.Empty;
        }
    }
}
