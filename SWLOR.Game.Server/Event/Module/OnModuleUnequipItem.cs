
using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.Contracts;
using static NWN._;

namespace SWLOR.Game.Server.Event.Module
{
    internal class OnModuleUnequipItem : IRegisteredEvent
    {
        
        
        
        private readonly IHelmetToggleService _helmetToggle;
        

        public OnModuleUnequipItem(
            
            
            
            IHelmetToggleService helmetToggle
            )
        {
            
            
            
            _helmetToggle = helmetToggle;
            
        }

        public bool Run(params object[] args)
        {
            NWObject equipper = Object.OBJECT_SELF;
            // Bioware Default
            _.ExecuteScript("x2_mod_def_unequ", equipper);

            if (equipper.GetLocalInt("IS_CUSTOMIZING_ITEM") == TRUE) return true; // Don't run heavy code when customizing equipment.

            SkillService.OnModuleItemUnequipped();
            PerkService.OnModuleItemUnequipped();
            _helmetToggle.OnModuleItemUnequipped();
            ItemService.OnModuleUnequipItem();
            
            return true;
        }
    }
}
