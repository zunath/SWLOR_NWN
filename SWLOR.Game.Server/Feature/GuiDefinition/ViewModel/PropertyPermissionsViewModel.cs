using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
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
        private const int PlayersPerPage = 25;

        private bool _isCategory;
        private PropertyType _propertyType;
        private string _cityId;
        private bool _suppressSearch;

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

        public GuiBindingList<GuiComboEntry> PageNumbers
        {
            get => Get<GuiBindingList<GuiComboEntry>>();
            set => Set(value);
        }

        public int SelectedPageIndex
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (!_suppressSearch)
                    Search();
            }
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

            var permissionStates = new GuiBindingList<bool>();
            var permissionGrantingStates = new GuiBindingList<bool>();
            var permissionNames = new GuiBindingList<string>();
            var permissionDescriptions = new GuiBindingList<string>();
            var permissionEnabled = new GuiBindingList<bool>();
            var grantPermissionEnabled = new GuiBindingList<bool>();

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

            foreach (var type in AvailablePermissions)
            {
                var permission = Property.GetPermissionByType(type);
                permissionNames.Add(permission.Name);
                permissionDescriptions.Add(permission.Description);

                permissionStates.Add(targetPermissions.Permissions[type]);
                permissionGrantingStates.Add(targetPermissions.GrantPermissions[type]);

                if(CanAdjustPermission(grantorPermissions, targetPermissions, type, targetPlayerId, ownerPlayerId))
                    permissionEnabled.Add(true);
                else
                    permissionEnabled.Add(false);

                if(CanAdjustGrantPermission(grantorPermissions, type, targetPlayerId, ownerPlayerId))
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

        private void EnsureUsableWindowGeometry()
        {
            var current = Geometry;
            if (current == null || current.Width >= PropertyPermissionsDefinition.WindowWidth)
                return;

            Geometry = new GuiRectangle(
                current.X,
                current.Y,
                PropertyPermissionsDefinition.WindowWidth,
                current.Height);
        }

        protected override void Initialize(PropertyPermissionPayload initialPayload)
        {
            // Saved sizes from before pagination are too narrow for the layout.
            EnsureUsableWindowGeometry();
            DelayCommand(0.0f, EnsureUsableWindowGeometry);

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

            _suppressSearch = true;
            SelectedPageIndex = 0;
            _suppressSearch = false;

            WatchOnClient(model => model.SearchText);
            WatchOnClient(model => model.SelectedPageIndex);
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
            _playerIds.Clear();
            var playerNames = new GuiBindingList<string>();
            var playerToggles = new GuiBindingList<bool>();
            IEnumerable<Player> dbPlayers;

            // If no search is specified, load only the users who currently have permissions.
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                dbPlayers = SearchPlayersWithPermissions();
            }
            // Otherwise look for players by their permission-management names.
            else
            {
                var matches = SearchPlayersByPermissionName();
                UpdatePagination(matches.Count);
                dbPlayers = matches;
            }

            foreach (var player in dbPlayers)
            {
                _playerIds.Add(player.Id);
                playerNames.Add(PlayerNameService.GetKnownNameOrFallbackByPlayerId(Player, player.Id, player.Name));
                playerToggles.Add(false);
            }

            PlayerNames = playerNames;
            PlayerToggles = playerToggles;

            PermissionStates.Clear();
            PermissionNames.Clear();
            PermissionDescriptions.Clear();
            PlayerName = string.Empty;
        }

        private void UpdatePagination(long totalRecordCount)
        {
            var wasSuppressingSearch = _suppressSearch;
            _suppressSearch = true;

            var pagination = GuiPaginationState.Create(
                totalRecordCount,
                PlayersPerPage,
                SelectedPageIndex);
            PageNumbers = pagination.PageNumbers;
            SelectedPageIndex = pagination.SelectedPageIndex;

            _suppressSearch = wasSuppressingSearch;
        }

        private List<string> GetPermissionPlayerIds()
        {
            var query = new DBQuery<WorldPropertyPermission>()
                .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), PropertyId, false);
            var permissionCount = (int)DB.SearchCount(query);
            if (permissionCount <= 0)
                return new List<string>();

            query.AddPaging(permissionCount, 0);
            return DB.Search(query)
                .Select(permission => permission.PlayerId)
                .ToList();
        }

        private IEnumerable<Player> SearchPlayersWithPermissions()
        {
            var playerIds = GetPermissionPlayerIds();
            if (playerIds.Count <= 0)
            {
                UpdatePagination(0);
                return Enumerable.Empty<Player>();
            }

            var query = new DBQuery<Player>()
                .AddFieldSearch(nameof(Entity.Player.Id), playerIds)
                .AddFieldSearch(nameof(Entity.Player.IsDeleted), false)
                .OrderBy(nameof(Entity.Player.Name));
            UpdatePagination(DB.SearchCount(query));

            query.AddPaging(PlayersPerPage, SelectedPageIndex * PlayersPerPage);
            return DB.Search(query);
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

        private void SearchFromFirstPage()
        {
            _suppressSearch = true;
            SelectedPageIndex = 0;
            _suppressSearch = false;

            Search();
        }

        public Action OnClickSearch() => SearchFromFirstPage;

        public Action OnClickClearSearch() => () =>
        {
            Instruction = string.Empty;
            SearchText = string.Empty;
            SearchFromFirstPage();
        };

        public Action OnClickPreviousPage() => () =>
        {
            SelectedPageIndex = Math.Max(0, SelectedPageIndex - 1);
        };

        public Action OnClickNextPage() => () =>
        {
            SelectedPageIndex = Math.Min(PageNumbers.Count - 1, SelectedPageIndex + 1);
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

                var permissionStates = new GuiBindingList<bool>();
                var grantPermissionStates = new GuiBindingList<bool>();

                for (var index = 0; index < AvailablePermissions.Count; index++)
                {
                    var permission = AvailablePermissions[index];

                    permissionStates.Add(permissions.Permissions[permission]);
                    grantPermissionStates.Add(permissions.GrantPermissions[permission]);
                }

                PermissionStates = permissionStates;
                PermissionGrantingStates = grantPermissionStates;
            }

            var dbProperty = DB.Get<WorldProperty>(PropertyId);
            IsPublic = dbProperty.IsPubliclyAccessible;
        };
    }
}
