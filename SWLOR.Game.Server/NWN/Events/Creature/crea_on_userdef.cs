﻿using SWLOR.Game.Server;
using SWLOR.Game.Server.Event.Creature;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    internal class crea_on_userdef
#pragma warning restore IDE1006 // Naming Styles
    {
        public static void Main()
        {
            App.RunEvent<OnUserDefined>();
        }
    }
}
