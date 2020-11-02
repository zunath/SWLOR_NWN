using System;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Messaging;
using SWLOR.Game.Server.Legacy.Service;
using SWLOR.Game.Server.Service;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Legacy.Event.Module
{
    public static class ModuleNWNXEvents
    {
        [NWNEventHandler("examine_aft")]
        public static void OnNWNXExamine()
        {
            // Breaking the rules for the examine event because the result of the services is used in the following 
            // service call. We still signal an event for this, but in general all of the logic should go into this method.
            NWPlayer examiner = (OBJECT_SELF);
            NWObject examinedObject = StringToObject(Events.GetEventData("EXAMINEE_OBJECT_ID"));
            if (ExaminationService.OnModuleExamine(examiner, examinedObject))
            {
                MessageHub.Instance.Publish(new OnModuleExamine());
                return;
            }

            string description;

            if (GetIsPC(examinedObject.Object))
            {
                // https://github.com/zunath/SWLOR_NWN/issues/853
                // safest probably to get the modified (non-original) description only for players
                // may want to always get the modified description for later flexibility?
                description = GetDescription(examinedObject.Object) + "\n\n";
            }
            else
            {
                description = GetDescription(examinedObject.Object, true) + "\n\n";
            }

            if (examinedObject.IsCreature)
            {
                var racialID = Convert.ToInt32(Get2DAString("racialtypes", "Name", (int)GetRacialType(examinedObject)));
                var racialtype = GetStringByStrRef(racialID);
                if (!description.Contains(ColorToken.Green("Racial Type: ") + racialtype))
                {
                    description += ColorToken.Green("Racial Type: ") + racialtype;
                }
            }

            description = ModService.OnModuleExamine(description, examiner, examinedObject);
            description = ItemService.OnModuleExamine(description, examinedObject);
            description = DurabilityService.OnModuleExamine(description, examinedObject);

            if (!string.IsNullOrWhiteSpace(description))
            {
                SetDescription(examinedObject.Object, description, false);
                SetDescription(examinedObject.Object, description);
            }
            
            MessageHub.Instance.Publish(new OnModuleExamine());
        }

        [NWNEventHandler("on_nwnx_dmg")]
        public static void OnNWNXApplyDamage()
        {
            MessageHub.Instance.Publish(new OnModuleApplyDamage(), false);
        }

        [NWNEventHandler("mod_attack")]
        public static void OnNWNXAttack()
        {
            MessageHub.Instance.Publish(new OnModuleAttack());
        }

        [NWNEventHandler("feat_use_bef")]
        public static void OnNWNXUseFeat()
        {
            MessageHub.Instance.Publish(new OnModuleUseFeat());
        }

        [NWNEventHandler("on_nwnx_chat")]
        public static void OnNWNXChat()
        {
            MessageHub.Instance.Publish(new OnModuleNWNXChat());
        }

        [NWNEventHandler("stlent_ent_aft")]
        public static void OnNWNXEnterStealth()
        {
            NWObject stealther = OBJECT_SELF;
            SetActionMode(stealther, ActionMode.Stealth, false);
            FloatingTextStringOnCreature("NWN stealth mode is disabled on this server.", stealther, false);
            MessageHub.Instance.Publish(new OnModuleEnterStealthAfter());
        }
    }
}
