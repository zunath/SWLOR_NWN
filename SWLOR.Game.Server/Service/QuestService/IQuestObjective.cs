using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Data.Entity;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Service;
using Player = SWLOR.Shared.Core.Data.Entity.Player;

namespace SWLOR.Game.Server.Service.QuestService
{
    public interface IQuestObjective
    {
        void Initialize(uint player, string questId);
        void Advance(uint player, string questId);
        bool IsComplete(uint player, string questId);
        string GetCurrentStateText(uint player, string questId);
    }
}
