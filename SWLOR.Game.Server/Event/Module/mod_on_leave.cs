using SWLOR.Game.Server;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWN.Events.Module;
using SWLOR.Game.Server.Service;


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

            if (pc.IsDM)
            {
                AppCache.ConnectedDMs.Remove(pc);
            }

            if (pc.IsPlayer)
            {
                _.ExportSingleCharacter(pc.Object);
            }

            MessageHub.Instance.Publish(new OnModuleLeave());


            DataService.RemoveCachedPlayerData(pc); // Ensure this is called LAST.
        }
    }
}
