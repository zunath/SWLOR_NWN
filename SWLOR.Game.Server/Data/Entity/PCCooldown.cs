
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("PCCooldowns")]
    public partial class PCCooldown: IEntity
    {
        [Key]
        public int PCCooldownID { get; set; }
        public string PlayerID { get; set; }
        public int CooldownCategoryID { get; set; }
        public System.DateTime DateUnlocked { get; set; }
    
        public virtual CooldownCategory CooldownCategory { get; set; }
        public virtual PlayerCharacter PlayerCharacter { get; set; }
    }
}
