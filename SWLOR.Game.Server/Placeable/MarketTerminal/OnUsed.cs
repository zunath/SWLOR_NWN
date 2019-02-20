using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Placeable.MarketTerminal
{
    public class OnUsed: IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IDialogService _dialog;

        public OnUsed(INWScript script, IDialogService dialog)
        {
            _ = script;
            _dialog = dialog;
        }

        public bool Run(params object[] args)
        {
            NWPlayer player = _.GetLastUsedBy();
            NWPlaceable device = Object.OBJECT_SELF;

            if (player.IsBusy)
            {
                player.SendMessage("You are too busy to do that right now.");
                return false;
            }

            _dialog.StartConversation(player, device, "MarketTerminal");
            return true;
        }
    }
}
