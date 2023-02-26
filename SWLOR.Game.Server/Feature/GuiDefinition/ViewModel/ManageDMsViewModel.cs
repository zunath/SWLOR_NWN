using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.LogService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class ManageDMsViewModel: GuiViewModelBase<ManageDMsViewModel, GuiPayloadBase>
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

        public GuiBindingList<string> Names
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> UserToggles
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public string ActiveUserName
        {
            get => Get<string>();
            set => Set(value);
        }

        public string ActiveUserCDKey
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsDeleteEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsUserSelected
        {
            get => Get<bool>();
            set => Set(value);
        }

        public int SelectedRoleId
        {
            get => Get<int>();
            set => Set(value);
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            SelectedUserIndex = -1;
            ActiveUserCDKey = string.Empty;
            ActiveUserName = string.Empty;
            SelectedRoleId = 0;

            _userIds.Clear();
            var query = new DBQuery<AuthorizedDM>();
            var users = DB.Search(query);

            var names = new GuiBindingList<string>();
            var toggles = new GuiBindingList<bool>();

            foreach (var user in users)
            {
                _userIds.Add(user.Id.ToString());
                names.Add(user.Name);
                toggles.Add(false);
            }

            Names = names;
            UserToggles = toggles;

            WatchOnClient(model => model.ActiveUserCDKey);
            WatchOnClient(model => model.ActiveUserName);
            WatchOnClient(model => model.SelectedRoleId);
        }

        public Action OnSelectUser() => () =>
        {
            if (SelectedUserIndex > -1)
                UserToggles[SelectedUserIndex] = false;
            
            var index = NuiGetEventArrayIndex();
            SelectedUserIndex = index;
            var userId = _userIds[index];
            var dbUser = DB.Get<AuthorizedDM>(userId);
            var userCDKey = GetPCPublicCDKey(Player);

            IsUserSelected = true;
            IsDeleteEnabled = userCDKey != dbUser.CDKey;
            ActiveUserName = dbUser.Name;
            ActiveUserCDKey = dbUser.CDKey;
            UserToggles[index] = true;
            SelectedRoleId = dbUser.Authorization == AuthorizationLevel.DM ? 0 : 1;

            StatusText = string.Empty;
        };

        public Action OnClickNewUser() => () =>
        {
            var newUser = new AuthorizedDM
            {
                Authorization = AuthorizationLevel.DM,
                CDKey = string.Empty,
                Name = "New User"
            };
            
            _userIds.Add(newUser.Id.ToString());
            Names.Add(newUser.Name);
            UserToggles.Add(false);

            DB.Set(newUser);

            StatusText = string.Empty;
        };

        public Action OnClickDeleteUser() => () =>
        {
            ShowModal("Are you sure you want to delete this staff member?", () =>
            {
                var userCDKey = GetPCPublicCDKey(Player);
                var userId = _userIds[SelectedUserIndex];
                var dbUser = DB.Get<AuthorizedDM>(userId);

                if (dbUser.CDKey == userCDKey)
                {
                    StatusText = "You can't delete yourself.";
                    StatusColor = GuiColor.Red;
                    return;
                }

                DB.Delete<AuthorizedDM>(userId);

                IsUserSelected = false;
                IsDeleteEnabled = false;
                ActiveUserCDKey = string.Empty;
                ActiveUserName = string.Empty;

                _userIds.RemoveAt(SelectedUserIndex);
                Names.RemoveAt(SelectedUserIndex);
                UserToggles.RemoveAt(SelectedUserIndex);

                SelectedUserIndex = -1;
                StatusText = "User deleted successfully.";
                StatusColor = GuiColor.Green;

                Log.Write(LogGroup.DM, $"User deleted from authorized DM list. Name: {dbUser.Name}, CDKey: {dbUser.CDKey}, Role: {dbUser.Authorization}");
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
            var dbUser = DB.Get<AuthorizedDM>(userId);
            var userCDKey = GetPCPublicCDKey(Player);

            dbUser.Name = ActiveUserName;
            dbUser.CDKey = ActiveUserCDKey;
            dbUser.Authorization = SelectedRoleId == 0 ? AuthorizationLevel.DM : AuthorizationLevel.Admin;

            IsDeleteEnabled = userCDKey != dbUser.CDKey;

            DB.Set(dbUser);

            Names[SelectedUserIndex] = dbUser.Name;

            StatusText = "Saved successfully.";
            StatusColor = GuiColor.Green;

            Log.Write(LogGroup.DM, $"User updated on authorized DM list. Name: {dbUser.Name}, CDKey: {dbUser.CDKey}, Role: {dbUser.Authorization}");
        };

        public Action OnClickDiscardChanges() => () =>
        {
            var userId = _userIds[SelectedUserIndex];
            var dbUser = DB.Get<AuthorizedDM>(userId);

            ActiveUserName = dbUser.Name;
            ActiveUserCDKey = dbUser.CDKey;

            StatusText = string.Empty;
        };
    }
}
