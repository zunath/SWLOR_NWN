
using System;
using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("PCQuestStatus")]
    public class PCQuestStatus: IEntity
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public PCQuestStatus()
        {
            PCQuestItemProgresses = new HashSet<PCQuestItemProgress>();
            PCQuestKillTargetProgresses = new HashSet<PCQuestKillTargetProgress>();
        }

        [Key]
        public int PCQuestStatusID { get; set; }
        public string PlayerID { get; set; }
        public int QuestID { get; set; }
        public int CurrentQuestStateID { get; set; }
        public DateTime? CompletionDate { get; set; }
        public int? SelectedItemRewardID { get; set; }
    
        public virtual ICollection<PCQuestItemProgress> PCQuestItemProgresses { get; set; }
        public virtual ICollection<PCQuestKillTargetProgress> PCQuestKillTargetProgresses { get; set; }
    }
}
