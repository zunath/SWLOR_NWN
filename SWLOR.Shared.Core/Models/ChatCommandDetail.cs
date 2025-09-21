using SWLOR.Shared.Core.Enums;

namespace SWLOR.Shared.Core.Models
{
    public class ChatCommandDetail
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public ChatCommandUserType UserType { get; set; }
        public bool IsEmote { get; set; }
    }
}
