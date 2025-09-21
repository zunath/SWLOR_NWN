using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Enums;

namespace SWLOR.Shared.Abstractions.Models
{
    public class ChatCommandDetail
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public ChatCommandUserType UserType { get; set; }
        public bool IsEmote { get; set; }
    }
}
