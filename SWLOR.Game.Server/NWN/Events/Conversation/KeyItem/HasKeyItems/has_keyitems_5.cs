using SWLOR.Game.Server;
using SWLOR.Game.Server.Event.Conversation;
using static NWN._;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    internal class has_keyitems_5
#pragma warning restore IDE1006 // Naming Styles
    {
        public static int Main()
        {
            return App.RunEvent<KeyItemCheck>(5, 1) ? TRUE : FALSE;
        }
    }
}
