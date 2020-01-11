
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Data.Entity
{
    [JsonObject(MemberSerialization.OptIn)]
    public class PCQuestStatus: IEntity
    {
        public PCQuestStatus()
        {
            ID = Guid.NewGuid();

            KillTargets = new Dictionary<NPCGroup, int>();
            Items = new Dictionary<string, PCQuestItemProgress>();
        }
        [Key]
        [JsonProperty]
        public Guid ID { get; set; }
        [JsonProperty]
        public Guid PlayerID { get; set; }
        [JsonProperty]
        public int QuestID { get; set; }
        [JsonProperty]
        public int QuestState { get; set; }
        [JsonProperty]
        public DateTime? CompletionDate { get; set; }
        [JsonProperty]
        public int TimesCompleted { get; set; }

        [JsonProperty]
        public Dictionary<NPCGroup, int> KillTargets { get; set; }
        [JsonProperty]
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
