using System;
using SWLOR.Game.Server;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.ValueObject;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    internal class mod_on_examine
#pragma warning restore IDE1006 // Naming Styles
    {
        // ReSharper disable once UnusedMember.Local
        private static void Main()
        {
            // Breaking the rules for the examine event because the result of the services is used in the following 
            // service call. We still signal an event for this, but in general all of the logic should go into this method.

            using (new Profiler(nameof(mod_on_examine)))
            {
                NWPlayer examiner = (NWGameObject.OBJECT_SELF);
                NWObject examinedObject = NWNXEvents.OnExamineObject_GetTarget();
                if (ExaminationService.OnModuleExamine(examiner, examinedObject))
                {
                    MessageHub.Instance.Publish(new OnModuleExamine());
                    return;
                }

                string description;

                if (_.GetIsPC(examinedObject.Object) == _.TRUE)
                {
                    // https://github.com/zunath/SWLOR_NWN/issues/853
                    // safest probably to get the modified (non-original) description only for players
                    // may want to always get the modified description for later flexibility?
                    description = _.GetDescription(examinedObject.Object, _.FALSE) + "\n\n";
                }
                else
                {
                    description = _.GetDescription(examinedObject.Object, _.TRUE) + "\n\n";
                }                

                if (examinedObject.IsCreature)
                {
                    int racialID = Convert.ToInt32(_.Get2DAString("racialtypes", "Name", _.GetRacialType(examinedObject)));
                    string racialtype = _.GetStringByStrRef(racialID);
                    description += ColorTokenService.Green("Racial Type: ") + racialtype;
                }

                description = ModService.OnModuleExamine(description, examiner, examinedObject);
                description = ItemService.OnModuleExamine(description, examinedObject);
                description = DurabilityService.OnModuleExamine(description, examinedObject);

                if (!string.IsNullOrWhiteSpace(description))
                {
                    _.SetDescription(examinedObject.Object, description, _.FALSE);
                    _.SetDescription(examinedObject.Object, description);
                }
            }

            MessageHub.Instance.Publish(new OnModuleExamine());
        }
    }
}
