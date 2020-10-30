using SWLOR.Game.Server;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.Service.Legacy;
using SWLOR.Game.Server.ValueObject;
using static SWLOR.Game.Server.Core.NWScript.NWScript;


// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    public class mod_on_enter
#pragma warning restore IDE1006 // Naming Styles
    {
        // ReSharper disable once UnusedMember.Local
        public static void Main()
        {
            // The order of the following procedures matters.
            NWPlayer player = GetEnteringObject();

            using (new Profiler(nameof(mod_on_enter) + ":AddDMToCache"))
            {
                if (player.IsDM)
                {
                    AppCache.ConnectedDMs.Add(player);
                }
            }

            using (new Profiler(nameof(mod_on_enter) + ":BiowareDefault"))
            {
                player.DeleteLocalInt("IS_CUSTOMIZING_ITEM");
                ExecuteScript("dmfi_onclienter ", OBJECT_SELF); // DMFI also calls "x3_mod_def_enter"
            }

            using (new Profiler(nameof(mod_on_enter) + ":PlayerValidation"))
            {
                PlayerValidationService.OnModuleEnter();
            }

            using (new Profiler(nameof(mod_on_enter) + ":InitializePlayer"))
            {
                PlayerService.InitializePlayer(player);
            }

            using (new Profiler(nameof(mod_on_enter) + ":SkillServiceEnter"))
            {
                SkillService.OnModuleEnter();
            }

            using (new Profiler(nameof(mod_on_enter) + ":PerkServiceEnter"))
            {
                PerkService.OnModuleEnter();
            }
            
            MessageHub.Instance.Publish(new OnModuleEnter());
            SetLocalBool(player, "LOGGED_IN_ONCE", true);
        }
    }
}
