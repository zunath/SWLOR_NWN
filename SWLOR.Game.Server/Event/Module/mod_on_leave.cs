using SWLOR.Game.Server;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.ValueObject;


// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    internal class mod_on_leave
#pragma warning restore IDE1006 // Naming Styles
    {
        // ReSharper disable once UnusedMember.Local
        private static void Main()
        {
            NWPlayer pc = (_.GetExitingObject());

            using (new Profiler(nameof(mod_on_leave) + ":RemoveDMFromCache"))
            {
                if (pc.IsDM)
                {
                    AppCache.ConnectedDMs.Remove(pc);
                }
            }

            using(new Profiler(nameof(mod_on_leave) + ":ExportSingleCharacter"))
            {
                if (pc.IsPlayer)
                {
                    _.ExportSingleCharacter(pc.Object);
                }
            }


            MessageHub.Instance.Publish(new OnModuleLeave());
        }
    }
}
