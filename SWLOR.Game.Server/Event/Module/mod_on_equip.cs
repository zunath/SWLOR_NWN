using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWN;


// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    public class mod_on_equip
#pragma warning restore IDE1006 // Naming Styles
    {
        // ReSharper disable once UnusedMember.Local
        public static void Main()
        {
            NWObject equipper = NWScript.OBJECT_SELF;
            // Bioware Default
            NWScript.ExecuteScript("x2_mod_def_equ", equipper);

            MessageHub.Instance.Publish(new OnModuleEquipItem());
        }
    }
}
