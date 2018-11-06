﻿using System;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IDialogService
    {
        int NumberOfResponsesPerPage { get; }
        PlayerDialog LoadPlayerDialog(Guid globalID);
        void RemovePlayerDialog(Guid globalID);
        void LoadConversation(NWPlayer player, NWObject talkTo, string @class, int dialogNumber);
        void StartConversation(NWPlayer player, NWObject talkTo, string @class);
        void StartConversation(NWCreature player, NWObject talkTo, string @class);
        void EndConversation(NWPlayer player);
    }
}
