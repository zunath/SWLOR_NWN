using SWLOR.Game.Server;
using SWLOR.Game.Server.Event.Conversation.KeyItem;
using static NWN._;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    internal class any_keyitems_5
#pragma warning restore IDE1006 // Naming Styles
    {
        public int Main()
        {
            return KeyItemCheck.Check(5, 2) ? 1 : 0;
        }
    }
}
