using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Quest.Contracts;
using SWLOR.Game.Server.Legacy.Service;

namespace SWLOR.Game.Server.Legacy.Quest.Prerequisite
{
    public class KeyItemPrerequisite: IQuestPrerequisite
    {
        private readonly int _keyItemID;

        public KeyItemPrerequisite(int keyItemID)
        {
            _keyItemID = keyItemID;
        }

        public bool MeetsPrerequisite(NWPlayer player)
        {
            return KeyItemService.PlayerHasKeyItem(player, _keyItemID);
        }
    }
}
