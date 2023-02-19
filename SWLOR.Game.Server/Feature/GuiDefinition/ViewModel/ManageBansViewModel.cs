using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.LogService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class ManageBansViewModel: GuiViewModelBase<ManageBansViewModel, GuiPayloadBase>
    {
        private int SelectedUserIndex { get; set; }
        private readonly List<string> _userIds = new List<string>();

        public string StatusText
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiColor StatusColor
        {
            get => Get<GuiColor>();
            set => Set(value);
        }

        public GuiBindingList<string> CDKeys
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> UserToggles
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public string ActiveBanReason
        {
            get => Get<string>();
            set => Set(value);
        }

        public string ActiveUserCDKey
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsUserSelected
        {
            get => Get<bool>();
            set => Set(value);
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            SelectedUserIndex = -1;
            ActiveUserCDKey = string.Empty;
            ActiveBanReason = string.Empty;

            _userIds.Clear();
            var query = new DBQuery<PlayerBan>();
            var users = DB.Search(query);

            var cdKeys = new GuiBindingList<string>();
            var toggles = new GuiBindingList<bool>();

            foreach (var user in users)
            {
                _userIds.Add(user.Id);
                toggles.Add(false);
                cdKeys.Add(user.CDKey);
            }

            CDKeys = cdKeys;
            UserToggles = toggles;

            WatchOnClient(model => model.ActiveUserCDKey);
            WatchOnClient(model => model.ActiveBanReason);
        }

        public Action OnSelectUser() => () =>
        {
            if (SelectedUserIndex > -1)
                UserToggles[SelectedUserIndex] = false;
            
            var index = NuiGetEventArrayIndex();
            SelectedUserIndex = index;
            var userId = _userIds[index];
            var dbUser = DB.Get<PlayerBan>(userId);

            IsUserSelected = true;
            ActiveBanReason = dbUser.Reason;
            ActiveUserCDKey = dbUser.CDKey;
            UserToggles[index] = true;

            StatusText = string.Empty;
        };

        public Action OnClickNewUser() => () =>
        {
            var newBan = new PlayerBan
            {
                CDKey = string.Empty,
                Reason = string.Empty
            };
            
            _userIds.Add(newBan.Id);
            CDKeys.Add(newBan.CDKey);
            UserToggles.Add(false);

            DB.Set(newBan);

            StatusText = string.Empty;
        };

        public Action OnClickDeleteUser() => () =>
        {
            ShowModal("Are you sure you want to delete this ban?", () =>
            {
                var userId = _userIds[SelectedUserIndex];
                var dbUser = DB.Get<PlayerBan>(userId);
                if (dbUser == null)
                    return;

                DB.Delete<PlayerBan>(userId);

                IsUserSelected = false;
                ActiveUserCDKey = string.Empty;
                ActiveBanReason = string.Empty;

                _userIds.RemoveAt(SelectedUserIndex);
                CDKeys.RemoveAt(SelectedUserIndex);
                UserToggles.RemoveAt(SelectedUserIndex);

                SelectedUserIndex = -1;
                StatusText = "User ban deleted successfully.";
                StatusColor = GuiColor.Green;

                AdministrationPlugin.RemoveBannedCDKey(dbUser.CDKey);

                Log.Write(LogGroup.Server, $"User deleted from ban list. CDKey: {dbUser.CDKey}, Reason: {dbUser.Reason}");
            });
        };

        public Action OnClickSave() => () =>
        {
            if (ActiveUserCDKey.Length != 8)
            {
                StatusText = "CD Keys must be 8 exactly digits.";
                StatusColor = GuiColor.Red;
                return;
            }

            var userId = _userIds[SelectedUserIndex];
            var dbUser = DB.Get<PlayerBan>(userId);

            AdministrationPlugin.RemoveBannedCDKey(dbUser.CDKey);

            dbUser.Reason = ActiveBanReason;
            dbUser.CDKey = ActiveUserCDKey;

            DB.Set(dbUser);

            AdministrationPlugin.AddBannedCDKey(dbUser.CDKey);

            CDKeys[SelectedUserIndex] = dbUser.CDKey;

            StatusText = "Saved successfully.";
            StatusColor = GuiColor.Green;

            Log.Write(LogGroup.Server, $"User added to ban list. CDKey: {dbUser.CDKey}, Reason: {dbUser.Reason}");
        };

        public Action OnClickDiscardChanges() => () =>
        {
            var userId = _userIds[SelectedUserIndex];
            var dbUser = DB.Get<PlayerBan>(userId);

            ActiveBanReason = dbUser.Reason;
            ActiveUserCDKey = dbUser.CDKey;

            StatusText = string.Empty;
        };
    }
}
