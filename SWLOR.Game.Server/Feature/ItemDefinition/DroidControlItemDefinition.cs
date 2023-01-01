using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;
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
                    if (Space.IsPlayerInSpaceMode(user))
                    {
                        return "Droids cannot be activated or adjusted while in space.";
                    }

                    // Droid Activation
                    if (itemPropertyIndex == 0)
                    {
                        var droid = Droid.GetDroid(user);

                        if (GetIsObjectValid(droid))
                        {
                            return "Only one droid may be activated at a time.";
                        }
                    }
                    // Reprogramming
                    else if (itemPropertyIndex == 1)
                    {
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
