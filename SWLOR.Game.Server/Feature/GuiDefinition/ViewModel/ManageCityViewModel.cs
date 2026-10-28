using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.StationService;
using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class ManageCityViewModel : GuiViewModelBase<ManageCityViewModel, GuiPayloadBase>, IGuiAcceptsPriceChange
    {
        private const int MaxUpgradeLevel = 5;
        private string _cityId;

        public string Instructions
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiColor InstructionsColor
        {
            get => Get<GuiColor>();
            set => Set(value);
        }

        public string CityName
        {
            get => Get<string>();
            set => Set(value);
        }

        public string CityLevel
        {
            get => Get<string>();
            set => Set(value);
        }

        public string MedicalCenterLevel
        {
            get => Get<string>();
            set => Set(value);
        }

        public string StarportLevel
        {
            get => Get<string>();
            set => Set(value);
        }

        public string CantinaLevel
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Treasury
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiBindingList<string> CitizenNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> CitizenCreditsOwed
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public string CitizenshipTax
        {
            get => Get<string>();
            set => Set(value);
        }

        public string TransportationTax
        {
            get => Get<string>();
            set => Set(value);
        }

        public string ExchangeTariff
        {
            get => Get<string>();
            set => Set(value);
        }

        /// <summary>The Shipyard fee per Capital hull (player stations only).</summary>
        public string ShipyardFee
        {
            get => Get<string>();
            set => Set(value);
        }

        /// <summary>Whether the city is a player station, which shows the Jump Beacon upgrade and the Shipyard fee.</summary>
        public bool IsStation
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string JumpBeaconLevel
        {
            get => Get<string>();
            set => Set(value);
        }

        public string JumpBeaconCurrentUpgrade
        {
            get => Get<string>();
            set => Set(value);
        }

        public string JumpBeaconNextUpgrade
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool CanUpgradeJumpBeacons
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool CanEditTaxes
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool CanAccessTreasury
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool CanUpgradeMedicalCenters
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool CanUpgradeStarports
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool CanUpgradeCantinas
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool CanManageUpkeep
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string MedicalCenterCurrentUpgrade
        {
            get => Get<string>();
            set => Set(value);
        }

        public string StarportCurrentUpgrade
        {
            get => Get<string>();
            set => Set(value);
        }

        public string CantinaCurrentUpgrade
        {
            get => Get<string>();
            set => Set(value);
        }

        public string MedicalCenterNextUpgrade
        {
            get => Get<string>();
            set => Set(value);
        }

        public string StarportNextUpgrade
        {
            get => Get<string>();
            set => Set(value);
        }

        public string CantinaNextUpgrade
        {
            get => Get<string>();
            set => Set(value);
        }

        public string UpkeepText
        {
            get => Get<string>();
            set => Set(value);
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            var area = GetArea(TetherObject);
            var propertyId = Property.GetPropertyId(area);
            var dbProperty = DB.Get<WorldProperty>(propertyId);
            var dbBuilding = DB.Get<WorldProperty>(dbProperty.ParentPropertyId);
            _cityId = dbBuilding.ParentPropertyId;

            RefreshPermissions();
            RefreshCitizenList();
            RefreshUpgradeLevels();
            RefreshPropertyDetails();
            RefreshTaxesAndFees();
            RefreshUpkeep();

            WatchOnClient(model => model.CityName);
            WatchOnClient(model => model.TransportationTax);
            WatchOnClient(model => model.CitizenshipTax);
            WatchOnClient(model => model.ExchangeTariff);
            WatchOnClient(model => model.ShipyardFee);
        }

        private void RefreshPermissions()
        {
            var playerId = GetObjectUUID(Player);
            var dbCity = DB.Get<WorldProperty>(_cityId);
            var permission = DB.Search(new DBQuery<WorldPropertyPermission>()
                .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), playerId, false)
                .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), _cityId, false))
                .Single();

            // Permissions
            CanAccessTreasury = permission.Permissions[PropertyPermissionType.AccessTreasury];
            CanEditTaxes = permission.Permissions[PropertyPermissionType.EditTaxes];
            CanManageUpkeep = permission.Permissions[PropertyPermissionType.ManageUpkeep];

            // Upgrades
            CanUpgradeMedicalCenters = permission.Permissions[PropertyPermissionType.ManageUpgrades] &&
                              dbCity.Upgrades[PropertyUpgradeType.MedicalCenterLevel] < MaxUpgradeLevel &&
                              dbCity.Upgrades[PropertyUpgradeType.CityLevel] >= dbCity.Upgrades[PropertyUpgradeType.MedicalCenterLevel] + 1;
            CanUpgradeStarports = permission.Permissions[PropertyPermissionType.ManageUpgrades] &&
                              dbCity.Upgrades[PropertyUpgradeType.StarportLevel] < MaxUpgradeLevel &&
                              dbCity.Upgrades[PropertyUpgradeType.CityLevel] >= dbCity.Upgrades[PropertyUpgradeType.StarportLevel] + 1;
            CanUpgradeCantinas = permission.Permissions[PropertyPermissionType.ManageUpgrades] &&
                              dbCity.Upgrades[PropertyUpgradeType.CantinaLevel] < MaxUpgradeLevel &&
                              dbCity.Upgrades[PropertyUpgradeType.CityLevel] >= dbCity.Upgrades[PropertyUpgradeType.CantinaLevel] + 1;

            // A player station's Jump Beacon level (1-3) caps the hulls it jumps.
            var beaconLevel = GetJumpBeaconLevel(dbCity);
            IsStation = Station.IsStation(dbCity);
            CanUpgradeJumpBeacons = IsStation &&
                              permission.Permissions[PropertyPermissionType.ManageUpgrades] &&
                              beaconLevel < StationTuning.BeaconMaxLevel &&
                              dbCity.Upgrades[PropertyUpgradeType.CityLevel] >= beaconLevel + 1;
        }

        /// <summary>A station's Jump Beacon upgrade level; stations founded before beacons existed start at 1.</summary>
        private static int GetJumpBeaconLevel(WorldProperty dbCity) =>
            dbCity.Upgrades.GetValueOrDefault(PropertyUpgradeType.JumpBeaconLevel, 1);

        private static int GetMaxUpgradeLevel(PropertyUpgradeType upgradeType) =>
            upgradeType == PropertyUpgradeType.JumpBeaconLevel ? StationTuning.BeaconMaxLevel : MaxUpgradeLevel;

        private void RefreshUpgradeLevels()
        {
            var dbCity = DB.Get<WorldProperty>(_cityId);

            MedicalCenterLevel = $"Medical Center: Lvl {dbCity.Upgrades[PropertyUpgradeType.MedicalCenterLevel]}";
            StarportLevel = $"Starport: Lvl {dbCity.Upgrades[PropertyUpgradeType.StarportLevel]}";
            CantinaLevel = $"Cantina: Lvl {dbCity.Upgrades[PropertyUpgradeType.CantinaLevel]}";

            MedicalCenterCurrentUpgrade = GetMedicalCenterUpgrade(dbCity.Upgrades[PropertyUpgradeType.MedicalCenterLevel]);
            MedicalCenterNextUpgrade = GetMedicalCenterUpgrade(dbCity.Upgrades[PropertyUpgradeType.MedicalCenterLevel] + 1);
            StarportCurrentUpgrade = GetStarportUpgrade(dbCity.Upgrades[PropertyUpgradeType.StarportLevel]);
            StarportNextUpgrade = GetStarportUpgrade(dbCity.Upgrades[PropertyUpgradeType.StarportLevel] + 1);
            CantinaCurrentUpgrade = GetCantinaUpgrade(dbCity.Upgrades[PropertyUpgradeType.CantinaLevel]);
            CantinaNextUpgrade = GetCantinaUpgrade(dbCity.Upgrades[PropertyUpgradeType.CantinaLevel] + 1);

            var beaconLevel = GetJumpBeaconLevel(dbCity);
            JumpBeaconLevel = $"Jump Beacon: Lvl {beaconLevel}";
            JumpBeaconCurrentUpgrade = GetJumpBeaconUpgrade(beaconLevel);
            JumpBeaconNextUpgrade = GetJumpBeaconUpgrade(beaconLevel + 1);
        }

        private void RefreshCitizenList()
        {
            var dbCitizens = DB.Search(new DBQuery<Player>()
                .AddFieldSearch(nameof(Entity.Player.CitizenPropertyId), _cityId, false)
                .AddFieldSearch(nameof(Entity.Player.IsDeleted), false));

            var citizenNames = new GuiBindingList<string>();
            var citizenCreditsOwed = new GuiBindingList<string>();

            foreach (var citizen in dbCitizens)
            {
                citizenNames.Add(PlayerName.GetPlainDisplayNameByPlayerId(Player, citizen.Id, citizen.Name));
                citizenCreditsOwed.Add($"Owes {citizen.PropertyOwedTaxes} cr");
            }

            CitizenNames = citizenNames;
            CitizenCreditsOwed = citizenCreditsOwed;
        }

        private void RefreshPropertyDetails()
        {
            var dbCity = DB.Get<WorldProperty>(_cityId);
            var level = dbCity.Upgrades[PropertyUpgradeType.CityLevel];
            Instructions = string.Empty;
            InstructionsColor = GuiColor.Green;
            CityName = dbCity.CustomName;
            Treasury = $"Treasury: {dbCity.Treasury} cr";
            CityLevel = $"Level: {Property.GetCityLevelName(level)} (Lvl. {level})";
        }

        private void RefreshUpkeep()
        {
            var dbCity = DB.Get<WorldProperty>(_cityId);
            UpkeepText = $"Pay Upkeep ({dbCity.Upkeep} cr)";
        }

        private void RefreshTaxesAndFees()
        {
            var dbCity = DB.Get<WorldProperty>(_cityId);

            CitizenshipTax = $"{dbCity.Taxes[PropertyTaxType.Citizenship]}";
            TransportationTax = $"{dbCity.Taxes[PropertyTaxType.Transportation]}";
            ExchangeTariff = $"{GetStoredExchangeTariff(dbCity)}";
            ShipyardFee = $"{StationBuildings.GetShipyardFee(dbCity)}";
        }

        /// <summary>The city's Trade Exchange tariff percent. Cities that never set one have none.</summary>
        private static int GetStoredExchangeTariff(WorldProperty dbCity)
        {
            return dbCity.Taxes.TryGetValue(PropertyTaxType.Exchange, out var tariff) ? TradeTariffRules.ClampPercent(tariff) : 0;
        }

        private string GetMedicalCenterUpgrade(int level)
        {
            switch (level)
            {
                case 1:
                    return "-5% XP debt on death";
                case 2:
                    return "-10% XP debt on death";
                case 3:
                    return "-15% XP debt on death";
                case 4:
                    return "-20% XP debt on death";
                case 5:
                    return "-25% XP debt on death";
                default:
                    return "UPGRADES MAXED";
            }
        }
        private string GetStarportUpgrade(int level)
        {
            switch (level)
            {
                case 1:
                    return "-5% repair price";
                case 2:
                    return "-10% repair price";
                case 3:
                    return "-15% repair price";
                case 4:
                    return "-20% repair price";
                case 5:
                    return "-25% repair price";
                default:
                    return "UPGRADES MAXED";
            }
        }
        private static string GetJumpBeaconUpgrade(int level)
        {
            if (level < 1 || level > StationTuning.BeaconMaxLevel)
                return "UPGRADES MAXED";

            return $"Jumps hulls up to {StationBuildingRules.GetMaxHull(level)}";
        }

        private string GetCantinaUpgrade(int level)
        {
            switch (level)
            {
                case 1:
                    return "+5% RPXP, -10% Training Terminal Costs";
                case 2:
                    return "+10% RPXP, -20% Training Terminal Costs";
                case 3:
                    return "+15% RPXP, -30% Training Terminal Costs";
                case 4:
                    return "+20% RPXP, -40% Training Terminal Costs";
                case 5:
                    return "+25% RPXP, -50% Training Terminal Costs";
                default:
                    return "UPGRADES MAXED";
            }
        }

        public Action Deposit() => () =>
        {
            var payload = new PriceSelectionPayload(GuiWindowType.ManageCity, "DEPOSIT", 0, string.Empty, "Deposit:");
            Gui.TogglePlayerWindow(Player, GuiWindowType.PriceSelection, payload, TetherObject);
        };

        public Action Withdraw() => () =>
        {
            var payload = new PriceSelectionPayload(GuiWindowType.ManageCity, "WITHDRAW", 0, string.Empty, "Withdraw:");
            Gui.TogglePlayerWindow(Player, GuiWindowType.PriceSelection, payload, TetherObject);
        };

        private bool ValidateUpgrade(PropertyUpgradeType upgradeType, int price)
        {
            var dbCity = DB.Get<WorldProperty>(_cityId);
            var mayor = DB.Get<Player>(dbCity.OwnerPlayerId);
            var currentLevel = dbCity.Upgrades.GetValueOrDefault(upgradeType, 1);
            var mayorPerkLevel = mayor.Perks.ContainsKey(PerkType.CityManagement)
                ? mayor.Perks[PerkType.CityManagement]
                : 0;

            if (currentLevel >= GetMaxUpgradeLevel(upgradeType))
            {
                Instructions = "Cannot upgrade further.";
                InstructionsColor = GuiColor.Red;
                return false;
            }

            if (dbCity.Upgrades[PropertyUpgradeType.CityLevel] <= currentLevel)
            {
                Instructions = "Increase city level to upgrade.";
                InstructionsColor = GuiColor.Red;
                return false;
            }

            if (mayorPerkLevel + 1 <= currentLevel)
            {
                Instructions = "Mayor city management perk too low.";
                InstructionsColor = GuiColor.Red;
                return false;
            }

            if (dbCity.Treasury < price)
            {
                Instructions = $"Treasury needs at least {price} cr to upgrade.";
                InstructionsColor = GuiColor.Red;
                return false;
            }

            return true;
        }

        private void HandleUpgrade(PropertyUpgradeType upgradeType, PropertyType propertyType)
        {
            var dbCity = DB.Get<WorldProperty>(_cityId);
            var currentLevel = dbCity.Upgrades.GetValueOrDefault(upgradeType, 1);
            var initialPrice = 50000 * (currentLevel + 1);

            if (!ValidateUpgrade(upgradeType, initialPrice))
                return;

            ShowModal($"Upgrading this structure type will cost an upfront amount of {initialPrice} credits and increase your weekly maintenance bill by an additional 10000 credits. Upgrades cannot be undone. Are you sure you want to upgrade this structure type?",
                () =>
                {
                    // Refresh entity in case another player is editing this city.
                    dbCity = DB.Get<WorldProperty>(_cityId);
                    currentLevel = dbCity.Upgrades.GetValueOrDefault(upgradeType, 1);
                    initialPrice = 50000 * (currentLevel + 1);

                    if (!ValidateUpgrade(upgradeType, initialPrice))
                        return;

                    dbCity.Treasury -= initialPrice;
                    dbCity.Upgrades[upgradeType] = currentLevel + 1;

                    DB.Set(dbCity);

                    RefreshPropertyDetails();
                    RefreshPermissions();
                    RefreshUpgradeLevels();

                    Instructions = "Upgrade purchased successfully!";
                    InstructionsColor = GuiColor.Green;

                    Log.Write(LogGroup.Property, $"City upgrade '{upgradeType}' purchased by {GetName(Player)} for property '{dbCity.CustomName}' ({dbCity.Id}).");

                    var structureTypes = Property.GetStructuresByInteriorPropertyType(propertyType);
                    var structureTypeIds = structureTypes.Select(s => (int)s).ToList();

                    if (structureTypeIds.Count > 0)
                    {
                        // Retrieve all interior property Ids in this city of the given type of property.
                        // I.E: All banks, all medical centers, etc.
                        var instancePropertyIds = DB.Search(new DBQuery<WorldProperty>()
                            .AddFieldSearch(nameof(WorldProperty.ParentPropertyId), _cityId, false)
                            .AddFieldSearch(nameof(WorldProperty.StructureType), structureTypeIds))
                            .SelectMany(s => s.ChildPropertyIds[PropertyChildType.Interior]);

                        foreach (var propertyId in instancePropertyIds)
                        {
                            if (!Property.TryGetLoadedInstance(propertyId, out var instance))
                                continue;

                            var layout = Property.GetLayoutByType(instance.LayoutType);

                            if (layout.OnCityUpgradeAction != null)
                            {
                                layout.OnCityUpgradeAction(instance.Area, upgradeType, dbCity.Upgrades[upgradeType]-1);
                            }
                        }
                    }
                });
        }


        public Action UpgradeMedicalCenterLevel() => () =>
        {
            HandleUpgrade(PropertyUpgradeType.MedicalCenterLevel, PropertyType.MedicalCenter);
        };

        public Action UpgradeStarportLevel() => () =>
        {
            HandleUpgrade(PropertyUpgradeType.StarportLevel, PropertyType.Starport);
        };

        public Action UpgradeCantinaLevel() => () =>
        {
            HandleUpgrade(PropertyUpgradeType.CantinaLevel, PropertyType.Cantina);
        };

        public Action UpgradeJumpBeaconLevel() => () =>
        {
            if (!Station.IsStation(DB.Get<WorldProperty>(_cityId)))
                return;

            HandleUpgrade(PropertyUpgradeType.JumpBeaconLevel, PropertyType.Invalid);
        };

        public Action PayUpkeep() => () =>
        {
            var dbCity = DB.Get<WorldProperty>(_cityId);
            if (dbCity.Upkeep == 0)
            {
                Instructions = "No upkeep is currently due.";
                InstructionsColor = GuiColor.Green;
                return;
            }

            ShowModal($"Your upkeep bill is {dbCity.Upkeep} cr. Note that credits must be deposited into your city's treasury. Will you pay this fee now?",
                () =>
                {
                    dbCity = DB.Get<WorldProperty>(_cityId);
                    if (dbCity.Upkeep == 0)
                    {
                        return;
                    }

                    if (dbCity.Upkeep > dbCity.Treasury)
                    {
                        Instructions = "Insufficient treasury funds.";
                        InstructionsColor = GuiColor.Red;
                        return;
                    }

                    Log.Write(LogGroup.Property, $"Player '{GetName(Player)}' ({GetPCPublicCDKey(Player)} / {GetObjectUUID(Player)}) paid city upkeep of {dbCity.Upkeep} credits for property '{dbCity.CustomName}' ({dbCity.Id}).");

                    dbCity.Treasury -= dbCity.Upkeep;
                    dbCity.Upkeep = 0;

                    DB.Set(dbCity);
                    Property.BroadcastCityUpkeepPaid(dbCity);

                    Instructions = "Upkeep paid successfully.";
                    InstructionsColor = GuiColor.Green;

                    RefreshPropertyDetails();
                    RefreshUpkeep();
                });

        };

        public Action SaveChanges() => () =>
        {
            if (CityName.Length <= 0)
            {
                Instructions = "City name must be at least one character.";
                InstructionsColor = GuiColor.Red;
                return;
            }

            var dbCity = DB.Get<WorldProperty>(_cityId);

            dbCity.CustomName = CityName;
            if (Station.IsStation(dbCity))
                Station.RenameExterior(dbCity);

            if (!int.TryParse(CitizenshipTax, out var citizenshipTax))
            {
                citizenshipTax = 0;
            }

            if (!int.TryParse(TransportationTax, out var transportationTax))
            {
                transportationTax = 0;
            }

            if (citizenshipTax < 0)
                citizenshipTax = 0;
            else if (citizenshipTax > 50000)
                citizenshipTax = 50000;

            if (transportationTax < 0)
                transportationTax = 0;
            else if (transportationTax > 25)
                transportationTax = 25;

            dbCity.Taxes[PropertyTaxType.Citizenship] = citizenshipTax;
            dbCity.Taxes[PropertyTaxType.Transportation] = transportationTax;

            // The Trade Exchange tariff (0 to the cap) is the city leadership's to set: it needs the Edit Taxes permission.
            RefreshPermissions();
            var tariffRefused = false;
            if (!int.TryParse(ExchangeTariff, out var exchangeTariff))
                exchangeTariff = GetStoredExchangeTariff(dbCity);
            exchangeTariff = TradeTariffRules.ClampPercent(exchangeTariff);
            if (exchangeTariff != GetStoredExchangeTariff(dbCity))
            {
                if (CanEditTaxes)
                    dbCity.Taxes[PropertyTaxType.Exchange] = exchangeTariff;
                else
                    tariffRefused = true;
            }

            // A station's Shipyard fee (0 to the cap) is set the same way, with the Edit Taxes permission.
            if (Station.IsStation(dbCity))
            {
                if (!int.TryParse(ShipyardFee, out var shipyardFee))
                    shipyardFee = StationBuildings.GetShipyardFee(dbCity);
                shipyardFee = StationBuildingRules.ClampShipyardFee(shipyardFee);
                if (shipyardFee != StationBuildings.GetShipyardFee(dbCity))
                {
                    if (CanEditTaxes)
                        dbCity.Taxes[PropertyTaxType.Shipyard] = shipyardFee;
                    else
                        tariffRefused = true;
                }
            }

            DB.Set(dbCity);

            RefreshPropertyDetails();
            RefreshTaxesAndFees();

            Instructions = tariffRefused
                ? "City details saved. You need the Edit Taxes permission to change the exchange tariff or the Shipyard fee."
                : "City details saved successfully.";
            InstructionsColor = tariffRefused ? GuiColor.Red : GuiColor.Green;

        };

        public Action ResetChanges() => () =>
        {
            RefreshPropertyDetails();
            RefreshTaxesAndFees();
        };

        public void ChangePrice(string recordId, int amount)
        {
            var dbCity = DB.Get<WorldProperty>(_cityId);
            RefreshPermissions();

            if (!CanAccessTreasury)
            {
                Instructions = "Insufficient permissions.";
                InstructionsColor = GuiColor.Red;
                return;
            }

            if (recordId == "WITHDRAW")
            {
                if (dbCity.Treasury < amount)
                {
                    Instructions = "Insufficient funds in treasury.";
                    InstructionsColor = GuiColor.Red;
                    return;
                }

                dbCity.Treasury -= amount;
                DB.Set(dbCity);

                GiveGoldToCreature(Player, amount);
                Log.Write(LogGroup.Property, $"Player '{GetName(Player)}' ({GetPCPublicCDKey(Player)} / {GetObjectUUID(Player)}) withdrew {amount} credits from treasury of property '{dbCity.CustomName}' ({dbCity.Id})");

                RefreshPropertyDetails();
                Instructions = $"Withdrew {amount} credits from treasury.";
                InstructionsColor = GuiColor.Green;
            }
            else if (recordId == "DEPOSIT")
            {
                if (GetGold(Player) < amount)
                {
                    Instructions = "Insufficient credits in your inventory.";
                    InstructionsColor = GuiColor.Red;
                    return;
                }

                AssignCommand(Player, () => TakeGoldFromCreature(amount, Player, true));
                dbCity.Treasury += amount;
                DB.Set(dbCity);
                Log.Write(LogGroup.Property, $"Player '{GetName(Player)}' ({GetPCPublicCDKey(Player)} / {GetObjectUUID(Player)}) deposited {amount} credits into treasury of property '{dbCity.CustomName}' ({dbCity.Id})");

                RefreshPropertyDetails();
                Instructions = $"Deposited {amount} credits into treasury.";
                InstructionsColor = GuiColor.Green;
            }
        }
    }
}
