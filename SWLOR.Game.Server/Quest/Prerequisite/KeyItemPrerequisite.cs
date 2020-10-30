using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Quest.Contracts;
using SWLOR.Game.Server.Service.Legacy;

namespace SWLOR.Game.Server.Quest.Prerequisite
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
