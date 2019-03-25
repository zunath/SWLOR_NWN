using SWLOR.Game.Server;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWN.Events.Module;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.ValueObject;


// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    internal class mod_on_enter
#pragma warning restore IDE1006 // Naming Styles
    {
        // ReSharper disable once UnusedMember.Local
        private static void Main()
        {
            // The order of the following procedures matters.
            NWPlayer player = _.GetEnteringObject();

            using (new Profiler(nameof(mod_on_enter)))
            {   
                if (player.IsDM)
                {
                    AppCache.ConnectedDMs.Add(player);
                }

                player.DeleteLocalInt("IS_CUSTOMIZING_ITEM");
                _.ExecuteScript("dmfi_onclienter ", Object.OBJECT_SELF); // DMFI also calls "x3_mod_def_enter"
                PlayerValidationService.OnModuleEnter();
                PlayerService.InitializePlayer(player);
                DataService.CachePlayerData(player);
                SkillService.OnModuleEnter();
                PerkService.OnModuleEnter();
            }
            
            MessageHub.Instance.Publish(new OnModuleEnter());
            player.SetLocalInt("LOGGED_IN_ONCE", _.TRUE);
        }
    }
}
