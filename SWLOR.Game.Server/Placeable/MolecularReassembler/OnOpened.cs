using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWNX.Contracts;

namespace SWLOR.Game.Server.Placeable.MolecularReassembler
{
    public class OnOpened: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            NWPlayer player = _.GetLastOpenedBy();
            player.FloatingText("Please insert an item you would like to reassemble.");
            return true;
        }
    }
}
