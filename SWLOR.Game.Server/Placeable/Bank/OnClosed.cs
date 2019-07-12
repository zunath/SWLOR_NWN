using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Placeable.Bank
{
    public class OnClosed: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            NWPlaceable terminal = NWGameObject.OBJECT_SELF;
            
            terminal.DestroyAllInventoryItems();
            
            terminal.IsLocked = false;
            return true;
        }
    }
}
