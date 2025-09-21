using SWLOR.Shared.Core.Enums;

namespace SWLOR.Shared.Core.Models
{
    public class QuestDetail
    {
        public string QuestId { get; set; }
        public string Name { get; set; }
        public bool IsRepeatable { get; set; }
        public GuildType GuildType { get; set; } = GuildType.Invalid;
        public int GuildRank { get; set; } = -1;
        public bool AllowRewardSelection { get; set; }

        public QuestDetail()
        {
            QuestId = string.Empty;
            Name = string.Empty;
        }
    }
}
