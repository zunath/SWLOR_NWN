
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("PCKeyItems")]
    public partial class PCKeyItem: IEntity
    {
        [Key]
        public int PCKeyItemID { get; set; }
        public string PlayerID { get; set; }
        public int KeyItemID { get; set; }
        public System.DateTime AcquiredDate { get; set; }
    
        public virtual KeyItem KeyItem { get; set; }
        public virtual PlayerCharacter PlayerCharacter { get; set; }
    }
}
