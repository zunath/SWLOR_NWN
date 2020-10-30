using SWLOR.Game.Server;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.ValueObject;


// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    public class mod_on_leave
#pragma warning restore IDE1006 // Naming Styles
    {
        // ReSharper disable once UnusedMember.Local
        public static void Main()
        {
            NWPlayer pc = (NWScript.GetExitingObject());

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
                    NWScript.ExportSingleCharacter(pc.Object);
                }
            }


            MessageHub.Instance.Publish(new OnModuleLeave());
        }
    }
}
