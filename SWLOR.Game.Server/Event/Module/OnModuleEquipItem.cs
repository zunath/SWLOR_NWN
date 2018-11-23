using NWN;
using SWLOR.Game.Server.GameObject;

using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Event.Module
{
    internal class OnModuleEquipItem : IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IDurabilityService _durability;
        private readonly ISkillService _skill;
        private readonly IPerkService _perk;
        private readonly IItemService _item;
        private readonly IHelmetToggleService _helmetToggle;

        public OnModuleEquipItem(INWScript script,
            IDurabilityService durability,
            ISkillService skill,
            IPerkService perk,
            IItemService item,
            IHelmetToggleService helmetToggle)
        {
            _ = script;
            _durability = durability;
            _skill = skill;
            _perk = perk;
            _item = item;
            _helmetToggle = helmetToggle;
        }

        public bool Run(params object[] args)
        {
            NWObject equipper = Object.OBJECT_SELF;
            // Bioware Default
            _.ExecuteScript("x2_mod_def_equ", equipper);

            if (equipper.GetLocalInt("IS_CUSTOMIZING_ITEM") == TRUE) return true; // Don't run heavy code when customizing equipment.

            _durability.OnModuleEquip();
            _skill.OnModuleItemEquipped();
            _perk.OnModuleItemEquipped();
            _item.OnModuleEquipItem();
            HandleEquipmentSwappingDelay();
            _helmetToggle.OnModuleItemEquipped();
            return true;

        }


        // Players abuse a bug in NWN which allows them to gain an extra attack.
        // To work around this I force them to clear all actions.
        private void HandleEquipmentSwappingDelay()
        {
            NWPlayer oPC = (_.GetPCItemLastEquippedBy());
            NWItem oItem = (_.GetPCItemLastEquipped());
            NWItem rightHand = oPC.RightHand;
            NWItem leftHand = oPC.LeftHand;

            if (!oPC.IsInCombat) return;
            if (Equals(oItem, rightHand) && Equals(oItem, leftHand)) return;
            if (!Equals(oItem, leftHand)) return;

            oPC.ClearAllActions();
        }
    }
}
