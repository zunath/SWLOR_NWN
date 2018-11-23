
using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Event.Module
{
    internal class OnModuleUnequipItem : IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly ISkillService _skill;
        private readonly IPerkService _perk;
        private readonly IHelmetToggleService _helmetToggle;
        private readonly IItemService _item;

        public OnModuleUnequipItem(
            INWScript script,
            ISkillService skill,
            IPerkService perk,
            IHelmetToggleService helmetToggle,
            IItemService item)
        {
            _ = script;
            _skill = skill;
            _perk = perk;
            _helmetToggle = helmetToggle;
            _item = item;
        }

        public bool Run(params object[] args)
        {
            NWObject equipper = Object.OBJECT_SELF;
            // Bioware Default
            _.ExecuteScript("x2_mod_def_unequ", equipper);

            if (equipper.GetLocalInt("IS_CUSTOMIZING_ITEM") == TRUE) return true; // Don't run heavy code when customizing equipment.

            _skill.OnModuleItemUnequipped();
            _perk.OnModuleItemUnequipped();
            _helmetToggle.OnModuleItemUnequipped();
            _item.OnModuleUnequipItem();
            
            return true;
        }
    }
}
