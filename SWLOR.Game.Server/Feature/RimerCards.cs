using System;
using SWLOR.Game.Server.Service;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature
{
    public static class RimerCards
    {
        public static void CreateInstances()
        {
            var source = Cache.GetAreaByResref("cardgame003");
            if (!GetIsObjectValid(source)) return;

            // Create 20 instances of the card game area.
            const int CopyCount = 20;

            for (var x = 1; x <= CopyCount; x++)
            {
                CopyArea(source);
            }

            Console.WriteLine("Created " + CopyCount + " copies of Rimer Cards areas.");
        }
    }
}
