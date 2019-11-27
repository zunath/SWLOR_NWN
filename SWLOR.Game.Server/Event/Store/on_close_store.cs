using SWLOR.Game.Server.Event.Store;
using SWLOR.Game.Server.Messaging;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    public class on_close_store
#pragma warning restore IDE1006 // Naming Styles
    {
        // ReSharper disable once UnusedMember.Local
        public void Main()
        {
            MessageHub.Instance.Publish(new OnCloseStore());
        }
    }
}