﻿using SWLOR.Game.Server;
using SWLOR.Game.Server.Event.Conversation.Quest.AcceptQuest;
using static NWN._;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    internal class accept_quest_6
#pragma warning restore IDE1006 // Naming Styles
    {
        public int Main()
        {
            return QuestAccept.Check(6) ? 1 : 0;
        }
    }
}
