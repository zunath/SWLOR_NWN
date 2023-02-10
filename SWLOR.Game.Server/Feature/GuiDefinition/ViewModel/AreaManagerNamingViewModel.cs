using System;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Area;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class AreaManagerNamingViewModel : GuiViewModelBase<AreaManagerNamingViewModel, GuiPayloadBase>
    {
        public string TemplateAreaNewName
        {
            get => Get<string>();
            set => Set(value);
        }

        public const int MaxTemplateAreaNewNameLength = 40;

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            TemplateAreaNewName = string.Empty;
            WatchOnClient(model => model.TemplateAreaNewName);
        }

        public Action OnClickSubmit() => () =>
        {
            if (string.IsNullOrWhiteSpace(TemplateAreaNewName))
            {
                return;
            }

            var templateAreaName = "[Template] " + TemplateAreaNewName;

            if (templateAreaName.Length > MaxTemplateAreaNewNameLength)
            {
                SendMessageToPC(Player, $"The template area's new name is to long. Please shorten it to no longer than {TemplateAreaNewName} characters and resubmit.");
                return;
            }

            var area = GetArea(Player);

            if (AreaManagerViewModel.GetIsTemplateArea(area))
            {
                SendMessageToPC(Player, $"Please do not create a template area of a template area. Go to the area that is part of the core module and template that instead.");
                Gui.TogglePlayerWindow(Player, GuiWindowType.AreaManagerNaming);
                return;
            }

            // Stores transition targets to restore after CreateArea is called.
            for (var x = GetFirstObjectInArea(area); GetIsObjectValid(x); x = GetNextObjectInArea(area))
            {
                if (GetTransitionTarget(x) != OBJECT_INVALID)
                    SetLocalObject(x, "ORIGINAL_TRANSITION_TARGET", GetTransitionTarget(x));
            }

            // Generating a random number for the area tag allows for duplicates of the same area without messing up the way in which objects are saved
            // in each template area. It's very improbable that two duplicate areas would have the exact same tag name and randomize the exact same number.
            // However, it's less improbable that two areas may have the exact same tag names. For example, if a DM names all his template areas "Ziya's Corellian Event Zone 1" then
            // there's a high chance all his areas would be "tmp_ziyascorelli" since it caps at 16 characters, which could mess up the ways objects are saved
            // to each template area. By adding a random 3-digit number to each template area's tag, it makes that duplication highly improbable.
            var randomNumber = Random(999);
            var targetArea = CreateArea(GetResRef(area), "tmp_" + randomNumber + GetTag(area), templateAreaName);
            SetLocalBool(targetArea, "IS_TEMPLATE_AREA", true);

            // This creates the area and stores it in the database so that it can be loaded with each module load.
            var templateAreaTag = GetTag(targetArea);
            var templateAreaResRef = GetResRef(area);

            var templateArea = new TemplateArea
            {
                TemplateAreaName = templateAreaName,
                TemplateAreaTag = templateAreaTag,
                TemplateAreaResRef = templateAreaResRef,
            };
            DB.Set(templateArea);

            // This deletes all of the creatures, waypoints, and transitions. Since many of our transitions are
            // placeables with the DESTINATION tag, the GetObjectType(x) == ObjectType.Placeable && GetLocalBool(x, "DESTINATION")) line
            // deletes placeables that serve as transitions, which would solve bugs like the placeable teleport
            // in Jim's Cantina surviving to the template area and teleporting people to weird places. 
            for (var x = GetFirstObjectInArea(targetArea); GetIsObjectValid(x); x = GetNextObjectInArea(targetArea))
            {
                var isTeleportPlaceable = GetLocalString(x, "DESTINATION");

                if (GetObjectType(x) == ObjectType.Creature ||
                    GetObjectType(x) == ObjectType.Waypoint ||
                    GetObjectType(x) == ObjectType.Trigger ||
                    (GetObjectType(x) == ObjectType.Placeable && (isTeleportPlaceable != "")))
                {
                    DestroyObject(x);
                }

                if (GetObjectType(x) == ObjectType.Door)
                {
                    SetTransitionTarget(x, OBJECT_INVALID);
                }
            }
            var creatorName = GetName(Player);
            var areaName = GetName(area);

            Console.WriteLine($"{creatorName} has created a new template area named: {areaName}");

            AreaManagerViewModel.CacheTemplateArea(GetResRef(targetArea), targetArea);

            // Restores the transition targets.
            for (var x = GetFirstObjectInArea(area); GetIsObjectValid(x); x = GetNextObjectInArea(area))
            {
                var transitionTarget = GetLocalObject(x, "ORIGINAL_TRANSITION_TARGET");
                if (transitionTarget != OBJECT_INVALID)
                {
                    SetTransitionTarget(transitionTarget, x);
                    DeleteLocalObject(x, "ORIGINAL_TRANSITION_TARGET");
                }
            }
            Gui.TogglePlayerWindow(Player, GuiWindowType.AreaManagerNaming);
            Gui.TogglePlayerWindow(Player, GuiWindowType.AreaManager);

            // Sends the creator of the template area to the template area after it's created.
            var centerHeightOfTemplateArea = GetAreaSize(Dimension.Height, targetArea) / 2f;
            var centerWidthOfTemplateArea = GetAreaSize(Dimension.Width, targetArea) / 2f;
            var centerHeightOfTemplateAreaInMeters = centerHeightOfTemplateArea * 10f;
            var centerWidthOfTemplateAreaInMeters = centerWidthOfTemplateArea * 10f;
            var vectorOfTemplateArea = Vector3(centerHeightOfTemplateAreaInMeters, centerWidthOfTemplateAreaInMeters, 0.0f);
            var position = Location(targetArea, vectorOfTemplateArea, 0.0f);

            AssignCommand(Player, () => ActionJumpToLocation(position));
        };

        public Action OnClickCancel() => () =>
        {
            Gui.TogglePlayerWindow(Player, GuiWindowType.AreaManagerNaming);
        };
    }
}
