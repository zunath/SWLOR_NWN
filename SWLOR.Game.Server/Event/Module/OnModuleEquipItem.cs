using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

using static NWN._;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Event.Module
{
    internal class OnModuleEquipItem : IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            NWObject equipper = Object.OBJECT_SELF;
            // Bioware Default
            _.ExecuteScript("x2_mod_def_equ", equipper);

            if (equipper.GetLocalInt("IS_CUSTOMIZING_ITEM") == TRUE) return true; // Don't run heavy code when customizing equipment.

            DurabilityService.OnModuleEquip();
            SkillService.OnModuleItemEquipped();
            PerkService.OnModuleItemEquipped();
            ItemService.OnModuleEquipItem();
            HandleEquipmentSwappingDelay();
            HelmetToggleService.OnModuleItemEquipped();
            SpaceService.OnModuleItemEquipped();
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
