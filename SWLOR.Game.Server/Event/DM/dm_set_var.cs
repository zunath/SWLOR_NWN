﻿using SWLOR.Game.Server;
using SWLOR.Game.Server.Event.DM;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWN.Events.Module;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    internal class dm_set_var
#pragma warning restore IDE1006 // Naming Styles
    {
        // ReSharper disable once UnusedMember.Local
        private static void Main()
        {
            MessageHub.Instance.Publish(new OnDMAction(31));
        }
    }
}