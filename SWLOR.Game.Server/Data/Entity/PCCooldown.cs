
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("PCCooldowns")]
    public class PCCooldown: IEntity
    {
        [Key]
        public int PCCooldownID { get; set; }
        public string PlayerID { get; set; }
        public int CooldownCategoryID { get; set; }
        public System.DateTime DateUnlocked { get; set; }
    }
}
