using SWLOR.Game.Server;
using SWLOR.Game.Server.Event.DM;
using SWLOR.Game.Server.Messaging;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    public class dm_toggle_lock
#pragma warning restore IDE1006 // Naming Styles
    {
        // ReSharper disable once UnusedMember.Local
        public void Main()
        {
            MessageHub.Instance.Publish(new OnDMAction(18));
        }
    }
}