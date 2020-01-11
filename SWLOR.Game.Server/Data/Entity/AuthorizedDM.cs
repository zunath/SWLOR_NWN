using Newtonsoft.Json;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [JsonObject(MemberSerialization.OptIn)]
    public class AuthorizedDM: IEntity
    {
        [Key]
        [JsonProperty]
        public int ID { get; set; }
        [JsonProperty]
        public string Name { get; set; }
        [JsonProperty]
        public string CDKey { get; set; }
        [JsonProperty]
        public int DMRole { get; set; }
        [JsonProperty]
        public bool IsActive { get; set; }
    }
}
