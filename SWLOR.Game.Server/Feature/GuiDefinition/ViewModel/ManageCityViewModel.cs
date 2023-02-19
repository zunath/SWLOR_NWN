using System;
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

        public string BankUpgradeLevel
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

        public bool CanUpgradeBanks
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

        public string BankCurrentUpgrade
        {
            get => Get<string>();
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

        public string BankNextUpgrade
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
            CanUpgradeBanks = permission.Permissions[PropertyPermissionType.ManageUpgrades] &&
                              dbCity.Upgrades[PropertyUpgradeType.BankLevel] < MaxUpgradeLevel &&
                              dbCity.Upgrades[PropertyUpgradeType.CityLevel] >= dbCity.Upgrades[PropertyUpgradeType.BankLevel] + 1;
            CanUpgradeMedicalCenters = permission.Permissions[PropertyPermissionType.ManageUpgrades] &&
                              dbCity.Upgrades[PropertyUpgradeType.MedicalCenterLevel] < MaxUpgradeLevel &&
                              dbCity.Upgrades[PropertyUpgradeType.CityLevel] >= dbCity.Upgrades[PropertyUpgradeType.MedicalCenterLevel] + 1;
            CanUpgradeStarports = permission.Permissions[PropertyPermissionType.ManageUpgrades] &&
                              dbCity.Upgrades[PropertyUpgradeType.StarportLevel] < MaxUpgradeLevel &&
                              dbCity.Upgrades[PropertyUpgradeType.CityLevel] >= dbCity.Upgrades[PropertyUpgradeType.StarportLevel] + 1;
            CanUpgradeCantinas = permission.Permissions[PropertyPermissionType.ManageUpgrades] &&
                              dbCity.Upgrades[PropertyUpgradeType.CantinaLevel] < MaxUpgradeLevel &&
                              dbCity.Upgrades[PropertyUpgradeType.CityLevel] >= dbCity.Upgrades[PropertyUpgradeType.CantinaLevel] + 1;
        }

        private void RefreshUpgradeLevels()
        {
            var dbCity = DB.Get<WorldProperty>(_cityId);

            BankUpgradeLevel = $"Bank: Lvl {dbCity.Upgrades[PropertyUpgradeType.BankLevel]}";
            MedicalCenterLevel = $"Medical Center: Lvl {dbCity.Upgrades[PropertyUpgradeType.MedicalCenterLevel]}";
            StarportLevel = $"Starport: Lvl {dbCity.Upgrades[PropertyUpgradeType.StarportLevel]}";
            CantinaLevel = $"Cantina: Lvl {dbCity.Upgrades[PropertyUpgradeType.CantinaLevel]}";

            BankCurrentUpgrade = GetBankUpgrade(dbCity.Upgrades[PropertyUpgradeType.BankLevel]);
            BankNextUpgrade = GetBankUpgrade(dbCity.Upgrades[PropertyUpgradeType.BankLevel] + 1);
            MedicalCenterCurrentUpgrade = GetMedicalCenterUpgrade(dbCity.Upgrades[PropertyUpgradeType.MedicalCenterLevel]);
            MedicalCenterNextUpgrade = GetMedicalCenterUpgrade(dbCity.Upgrades[PropertyUpgradeType.MedicalCenterLevel] + 1);
            StarportCurrentUpgrade = GetStarportUpgrade(dbCity.Upgrades[PropertyUpgradeType.StarportLevel]);
            StarportNextUpgrade = GetStarportUpgrade(dbCity.Upgrades[PropertyUpgradeType.StarportLevel] + 1);
            CantinaCurrentUpgrade = GetCantinaUpgrade(dbCity.Upgrades[PropertyUpgradeType.CantinaLevel]);
            CantinaNextUpgrade = GetCantinaUpgrade(dbCity.Upgrades[PropertyUpgradeType.CantinaLevel] + 1);
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
                citizenNames.Add(citizen.Name);
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
        }

        private string GetBankUpgrade(int level)
        {
            switch (level)
            {
                case 1:
                    return "Storage Cap: 20 items per citizen";
                case 2:
                    return "Storage Cap: 40 items per citizen";
                case 3:
                    return "Storage Cap: 60 items per citizen";
                case 4:
                    return "Storage Cap: 80 items per citizen";
                case 5:
                    return "Storage Cap: 100 items per citizen";
                default:
                    return "UPGRADES MAXED";
            }
        }
        private string GetMedicalCenterUpgrade(int level)
        {
            switch (level)
            {
                case 1:
                    return "No benefit";
                case 2:
                    return "-5% XP debt on death";
                case 3:
                    return "-10% XP debt on death";
                case 4:
                    return "-15% XP debt on death";
                case 5:
                    return "-20% XP debt on death";
                default:
                    return "UPGRADES MAXED";
            }
        }
        private string GetStarportUpgrade(int level)
        {
            switch (level)
            {
                case 1:
                    return "No benefit";
                case 2:
                    return "-5% repair price";
                case 3:
                    return "-10% repair price";
                case 4:
                    return "-15% repair price";
                case 5:
                    return "-20% repair price";
                default:
                    return "UPGRADES MAXED";
            }
        }
        private string GetCantinaUpgrade(int level)
        {
            switch (level)
            {
                case 1:
                    return "No benefit";
                case 2:
                    return "+5% RPXP";
                case 3:
                    return "+10% RPXP";
                case 4:
                    return "+15% RPXP";
                case 5:
                    return "+20% RPXP";
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
            var currentLevel = dbCity.Upgrades[upgradeType];

            if (currentLevel >= MaxUpgradeLevel)
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

            if (mayor.Perks[PerkType.CityManagement] + 1 <= currentLevel)
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
            var currentLevel = dbCity.Upgrades[upgradeType];
            var initialPrice = 50000 * (currentLevel + 1);

            if (!ValidateUpgrade(upgradeType, initialPrice))
                return;

            ShowModal($"Upgrading this structure type will cost an upfront amount of {initialPrice} credits and increase your weekly maintenance bill by an additional 10000 credits. Upgrades cannot be undone. Are you sure you want to upgrade this structure type?",
                () =>
                {
                    // Refresh entity in case another player is editing this city.
                    dbCity = DB.Get<WorldProperty>(_cityId);
                    currentLevel = dbCity.Upgrades[upgradeType];
                    initialPrice = 50000 * (currentLevel + 1);

                    if (!ValidateUpgrade(upgradeType, initialPrice))
                        return;

                    dbCity.Treasury -= initialPrice;
                    dbCity.Upgrades[upgradeType]++;

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
                            var instance = Property.GetRegisteredInstance(propertyId);
                            var layout = Property.GetLayoutByType(instance.LayoutType);

                            if (layout.OnCityUpgradeAction != null)
                            {
                                layout.OnCityUpgradeAction(instance.Area, upgradeType, dbCity.Upgrades[upgradeType]-1);
                            }
                        }
                    }
                });
        }
        

        public Action UpgradeBankLevel() => () =>
        {
            HandleUpgrade(PropertyUpgradeType.BankLevel, PropertyType.Bank);
        };

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

        public Action PayUpkeep() => () =>
        {
            var dbCity = DB.Get<WorldProperty>(_cityId);

            ShowModal($"Your upkeep bill is {dbCity.Upkeep} cr. Note that credits must be deposited into your city's treasury. Will you pay this fee now?",
                () =>
                {
                    dbCity = DB.Get<WorldProperty>(_cityId);

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

            DB.Set(dbCity);

            RefreshPropertyDetails();
            RefreshTaxesAndFees();

            Instructions = "City details saved successfully.";
            InstructionsColor = GuiColor.Green;

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
