
using NWN;
using SWLOR.Game.Server.Service.Contracts;

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
            // Bioware Default
            _.ExecuteScript("x2_mod_def_unequ", Object.OBJECT_SELF);
            _skill.OnModuleItemUnequipped();
            _perk.OnModuleItemUnequipped();
            _helmetToggle.OnModuleItemUnequipped();
            _item.OnModuleUnequipItem();
            
            return true;
        }
    }
}
