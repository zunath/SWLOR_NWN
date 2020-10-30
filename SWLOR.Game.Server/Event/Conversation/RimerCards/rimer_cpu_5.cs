
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Event.Conversation.RimerCards;
using SWLOR.Game.Server.ValueObject;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    internal static class rimer_cpu_5
#pragma warning restore IDE1006 // Naming Styles
    {
        public static int Main()
        {
            using (new Profiler(nameof(rimer_cpu_5)))
            {
                RimerCPU.ConfigureGameSettings(NWScript.OBJECT_SELF, RimerDeckType.Random, RimerAIDifficulty.Normal);
                return 0;
            }
        }
    }
}
