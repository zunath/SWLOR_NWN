using System;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.ValueObject;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    public class mod_on_examine
#pragma warning restore IDE1006 // Naming Styles
    {
        // ReSharper disable once UnusedMember.Local
        public static void Main()
        {
            // Breaking the rules for the examine event because the result of the services is used in the following 
            // service call. We still signal an event for this, but in general all of the logic should go into this method.

            using (new Profiler(nameof(mod_on_examine)))
            {
                NWPlayer examiner = (OBJECT_SELF);
                NWObject examinedObject = StringToObject(Events.GetEventData("EXAMINEE_OBJECT_ID"));
                if (ExaminationService.OnModuleExamine(examiner, examinedObject))
                {
                    MessageHub.Instance.Publish(new OnModuleExamine());
                    return;
                }

                string description;

                if (GetIsPC(examinedObject.Object) == true)
                {
                    // https://github.com/zunath/SWLOR_NWN/issues/853
                    // safest probably to get the modified (non-original) description only for players
                    // may want to always get the modified description for later flexibility?
                    description = GetDescription(examinedObject.Object, false) + "\n\n";
                }
                else
                {
                    description = GetDescription(examinedObject.Object, true) + "\n\n";
                }                

                if (examinedObject.IsCreature)
                {
                    var racialID = Convert.ToInt32(Get2DAString("racialtypes", "Name", (int)GetRacialType(examinedObject)));
                    var racialtype = GetStringByStrRef(racialID);
                    if (!description.Contains(ColorTokenService.Green("Racial Type: ") + racialtype))
                    {
                        description += ColorTokenService.Green("Racial Type: ") + racialtype;
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
            }

            MessageHub.Instance.Publish(new OnModuleExamine());
        }
    }
}
