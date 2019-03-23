
using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

using static NWN._;

namespace SWLOR.Game.Server.Event.Module
{
    internal class OnModuleUnequipItem : IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            NWObject equipper = Object.OBJECT_SELF;
            // Bioware Default
            _.ExecuteScript("x2_mod_def_unequ", equipper);

            if (equipper.GetLocalInt("IS_CUSTOMIZING_ITEM") == TRUE) return true; // Don't run heavy code when customizing equipment.

            SkillService.OnModuleItemUnequipped();
            PerkService.OnModuleItemUnequipped();
            HelmetToggleService.OnModuleItemUnequipped();
            ItemService.OnModuleUnequipItem();
            
            return true;
        }
    }
}
