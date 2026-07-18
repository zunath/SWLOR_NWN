using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Component;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.PropertyService;
using PlayerNameService = SWLOR.Game.Server.Service.PlayerName;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class PropertyPermissionsViewModel: GuiViewModelBase<PropertyPermissionsViewModel, PropertyPermissionPayload>
    {
        private bool _isCategory;
        private PropertyType _propertyType;
        private string _cityId;

        private string PropertyId { get; set; }
        private List<PropertyPermissionType> AvailablePermissions { get; set; }
        private int SelectedPlayerIndex { get; set; }

        private readonly List<string> _playerIds = new();

        // One row DTO per matched player, replacing the two hand-synced parallel
        // GuiBindingList instances Search used to build in lockstep.
        private sealed class PlayerRowEntry
        {
            public string Id { get; }
            public string Name { get; }
            public bool Toggle { get; }

            public PlayerRowEntry(string id, string name, bool toggle)
            {
                Id = id;
                Name = name;
                Toggle = toggle;
            }
        }

        private static readonly GuiTableSource<PropertyPermissionsViewModel, PlayerRowEntry> PlayersTable =
            new GuiTableSource<PropertyPermissionsViewModel, PlayerRowEntry>()
                .Column((m, v) => m.PlayerNames = v, r => r.Name)
                .Column((m, v) => m.PlayerToggles = v, r => r.Toggle);

        // One row DTO per available permission, replacing the six hand-synced
        // parallel GuiBindingList instances LoadPlayerInfo used to build in lockstep.
        private sealed class PermissionEntry
        {
            public bool State { get; }
            public bool GrantingState { get; }
            public string Name { get; }
            public string Description { get; }
            public bool Enabled { get; }
            public bool GrantEnabled { get; }

            public PermissionEntry(bool state, bool grantingState, string name, string description, bool enabled, bool grantEnabled)
            {
                State = state;
                GrantingState = grantingState;
                Name = name;
                Description = description;
                Enabled = enabled;
                GrantEnabled = grantEnabled;
            }
        }

        private static readonly GuiTableSource<PropertyPermissionsViewModel, PermissionEntry> PermissionsTable =
            new GuiTableSource<PropertyPermissionsViewModel, PermissionEntry>()
                .Column((m, v) => m.PermissionStates = v, r => r.State)
                .Column((m, v) => m.PermissionGrantingStates = v, r => r.GrantingState)
                .Column((m, v) => m.PermissionNames = v, r => r.Name)
                .Column((m, v) => m.PermissionDescriptions = v, r => r.Description)
                .Column((m, v) => m.PermissionEnabled = v, r => r.Enabled)
                .Column((m, v) => m.GrantPermissionEnabled = v, r => r.GrantEnabled);

        // Row DTO for OnClickReset, which only rebuilds the state/granting-state
        // pair (not the name/description/enabled columns LoadPlayerInfo also owns).
        private sealed class PermissionToggleEntry
        {
            public bool State { get; }
            public bool GrantingState { get; }

            public PermissionToggleEntry(bool state, bool grantingState)
            {
                State = state;
                GrantingState = grantingState;
            }
        }

        private static readonly GuiTableSource<PropertyPermissionsViewModel, PermissionToggleEntry> PermissionResetTable =
            new GuiTableSource<PropertyPermissionsViewModel, PermissionToggleEntry>()
                .Column((m, v) => m.PermissionStates = v, r => r.State)
                .Column((m, v) => m.PermissionGrantingStates = v, r => r.GrantingState);

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

        public bool IsPlayerSelected
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool CanChangePublicSetting
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsPublic
        {
            get => Get<bool>();
            set => Set(value);
        }

        private WorldPropertyPermission CreateEmptyPermissions(string targetPlayerId)
        {
            return new WorldPropertyPermission
            {
                PropertyId = PropertyId,
                PlayerId = targetPlayerId,
                Permissions = Property.GetPermissionsByPropertyType(_propertyType).ToDictionary(x => x, _ => false),
                GrantPermissions = Property.GetPermissionsByPropertyType(_propertyType).ToDictionary(x => x, _ => false)
            };
        }

        private bool CanAdjustPermission(
            WorldPropertyPermission grantorPermissions,
            WorldPropertyPermission targetPermissions,
            PropertyPermissionType type,
            string targetPlayerId,
            string ownerPlayerId)
        {
            var playerId = GetObjectUUID(Player);
            var isTargetOwner = targetPlayerId == ownerPlayerId;
            return grantorPermissions.GrantPermissions[type] // Player must have grant permission for this property
                && playerId != targetPlayerId // Player can't adjust their own permissions
                && (!targetPermissions.GrantPermissions[type] || playerId == ownerPlayerId) // Player can't adjust permissions of another grantor, unless owner
                && !isTargetOwner; // Player can't adjust the owner's permissions.
        }

        private bool CanAdjustGrantPermission(
            WorldPropertyPermission grantorPermissions,
            PropertyPermissionType type,
            string targetPlayerId,
            string ownerPlayerId)
        {
            var playerId = GetObjectUUID(Player);
            var isTargetOwner = targetPlayerId == ownerPlayerId;
            return grantorPermissions.GrantPermissions[type] // Player must have grantor permission
                   && playerId == ownerPlayerId // Can't adjust owner's permissions
                   && playerId != targetPlayerId // Can't adjust your own permissions
                   && !isTargetOwner; // Can't adjust owner's permissions
        }

        private void LoadPlayerInfo()
        {
            var playerId = GetObjectUUID(Player);
            var targetPlayerId = _playerIds[SelectedPlayerIndex];

            var dbPlayer = DB.Get<Player>(targetPlayerId);
            var grantorPermissions = DB.Search(new DBQuery<WorldPropertyPermission>()
                .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), playerId, false)
                .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), PropertyId, false))
                .First();

            var targetPermissions = DB.Search(new DBQuery<WorldPropertyPermission>()
                .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), targetPlayerId, false)
                .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), PropertyId, false))
                .FirstOrDefault() ?? CreateEmptyPermissions(targetPlayerId);

            PlayerName = PlayerNameService.GetKnownNameOrFallbackByPlayerId(Player, targetPlayerId, dbPlayer.Name);

            string ownerPlayerId;
            if (_isCategory)
            {
                var dbCategory = DB.Get<WorldPropertyCategory>(PropertyId);
                var dbProperty = DB.Get<WorldProperty>(dbCategory.ParentPropertyId);
                ownerPlayerId = dbProperty.OwnerPlayerId;
            }
            else
            {
                var dbProperty = DB.Get<WorldProperty>(PropertyId);
                ownerPlayerId = dbProperty.OwnerPlayerId;
            }

            var rows = new List<PermissionEntry>();

            foreach (var type in AvailablePermissions)
            {
                var permission = Property.GetPermissionByType(type);
                var enabled = CanAdjustPermission(grantorPermissions, targetPermissions, type, targetPlayerId, ownerPlayerId);
                var grantEnabled = CanAdjustGrantPermission(grantorPermissions, type, targetPlayerId, ownerPlayerId);

                rows.Add(new PermissionEntry(
                    targetPermissions.Permissions[type],
                    targetPermissions.GrantPermissions[type],
                    permission.Name,
                    permission.Description,
                    enabled,
                    grantEnabled));
            }

            PermissionsTable.Refresh(this, rows);
        }

        protected override void Initialize(PropertyPermissionPayload initialPayload)
        {

            SelectedPlayerIndex = -1;
            PropertyId = initialPayload.PropertyId;
            SearchText = string.Empty;
            _playerIds.Clear();
            _isCategory = initialPayload.IsCategory;
            _propertyType = initialPayload.PropertyType;
            _cityId = initialPayload.CityId;
            IsPlayerSelected = false;

            AvailablePermissions = Property.GetPermissionsByPropertyType(_propertyType);

            if (_isCategory)
            {
                var category = DB.Get<WorldPropertyCategory>(PropertyId);
                PropertyName = category.Name;
                CanChangePublicSetting = false;
                IsPublic = false;
            }
            else
            {
                var playerId = GetObjectUUID(Player);
                var grantorPermissions = DB.Search(new DBQuery<WorldPropertyPermission>()
                        .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), playerId, false)
                        .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), PropertyId, false))
                    .First();

                var property = DB.Get<WorldProperty>(PropertyId);
                var propertyDetail = Property.GetPropertyDetail(property.PropertyType);

                PropertyName = property.CustomName;
                CanChangePublicSetting = grantorPermissions.GrantPermissions.ContainsKey(PropertyPermissionType.EnterProperty) &&
                                         grantorPermissions.GrantPermissions[PropertyPermissionType.EnterProperty] &&
                                         propertyDetail.PublicSetting == PropertyPublicType.Adjustable;
                IsPublic = property.IsPubliclyAccessible;
            }

            PlayerNames = new GuiBindingList<string>();
            PlayerToggles = new GuiBindingList<bool>();
            PermissionStates = new GuiBindingList<bool>();
            PermissionGrantingStates = new GuiBindingList<bool>();
            PermissionNames = new GuiBindingList<string>();
            PermissionDescriptions = new GuiBindingList<string>();


            WatchOnClient(model => model.SearchText);
            WatchOnClient(model => model.PermissionStates);
            WatchOnClient(model => model.PermissionGrantingStates);
            WatchOnClient(model => model.IsPublic);

            Search();
        }

        private void Search()
        {
            Instruction = string.Empty;
            SelectedPlayerIndex = -1;
            IsPlayerSelected = false;
            IEnumerable<Player> dbPlayers;

            // If no search is specified, load only the users who currently have permissions.
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                var permissionQuery = new DBQuery<WorldPropertyPermission>()
                    .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), PropertyId, false);
                var playerIds = DB.Search(permissionQuery).Select(s => s.PlayerId);
                var query = new DBQuery<Player>()
                    .AddFieldSearch(nameof(Entity.Player.Id), playerIds)
                    .AddFieldSearch(nameof(Entity.Player.IsDeleted), false);
                dbPlayers = DB.Search(query);
            }
            // Otherwise look for players by their permission-management names.
            else
            {
                dbPlayers = SearchPlayersByPermissionName();
            }

            var rows = new List<PlayerRowEntry>();

            foreach (var player in dbPlayers)
            {
                rows.Add(new PlayerRowEntry(
                    player.Id,
                    PlayerNameService.GetKnownNameOrFallbackByPlayerId(Player, player.Id, player.Name),
                    false));
            }

            // Row-index lookups (OnSelectPlayer, OnClickSaveChanges, OnClickReset) index
            // into this in lockstep with the bound lists.
            _playerIds.Clear();
            foreach (var row in rows)
                _playerIds.Add(row.Id);

            PlayersTable.Refresh(this, rows);

            PermissionStates.Clear();
            PermissionNames.Clear();
            PermissionDescriptions.Clear();
            PlayerName = string.Empty;
        }

        private List<Player> SearchPlayersByPermissionName()
        {
            var sanitizedSearch = PlayerNameService.SanitizeKnownName(SearchText);
            if (string.IsNullOrWhiteSpace(sanitizedSearch))
                return new List<Player>();

            var playersById = new Dictionary<string, Player>();
            var knownPlayerIds = PlayerNameService.SearchKnownPlayerIdsByName(Player, SearchText, int.MaxValue);
            foreach (var player in SearchPlayersByIds(knownPlayerIds))
            {
                playersById[player.Id] = player;
            }

            var canonicalQuery = BuildEligiblePlayerQuery()
                .AddFieldSearch(nameof(Entity.Player.Name), sanitizedSearch, true)
                .AddPaging(25, 0);
            foreach (var player in DB.Search(canonicalQuery))
            {
                playersById[player.Id] = player;
            }

            return playersById.Values
                .OrderBy(player => PlayerNameService.GetKnownNameOrFallbackByPlayerId(Player, player.Id, player.Name))
                .Take(25)
                .ToList();
        }

        private IEnumerable<Player> SearchPlayersByIds(List<string> playerIds)
        {
            if (playerIds.Count <= 0)
                return Enumerable.Empty<Player>();

            var query = BuildEligiblePlayerQuery()
                .AddFieldSearch(nameof(Entity.Player.Id), playerIds)
                .AddPaging(playerIds.Count, 0);

            return DB.Search(query);
        }

        private DBQuery<Player> BuildEligiblePlayerQuery()
        {
            var query = new DBQuery<Player>()
                .AddFieldSearch(nameof(Entity.Player.IsDeleted), false);

            // Searches within City properties require that the players be a citizen.
            if (!string.IsNullOrWhiteSpace(_cityId))
            {
                query.AddFieldSearch(nameof(Entity.Player.CitizenPropertyId), _cityId, false);
            }

            return query;
        }

        public Action OnClickSearch() => Search;

        public Action OnClickClearSearch() => () =>
        {
            Instruction = string.Empty;
            SearchText = string.Empty;
            Search();
        };

        public Action OnSelectPlayer() => () =>
        {
            Instruction = string.Empty;
            if (SelectedPlayerIndex > -1)
                PlayerToggles[SelectedPlayerIndex] = false;

            SelectedPlayerIndex = NuiGetEventArrayIndex();
            PlayerToggles[SelectedPlayerIndex] = true;

            LoadPlayerInfo();
            IsPlayerSelected = true;
        };

        public Action OnClickSaveChanges() => () =>
        {
            var playerId = GetObjectUUID(Player);
            var query = new DBQuery<WorldPropertyPermission>()
                .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), playerId, false)
                .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), PropertyId, false);
            var grantorPermissions = DB.Search(query).FirstOrDefault();

            // Safety check to ensure the user still has grant permissions.
            // If they lost them while the window was open, they could still send this command even though they no longer have permission.
            if (grantorPermissions == null)
                return;

            // Handle specific player permissions.
            if (IsPlayerSelected)
            {
                var targetPlayerId = _playerIds[SelectedPlayerIndex];

                // Safety check to ensure the user isn't modifying their own permissions.
                if (playerId == targetPlayerId)
                    return;

                query = new DBQuery<WorldPropertyPermission>()
                    .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), targetPlayerId, false)
                    .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), PropertyId, false);
                var targetPermissions = DB.Search(query).FirstOrDefault() ?? CreateEmptyPermissions(targetPlayerId);

                for (var index = 0; index < AvailablePermissions.Count; index++)
                {
                    var permission = AvailablePermissions[index];

                    // Only permissions the player can grant should be updated.
                    if (grantorPermissions.GrantPermissions[permission])
                    {
                        var canGrant = PermissionGrantingStates[index];

                        // Automatically assign permission if the grant permission is assigned.
                        if (canGrant)
                            PermissionStates[index] = true;

                        var hasPermission = PermissionStates[index];

                        targetPermissions.Permissions[permission] = hasPermission;
                        targetPermissions.GrantPermissions[permission] = canGrant;
                    }
                }

                // Player has at least one permission. Set the changes in the DB.
                if (targetPermissions.Permissions.Any(x => x.Value) ||
                    targetPermissions.GrantPermissions.Any(x => x.Value))
                {
                    DB.Set(targetPermissions);
                }
                // Player doesn't have any permissions. Remove the entry.
                else
                {
                    DB.Delete<WorldPropertyPermission>(targetPermissions.Id);
                }
            }

            // Now handle property permissions
            var dbProperty = DB.Get<WorldProperty>(PropertyId);
            if (dbProperty == null)
                return;

            var propertyDetail = Property.GetPropertyDetail(dbProperty.PropertyType);

            if (propertyDetail.PublicSetting == PropertyPublicType.Adjustable &&
                grantorPermissions.GrantPermissions[PropertyPermissionType.EnterProperty])
            {
                dbProperty.IsPubliclyAccessible = IsPublic;
                DB.Set(dbProperty);
            }

            Instruction = $"Permissions updated!";
            InstructionColor = GuiColor.Green;
        };

        public Action OnClickReset() => () =>
        {
            Instruction = string.Empty;

            if (IsPlayerSelected)
            {
                var targetPlayerId = _playerIds[SelectedPlayerIndex];
                var query = new DBQuery<WorldPropertyPermission>()
                    .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), targetPlayerId, false)
                    .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), PropertyId, false);
                var permissions = DB.Search(query).FirstOrDefault() ?? CreateEmptyPermissions(targetPlayerId);

                var rows = new List<PermissionToggleEntry>();

                foreach (var permission in AvailablePermissions)
                {
                    rows.Add(new PermissionToggleEntry(
                        permissions.Permissions[permission],
                        permissions.GrantPermissions[permission]));
                }

                PermissionResetTable.Refresh(this, rows);
            }

            var dbProperty = DB.Get<WorldProperty>(PropertyId);
            IsPublic = dbProperty.IsPubliclyAccessible;
        };
    }
}
