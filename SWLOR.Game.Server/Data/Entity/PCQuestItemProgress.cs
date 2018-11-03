
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("PCQuestItemProgress")]
    public partial class PCQuestItemProgress: IEntity
    {
        [Key]
        public int PCQuestItemProgressID { get; set; }
        public string PlayerID { get; set; }
        public int PCQuestStatusID { get; set; }
        public string Resref { get; set; }
        public int Remaining { get; set; }
        public bool MustBeCraftedByPlayer { get; set; }
    
        public virtual PCQuestStatus PCQuestStatus { get; set; }
        public virtual PlayerCharacter PlayerCharacter { get; set; }
    }
}
