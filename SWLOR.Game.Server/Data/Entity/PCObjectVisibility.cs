
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("PCObjectVisibility")]
    public partial class PCObjectVisibility: IEntity
    {
        [Key]
        public int PCObjectVisibilityID { get; set; }
        public string PlayerID { get; set; }
        public string VisibilityObjectID { get; set; }
        public bool IsVisible { get; set; }
    
        public virtual PlayerCharacter PlayerCharacter { get; set; }
    }
}
