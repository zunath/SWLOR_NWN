
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("PCPerkRefunds")]
    public partial class PCPerkRefund: IEntity
    {
        [Key]
        public int PCPerkRefundID { get; set; }
        public string PlayerID { get; set; }
        public int PerkID { get; set; }
        public int Level { get; set; }
        public System.DateTime DateAcquired { get; set; }
        public System.DateTime DateRefunded { get; set; }
    
        public virtual Perk Perk { get; set; }
        public virtual PlayerCharacter PlayerCharacter { get; set; }
    }
}
