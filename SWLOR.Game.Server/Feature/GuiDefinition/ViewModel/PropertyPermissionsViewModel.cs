using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.PropertyService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class PropertyPermissionsViewModel: GuiViewModelBase<PropertyPermissionsViewModel, PropertyPermissionPayload>
    {
        private static readonly GuiColor _red = new GuiColor(255, 0, 0);

        private string PropertyId { get; set; }
        private List<PropertyPermissionType> AvailablePermissions { get; set; }
        private int SelectedPlayerIndex { get; set; }

        private readonly List<string> _playerIds = new();

        public string Instruction
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiColor InstructionColor
        {
            get => Get<GuiColor>();
            set => Set(value);
        }

        public string PropertyName
        {
            get => Get<string>();
            set => Set(value);
        }

        public string SearchText
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiBindingList<string> PlayerNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> PlayerToggles
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public GuiBindingList<bool> PermissionStates
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public GuiBindingList<bool> PermissionGrantingStates
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public GuiBindingList<string> PermissionNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> PermissionDescriptions
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public string PlayerName
        {
            get => Get<string>();
            set => Set(value);
        }

        private void LoadPlayerInfo()
        {
            var playerId = _playerIds[SelectedPlayerIndex];

            var query = new DBQuery<Player>()
                .AddFieldSearch(nameof(Entity.Player.Id), playerId, false);
            var dbPlayer = DB.Search(query).Single();
            var dbProperty = DB.Get<WorldProperty>(PropertyId);
            var playerPermissions = dbProperty.Permissions.ContainsKey(playerId)
                ? dbProperty.Permissions[playerId]
                : new Dictionary<PropertyPermissionType, bool>();
            var playerGrantPermissions = dbProperty.Permissions.ContainsKey(playerId)
                ? dbProperty.GrantPermissions[playerId]
                : new Dictionary<PropertyPermissionType, bool>();

            var permissionStates = new GuiBindingList<bool>();
            var permissionGrantingStates = new GuiBindingList<bool>();
            var permissionNames = new GuiBindingList<string>();
            var permissionDescriptions = new GuiBindingList<string>();

            PlayerName = dbPlayer.Name;

            foreach (var type in AvailablePermissions)
            {
                var permission = Property.GetPermissionByType(type);
                permissionNames.Add(permission.Name);
                permissionDescriptions.Add(permission.Description);

                if(playerPermissions.ContainsKey(type))
                    permissionStates.Add(playerPermissions[type]);
                else
                    permissionStates.Add(false);

                if(playerPermissions.ContainsKey(type))
                    permissionGrantingStates.Add(playerGrantPermissions[type]);
                else 
                    permissionGrantingStates.Add(false);
            }
            
            PermissionStates = permissionStates;
            PermissionGrantingStates = permissionGrantingStates;
            PermissionNames = permissionNames;
            PermissionDescriptions = permissionDescriptions;
        }

        protected override void Initialize(PropertyPermissionPayload initialPayload)
        {
            SelectedPlayerIndex = -1;
            PropertyId = initialPayload.PropertyId;
            SearchText = string.Empty;
            _playerIds.Clear();

            AvailablePermissions = initialPayload.AvailablePermissions;

            var property = DB.Get<WorldProperty>(PropertyId);
            PropertyName = property.CustomName;
            PlayerNames = new GuiBindingList<string>();
            PlayerToggles = new GuiBindingList<bool>();
            PermissionStates = new GuiBindingList<bool>();
            PermissionNames = new GuiBindingList<string>();
            PermissionDescriptions = new GuiBindingList<string>();

            WatchOnClient(model => model.SearchText);
        }

        private void Search()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                Instruction = "Enter a name to search.";
                InstructionColor = _red;
                return;
            }

            SelectedPlayerIndex = -1;
            var query = new DBQuery<Player>()
                .AddFieldSearch(nameof(Entity.Player.Name), SearchText, true)
                .AddFieldSearch(nameof(Entity.Player.IsDeleted), false)
                .AddPaging(25, 0);
            var dbPlayers = DB.Search(query);

            var playerNames = new GuiBindingList<string>();
            var playerToggles = new GuiBindingList<bool>();

            foreach (var player in dbPlayers)
            {
                _playerIds.Add(player.Id.ToString());
                playerNames.Add(player.Name);
                playerToggles.Add(false);
            }

            PlayerNames = playerNames;
            PlayerToggles = playerToggles;

            PermissionStates.Clear();
            PermissionNames.Clear();
            PermissionDescriptions.Clear();
            PlayerName = string.Empty;
        }

        public Action OnClickSearch() => Search;

        public Action OnClickClearSearch() => () =>
        {
            SearchText = string.Empty;
            Search();
        };

        public Action OnSelectPlayer() => () =>
        {
            if (SelectedPlayerIndex > -1)
                PlayerToggles[SelectedPlayerIndex] = false;

            SelectedPlayerIndex = NuiGetEventArrayIndex();
            PlayerToggles[SelectedPlayerIndex] = true;

            LoadPlayerInfo();
        };

        public Action OnClickSaveChanges() => () =>
        {

        };

        public Action OnClickReset() => () =>
        {

        };
    }
}
