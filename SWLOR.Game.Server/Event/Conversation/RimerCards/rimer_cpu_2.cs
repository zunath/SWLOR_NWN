﻿
using SWLOR.Game.Server.Event.Conversation.RimerCards;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.ValueObject;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    internal class rimer_cpu_2
#pragma warning restore IDE1006 // Naming Styles
    {
        public int Main()
        {
            using (new Profiler(nameof(rimer_cpu_2)))
            {
                RimerDeckType deck = RandomService.Random(4) <= 3 ? RimerDeckType.Goblins : RimerDeckType.Wolves;
                RimerCPU.ConfigureGameSettings(NWGameObject.OBJECT_SELF, deck, RimerAIDifficulty.Easy);
                return 0;
            }
        }
    }
}
