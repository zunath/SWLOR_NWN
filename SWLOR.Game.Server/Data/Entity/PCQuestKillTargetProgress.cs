
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[PCQuestKillTargetProgress]")]
    public class PCQuestKillTargetProgress: IEntity
    {
        [Key]
        public int PCQuestKillTargetProgressID { get; set; }
        public string PlayerID { get; set; }
        public int PCQuestStatusID { get; set; }
        public int NPCGroupID { get; set; }
        public int RemainingToKill { get; set; }
    }
}
