using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Properties.Service;
using SWLOR.Shared.Abstractions.Models;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Properties.Entities;
using SWLOR.Shared.Domain.Properties.Enums;
using SWLOR.Shared.Domain.Repositories;
using SWLOR.Shared.Domain.UI.Payloads;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Model;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.Properties.UI.ViewModel
{
    public class PropertyPermissionsViewModel: GuiViewModelBase<PropertyPermissionsViewModel, PropertyPermissionPayload>
    {
        private readonly IWorldPropertyPermissionRepository _worldPropertyPermissionRepository;
        private readonly IPlayerRepository _playerRepository;
        private readonly IWorldPropertyRepository _worldPropertyRepository;
        private readonly IWorldPropertyCategoryRepository _worldPropertyCategoryRepository;
        private readonly IServiceProvider _serviceProvider;

        public PropertyPermissionsViewModel(IGuiService guiService, IWorldPropertyPermissionRepository worldPropertyPermissionRepository, IPlayerRepository playerRepository, IWorldPropertyRepository worldPropertyRepository, IWorldPropertyCategoryRepository worldPropertyCategoryRepository, IServiceProvider serviceProvider) : base(guiService)
        {
            _worldPropertyPermissionRepository = worldPropertyPermissionRepository;
            _playerRepository = playerRepository;
            _worldPropertyRepository = worldPropertyRepository;
            _worldPropertyCategoryRepository = worldPropertyCategoryRepository;
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded service to break circular dependency
        private PropertyService Property => _serviceProvider.GetRequiredService<PropertyService>();
        
        private bool _isCategory;
        private PropertyType PropertyType;
        private string _cityId;

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
                Permissions = Property.GetPermissionsByPropertyType(PropertyType).ToDictionary(x => x, _ => false),
                GrantPermissions = Property.GetPermissionsByPropertyType(PropertyType).ToDictionary(x => x, _ => false)
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
            
            var dbPlayer = _playerRepository.GetById(targetPlayerId);
            var grantorPermissions = _worldPropertyPermissionRepository.GetByPropertyIdAndPlayerId(PropertyId, playerId).First();
            
            var targetPermissions = _worldPropertyPermissionRepository.GetByPropertyIdAndPlayerId(PropertyId, targetPlayerId).FirstOrDefault() ?? CreateEmptyPermissions(targetPlayerId);

            var permissionStates = new GuiBindingList<bool>();
            var permissionGrantingStates = new GuiBindingList<bool>();
            var permissionNames = new GuiBindingList<string>();
            var permissionDescriptions = new GuiBindingList<string>();
            var permissionEnabled = new GuiBindingList<bool>();
            var grantPermissionEnabled = new GuiBindingList<bool>();

            PlayerName = dbPlayer.Name;

            string ownerPlayerId;
            if (_isCategory)
            {
                var dbCategory = _worldPropertyCategoryRepository.GetById(PropertyId);
                var dbProperty = _worldPropertyRepository.GetById(dbCategory.ParentPropertyId);
                ownerPlayerId = dbProperty.OwnerPlayerId;
            }
            else
            {
                var dbProperty = _worldPropertyRepository.GetById(PropertyId);
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

        protected override void Initialize(PropertyPermissionPayload initialPayload)
        {

            SelectedPlayerIndex = -1;
            PropertyId = initialPayload.PropertyId;
            SearchText = string.Empty;
            _playerIds.Clear();
            _isCategory = initialPayload.IsCategory;
            PropertyType = initialPayload.PropertyType;
            _cityId = initialPayload.CityId;
            IsPlayerSelected = false;
            
            AvailablePermissions = Property.GetPermissionsByPropertyType(PropertyType);

            if (_isCategory)
            {
                var category = _worldPropertyCategoryRepository.GetById(PropertyId);
                PropertyName = category.Name;
                CanChangePublicSetting = false;
                IsPublic = false;
            }
            else
            {
                var playerId = GetObjectUUID(Player);
                var grantorPermissions = _worldPropertyPermissionRepository.GetByPropertyIdAndPlayerId(PropertyId, playerId).First();

                var property = _worldPropertyRepository.GetById(PropertyId);
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
            _playerIds.Clear();
            var playerNames = new GuiBindingList<string>();
            var playerToggles = new GuiBindingList<bool>();
            IEnumerable<Player> dbPlayers;

            // If no search is specified, load only the users who currently have permissions.
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                var playerIds = _worldPropertyPermissionRepository.GetByPropertyId(PropertyId).Select(s => s.PlayerId);
                dbPlayers = _playerRepository.GetActivePlayers().Where(p => playerIds.Contains(p.Id));
            }
            // Otherwise look for players by their names.
            else
            {
                // For now, we'll search through all active players by name
                // This could be optimized with a more specific repository method if needed
                dbPlayers = _playerRepository.GetActivePlayers()
                    .Where(p => p.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                    .Take(25);

                // Searches within City properties require that the players be a citizen.
                if (!string.IsNullOrWhiteSpace(_cityId))
                {
                    dbPlayers = dbPlayers.Where(p => p.CitizenPropertyId == _cityId);
                }
            }

            foreach (var player in dbPlayers)
            {
                _playerIds.Add(player.Id);
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
            var grantorPermissions = _worldPropertyPermissionRepository.GetByPropertyIdAndPlayerId(PropertyId, playerId).FirstOrDefault();

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

                var targetPermissions = _worldPropertyPermissionRepository.GetByPropertyIdAndPlayerId(PropertyId, targetPlayerId).FirstOrDefault() ?? CreateEmptyPermissions(targetPlayerId);

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
                    _worldPropertyPermissionRepository.Save(targetPermissions);
                }
                // Player doesn't have any permissions. Remove the entry.
                else
                {
                    _worldPropertyPermissionRepository.Delete(targetPermissions.Id);
                }
            }

            // Now handle property permissions
            var dbProperty = _worldPropertyRepository.GetById(PropertyId);
            if (dbProperty == null)
                return;

            var propertyDetail = Property.GetPropertyDetail(dbProperty.PropertyType);

            if (propertyDetail.PublicSetting == PropertyPublicType.Adjustable &&
                grantorPermissions.GrantPermissions[PropertyPermissionType.EnterProperty])
            {
                dbProperty.IsPubliclyAccessible = IsPublic;
                _worldPropertyRepository.Save(dbProperty);
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
                var permissions = _worldPropertyPermissionRepository.GetByPropertyIdAndPlayerId(PropertyId, targetPlayerId).FirstOrDefault() ?? CreateEmptyPermissions(targetPlayerId);

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

            var dbProperty = _worldPropertyRepository.GetById(PropertyId);
            IsPublic = dbProperty.IsPubliclyAccessible;
        };
    }
}
