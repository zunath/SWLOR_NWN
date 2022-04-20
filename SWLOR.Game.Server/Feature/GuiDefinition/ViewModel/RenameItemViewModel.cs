using System;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class RenameItemViewModel : GuiViewModelBase<RenameItemViewModel, GuiPayloadBase>
    {
        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            NewName = string.Empty;
            Header = String.Empty;
            WatchOnClient(model => model.NewName);

            var item = GetLocalObject(Player, "ITEM_BEING_RENAMED");
            OriginalName = GetLocalString(item, "RENAMED_ITEM_ORIGINAL_NAME");
            CurrentName = GetName(item);
            if (string.IsNullOrWhiteSpace(OriginalName))
                OriginalName = CurrentName;
        }
        public string NewName
        {
            get => Get<string>();
            set
            {
                Set(value);
            }
        }

        public string OriginalName
        {
            get => Get<string>();
            set
            {
                Set(value);
            }
        }
        public string CurrentName
        {
            get => Get<string>();
            set
            {
                Set(value);
            }
        }
        public string Header
        {
            get => Get<string>();
            set
            {
                Set(value);
            }
        }

        public Action OnClickSubmit() => () =>
        {
            // Player hasn't specified a new name for this item.
            if (string.IsNullOrWhiteSpace(NewName))
            {
                Header = "Enter a new name into the window, then select 'Change Name'.";
                return;
            }
            var item = GetLocalObject(Player, "ITEM_BEING_RENAMED");

            // Item isn't in player's inventory.
            if (GetItemPossessor(item) != Player)
            {
                Header = "Item must be in your inventory in order to rename it.";
                return;
            }

            // Item's original name isn't being stored. Do that now.
            if (string.IsNullOrWhiteSpace(GetLocalString(item, "RENAMED_ITEM_ORIGINAL_NAME")))
                SetLocalString(item, "RENAMED_ITEM_ORIGINAL_NAME", GetName(item));

            SetName(item, NewName);

            FloatingTextStringOnCreature("Item renamed to '" + NewName + "'.", Player);
            Gui.TogglePlayerWindow(Player, GuiWindowType.RenameItem);

        };

        public Action OnClickReset() => () =>
        {
            var item = GetLocalObject(Player, "ITEM_BEING_RENAMED");
            if (string.IsNullOrWhiteSpace(GetLocalString(item, "RENAMED_ITEM_ORIGINAL_NAME")))
                SetLocalString(item, "RENAMED_ITEM_ORIGINAL_NAME", GetName(item));

            SetName(item, GetLocalString(item, "RENAMED_ITEM_ORIGINAL_NAME"));

            FloatingTextStringOnCreature("Item reset to original name.", Player);
            Gui.TogglePlayerWindow(Player, GuiWindowType.RenameItem);
        };

        public Action OnClickCancel() => () =>
        {
            Gui.TogglePlayerWindow(Player, GuiWindowType.RenameItem);
        };
    }
}
