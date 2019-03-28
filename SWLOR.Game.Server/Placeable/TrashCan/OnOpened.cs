using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;


namespace SWLOR.Game.Server.Placeable.TrashCan
{
    public class OnOpened: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            NWPlayer oPC = (_.GetLastOpenedBy());
            oPC.FloatingText("Any item placed inside this trash can will be destroyed permanently.");

            return true;
        }
    }
}
