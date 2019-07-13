using SWLOR.Game.Server;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;


// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    internal class mod_on_equip
#pragma warning restore IDE1006 // Naming Styles
    {
        // ReSharper disable once UnusedMember.Local
        private static void Main()
        {
            NWObject equipper = NWGameObject.OBJECT_SELF;
            // Bioware Default
            _.ExecuteScript("x2_mod_def_equ", equipper);

            MessageHub.Instance.Publish(new OnModuleEquipItem());
        }
    }
}
