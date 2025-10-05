using SWLOR.Component.Admin.Entity;
using SWLOR.NWN.API.NWNX;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Abstractions.Models;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Core.Log.LogGroup;
using SWLOR.Shared.Domain.Repositories;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Model;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.Admin.UI.ViewModel
{
    public class ManageBansViewModel: GuiViewModelBase<ManageBansViewModel, IGuiPayload>
    {
        private readonly ILogger _logger;
        private readonly IDatabaseService _db;
        private readonly IAdministrationPluginService _administrationPlugin;
        private readonly IPlayerBanRepository _playerBanRepository;

        public ManageBansViewModel(IGuiService guiService, ILogger logger, IDatabaseService db, IAdministrationPluginService administrationPlugin, IPlayerBanRepository playerBanRepository) : base(guiService)
        {
            _logger = logger;
            _db = db;
            _administrationPlugin = administrationPlugin;
            _playerBanRepository = playerBanRepository;
        }
        
        private int SelectedUserIndex { get; set; }
        private readonly List<string> _userIds = new();

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

        protected override void Initialize(IGuiPayload initialPayload)
        {
            SelectedUserIndex = -1;
            ActiveUserCDKey = string.Empty;
            ActiveBanReason = string.Empty;

            _userIds.Clear();
            var users = _playerBanRepository.GetAll();

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
            var dbUser = _db.Get<PlayerBan>(userId);

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

            _db.Set(newBan);

            StatusText = string.Empty;
        };

        public Action OnClickDeleteUser() => () =>
        {
            ShowModal("Are you sure you want to delete this ban?", () =>
            {
                var userId = _userIds[SelectedUserIndex];
                var dbUser = _db.Get<PlayerBan>(userId);
                if (dbUser == null)
                    return;

                _db.Delete<PlayerBan>(userId);

                IsUserSelected = false;
                ActiveUserCDKey = string.Empty;
                ActiveBanReason = string.Empty;

                _userIds.RemoveAt(SelectedUserIndex);
                CDKeys.RemoveAt(SelectedUserIndex);
                UserToggles.RemoveAt(SelectedUserIndex);

                SelectedUserIndex = -1;
                StatusText = "User ban deleted successfully.";
                StatusColor = GuiColor.Green;

                _administrationPlugin.RemoveBannedCDKey(dbUser.CDKey);

                _logger.Write<ServerLogGroup>($"User deleted from ban list. CDKey: {dbUser.CDKey}, Reason: {dbUser.Reason}");
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
            var dbUser = _db.Get<PlayerBan>(userId);

            _administrationPlugin.RemoveBannedCDKey(dbUser.CDKey);

            dbUser.Reason = ActiveBanReason;
            dbUser.CDKey = ActiveUserCDKey;

            _db.Set(dbUser);

            _administrationPlugin.AddBannedCDKey(dbUser.CDKey);

            CDKeys[SelectedUserIndex] = dbUser.CDKey;

            StatusText = "Saved successfully.";
            StatusColor = GuiColor.Green;

            _logger.Write<ServerLogGroup>($"User added to ban list. CDKey: {dbUser.CDKey}, Reason: {dbUser.Reason}");
        };

        public Action OnClickDiscardChanges() => () =>
        {
            var userId = _userIds[SelectedUserIndex];
            var dbUser = _db.Get<PlayerBan>(userId);

            ActiveBanReason = dbUser.Reason;
            ActiveUserCDKey = dbUser.CDKey;

            StatusText = string.Empty;
        };
    }
}
