using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.ItemService;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.ItemDefinition
{
    public class DroidControlItemDefinition: IItemListDefinition
    {
        private readonly ItemBuilder _builder = new();

        public Dictionary<string, ItemDetail> BuildItems()
        {
            DroidControlUnit();

            return _builder.Build();
        }

        private void DroidControlUnit()
        {
            _builder.Create("droid_control")
                .Delay(3f)
                .PlaysAnimation(Animation.LoopingGetMid)
                .ValidationAction((user, item, target, location, itemPropertyIndex) =>
                {
                    var droid = Droid.GetDroid(user);
                    if (Space.IsPlayerInSpaceMode(user))
                    {
                        return "Droids cannot be activated or adjusted while in space.";
                    }

                    // Droid Activation (subject to cooldown)
                    if (itemPropertyIndex == 0)
                    {
                        var (onDelay, timeToWait) = Recast.IsOnRecastDelay(user, RecastGroup.DroidController);

                        if (onDelay)
                        {
                            return $"This item can be used in {timeToWait}.";
                        }

                        if (GetIsObjectValid(droid))
                        {
                            return "Only one droid may be activated at a time.";
                        }
                    }
                    // Reprogramming (not subject to cooldown)
                    else if (itemPropertyIndex == 1)
                    {
                        if (GetIsObjectValid(droid))
                        {
                            return "Droid AI cannot be adjusted while active. Please dismiss your droid and try again.";
                        }

                        var droidDetails = Droid.LoadDroidDetails(item);
                        var perkLevel = Perk.GetEffectivePerkLevel(user, PerkType.Programming);

                        if (perkLevel < droidDetails.Tier)
                        {
                            return $"Your Programming perk is too low to configure this droid's AI. (Required: {droidDetails.Tier})";
                        }
                    }

                    return string.Empty;
                })
                .ApplyAction((user, item, target, location, itemPropertyIndex) =>
                {
                    // Droid Activation
                    if (itemPropertyIndex == 0)
                    {
                        Droid.SpawnDroid(user, item);
                        SetItemCursedFlag(item, true);

                    }
                    // Reprogramming
                    else if (itemPropertyIndex == 1)
                    {
                        Gui.TogglePlayerWindow(user, GuiWindowType.DroidProgramming); // todo: Pass in item to payload.
                    }
                });
        }
    }
}
