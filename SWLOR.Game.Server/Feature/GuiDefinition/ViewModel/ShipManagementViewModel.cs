using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
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
        public const string ContentElement = "ship-management-content";
        public const string MainContentPartial = "ship-management-main";
        public string FittingSummary { get => Get<string>(); set => Set(value); }
        public string RecoveryText { get => Get<string>(); set => Set(value); }

        protected override void OnModalClosedRestore() => ChangePartialView(ContentElement, MainContentPartial);

        private const string _blank = "Blank";

        private int SelectedShipIndex { get; set; }
        private List<string> _shipIds { get; set; } = new List<string>();
        private Location _spaceLocation;
        private Location _landingLocation;
        private PlanetType _planetType;

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
            var playerId = GetObjectUUID(Player);
            var query = new DBQuery<PlayerShip>()
                .AddFieldSearch(nameof(PlayerShip.OwnerPlayerId), playerId, false);
            return DB.Search(query).ToList();
        }

        private List<PlayerShip> GetOtherShips()
        {
            var playerId = GetObjectUUID(Player);
            var permissionQuery = new DBQuery<WorldPropertyPermission>()
                .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), playerId, false);
            var permissionCount = (int)DB.SearchCount(permissionQuery);
            var propertyIds = DB.Search(permissionQuery.AddPaging(permissionCount, 0))
                .Select(s => s.PropertyId)
                .ToList();

            if (propertyIds.Count <= 0)
                return new List<PlayerShip>();

            var shipQuery = new DBQuery<PlayerShip>()
                .AddFieldSearch(nameof(PlayerShip.PropertyId), propertyIds);

            var ships = DB.Search(shipQuery)
                .Where(x => x.OwnerPlayerId != playerId)
                .ToList();

            return ships;
        }

        private int CalculateRepairBill(PlayerShip ship)
        {
            return ShipRecovery.DockPrice(ship.Status);
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

            LoadShips(dbPlayerShips);

            ShipCountRegistered = $"Ships: {dbPlayerShips.Count} / {Space.MaxRegisteredShips}";
            LoadShip();

            IsMyShipsToggled = true;
            IsOtherShipsToggled = false;
            ToggleRegisterButtons();

            ChangePartialView(ContentElement, MainContentPartial);
            WatchOnClient(model => model.ShipName);
        }

        private void LoadShip()
        {
            var playerId = GetObjectUUID(Player);

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
                FittingSummary = string.Empty;
                RecoveryText = "Recover equipment";
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
                ShipEquipmentTransfers.EnsureFitting(ship);
                var shipDetail = Space.GetShipDetailByItemTag(ship.Status.ItemTag);
                var profile = shipDetail.FittingProfile;
                FittingSummary = $"Power {ship.Status.FittingPowerUsed}/{profile.Power} · Cargo {ShipCargo.Occupied(ship.Status):0.#}/{ship.Status.CargoCapacity:0} · {profile.Role}";
                RecoveryText = $"Recover equipment ({ship.Status.RefitRecovery.Count})";
                var property = DB.Get<WorldProperty>(ship.PropertyId);

                var permissionQuery = new DBQuery<WorldPropertyPermission>()
                    .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), playerId, false)
                    .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), ship.PropertyId, false);
                var permission = DB.Search(permissionQuery).Single();
                var currentLocation = GetShipLocation(property, out var isDockInstanceLoading);
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

                ShieldRechargeRate = $"{ship.Status.OutOfCombatShieldRecovery:0.##}/s after 10s out of combat";

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

                IsBoardShipEnabled = isAtCurrentLocation;
                IsNameEnabled = permission.Permissions[PropertyPermissionType.RenameProperty] && isAtCurrentLocation;
                IsRefitEnabled = permission.Permissions[PropertyPermissionType.RefitShip] && isAtCurrentLocation;
                IsPermissionsEnabled = permission.GrantPermissions.Any(x => x.Value) && isAtCurrentLocation;
                ShipLocation = isInSpace
                    ? "In Space"
                    : isDockInstanceLoading
                        ? "Docked (loading...)"
                        : GetName(currentLocation);
                IsRepairEnabled = (repairPrice > 0 || ship.Status.Shield < ship.Status.MaxShield || ship.Status.Capacitor < ship.Status.MaxCapacitor) &&
                                  gold >= repairPrice && isAtCurrentLocation && !isInSpace && permission.Permissions.GetValueOrDefault(PropertyPermissionType.RefitShip);
                RepairText = $"Service ({repairPrice} cr)";
            }

            ToggleRegisterButtons();
        }

        private uint GetShipLocation(WorldProperty property, out bool isDockInstanceLoading)
        {
            isDockInstanceLoading = false;

            if (property.Positions.ContainsKey(PropertyLocationType.CurrentPosition))
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
                var shipLocation = GetShipLocation(dbProperty, out _);
                IsUnregisterEnabled = shipLocation == GetArea(Player) && playerId == dbProperty.OwnerPlayerId;
            }
            else
            {
                IsUnregisterEnabled = false;
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
                var dbPlayerShips = DB.Search(query).ToList();

                if (dbPlayerShips.Count >= Space.MaxRegisteredShips)
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
                        CapitalShip = shipDetail.CapitalShip
                    }
                };
                ship.Status = ShipFittingConversion.Convert(ship.Status, sourceIdentity: ship.Id);
                DB.Set(ship);

                // Update the UI with the new ship details.
                ShipCountRegistered = $"Ships: {dbPlayerShips.Count + 1} / {Space.MaxRegisteredShips}";
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
                        dbShip.Status.LowPowerModules.Count > 0 || dbShip.Status.ConfigurationModules.Count > 0 ||
                        dbShip.Status.RefitRecovery.Count > 0 || dbShip.Status.PendingInventoryTransfers.Count > 0 || dbShip.Status.PendingCargoTransfers.Count > 0 || dbShip.Status.Cargo.Count > 0 || dbShip.Status.PendingDockPayment != null)
                    {
                        FloatingTextStringOnCreature($"Please uninstall all modules before unregistering your ship.", Player, false);
                        return;
                    }

                    if (dbShip.Status.Hull < dbShip.Status.MaxHull ||
                        dbShip.Status.Shield < dbShip.Status.MaxShield)
                    {
                        FloatingTextStringOnCreature("Please repair your ship fully before unregistering it.", Player, false);
                        return;
                    }

                    if (dbPlayer.ActiveShipId == shipId)
                        dbPlayer.ActiveShipId = Guid.Empty.ToString();

                    dbProperty.IsQueuedForDeletion = true;

                    DB.Delete<PlayerShip>(shipId);
                    DB.Set(dbPlayer);
                    DB.Set(dbProperty);

                    var item = ObjectPlugin.Deserialize(dbShip.SerializedItem);
                    ObjectPlugin.AcquireItem(Player, item);

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

        private void ProcessHighPower(int slot) => ProcessFitting(ShipFittingBank.High, slot);
        private void ProcessLowPower(int slot) => ProcessFitting(ShipFittingBank.Low, slot);
        private void ProcessConfiguration(int slot) => ProcessFitting(ShipFittingBank.Configuration, slot);

        private void ProcessFitting(ShipFittingBank bank, int slot)
        {
            if (SelectedShipIndex < 0 || SelectedShipIndex >= _shipIds.Count) return;
            var shipId = _shipIds[SelectedShipIndex];
            try
            {
                var ship = ShipEquipmentTransfers.RequireDock(Player, shipId);
                if (!ShipRefitting.Bank(ship.Status, bank).TryGetValue(slot, out var module))
                {
                    Targeting.EnterTargetingMode(Player, ObjectType.Item, "Select ship equipment in your inventory.", item =>
                    {
                        try { ShipEquipmentTransfers.Install(Player, shipId, bank, slot, item); }
                        catch (InvalidOperationException error) { SendMessageToPC(Player, error.Message); }
                        catch (ArgumentException error) { SendMessageToPC(Player, error.Message); }
                        LoadShip();
                    });
                }
                else
                {
                    var detail = Space.GetShipModuleDetailByItemTag(module.ItemTag);
                    ShowModal($"Uninstall {detail.Name} ({module.Calibration}; condition {module.Condition}%) from {bank} slot {slot}?", () =>
                    {
                        try { ShipEquipmentTransfers.Remove(Player, shipId, bank, slot); }
                        catch (InvalidOperationException error) { SendMessageToPC(Player, error.Message); }
                        LoadShip();
                    });
                }
            }
            catch (InvalidOperationException error) { SendMessageToPC(Player, error.Message); }
        }

        public Action OnClickCargo() => () =>
        {
            if (SelectedShipIndex < 0 || SelectedShipIndex >= _shipIds.Count) return;
            Gui.TogglePlayerWindow(Player, GuiWindowType.ShipCargo, new ShipCargoPayload(_shipIds[SelectedShipIndex]));
        };

        public Action OnClickRecoverEquipment() => () =>
        {
            if (SelectedShipIndex < 0 || SelectedShipIndex >= _shipIds.Count) return;
            var shipId = _shipIds[SelectedShipIndex];
            try
            {
                var ship = ShipEquipmentTransfers.RequireDock(Player, shipId);
                foreach (var identity in ship.Status.RefitRecovery.Keys.ToArray())
                    ShipEquipmentTransfers.Withdraw(Player, shipId, identity);
            }
            catch (InvalidOperationException error) { SendMessageToPC(Player, error.Message); }
            LoadShip();
        };

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
            var shipId = _shipIds[SelectedShipIndex];
            var dbShip = DB.Get<PlayerShip>(shipId);
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

        public Action OnClickPermissions() => () =>
        {
            var shipId = _shipIds[SelectedShipIndex];
            var dbShip = DB.Get<PlayerShip>(shipId);

            var payload = new PropertyPermissionPayload(PropertyType.Starship, dbShip.PropertyId, string.Empty, false);
            Gui.TogglePlayerWindow(Player, GuiWindowType.PermissionManagement, payload, TetherObject);
        };

        private void LoadShips(List<PlayerShip> ships)
        {
            _shipIds.Clear();
            var shipNames = new GuiBindingList<string>();
            var shipToggles = new GuiBindingList<bool>();

            foreach (var ship in ships)
            {
                var property = DB.Get<WorldProperty>(ship.PropertyId);

                _shipIds.Add(ship.Id);
                shipToggles.Add(false);

                shipNames.Add(property.CustomName);
            }

            SelectedShipIndex = -1;
            ShipNames = shipNames;
            ShipToggles = shipToggles;
        }

        public Action OnClickMyShips() => () =>
        {
            IsMyShipsToggled = true;
            IsOtherShipsToggled = false;
            var ships = GetMyShips();
            LoadShips(ships);
            ToggleRegisterButtons();
            LoadShip();
        };

        public Action OnClickOtherShips() => () =>
        {
            IsMyShipsToggled = false;
            IsOtherShipsToggled = true;
            var ships = GetOtherShips();
            LoadShips(ships);
            ToggleRegisterButtons();
            LoadShip();
        };

        public Action OnClickRepair() => () =>
        {
            var shipId = _shipIds[SelectedShipIndex];
            var dbShip = DB.Get<PlayerShip>(shipId);
            var price = CalculateRepairBill(dbShip);

            ShowModal($"Repairs will cost you {price} credits. Will you pay for repairs?", () =>
            {
                try { ShipDockService.Repair(Player, shipId, price); FloatingTextStringOnCreature(ColorToken.Green("Ship serviced!"), Player, false); }
                catch (InvalidOperationException ex) { SendMessageToPC(Player, ex.Message); }
                LoadShip();
            });
        };
    }
}
