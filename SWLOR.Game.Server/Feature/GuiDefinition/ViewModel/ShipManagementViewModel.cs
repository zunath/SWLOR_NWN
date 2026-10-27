using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using PlayerShip = SWLOR.Game.Server.Entity.PlayerShip;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class ShipManagementViewModel : GuiViewModelBase<ShipManagementViewModel, ShipManagementPayload>
    {
        public const string BoardingOutOfCombatVariable = "SHIP_BOARDING_OUT_OF_COMBAT";

        private const string _blank = "Blank";
        private const int ShipsPerPage = 25;

        private int SelectedShipIndex { get; set; }
        private List<string> _shipIds { get; set; } = new List<string>();
        private Location _spaceLocation;
        private Location _landingLocation;
        private PlanetType _planetType;
        private bool _skipPageLoad;
        private List<PlayerShip> _myShips = new();
        private List<string> _sharedPropertyIds = new();

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

                if (!_skipPageLoad)
                    LoadPage();
            }
        }

        public string ShipCountRegistered
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiBindingList<string> ShipNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> ShipToggles
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public bool IsRegisterEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsUnregisterEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string ShipName
        {
            get => Get<string>();
            set => Set(value);
        }

        public string ShipType
        {
            get => Get<string>();
            set => Set(value);
        }

        public float Shields
        {
            get => Get<float>();
            set => Set(value);
        }

        public string ShieldsTooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public float Hull
        {
            get => Get<float>();
            set => Set(value);
        }

        public string HullTooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public float Capacitor
        {
            get => Get<float>();
            set => Set(value);
        }

        public string CapacitorTooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string ShieldRechargeRate
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool HighPower1Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool HighPower2Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool HighPower3Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool HighPower4Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool HighPower5Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool HighPower6Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool HighPower7Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool HighPower8Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string HighPower1Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string HighPower2Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string HighPower3Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string HighPower4Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string HighPower5Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string HighPower6Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string HighPower7Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string HighPower8Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string HighPower1Resref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string HighPower2Resref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string HighPower3Resref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string HighPower4Resref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string HighPower5Resref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string HighPower6Resref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string HighPower7Resref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string HighPower8Resref
        {
            get => Get<string>();
            set => Set(value);
        }


        public bool LowPower1Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool LowPower2Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool LowPower3Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool LowPower4Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool LowPower5Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool LowPower6Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool LowPower7Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool LowPower8Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool Configuration1Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool Configuration2Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool Configuration3Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool Configuration4Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool Configuration5Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool Configuration6Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool Configuration7Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool Configuration8Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string LowPower1Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string LowPower2Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string LowPower3Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string LowPower4Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string LowPower5Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string LowPower6Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string LowPower7Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string LowPower8Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Configuration1Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Configuration2Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Configuration3Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Configuration4Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Configuration5Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Configuration6Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Configuration7Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Configuration8Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string LowPower1Resref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string LowPower2Resref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string LowPower3Resref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string LowPower4Resref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string LowPower5Resref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string LowPower6Resref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string LowPower7Resref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string LowPower8Resref
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Configuration1Resref
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Configuration2Resref
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Configuration3Resref
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Configuration4Resref
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Configuration5Resref
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Configuration6Resref
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Configuration7Resref
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Configuration8Resref
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool MidPower1Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool MidPower2Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool MidPower3Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool MidPower4Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool MidPower5Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool MidPower6Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool MidPower7Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool MidPower8Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string MidPower1Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string MidPower2Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string MidPower3Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string MidPower4Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string MidPower5Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string MidPower6Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string MidPower7Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string MidPower8Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string MidPower1Resref
        {
            get => Get<string>();
            set => Set(value);
        }

        public string MidPower2Resref
        {
            get => Get<string>();
            set => Set(value);
        }

        public string MidPower3Resref
        {
            get => Get<string>();
            set => Set(value);
        }

        public string MidPower4Resref
        {
            get => Get<string>();
            set => Set(value);
        }

        public string MidPower5Resref
        {
            get => Get<string>();
            set => Set(value);
        }

        public string MidPower6Resref
        {
            get => Get<string>();
            set => Set(value);
        }

        public string MidPower7Resref
        {
            get => Get<string>();
            set => Set(value);
        }

        public string MidPower8Resref
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsMidPowerVisible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsConfigurationVisible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsBoardShipEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsNameEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsRefitEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsPermissionsEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsRemoveAccessEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string ShipLocation
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsMyShipsToggled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsMyShipsEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsOtherShipsToggled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsOtherShipsEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string RepairText
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsRepairEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        private List<PlayerShip> GetMyShips()
        {
            return PlayerShipAccess.GetOwnedShips(GetObjectUUID(Player));
        }

        private void SetPageIndexWithoutLoad(int pageIndex)
        {
            var wasSkipping = _skipPageLoad;
            _skipPageLoad = true;
            SelectedPageIndex = pageIndex;
            _skipPageLoad = wasSkipping;
        }

        private void UpdatePagination(int totalCount)
        {
            var pagination = GuiPaginationState.Create(totalCount, ShipsPerPage, SelectedPageIndex);
            PageNumbers = pagination.PageNumbers;
            SetPageIndexWithoutLoad(pagination.SelectedPageIndex);
        }

        /// <summary>
        /// Shows the selected page of the active tab. Other Ships reads only that page's ships; the
        /// candidate property Ids are loaded once when the tab opens.
        /// </summary>
        private void LoadPage()
        {
            if (IsOtherShipsToggled)
            {
                var page = PlayerShipAccess.GetSharedShipsPage(_sharedPropertyIds, SelectedPageIndex, ShipsPerPage, out var totalCount);
                UpdatePagination(totalCount);
                LoadShips(page.Select(entry => (entry.Ship.Id, entry.Property.CustomName)));
            }
            else
            {
                UpdatePagination(_myShips.Count);
                LoadShips(_myShips
                    .Skip(SelectedPageIndex * ShipsPerPage)
                    .Take(ShipsPerPage)
                    .Select(ship => (ship.Id, DB.Get<WorldProperty>(ship.PropertyId).CustomName)));
            }

            ToggleRegisterButtons();
            LoadShip();
        }

        /// <summary>
        /// Duplicate records resolve like Property.HasPropertyPermission. A missing record grants no access.
        /// </summary>
        private WorldPropertyPermission GetShipPermission(string propertyId)
        {
            var permissionQuery = new DBQuery<WorldPropertyPermission>()
                .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), GetObjectUUID(Player), false)
                .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), propertyId, false);
            return DB.Search(permissionQuery).FirstOrDefault();
        }

        /// <summary>Entering needs Enter Property, or a ship docked aboard that the player may pilot.</summary>
        private bool CanBoardShip(string propertyId)
        {
            return Property.HasPropertyPermission(Player, propertyId, PropertyPermissionType.EnterProperty) ||
                   Space.HasShipDockedAboard(Player, propertyId);
        }

        private int CalculateRepairBill(PlayerShip ship)
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);
            var starportLevel = Property.GetEffectiveUpgradeLevel(dbPlayer.CitizenPropertyId, PropertyUpgradeType.StarportLevel);
            var detail = Space.FindShipDetail(ship.Status.ItemTag);
            var sizeMultiplier = ShipRepairBill.GetSizeMultiplier(detail?.Hull, ship.Status.CapitalShip);

            return ShipRepairBill.Calculate(
                ship.Status.MaxShield - ship.Status.Shield,
                ship.Status.MaxHull - ship.Status.Hull,
                sizeMultiplier,
                starportLevel,
                GetAbilityScore(Player, AbilityType.Social));
        }

        protected override void Initialize(ShipManagementPayload initialPayload)
        {
            _planetType = initialPayload.PlanetType;

            List<PlayerShip> dbPlayerShips;
            if (!string.IsNullOrWhiteSpace(initialPayload.SpecificPropertyId))
            {
                var query = new DBQuery<PlayerShip>()
                    .AddFieldSearch(nameof(PlayerShip.PropertyId), initialPayload.SpecificPropertyId, false);
                dbPlayerShips = DB.Search(query).ToList();

                var dbProperty = DB.Get<WorldProperty>(initialPayload.SpecificPropertyId);
                var spacePropertyLocation = dbProperty.Positions[PropertyLocationType.SpacePosition];
                var spaceArea = Area.GetAreaByResref(spacePropertyLocation.AreaResref);
                var spacePosition = Vector3(spacePropertyLocation.X, spacePropertyLocation.Y, spacePropertyLocation.Z);
                _spaceLocation = Location(spaceArea, spacePosition, spacePropertyLocation.Orientation);

                var landingPropertyLocation = dbProperty.Positions[PropertyLocationType.DockPosition];
                uint landingArea;
                if (string.IsNullOrWhiteSpace(landingPropertyLocation.AreaResref))
                {
                    landingArea = Property.TryGetLoadedInstance(landingPropertyLocation.InstancePropertyId, out var landingInstance)
                        ? landingInstance.Area
                        : OBJECT_INVALID;
                }
                else
                {
                    landingArea = Area.GetAreaByResref(landingPropertyLocation.AreaResref);
                }

                var landingPosition = Vector3(landingPropertyLocation.X, landingPropertyLocation.Y, landingPropertyLocation.Z);
                _landingLocation = Location(landingArea, landingPosition, landingPropertyLocation.Orientation);

                IsOtherShipsEnabled = false;
            }
            else
            {
                _spaceLocation = initialPayload.SpaceLocation;
                _landingLocation = initialPayload.LandingLocation;
                dbPlayerShips = GetMyShips();

                IsOtherShipsEnabled = true;
            }

            _skipPageLoad = true;
            _myShips = dbPlayerShips;
            _sharedPropertyIds = new List<string>();
            ShipCountRegistered = $"Ships: {dbPlayerShips.Count} / {Space.MaxRegisteredShips}";
            IsMyShipsToggled = true;
            IsOtherShipsToggled = false;
            SelectedPageIndex = 0;
            LoadPage();

            WatchOnClient(model => model.ShipName);
            WatchOnClient(model => model.SelectedPageIndex);
            _skipPageLoad = false;
        }

        private void LoadShip()
        {
            if (SelectedShipIndex <= -1)
            {
                ShipName = "[Select a Ship]";
                ShipType = string.Empty;

                Shields = 0f;
                ShieldsTooltip = string.Empty;

                Hull = 0f;
                HullTooltip = string.Empty;

                Capacitor = 0f;
                CapacitorTooltip = string.Empty;

                ShieldRechargeRate = string.Empty;

                HighPower1Visible = false;
                HighPower2Visible = false;
                HighPower3Visible = false;
                HighPower4Visible = false;
                HighPower5Visible = false;
                HighPower6Visible = false;
                HighPower7Visible = false;
                HighPower8Visible = false;

                HighPower1Tooltip = string.Empty;
                HighPower2Tooltip = string.Empty;
                HighPower3Tooltip = string.Empty;
                HighPower4Tooltip = string.Empty;
                HighPower5Tooltip = string.Empty;
                HighPower6Tooltip = string.Empty;
                HighPower7Tooltip = string.Empty;
                HighPower8Tooltip = string.Empty;

                HighPower1Resref = _blank;
                HighPower2Resref = _blank;
                HighPower3Resref = _blank;
                HighPower4Resref = _blank;
                HighPower5Resref = _blank;
                HighPower6Resref = _blank;
                HighPower7Resref = _blank;
                HighPower8Resref = _blank;

                for (var slot = 1; slot <= 8; slot++)
                {
                    SetMidSlot(slot, false, _blank, string.Empty);
                }

                IsMidPowerVisible = false;
                IsConfigurationVisible = true;

                LowPower1Visible = false;
                LowPower2Visible = false;
                LowPower3Visible = false;
                LowPower4Visible = false;
                LowPower5Visible = false;
                LowPower6Visible = false;
                LowPower7Visible = false;
                LowPower8Visible = false;

                Configuration1Visible = false;
                Configuration2Visible = false;
                Configuration3Visible = false;
                Configuration4Visible = false;
                Configuration5Visible = false;
                Configuration6Visible = false;
                Configuration7Visible = false;
                Configuration8Visible = false;

                LowPower1Tooltip = string.Empty;
                LowPower2Tooltip = string.Empty;
                LowPower3Tooltip = string.Empty;
                LowPower4Tooltip = string.Empty;
                LowPower5Tooltip = string.Empty;
                LowPower6Tooltip = string.Empty;
                LowPower7Tooltip = string.Empty;
                LowPower8Tooltip = string.Empty;

                LowPower1Resref = _blank;
                LowPower2Resref = _blank;
                LowPower3Resref = _blank;
                LowPower4Resref = _blank;
                LowPower5Resref = _blank;
                LowPower6Resref = _blank;
                LowPower7Resref = _blank;
                LowPower8Resref = _blank;

                Configuration1Resref = _blank;
                Configuration2Resref = _blank;
                Configuration3Resref = _blank;
                Configuration4Resref = _blank;
                Configuration5Resref = _blank;
                Configuration6Resref = _blank;
                Configuration7Resref = _blank;
                Configuration8Resref = _blank;

                IsRefitEnabled = false;
                IsBoardShipEnabled = false;
                IsPermissionsEnabled = false;
                IsNameEnabled = false;
                ShipLocation = string.Empty;
                IsRepairEnabled = false;
                RepairText = "Repair";
            }
            else
            {
                var shipId = _shipIds[SelectedShipIndex];
                var ship = DB.Get<PlayerShip>(shipId);
                var shipDetail = Space.GetShipDetailByItemTag(ship.Status.ItemTag);
                var property = DB.Get<WorldProperty>(ship.PropertyId);

                var permission = GetShipPermission(ship.PropertyId);
                var hasAccess = permission != null;
                var isDockedAboard = Space.IsDockedAboardHost(ship, out var hostName);
                var currentLocation = GetShipLocation(ship, property, out var isDockInstanceLoading);
                var isAtCurrentLocation = currentLocation == GetArea(Player);
                var isInSpace = property.Positions.ContainsKey(PropertyLocationType.CurrentPosition);
                var gold = GetGold(Player);
                var repairPrice = CalculateRepairBill(ship);

                ShipName = property.CustomName;
                ShipType = $"Type: {shipDetail.Name}";

                Shields = (float)ship.Status.Shield / ship.Status.MaxShield;
                ShieldsTooltip = $"Shields: {ship.Status.Shield} / {ship.Status.MaxShield}";

                Hull = (float)ship.Status.Hull / ship.Status.MaxHull;
                HullTooltip = $"Hull: {ship.Status.Hull} / {ship.Status.MaxHull}";

                Capacitor = (float)ship.Status.Capacitor / ship.Status.MaxCapacitor;
                CapacitorTooltip = $"Capacitor: {ship.Status.Capacitor} / {ship.Status.MaxCapacitor}";

                ShieldRechargeRate = $"{ship.Status.ShieldRechargeRate}s";

                HighPower1Visible = shipDetail.HighPowerNodes >= 1;
                HighPower2Visible = shipDetail.HighPowerNodes >= 2;
                HighPower3Visible = shipDetail.HighPowerNodes >= 3;
                HighPower4Visible = shipDetail.HighPowerNodes >= 4;
                HighPower5Visible = shipDetail.HighPowerNodes >= 5;
                HighPower6Visible = shipDetail.HighPowerNodes >= 6;
                HighPower7Visible = shipDetail.HighPowerNodes >= 7;
                HighPower8Visible = shipDetail.HighPowerNodes >= 8;

                var module = ship.Status.HighPowerModules.ContainsKey(1)
                    ? ship.Status.HighPowerModules[1]
                    : null;
                if (module != null)
                {
                    var detail = Space.GetShipModuleDetailByItemTag(module.ItemTag);
                    HighPower1Resref = detail.Texture;
                    HighPower1Tooltip = detail.Name;
                }
                else
                {
                    HighPower1Resref = _blank;
                    HighPower1Tooltip = string.Empty;
                }

                module = ship.Status.HighPowerModules.ContainsKey(2)
                    ? ship.Status.HighPowerModules[2]
                    : null;
                if (module != null)
                {
                    var detail = Space.GetShipModuleDetailByItemTag(module.ItemTag);
                    HighPower2Resref = detail.Texture;
                    HighPower2Tooltip = detail.Name;
                }
                else
                {
                    HighPower2Resref = _blank;
                    HighPower2Tooltip = string.Empty;
                }

                module = ship.Status.HighPowerModules.ContainsKey(3)
                    ? ship.Status.HighPowerModules[3]
                    : null;
                if (module != null)
                {
                    var detail = Space.GetShipModuleDetailByItemTag(module.ItemTag);
                    HighPower3Resref = detail.Texture;
                    HighPower3Tooltip = detail.Name;
                }
                else
                {
                    HighPower3Resref = _blank;
                    HighPower3Tooltip = string.Empty;
                }

                module = ship.Status.HighPowerModules.ContainsKey(4)
                    ? ship.Status.HighPowerModules[4]
                    : null;
                if (module != null)
                {
                    var detail = Space.GetShipModuleDetailByItemTag(module.ItemTag);
                    HighPower4Resref = detail.Texture;
                    HighPower4Tooltip = detail.Name;
                }
                else
                {
                    HighPower4Resref = _blank;
                    HighPower4Tooltip = string.Empty;
                }

                module = ship.Status.HighPowerModules.ContainsKey(5)
                    ? ship.Status.HighPowerModules[5]
                    : null;
                if (module != null)
                {
                    var detail = Space.GetShipModuleDetailByItemTag(module.ItemTag);
                    HighPower5Resref = detail.Texture;
                    HighPower5Tooltip = detail.Name;
                }
                else
                {
                    HighPower5Resref = _blank;
                    HighPower5Tooltip = string.Empty;
                }

                module = ship.Status.HighPowerModules.ContainsKey(6)
                    ? ship.Status.HighPowerModules[6]
                    : null;
                if (module != null)
                {
                    var detail = Space.GetShipModuleDetailByItemTag(module.ItemTag);
                    HighPower6Resref = detail.Texture;
                    HighPower6Tooltip = detail.Name;
                }
                else
                {
                    HighPower6Resref = _blank;
                    HighPower6Tooltip = string.Empty;
                }

                module = ship.Status.HighPowerModules.ContainsKey(7)
                    ? ship.Status.HighPowerModules[7]
                    : null;
                if (module != null)
                {
                    var detail = Space.GetShipModuleDetailByItemTag(module.ItemTag);
                    HighPower7Resref = detail.Texture;
                    HighPower7Tooltip = detail.Name;
                }
                else
                {
                    HighPower7Resref = _blank;
                    HighPower7Tooltip = string.Empty;
                }

                module = ship.Status.HighPowerModules.ContainsKey(8)
                    ? ship.Status.HighPowerModules[8]
                    : null;
                if (module != null)
                {
                    var detail = Space.GetShipModuleDetailByItemTag(module.ItemTag);
                    HighPower8Resref = detail.Texture;
                    HighPower8Tooltip = detail.Name;
                }
                else
                {
                    HighPower8Resref = _blank;
                    HighPower8Tooltip = string.Empty;
                }

                LowPower1Visible = shipDetail.LowPowerNodes >= 1;
                LowPower2Visible = shipDetail.LowPowerNodes >= 2;
                LowPower3Visible = shipDetail.LowPowerNodes >= 3;
                LowPower4Visible = shipDetail.LowPowerNodes >= 4;
                LowPower5Visible = shipDetail.LowPowerNodes >= 5;
                LowPower6Visible = shipDetail.LowPowerNodes >= 6;
                LowPower7Visible = shipDetail.LowPowerNodes >= 7;
                LowPower8Visible = shipDetail.LowPowerNodes >= 8;

                module = ship.Status.LowPowerModules.ContainsKey(1)
                    ? ship.Status.LowPowerModules[1]
                    : null;
                if (module != null)
                {
                    var detail = Space.GetShipModuleDetailByItemTag(module.ItemTag);
                    LowPower1Resref = detail.Texture;
                    LowPower1Tooltip = detail.Name;
                }
                else
                {
                    LowPower1Resref = _blank;
                    LowPower1Tooltip = string.Empty;
                }

                module = ship.Status.LowPowerModules.ContainsKey(2)
                    ? ship.Status.LowPowerModules[2]
                    : null;
                if (module != null)
                {
                    var detail = Space.GetShipModuleDetailByItemTag(module.ItemTag);
                    LowPower2Resref = detail.Texture;
                    LowPower2Tooltip = detail.Name;
                }
                else
                {
                    LowPower2Resref = _blank;
                    LowPower2Tooltip = string.Empty;
                }

                module = ship.Status.LowPowerModules.ContainsKey(3)
                    ? ship.Status.LowPowerModules[3]
                    : null;
                if (module != null)
                {
                    var detail = Space.GetShipModuleDetailByItemTag(module.ItemTag);
                    LowPower3Resref = detail.Texture;
                    LowPower3Tooltip = detail.Name;
                }
                else
                {
                    LowPower3Resref = _blank;
                    LowPower3Tooltip = string.Empty;
                }

                module = ship.Status.LowPowerModules.ContainsKey(4)
                    ? ship.Status.LowPowerModules[4]
                    : null;
                if (module != null)
                {
                    var detail = Space.GetShipModuleDetailByItemTag(module.ItemTag);
                    LowPower4Resref = detail.Texture;
                    LowPower4Tooltip = detail.Name;
                }
                else
                {
                    LowPower4Resref = _blank;
                    LowPower4Tooltip = string.Empty;
                }

                module = ship.Status.LowPowerModules.ContainsKey(5)
                    ? ship.Status.LowPowerModules[5]
                    : null;
                if (module != null)
                {
                    var detail = Space.GetShipModuleDetailByItemTag(module.ItemTag);
                    LowPower5Resref = detail.Texture;
                    LowPower5Tooltip = detail.Name;
                }
                else
                {
                    LowPower5Resref = _blank;
                    LowPower5Tooltip = string.Empty;
                }

                module = ship.Status.LowPowerModules.ContainsKey(6)
                    ? ship.Status.LowPowerModules[6]
                    : null;
                if (module != null)
                {
                    var detail = Space.GetShipModuleDetailByItemTag(module.ItemTag);
                    LowPower6Resref = detail.Texture;
                    LowPower6Tooltip = detail.Name;
                }
                else
                {
                    LowPower6Resref = _blank;
                    LowPower6Tooltip = string.Empty;
                }

                module = ship.Status.LowPowerModules.ContainsKey(7)
                    ? ship.Status.LowPowerModules[7]
                    : null;
                if (module != null)
                {
                    var detail = Space.GetShipModuleDetailByItemTag(module.ItemTag);
                    LowPower7Resref = detail.Texture;
                    LowPower7Tooltip = detail.Name;
                }
                else
                {
                    LowPower7Resref = _blank;
                    LowPower7Tooltip = string.Empty;
                }

                module = ship.Status.LowPowerModules.ContainsKey(8)
                    ? ship.Status.LowPowerModules[8]
                    : null;
                if (module != null)
                {
                    var detail = Space.GetShipModuleDetailByItemTag(module.ItemTag);
                    LowPower8Resref = detail.Texture;
                    LowPower8Tooltip = detail.Name;
                }
                else
                {
                    LowPower8Resref = _blank;
                    LowPower8Tooltip = string.Empty;
                }

                for (var slot = 1; slot <= 8; slot++)
                {
                    var midModule = ship.Status.MidPowerModules.GetValueOrDefault(slot);
                    var midDetail = midModule == null ? null : Space.GetShipModuleDetailByItemTag(midModule.ItemTag);
                    SetMidSlot(slot, shipDetail.MidPowerNodes >= slot, midDetail?.Texture ?? _blank, midDetail?.Name ?? string.Empty);
                }

                // Legacy hulls have no mid slots and revamp hulls have no configuration slots.
                IsMidPowerVisible = shipDetail.MidPowerNodes > 0;
                IsConfigurationVisible = shipDetail.ConfigurationNodes > 0;

                Configuration1Visible = shipDetail.ConfigurationNodes >= 1;
                Configuration2Visible = shipDetail.ConfigurationNodes >= 2;
                Configuration3Visible = shipDetail.ConfigurationNodes >= 3;
                Configuration4Visible = shipDetail.ConfigurationNodes >= 4;
                Configuration5Visible = shipDetail.ConfigurationNodes >= 5;
                Configuration6Visible = shipDetail.ConfigurationNodes >= 6;
                Configuration7Visible = shipDetail.ConfigurationNodes >= 7;
                Configuration8Visible = shipDetail.ConfigurationNodes >= 8;

                module = ship.Status.ConfigurationModules.ContainsKey(1)
                    ? ship.Status.ConfigurationModules[1]
                    : null;
                if (module != null)
                {
                    var detail = Space.GetShipModuleDetailByItemTag(module.ItemTag);
                    Configuration1Resref = detail.Texture;
                    Configuration1Tooltip = detail.Name;
                }
                else
                {
                    Configuration1Resref = _blank;
                    Configuration1Tooltip = string.Empty;
                }

                module = ship.Status.ConfigurationModules.ContainsKey(2)
                    ? ship.Status.ConfigurationModules[2]
                    : null;
                if (module != null)
                {
                    var detail = Space.GetShipModuleDetailByItemTag(module.ItemTag);
                    Configuration2Resref = detail.Texture;
                    Configuration2Tooltip = detail.Name;
                }
                else
                {
                    Configuration2Resref = _blank;
                    Configuration2Tooltip = string.Empty;
                }

                module = ship.Status.ConfigurationModules.ContainsKey(3)
                    ? ship.Status.ConfigurationModules[3]
                    : null;
                if (module != null)
                {
                    var detail = Space.GetShipModuleDetailByItemTag(module.ItemTag);
                    Configuration3Resref = detail.Texture;
                    Configuration3Tooltip = detail.Name;
                }
                else
                {
                    Configuration3Resref = _blank;
                    Configuration3Tooltip = string.Empty;
                }

                module = ship.Status.ConfigurationModules.ContainsKey(4)
                    ? ship.Status.ConfigurationModules[4]
                    : null;
                if (module != null)
                {
                    var detail = Space.GetShipModuleDetailByItemTag(module.ItemTag);
                    Configuration4Resref = detail.Texture;
                    Configuration4Tooltip = detail.Name;
                }
                else
                {
                    Configuration4Resref = _blank;
                    Configuration4Tooltip = string.Empty;
                }

                module = ship.Status.ConfigurationModules.ContainsKey(5)
                    ? ship.Status.ConfigurationModules[5]
                    : null;
                if (module != null)
                {
                    var detail = Space.GetShipModuleDetailByItemTag(module.ItemTag);
                    Configuration5Resref = detail.Texture;
                    Configuration5Tooltip = detail.Name;
                }
                else
                {
                    Configuration5Resref = _blank;
                    Configuration5Tooltip = string.Empty;
                }

                module = ship.Status.ConfigurationModules.ContainsKey(6)
                    ? ship.Status.ConfigurationModules[6]
                    : null;
                if (module != null)
                {
                    var detail = Space.GetShipModuleDetailByItemTag(module.ItemTag);
                    Configuration6Resref = detail.Texture;
                    Configuration6Tooltip = detail.Name;
                }
                else
                {
                    Configuration6Resref = _blank;
                    Configuration6Tooltip = string.Empty;
                }

                module = ship.Status.ConfigurationModules.ContainsKey(7)
                    ? ship.Status.ConfigurationModules[7]
                    : null;
                if (module != null)
                {
                    var detail = Space.GetShipModuleDetailByItemTag(module.ItemTag);
                    Configuration7Resref = detail.Texture;
                    Configuration7Tooltip = detail.Name;
                }
                else
                {
                    Configuration7Resref = _blank;
                    Configuration7Tooltip = string.Empty;
                }

                module = ship.Status.ConfigurationModules.ContainsKey(8)
                    ? ship.Status.ConfigurationModules[8]
                    : null;
                if (module != null)
                {
                    var detail = Space.GetShipModuleDetailByItemTag(module.ItemTag);
                    Configuration8Resref = detail.Texture;
                    Configuration8Tooltip = detail.Name;
                }
                else
                {
                    Configuration8Resref = _blank;
                    Configuration8Tooltip = string.Empty;
                }

                IsBoardShipEnabled = CanBoardShip(ship.PropertyId) && isAtCurrentLocation;
                IsNameEnabled = hasAccess && permission.Permissions.GetValueOrDefault(PropertyPermissionType.RenameProperty) && isAtCurrentLocation;
                IsRefitEnabled = hasAccess && permission.Permissions.GetValueOrDefault(PropertyPermissionType.RefitShip) && isAtCurrentLocation;
                IsPermissionsEnabled = hasAccess && permission.GrantPermissions.Any(x => x.Value) && isAtCurrentLocation;
                ShipLocation = isDockedAboard
                    ? $"Docked aboard {hostName}"
                    : isInSpace
                        ? "In Space"
                        : isDockInstanceLoading
                            ? "Docked (loading...)"
                            : GetName(currentLocation);
                IsRepairEnabled = (ship.Status.Shield < ship.Status.MaxShield ||
                                  ship.Status.Hull < ship.Status.MaxHull) &&
                                  gold >= repairPrice &&
                                  isAtCurrentLocation;
                RepairText = $"Repair ({repairPrice} cr)";
            }

            ToggleRegisterButtons();
        }

        private void SetMidSlot(int slot, bool visible, string resref, string tooltip)
        {
            switch (slot)
            {
                case 1: MidPower1Visible = visible; MidPower1Resref = resref; MidPower1Tooltip = tooltip; break;
                case 2: MidPower2Visible = visible; MidPower2Resref = resref; MidPower2Tooltip = tooltip; break;
                case 3: MidPower3Visible = visible; MidPower3Resref = resref; MidPower3Tooltip = tooltip; break;
                case 4: MidPower4Visible = visible; MidPower4Resref = resref; MidPower4Tooltip = tooltip; break;
                case 5: MidPower5Visible = visible; MidPower5Resref = resref; MidPower5Tooltip = tooltip; break;
                case 6: MidPower6Visible = visible; MidPower6Resref = resref; MidPower6Tooltip = tooltip; break;
                case 7: MidPower7Visible = visible; MidPower7Resref = resref; MidPower7Tooltip = tooltip; break;
                case 8: MidPower8Visible = visible; MidPower8Resref = resref; MidPower8Tooltip = tooltip; break;
            }
        }

        private uint GetShipLocation(PlayerShip ship, WorldProperty property, out bool isDockInstanceLoading)
        {
            isDockInstanceLoading = false;

            // A ship docked aboard another travels with it and is reached through that ship.
            if (property.Positions.ContainsKey(PropertyLocationType.CurrentPosition) ||
                Space.IsDockedAboardHost(ship, out _))
            {
                return OBJECT_INVALID;
            }

            var landingLocation = property.Positions[PropertyLocationType.DockPosition];
            uint area;
            if (string.IsNullOrWhiteSpace(landingLocation.AreaResref))
            {
                if (Property.TryGetLoadedInstance(landingLocation.InstancePropertyId, out var instance))
                {
                    area = instance.Area;
                }
                else
                {
                    isDockInstanceLoading = true;
                    area = OBJECT_INVALID;
                }
            }
            else
            {
                area = Area.GetAreaByResref(landingLocation.AreaResref);
            }

            return area;
        }

        private void ToggleRegisterButtons()
        {
            IsRegisterEnabled = _shipIds.Count < Space.MaxRegisteredShips && IsMyShipsToggled;

            if (SelectedShipIndex > -1)
            {
                var playerId = GetObjectUUID(Player);
                var shipId = _shipIds[SelectedShipIndex];
                var dbShip = DB.Get<PlayerShip>(shipId);
                var dbProperty = DB.Get<WorldProperty>(dbShip.PropertyId);
                var shipLocation = GetShipLocation(dbShip, dbProperty, out _);
                IsUnregisterEnabled = shipLocation == GetArea(Player) && playerId == dbProperty.OwnerPlayerId;
                IsRemoveAccessEnabled = IsOtherShipsToggled && playerId != dbShip.OwnerPlayerId;
            }
            else
            {
                IsUnregisterEnabled = false;
                IsRemoveAccessEnabled = false;
            }
        }

        public Action OnClickShip() => () =>
        {
            if (SelectedShipIndex > -1)
                ShipToggles[SelectedShipIndex] = false;

            var index = NuiGetEventArrayIndex();
            SelectedShipIndex = index;
            ShipToggles[index] = true;

            LoadShip();
        };

        public Action OnClickRegisterShip() => () =>
        {
            Targeting.EnterTargetingMode(Player, ObjectType.Item, "Please click on a ship deed within your inventory.",
                item =>
            {
                if (GetItemPossessor(item) != Player)
                {
                    FloatingTextStringOnCreature("Item must be in your inventory.", Player, false);
                    return;
                }

                if (!Space.IsItemShip(item))
                {
                    FloatingTextStringOnCreature("Only ship deeds may be targeted.", Player, false);
                    return;
                }

                if (!Item.CanCreatureUseItem(Player, item))
                {
                    FloatingTextStringOnCreature("You do not meet the requirements necessary to register this ship.", Player, false);
                    return;
                }

                var playerId = GetObjectUUID(Player);
                var query = new DBQuery<PlayerShip>()
                    .AddFieldSearch(nameof(PlayerShip.OwnerPlayerId), playerId, false);
                var registeredShipCount = (int)DB.SearchCount(query);

                if (registeredShipCount >= Space.MaxRegisteredShips)
                {
                    FloatingTextStringOnCreature($"You may only have {Space.MaxRegisteredShips} ships registered at a time.", Player, false);
                    return;
                }

                // Validation passed. Add the ship and register it under this player.
                var itemTag = GetTag(item);
                var shipDetail = Space.GetShipDetailByItemTag(itemTag);
                var bonuses = Space.GetShipBonuses(item);

                // Spawn the property associated with this ship.
                var property = Property.CreateStarship(
                    Player,
                    shipDetail.Layout,
                    _planetType,
                    _spaceLocation,
                    _landingLocation);

                var ship = new PlayerShip
                {
                    OwnerPlayerId = playerId,
                    PropertyId = property.Id,
                    SerializedItem = ObjectPlugin.Serialize(item),
                    Status = new ShipStatus
                    {
                        ItemTag = itemTag,
                        RequiredSkills = Item.GetRequiredSkills(item),
                        Shield = shipDetail.MaxShield + bonuses.Shield,
                        MaxShield = shipDetail.MaxShield + bonuses.Shield,
                        Hull = shipDetail.MaxHull + bonuses.Hull,
                        MaxHull = shipDetail.MaxHull + bonuses.Hull,
                        Capacitor = shipDetail.MaxCapacitor + bonuses.Capacitor,
                        MaxCapacitor = shipDetail.MaxCapacitor + bonuses.Capacitor,
                        EMDamage = bonuses.EMDamage,
                        ExplosiveDamage = bonuses.ExplosiveDamage,
                        ThermalDamage = bonuses.ThermalDamage,
                        EMDefense = shipDetail.EMDefense + bonuses.EMDefense,
                        ExplosiveDefense = shipDetail.ExplosiveDefense + bonuses.ExplosiveDefense,
                        ThermalDefense = shipDetail.ThermalDefense + bonuses.ThermalDefense,
                        Accuracy = shipDetail.Accuracy + bonuses.Accuracy,
                        Evasion = shipDetail.Evasion + bonuses.Evasion,
                        ShieldRechargeRate = shipDetail.ShieldRechargeRate - bonuses.ShieldRechargeRate,
                        CapitalShip = shipDetail.CapitalShip,
                        Size = shipDetail.Size == ShipSize.Invalid ? ShipSize.Small : shipDetail.Size
                    }
                };
                // A revamp hull takes its stats from the hull and the pilot, and starts full.
                Space.RecalculateShipStats(ship.Status, GetAbilityScore(Player, AbilityType.Willpower), true);
                DB.Set(ship);

                // Update the UI with the new ship details.
                ShipCountRegistered = $"Ships: {registeredShipCount + 1} / {Space.MaxRegisteredShips}";
                _myShips.Add(ship);
                _shipIds.Add(ship.Id);
                ShipNames.Add(property.CustomName);
                ShipToggles.Add(false);
                ToggleRegisterButtons();

                DestroyObject(item);

                FloatingTextStringOnCreature("Ship registered!", Player, false);
            });
        };

        public Action OnClickUnregisterShip() => () =>
        {
            ShowModal($"Unregistering a ship will convert it back into a deed item. Any structures contained inside will be permanently lost. Are you sure you wish to unregister this ship?",
                () =>
                {
                    var playerId = GetObjectUUID(Player);
                    var shipId = _shipIds[SelectedShipIndex];
                    var dbPlayer = DB.Get<Player>(playerId);
                    var dbShip = DB.Get<PlayerShip>(shipId);
                    var dbProperty = DB.Get<WorldProperty>(dbShip.PropertyId);

                    if (dbShip.Status.HighPowerModules.Count > 0 ||
                        dbShip.Status.LowPowerModules.Count > 0)
                    {
                        FloatingTextStringOnCreature($"Please uninstall all modules before unregistering your ship.", Player, false);
                        return;
                    }

                    if (dbShip.Cargo.Count > 0)
                    {
                        FloatingTextStringOnCreature("Please sell the cargo in the cargo bay before unregistering your ship.", Player, false);
                        return;
                    }

                    if (dbShip.Status.Hull < dbShip.Status.MaxHull ||
                        dbShip.Status.Shield < dbShip.Status.MaxShield)
                    {
                        FloatingTextStringOnCreature("Please repair your ship fully before unregistering it.", Player, false);
                        return;
                    }

                    if (!Item.TryAcquireItem(Player, ObjectPlugin.Deserialize(dbShip.SerializedItem)))
                    {
                        FloatingTextStringOnCreature(Item.InventoryFullMessage, Player, false);
                        return;
                    }

                    if (dbPlayer.ActiveShipId == shipId)
                        dbPlayer.ActiveShipId = Guid.Empty.ToString();

                    Space.UndockAllToStarport(dbShip, "was unregistered");
                    dbProperty.IsQueuedForDeletion = true;

                    DB.Delete<PlayerShip>(shipId);
                    DB.Set(dbPlayer);
                    DB.Set(dbProperty);

                    _myShips.RemoveAll(ship => ship.Id == shipId);
                    _shipIds.RemoveAt(SelectedShipIndex);
                    ShipNames.RemoveAt(SelectedShipIndex);
                    ShipToggles.RemoveAt(SelectedShipIndex);
                    SelectedShipIndex = -1;
                    ToggleRegisterButtons();
                    ShipCountRegistered = $"Ships: {_shipIds.Count} / {Space.MaxRegisteredShips}";
                    LoadShip();

                    FloatingTextStringOnCreature("Ship unregistered!", Player, false);
                });
        };

        public Action OnClickSaveShipName() => () =>
        {
            if (string.IsNullOrWhiteSpace(ShipName))
            {
                FloatingTextStringOnCreature("A ship name is required.", Player, false);
                return;
            }

            var shipId = _shipIds[SelectedShipIndex];
            var dbShip = DB.Get<PlayerShip>(shipId);
            var dbProperty = DB.Get<WorldProperty>(dbShip.PropertyId);

            dbProperty.CustomName = ShipName;
            DB.Set(dbProperty);

            if (Property.TryGetLoadedInstance(dbShip.PropertyId, out var instance))
                SetName(instance.Area, "{PC} " + ShipName);

            ShipNames[SelectedShipIndex] = ShipName;
        };

        /// <param name="hotbarSlot">The slot (1-30) the module goes into, or 0 for a configuration slot. The owner
        /// fits a crew-station slot without rank checks: its crew is checked when they switch it on.</param>
        private bool ValidateModuleEquip(PlayerShip dbShip, uint item, int hotbarSlot = 0)
        {
            var itemTag = GetTag(item);

            // Not a valid module type.
            if (!Space.IsRegisteredShipModule(itemTag))
            {
                SendMessageToPC(Player, "Only starship modules may be installed.");
                return false;
            }

            if (GetItemPossessor(item) != Player)
            {
                SendMessageToPC(Player, "Item must be in your inventory.");
                return false;
            }

            var moduleDetails = Space.GetShipModuleDetailByItemTag(itemTag);
            var shipDetails = Space.GetShipDetailByItemTag(dbShip.Status.ItemTag);

            // A revamp hull takes only revamp modules, a legacy hull only legacy modules, and capital modules need a capital hull.
            var fitError = ShipFitRules.GetFitError(shipDetails, moduleDetails);
            if (!string.IsNullOrEmpty(fitError))
            {
                SendMessageToPC(Player, fitError);
                return false;
            }

            // No high power nodes available.
            if (moduleDetails.PowerType == ShipModulePowerType.High &&
                dbShip.Status.HighPowerModules.Count >= shipDetails.HighPowerNodes)
            {
                SendMessageToPC(Player, "No high power nodes are available.");
                return false;
            }

            // No mid power nodes available.
            if (moduleDetails.PowerType == ShipModulePowerType.Mid &&
                dbShip.Status.MidPowerModules.Count >= shipDetails.MidPowerNodes)
            {
                SendMessageToPC(Player, "No mid power nodes are available.");
                return false;
            }

            // No low power nodes available.
            if (moduleDetails.PowerType == ShipModulePowerType.Low &&
                dbShip.Status.LowPowerModules.Count >= shipDetails.LowPowerNodes)
            {
                SendMessageToPC(Player, "No low power nodes are available.");
                return false;
            }

            // A crew-station module is checked against its crew, not the owner (plan section 10).
            if (hotbarSlot > 0 && Space.IsStationSlot(dbShip.Status, hotbarSlot))
                return true;

            // Doesn't meet perk requirements.
            if (!Space.CanPlayerUseShipModule(Player, itemTag))
            {
                SendMessageToPC(Player, "You do not meet the perk requirements necessary to install this module.");
                return false;
            }

            // Doesn't meet skill requirements.
            var skillError = Item.GetSkillRequirementsError(Player, Item.GetRequiredSkills(item));
            if (!string.IsNullOrEmpty(skillError))
            {
                SendMessageToPC(Player, skillError);
                return false;
            }

            return true;
        }

        private void ProcessHighPower(int slot) => ProcessPower(ShipModulePowerType.High, slot);

        private void ProcessMidPower(int slot) => ProcessPower(ShipModulePowerType.Mid, slot);

        private void ProcessLowPower(int slot) => ProcessPower(ShipModulePowerType.Low, slot);

        private static Dictionary<int, ShipStatus.ShipStatusModule> GetPowerModules(ShipStatus status, ShipModulePowerType row)
        {
            return row switch
            {
                ShipModulePowerType.High => status.HighPowerModules,
                ShipModulePowerType.Mid => status.MidPowerModules,
                _ => status.LowPowerModules
            };
        }

        private static string GetPowerRowName(ShipModulePowerType row)
        {
            return row switch
            {
                ShipModulePowerType.High => "high",
                ShipModulePowerType.Mid => "mid",
                _ => "low"
            };
        }

        private void ProcessPower(ShipModulePowerType row, int slot)
        {
            var rowName = GetPowerRowName(row);
            var shipId = _shipIds[SelectedShipIndex];
            var dbShip = DB.Get<PlayerShip>(shipId);
            var modules = GetPowerModules(dbShip.Status, row);
            var module = modules.ContainsKey(slot)
                ? modules[slot]
                : null;

            // No module is equipped in this slot.
            // Put player into targeting mode to select a module.
            if (module == null)
            {
                Targeting.EnterTargetingMode(Player, ObjectType.Item, $"Please click on a ship {rowName}-powered module within your inventory.",
                    item =>
                {
                    dbShip = DB.Get<PlayerShip>(shipId);
                    modules = GetPowerModules(dbShip.Status, row);
                    var itemTag = GetTag(item);
                    if (!Space.IsRegisteredShipModule(itemTag))
                    {
                        SendMessageToPC(Player, $"Only {rowName}-powered ship modules may be installed to this slot.");
                        return;
                    }

                    var moduleDetails = Space.GetShipModuleDetailByItemTag(itemTag);

                    if (!ValidateModuleEquip(dbShip, item, ShipStatCalculator.GetHotbarSlot(row, slot)))
                        return;

                    if (moduleDetails.PowerType != row)
                    {
                        SendMessageToPC(Player, $"Only {rowName}-powered ship modules may be installed to this slot.");
                        return;
                    }

                    var moduleBonus = Space.GetModuleBonus(item);
                    modules[slot] = new ShipStatus.ShipStatusModule
                    {
                        ItemInstanceId = GetObjectUUID(item),
                        SerializedItem = ObjectPlugin.Serialize(item),
                        ItemTag = itemTag,
                        RecastTime = DateTime.MinValue,
                        ModuleBonus = moduleBonus,
                        RequiredSkills = Item.GetRequiredSkills(item)
                    };

                    moduleDetails.ModuleEquippedAction?.Invoke(dbShip.Status, moduleBonus);
                    Space.RecalculateShipStats(dbShip.Status, GetAbilityScore(Player, AbilityType.Willpower));

                    DB.Set(dbShip);

                    DestroyObject(item);
                    LoadShip();
                });
            }
            // A module exists. Prompt user whether they'd like to uninstall it.
            else
            {
                var moduleDetail = Space.GetShipModuleDetailByItemTag(module.ItemTag);
                ShowModal($"{moduleDetail.Name} is equipped to {rowName} slot #{slot}. Would you like to uninstall it?", () =>
                {
                    var item = ObjectPlugin.Deserialize(module.SerializedItem);
                    var moduleBonus = Space.GetModuleBonus(item);
                    if (!Item.TryAcquireItem(Player, item))
                    {
                        SendMessageToPC(Player, Item.InventoryFullMessage);
                        return;
                    }

                    moduleDetail.ModuleUnequippedAction?.Invoke(dbShip.Status, moduleBonus);
                    modules.Remove(slot);
                    Space.RecalculateShipStats(dbShip.Status, GetAbilityScore(Player, AbilityType.Willpower));
                    DB.Set(dbShip);
                    LoadShip();
                });
            }
        }

        private void ProcessConfiguration(int slot)
        {
            var shipId = _shipIds[SelectedShipIndex];
            var dbShip = DB.Get<PlayerShip>(shipId);
            var module = dbShip.Status.ConfigurationModules.ContainsKey(slot)
                ? dbShip.Status.ConfigurationModules[slot]
                : null;

            // No module is equipped in this slot.
            // Put player into targeting mode to select a module.
            if (module == null)
            {
                Targeting.EnterTargetingMode(Player, ObjectType.Item, "Please click on a ship configuration module within your inventory",
                    item =>
                    {
                        dbShip = DB.Get<PlayerShip>(shipId);
                        var itemTag = GetTag(item);
                        if (!Space.IsRegisteredShipModule(itemTag))
                        {
                            SendMessageToPC(Player, "Only ship configuration modules may be installed to this slot.");
                            return;
                        }

                        var moduleDetails = Space.GetShipModuleDetailByItemTag(itemTag);
                        var moduleBonus = Space.GetModuleBonus(item);

                        if (!ValidateModuleEquip(dbShip, item))
                            return;

                        if (moduleDetails.PowerType != ShipModulePowerType.Config)
                        {
                            SendMessageToPC(Player, "Only ship configuration modules may be installed to this slot.");
                            return;
                        }

                        dbShip.Status.ConfigurationModules[slot] = new ShipStatus.ShipStatusModule
                        {
                            ItemInstanceId = GetObjectUUID(item),
                            SerializedItem = ObjectPlugin.Serialize(item),
                            ItemTag = itemTag,
                            RecastTime = DateTime.MinValue,
                            ModuleBonus = moduleBonus,
                            RequiredSkills = Item.GetRequiredSkills(item)
                        };

                        moduleDetails.ModuleEquippedAction?.Invoke(dbShip.Status, moduleBonus);

                        DB.Set(dbShip);

                        DestroyObject(item);
                        LoadShip();
                    });
            }
            // A module exists. Prompt user whether they'd like to uninstall it.
            else
            {
                var moduleDetail = Space.GetShipModuleDetailByItemTag(module.ItemTag);
                ShowModal($"{moduleDetail.Name} is equipped to ship configuration slot #{slot}. Would you like to uninstall it?", () =>
                {
                    var item = ObjectPlugin.Deserialize(module.SerializedItem);
                    var moduleBonus = Space.GetModuleBonus(item);
                    if (!Item.TryAcquireItem(Player, item))
                    {
                        SendMessageToPC(Player, Item.InventoryFullMessage);
                        return;
                    }

                    moduleDetail.ModuleUnequippedAction?.Invoke(dbShip.Status, moduleBonus);
                    dbShip.Status.ConfigurationModules.Remove(slot);
                    DB.Set(dbShip);
                    LoadShip();
                });
            }

        }

        public Action OnClickHighPower1() => () =>
        {
            ProcessHighPower(1);
        };
        public Action OnClickHighPower2() => () =>
        {
            ProcessHighPower(2);
        };
        public Action OnClickHighPower3() => () =>
        {
            ProcessHighPower(3);
        };
        public Action OnClickHighPower4() => () =>
        {
            ProcessHighPower(4);
        };
        public Action OnClickHighPower5() => () =>
        {
            ProcessHighPower(5);
        };
        public Action OnClickHighPower6() => () =>
        {
            ProcessHighPower(6);
        };
        public Action OnClickHighPower7() => () =>
        {
            ProcessHighPower(7);
        };
        public Action OnClickHighPower8() => () =>
        {
            ProcessHighPower(8);
        };


        public Action OnClickMidPower1() => () =>
        {
            ProcessMidPower(1);
        };

        public Action OnClickMidPower2() => () =>
        {
            ProcessMidPower(2);
        };

        public Action OnClickMidPower3() => () =>
        {
            ProcessMidPower(3);
        };

        public Action OnClickMidPower4() => () =>
        {
            ProcessMidPower(4);
        };

        public Action OnClickMidPower5() => () =>
        {
            ProcessMidPower(5);
        };

        public Action OnClickMidPower6() => () =>
        {
            ProcessMidPower(6);
        };

        public Action OnClickMidPower7() => () =>
        {
            ProcessMidPower(7);
        };

        public Action OnClickMidPower8() => () =>
        {
            ProcessMidPower(8);
        };

        public Action OnClickLowPower1() => () =>
        {
            ProcessLowPower(1);
        };
        public Action OnClickLowPower2() => () =>
        {
            ProcessLowPower(2);
        };
        public Action OnClickLowPower3() => () =>
        {
            ProcessLowPower(3);
        };
        public Action OnClickLowPower4() => () =>
        {
            ProcessLowPower(4);
        };
        public Action OnClickLowPower5() => () =>
        {
            ProcessLowPower(5);
        };
        public Action OnClickLowPower6() => () =>
        {
            ProcessLowPower(6);
        };
        public Action OnClickLowPower7() => () =>
        {
            ProcessLowPower(7);
        };
        public Action OnClickLowPower8() => () =>
        {
            ProcessLowPower(8);
        };

        public Action OnClickConfiguration1() => () =>
        {
            ProcessConfiguration(1);
        };

        public Action OnClickConfiguration2() => () =>
        {
            ProcessConfiguration(2);
        };

        public Action OnClickConfiguration3() => () =>
        {
            ProcessConfiguration(3);
        };

        public Action OnClickConfiguration4() => () =>
        {
            ProcessConfiguration(4);
        };

        public Action OnClickConfiguration5() => () =>
        {
            ProcessConfiguration(5);
        };

        public Action OnClickConfiguration6() => () =>
        {
            ProcessConfiguration(6);
        };

        public Action OnClickConfiguration7() => () =>
        {
            ProcessConfiguration(7);
        };

        public Action OnClickConfiguration8() => () =>
        {
            ProcessConfiguration(8);
        };

        public Action OnClickBoardShip() => () =>
        {
            if (GetLocalBool(TetherObject, BoardingOutOfCombatVariable) &&
                (GetIsInCombat(Player) || Enmity.HasEnmity(Player)))
            {
                SendMessageToPC(Player, "You are in combat.");
                return;
            }

            var shipId = _shipIds[SelectedShipIndex];
            var dbShip = DB.Get<PlayerShip>(shipId);

            // Access can be revoked after the ship was selected.
            if (!CanBoardShip(dbShip.PropertyId))
            {
                FloatingTextStringOnCreature("You do not have permission to access that property.", Player, false);
                LoadShip();
                return;
            }

            var shipDetail = Space.GetShipDetailByItemTag(dbShip.Status.ItemTag);
            if (!Property.TryResolveEnterableInstance(Player, dbShip.PropertyId, out var instance))
                return;

            var entrance = Property.GetEntrancePosition(shipDetail.Layout);
            var location = Location(instance.Area, Vector3(entrance.X, entrance.Y, entrance.Z), entrance.W);

            AssignCommand(Player, () =>
            {
                ActionJumpToLocation(location);
            });

            Gui.TogglePlayerWindow(Player, GuiWindowType.ShipManagement);
        };

        public Action OnClickRemoveAccess() => () =>
        {
            if (SelectedShipIndex <= -1)
                return;

            var playerId = GetObjectUUID(Player);
            var shipId = _shipIds[SelectedShipIndex];
            var dbShip = DB.Get<PlayerShip>(shipId);
            if (dbShip == null || dbShip.OwnerPlayerId == playerId)
                return;

            var dbProperty = DB.Get<WorldProperty>(dbShip.PropertyId);
            ShowModal($"Remove your access to {dbProperty.CustomName}? It will no longer appear under Other Ships, and you would need to be granted permissions again to use it.",
                () =>
                {
                    PlayerShipAccess.RemoveSharedAccess(playerId, DB.Get<PlayerShip>(shipId));
                    _sharedPropertyIds.Remove(dbShip.PropertyId);
                    LoadPage();

                    FloatingTextStringOnCreature("Ship access removed.", Player, false);
                });
        };

        public Action OnClickPermissions() => () =>
        {
            var shipId = _shipIds[SelectedShipIndex];
            var dbShip = DB.Get<PlayerShip>(shipId);

            var payload = new PropertyPermissionPayload(PropertyType.Starship, dbShip.PropertyId, string.Empty, false);
            Gui.TogglePlayerWindow(Player, GuiWindowType.PermissionManagement, payload, TetherObject);
        };

        private void LoadShips(IEnumerable<(string ShipId, string Name)> ships)
        {
            _shipIds.Clear();
            var shipNames = new GuiBindingList<string>();
            var shipToggles = new GuiBindingList<bool>();

            foreach (var (shipId, name) in ships)
            {
                _shipIds.Add(shipId);
                shipToggles.Add(false);
                shipNames.Add(name);
            }

            SelectedShipIndex = -1;
            ShipNames = shipNames;
            ShipToggles = shipToggles;
        }

        public Action OnClickMyShips() => () =>
        {
            IsMyShipsToggled = true;
            IsOtherShipsToggled = false;
            _myShips = GetMyShips();
            SetPageIndexWithoutLoad(0);
            LoadPage();
        };

        public Action OnClickOtherShips() => () =>
        {
            IsMyShipsToggled = false;
            IsOtherShipsToggled = true;
            _sharedPropertyIds = PlayerShipAccess.GetSharedPropertyIds(GetObjectUUID(Player));
            SetPageIndexWithoutLoad(0);
            LoadPage();
        };

        public Action OnPreviousPage() => () =>
        {
            ChangePage(SelectedPageIndex - 1);
        };

        public Action OnNextPage() => () =>
        {
            ChangePage(SelectedPageIndex + 1);
        };

        private void ChangePage(int pageIndex)
        {
            pageIndex = Math.Clamp(pageIndex, 0, Math.Max(0, PageNumbers.Count - 1));
            if (pageIndex == SelectedPageIndex)
                return;

            SetPageIndexWithoutLoad(pageIndex);
            LoadPage();
        }

        public Action OnClickRepair() => () =>
        {
            var shipId = _shipIds[SelectedShipIndex];
            var dbShip = DB.Get<PlayerShip>(shipId);
            var price = CalculateRepairBill(dbShip);

            ShowModal($"Repairs will cost you {price} credits. Will you pay for repairs?", () =>
            {
                var gold = GetGold(Player);

                if (gold < price)
                {
                    FloatingTextStringOnCreature(ColorToken.Red("Not enough credits!"), Player, false);
                    return;
                }

                AssignCommand(Player, () =>
                {
                    TakeGoldFromCreature(price, Player, true);
                });

                dbShip.Status.Shield = dbShip.Status.MaxShield;
                dbShip.Status.Hull = dbShip.Status.MaxHull;
                DB.Set(dbShip);

                FloatingTextStringOnCreature(ColorToken.Green("Ship repaired!"), Player, false);
                LoadShip();
            });
        };
    }
}
