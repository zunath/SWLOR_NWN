
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("PCSearchSites")]
    public partial class PCSearchSite: IEntity
    {
        [Key]
        public int PCSearchSiteID { get; set; }
        public string PlayerID { get; set; }
        public int SearchSiteID { get; set; }
        public System.DateTime UnlockDateTime { get; set; }
    
        public virtual PlayerCharacter PlayerCharacter { get; set; }
    }
}
