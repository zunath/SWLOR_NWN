using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Quest.Contracts;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Quest.Prerequisite
{
    public class KeyItemPrerequisite: IQuestPrerequisite
    {
        private readonly KeyItem _keyItemID;

        public KeyItemPrerequisite(KeyItem keyItemID)
        {
            _keyItemID = keyItemID;
        }

        public bool MeetsPrerequisite(NWPlayer player)
        {
            return KeyItemService.PlayerHasKeyItem(player, _keyItemID);
        }
    }
}
