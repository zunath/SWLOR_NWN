using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Scripts.Placeable.Bank
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
            NWPlaceable terminal = NWScript.OBJECT_SELF;
            terminal.DestroyAllInventoryItems();
            terminal.IsLocked = false;
        }
    }
}
