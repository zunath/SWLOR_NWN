using SWLOR.Game.Server;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.Messaging;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    public static class mod_on_csabort
#pragma warning restore IDE1006 // Naming Styles
    {
        // ReSharper disable once UnusedMember.Local
        public static void Main()
        {
            MessageHub.Instance.Publish(new OnModuleCutsceneAbort());
        }
    }
}
