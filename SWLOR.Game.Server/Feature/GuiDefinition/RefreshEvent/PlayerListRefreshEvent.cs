using System.Collections.Generic;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.Social;

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
