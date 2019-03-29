using SWLOR.Game.Server.Event.Area;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.ValueObject;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    internal static class area_on_enter
#pragma warning restore IDE1006 // Naming Styles
    {
        public static void Main()
        {
            using (new Profiler(nameof(area_on_enter)))
            {
                // Location loading code is in the BaseService, to support
                // logging on within an instance.  This must be called before
                // the other services.
                BaseService.OnAreaEnter();
            }

            MessageHub.Instance.Publish(new OnAreaEnter());
        }
    }
}
