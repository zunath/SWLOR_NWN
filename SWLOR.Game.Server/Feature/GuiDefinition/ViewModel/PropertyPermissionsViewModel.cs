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
        private static readonly GuiColor _green = new GuiColor(0, 255, 0);

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

        public GuiBindingList<bool> PermissionEnabled
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public GuiBindingList<bool> GrantPermissionEnabled
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public string PlayerName
        {
            get => Get<string>();
            set => Set(value);
        }

        private void LoadPlayerInfo()
        {
            var playerId = GetObjectUUID(Player);
            var targetPlayerId = _playerIds[SelectedPlayerIndex];
            
            var dbPlayer = DB.Get<Player>(targetPlayerId);
            var dbProperty = DB.Get<WorldProperty>(PropertyId);
            var playerGrantPermissions = dbProperty.GrantPermissions[playerId];
            
            var targetPermissions = dbProperty.Permissions.ContainsKey(targetPlayerId)
                ? dbProperty.Permissions[targetPlayerId]
                : new Dictionary<PropertyPermissionType, bool>();
            var targetGrantPermissions = dbProperty.GrantPermissions.ContainsKey(targetPlayerId)
                ? dbProperty.GrantPermissions[targetPlayerId]
                : new Dictionary<PropertyPermissionType, bool>();

            var permissionStates = new GuiBindingList<bool>();
            var permissionGrantingStates = new GuiBindingList<bool>();
            var permissionNames = new GuiBindingList<string>();
            var permissionDescriptions = new GuiBindingList<string>();
            var permissionEnabled = new GuiBindingList<bool>();
            var grantPermissionEnabled = new GuiBindingList<bool>();

            PlayerName = dbPlayer.Name;

            foreach (var type in AvailablePermissions)
            {
                var permission = Property.GetPermissionByType(type);
                permissionNames.Add(permission.Name);
                permissionDescriptions.Add(permission.Description);

                if(targetPermissions.ContainsKey(type))
                    permissionStates.Add(targetPermissions[type]);
                else
                    permissionStates.Add(false);

                if(targetPermissions.ContainsKey(type))
                    permissionGrantingStates.Add(targetGrantPermissions[type]);
                else 
                    permissionGrantingStates.Add(false);

                if(playerGrantPermissions.ContainsKey(type) && playerGrantPermissions[type] && playerId != targetPlayerId)
                    permissionEnabled.Add(true);
                else
                    permissionEnabled.Add(false);

                if(playerId == dbProperty.OwnerPlayerId && playerId != targetPlayerId)
                    grantPermissionEnabled.Add(true);
                else
                    grantPermissionEnabled.Add(false);
            }
            
            PermissionStates = permissionStates;
            PermissionGrantingStates = permissionGrantingStates;
            PermissionNames = permissionNames;
            PermissionDescriptions = permissionDescriptions;
            PermissionEnabled = permissionEnabled;
            GrantPermissionEnabled = grantPermissionEnabled;
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
            PermissionGrantingStates = new GuiBindingList<bool>();
            PermissionNames = new GuiBindingList<string>();
            PermissionDescriptions = new GuiBindingList<string>();

            WatchOnClient(model => model.SearchText);
            WatchOnClient(model => model.PermissionStates);
            WatchOnClient(model => model.PermissionGrantingStates);

            Search();
        }

        private void Search()
        {
            SelectedPlayerIndex = -1;
            _playerIds.Clear();
            var playerNames = new GuiBindingList<string>();
            var playerToggles = new GuiBindingList<bool>();
            IEnumerable<Player> dbPlayers;

            // If no search is specified, load only the users who currently have permissions.
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                var dbProperty = DB.Get<WorldProperty>(PropertyId);
                var playerIds = dbProperty.Permissions.Keys.ToList();
                var query = new DBQuery<Player>()
                    .AddFieldSearch(nameof(Entity.Player.Id), playerIds);
                dbPlayers = DB.Search(query);
            }
            // Otherwise look for players by their names.
            else
            {
                var query = new DBQuery<Player>()
                    .AddFieldSearch(nameof(Entity.Player.Name), SearchText, true)
                    .AddFieldSearch(nameof(Entity.Player.IsDeleted), false)
                    .AddPaging(25, 0);
                dbPlayers = DB.Search(query);
            }

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
            var dbProperty = DB.Get<WorldProperty>(PropertyId);

            var playerId = GetObjectUUID(Player);
            var targetPlayerId = _playerIds[SelectedPlayerIndex];

            // Safety check to ensure the user still has grant permissions.
            // If they lost them while the window was open, they could still send this command even though they no longer have permission.
            if (!dbProperty.GrantPermissions.ContainsKey(playerId))
                return;

            // Safety check to ensure the user isn't modifying their own permissions.
            if (playerId == targetPlayerId)
                return;

            var grantorPermissions = dbProperty.GrantPermissions[playerId];
            var permissions = dbProperty.Permissions.ContainsKey(targetPlayerId)
                ? dbProperty.Permissions[targetPlayerId]
                : new Dictionary<PropertyPermissionType, bool>();
            var grantPermissions = dbProperty.GrantPermissions.ContainsKey(targetPlayerId)
                ? dbProperty.GrantPermissions[targetPlayerId]
                : new Dictionary<PropertyPermissionType, bool>();

            var hasAtLeastOne = false;
            for (var index = 0; index < AvailablePermissions.Count; index++)
            {
                var permission = AvailablePermissions[index];

                // Only permissions the player can grant should be updated.
                if (grantorPermissions[permission])
                {
                    var canGrant = PermissionGrantingStates[index];

                    // Automatically assign permission if the grant permission is assigned.
                    if (canGrant)
                        PermissionStates[index] = true;

                    var hasPermission = PermissionStates[index];

                    if (hasPermission)
                        hasAtLeastOne = true;

                    permissions[permission] = hasPermission;
                    grantPermissions[permission] = canGrant;
                }
                // Otherwise keep it on the same setting, or set to false if it doesn't exist.
                else
                {
                    if (dbProperty.Permissions.ContainsKey(targetPlayerId))
                        permissions[permission] = dbProperty.Permissions[targetPlayerId][permission];
                    else
                        permissions[permission] = false;

                    if (dbProperty.GrantPermissions.ContainsKey(targetPlayerId))
                        grantPermissions[permission] = dbProperty.Permissions[targetPlayerId][permission];
                    else
                        permissions[permission] = false;
                }
            }

            // Player has at least one permission. Set the changes in the DB.
            if (hasAtLeastOne)
            {
                dbProperty.Permissions[targetPlayerId] = permissions;
                dbProperty.GrantPermissions[targetPlayerId] = grantPermissions;
            }
            // Player doesn't have any permissions. If they currently exist in the permissions lists, remove them.
            else
            {
                if (dbProperty.Permissions.ContainsKey(targetPlayerId))
                    dbProperty.Permissions.Remove(targetPlayerId);
                if (dbProperty.GrantPermissions.ContainsKey(targetPlayerId))
                    dbProperty.GrantPermissions.Remove(targetPlayerId);
            }

            DB.Set(dbProperty);

            Instruction = $"Permissions updated!";
            InstructionColor = _green;
        };

        public Action OnClickReset() => () =>
        {
            var dbProperty = DB.Get<WorldProperty>(PropertyId);
            var playerId = _playerIds[SelectedPlayerIndex];

            var permissionStates = new GuiBindingList<bool>();
            var grantPermissionStates = new GuiBindingList<bool>();

            for (var index = 0; index < AvailablePermissions.Count; index++)
            {
                var permission = AvailablePermissions[index];

                permissionStates.Add(dbProperty.Permissions[playerId][permission]);
                grantPermissionStates.Add(dbProperty.GrantPermissions[playerId][permission]);
            }

            PermissionStates = permissionStates;
            PermissionGrantingStates = grantPermissionStates;
        };
    }
}
