using SWLOR.Game.Server;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.NWN.Enum;
using SWLOR.Game.Server.ValueObject;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    public class mod_on_entstlth
#pragma warning restore IDE1006 // Naming Styles
    {
        // ReSharper disable once UnusedMember.Local
        public static void Main()
        {
            using (new Profiler(nameof(mod_on_entstlth)))
            {
                NWObject stealther = _.OBJECT_SELF;
                _.SetActionMode(stealther, ActionMode.Stealth, false);
                _.FloatingTextStringOnCreature("NWN stealth mode is disabled on this server.", stealther, false);
            }

            MessageHub.Instance.Publish(new OnModuleEnterStealthAfter());
        }
    }
}