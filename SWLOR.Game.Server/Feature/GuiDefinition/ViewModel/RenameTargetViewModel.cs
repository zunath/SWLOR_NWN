using System;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Core.Log;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class RenameTargetViewModel : GuiViewModelBase<RenameTargetViewModel, RenameItemPayload>
    {
        private uint _target;
        private const string RenamedItemOriginalName = "RENAMED_ITEM_ORIGINAL_NAME";
        public const string EditorPartialId = "EDITOR_PARTIAL";
        public const string ItemEditorPartialName = "PARTIAL_ITEM";
        public const string PlayerEditorPartialName = "PARTIAL_PLAYER";

        protected override void Initialize(RenameItemPayload initialPayload)
        {
            NewName = string.Empty;
            NewFirstName = string.Empty;
            NewLastName = string.Empty;
            Header = String.Empty;
            WatchOnClient(model => model.NewName);
            WatchOnClient(model => model.NewFirstName);
            WatchOnClient(model => model.NewLastName);

            if (GetIsPC(initialPayload.Target))
            {
                ChangePartialView(EditorPartialId, PlayerEditorPartialName);
            }
            else
            {
                ChangePartialView(EditorPartialId, ItemEditorPartialName);
            }

            _target = initialPayload.Target;
            OriginalName = GetLocalString(_target, RenamedItemOriginalName);
            CurrentName = GetName(_target);
            if (string.IsNullOrWhiteSpace(OriginalName))
                OriginalName = CurrentName;
        }
        public string NewName
        {
            get => Get<string>();
            set => Set(value);
        }

        public string NewFirstName
        {
            get => Get<string>();
            set => Set(value);
        }

        public string NewLastName
        {
            get => Get<string>();
            set => Set(value);
        }

        public string OriginalName
        {
            get => Get<string>();
            set => Set(value);
        }
        public string CurrentName
        {
            get => Get<string>();
            set => Set(value);
        }
        public string Header
        {
            get => Get<string>();
            set => Set(value);
        }

        public Action OnClickSubmit() => () =>
        {
            var isDM = GetIsDM(Player) || GetIsDMPossessed(Player);
            var type = GetObjectType(_target);

            // Player hasn't specified a new name for this item.
            if ((type == ObjectType.Item && string.IsNullOrWhiteSpace(NewName)) || 
                (GetIsPC(_target) && string.IsNullOrWhiteSpace(NewFirstName) && string.IsNullOrWhiteSpace(NewLastName)))
            {
                Header = "Enter a new name then click Change Name.";
                return;
            }

            // Item isn't in player's inventory.
            if (!isDM)
            {
                if (type != ObjectType.Item)
                {
                    Header = "Only items may be targeted.";
                    return;
                }

                if (GetItemPossessor(_target) != Player)
                {
                    Header = "Item must be in your inventory.";
                    return;
                }
            }

            if (!GetIsObjectValid(_target))
            {
                Header = "Target not found.";
                return;
            }

            // Item's original name isn't being stored. Do that now.
            if (type == ObjectType.Item)
            {
                if (string.IsNullOrWhiteSpace(GetLocalString(_target, RenamedItemOriginalName)))
                    SetLocalString(_target, RenamedItemOriginalName, GetName(_target));
            }

            if (GetIsPC(_target) && !GetIsDM(_target))
            {
                var dmName = GetName(Player);
                var oldName = GetName(_target);
                CreaturePlugin.SetOriginalName(_target, NewFirstName, false);
                CreaturePlugin.SetOriginalName(_target, NewLastName, true);

                var newFullName = NewFirstName;
                if (!string.IsNullOrWhiteSpace(NewLastName))
                    newFullName += $" {NewLastName}";

                var playerId = GetObjectUUID(_target);
                var dbPlayer = DB.Get<Player>(playerId);
                dbPlayer.Name = newFullName;
                DB.Set(dbPlayer);

                BootPC(_target, $"Your name has been changed to '{newFullName}'. Please reconnect to the server.");

                FloatingTextStringOnCreature($"Target renamed to '{newFullName}'.", Player);
                LogLegacy.Write(LogGroupType.DM, $"DM '{dmName}' renamed player '{oldName}' to '{newFullName}'.");
            }
            else
            {
                SetName(_target, NewName);
                FloatingTextStringOnCreature($"Target renamed to '{NewName}'.", Player);
            }

            Gui.TogglePlayerWindow(Player, GuiWindowType.RenameItem);

        };

        public Action OnClickReset() => () =>
        {
            if (string.IsNullOrWhiteSpace(GetLocalString(_target, RenamedItemOriginalName)))
                SetLocalString(_target, RenamedItemOriginalName, GetName(_target));

            SetName(_target, GetLocalString(_target, RenamedItemOriginalName));

            FloatingTextStringOnCreature("Target reset to original name.", Player);
            Gui.TogglePlayerWindow(Player, GuiWindowType.RenameItem);
        };

        public Action OnClickCancel() => () =>
        {
            Gui.TogglePlayerWindow(Player, GuiWindowType.RenameItem);
        };
    }
}
