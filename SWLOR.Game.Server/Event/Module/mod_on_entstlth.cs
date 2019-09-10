using SWLOR.Game.Server;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.ValueObject;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    internal class mod_on_entstlth
#pragma warning restore IDE1006 // Naming Styles
    {
        // ReSharper disable once UnusedMember.Local
        private static void Main()
        {
            using (new Profiler(nameof(mod_on_entstlth)))
            {
                NWObject stealther = NWGameObject.OBJECT_SELF;
                _.SetActionMode(stealther, _.ACTION_MODE_STEALTH, _.FALSE);
                _.FloatingTextStringOnCreature("NWN stealth mode is disabled on this server.", stealther, _.FALSE);
            }

            MessageHub.Instance.Publish(new OnModuleEnterStealthAfter());
        }
    }
}