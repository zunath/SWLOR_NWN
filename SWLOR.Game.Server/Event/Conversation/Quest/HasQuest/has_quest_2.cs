﻿using SWLOR.Game.Server;
using SWLOR.Game.Server.Event.Conversation.Quest.HasQuest;
using static NWN._;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    internal class has_quest_2
#pragma warning restore IDE1006 // Naming Styles
    {
        public int Main()
        {
            return QuestCheck.Check(2) ? 1 : 0;
        }
    }
}
