using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.AreaInstance
{
    public static class RimerCardsInstance
    {
        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnModuleLoad>(msg => CreateInstances());
        }

        private static void CreateInstances()
        {
            NWArea source = NWModule.Get().Areas.SingleOrDefault(x => x.Resref == "cardgame003");
            if (source == null) return;

            // Create 20 instances of the card game area.
            const int CopyCount = 20;

            for (int x = 1; x <= CopyCount; x++)
            {
                _.CopyArea(source);
            }

            Console.WriteLine("Created " + CopyCount + " copies of Rimer Cards areas.");
        }
    }
}
