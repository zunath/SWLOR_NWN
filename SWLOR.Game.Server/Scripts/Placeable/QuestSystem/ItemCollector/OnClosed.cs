using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Scripts.Placeable.QuestSystem.ItemCollector
{
    public class OnClosed: IScript
    {
        public void SubscribeEvents()
        {
        }

        public void UnsubscribeEvents()
        {
        }

        public void Main()
        {
            NWObject container = NWScript.OBJECT_SELF;
            container.DestroyAllInventoryItems();
            container.Destroy();
        }
    }
}
