
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[PCMapProgression]")]
    public class PCMapProgression: IEntity
    {
        [Key]
        public int PCMapProgressionID { get; set; }
        public string PlayerID { get; set; }
        public string AreaResref { get; set; }
        public string Progression { get; set; }
    }
}
