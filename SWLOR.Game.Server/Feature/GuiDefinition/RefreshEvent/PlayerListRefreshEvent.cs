using System.Collections.Generic;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.PlayerListService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent
{
    public class PlayerListRefreshEvent : IGuiRefreshEvent
    {
        public IReadOnlyList<PlayerListSubject> Subjects { get; }

        public PlayerListRefreshEvent(IReadOnlyList<PlayerListSubject> subjects)
        {
            Subjects = subjects;
        }
    }
}
