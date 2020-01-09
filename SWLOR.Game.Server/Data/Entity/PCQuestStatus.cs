
using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Data.Entity
{
    public class PCQuestStatus: IEntity
    {
        public PCQuestStatus()
        {
            ID = Guid.NewGuid();

            KillTargets = new Dictionary<NPCGroup, int>();
            Items = new Dictionary<string, PCQuestItemProgress>();
        }
        [Key]
        public Guid ID { get; set; }
        public Guid PlayerID { get; set; }
        public int QuestID { get; set; }
        public int QuestState { get; set; }
        public DateTime? CompletionDate { get; set; }
        public int TimesCompleted { get; set; }

        public Dictionary<NPCGroup, int> KillTargets { get; set; }
        public Dictionary<string, PCQuestItemProgress> Items { get; set; }
    }

    public class PCQuestItemProgress
    {
        public int Remaining { get; set; }
        public bool MustBeCraftedByPlayer { get; set; }

        public PCQuestItemProgress(int remaining, bool mustBeCraftedByPlayer)
        {
            Remaining = remaining;
            MustBeCraftedByPlayer = mustBeCraftedByPlayer;
        }
    }
}
