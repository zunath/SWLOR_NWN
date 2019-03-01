using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.NWNX.Contracts;

namespace SWLOR.Game.Server.Placeable.AtomicReassembler
{
    public class OnOpened: IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly INWNXItemProperty _nwnxItemProperty;

        public OnOpened(INWScript script, INWNXItemProperty nwnxItemProperty)
        {
            _ = script;
            _nwnxItemProperty = nwnxItemProperty;
        }

        public bool Run(params object[] args)
        {
            NWPlayer player = _.GetLastOpenedBy();
            player.FloatingText("Please insert an item you would like to reassemble.");
            return true;
        }
    }
}
