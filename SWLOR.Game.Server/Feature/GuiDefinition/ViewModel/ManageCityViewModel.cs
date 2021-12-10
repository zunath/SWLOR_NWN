using System;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.PropertyService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class ManageCityViewModel : GuiViewModelBase<ManageCityViewModel, GuiPayloadBase>
    {
        private static GuiColor _green = new GuiColor(0, 255, 0);
        private static GuiColor _red = new GuiColor(255, 0, 0);

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

        public GuiBindingList<string> StructureNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> StructureFees
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

        public bool CanManageUpgrades
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

            WatchOnClient(model => model.CityName);
        }

        private void RefreshPermissions()
        {
            var playerId = GetObjectUUID(Player);
            var permission = DB.Search(new DBQuery<WorldPropertyPermission>()
                .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), playerId, false)
                .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), _cityId, false))
                .Single();

            CanAccessTreasury = permission.Permissions[PropertyPermissionType.AccessTreasury];
            CanEditTaxes = permission.Permissions[PropertyPermissionType.EditTaxes];
            CanManageUpgrades = permission.Permissions[PropertyPermissionType.ManageUpgrades];
            CanManageUpkeep = permission.Permissions[PropertyPermissionType.ManageUpkeep];
        }

        private void RefreshUpgradeLevels()
        {
            var dbCity = DB.Get<WorldProperty>(_cityId);

            BankUpgradeLevel = $"Bank: Lvl {dbCity.Upgrades[PropertyUpgradeType.BankLevel]}";
            MedicalCenterLevel = $"Medical Center: Lvl {dbCity.Upgrades[PropertyUpgradeType.MedicalCenterLevel]}";
            StarportLevel = $"Starport: Lvl {dbCity.Upgrades[PropertyUpgradeType.StarportLevel]}";
            CantinaLevel = $"Cantina: Lvl {dbCity.Upgrades[PropertyUpgradeType.CantinaLevel]}";
        }

        private void RefreshCitizenList()
        {
            var dbCitizens = DB.Search(new DBQuery<Player>()
                .AddFieldSearch(nameof(Entity.Player.CitizenPropertyId), _cityId, false));

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
            Instructions = string.Empty;
            InstructionsColor = _green;
            CityName = dbCity.CustomName;
            Treasury = $"Treasury: {dbCity.Treasury} cr";
            
            BankCurrentUpgrade = GetBankUpgrade(dbCity.Upgrades[PropertyUpgradeType.BankLevel]);
            BankNextUpgrade = GetBankUpgrade(dbCity.Upgrades[PropertyUpgradeType.BankLevel] + 1);
            MedicalCenterCurrentUpgrade = GetMedicalCenterUpgrade(dbCity.Upgrades[PropertyUpgradeType.MedicalCenterLevel]);
            MedicalCenterNextUpgrade = GetMedicalCenterUpgrade(dbCity.Upgrades[PropertyUpgradeType.MedicalCenterLevel] + 1);
            StarportCurrentUpgrade = GetStarportUpgrade(dbCity.Upgrades[PropertyUpgradeType.StarportLevel]);
            StarportNextUpgrade = GetStarportUpgrade(dbCity.Upgrades[PropertyUpgradeType.StarportLevel] + 1);
            CantinaCurrentUpgrade = GetCantinaUpgrade(dbCity.Upgrades[PropertyUpgradeType.CantinaLevel]);
            CantinaNextUpgrade = GetCantinaUpgrade(dbCity.Upgrades[PropertyUpgradeType.CantinaLevel] + 1);
        }

        private void RefreshTaxesAndFees()
        {
            var dbCity = DB.Get<WorldProperty>(_cityId);

            CitizenshipTax = $"{(int)dbCity.Taxes[PropertyTaxType.Citizenship]}";
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
            
        };

        public Action Withdraw() => () =>
        {

        };

        public Action UpgradeBankLevel() => () =>
        {

        };

        public Action UpgradeMedicalCenterLevel() => () =>
        {

        };

        public Action UpgradeStarportLevel() => () =>
        {

        };

        public Action UpgradeCantinaLevel() => () =>
        {

        };

        public Action PayStructureFee() => () =>
        {

        };

        public Action PayAllFees() => () =>
        {

        };

        public Action SaveChanges() => () =>
        {

        };

        public Action ResetChanges() => () =>
        {

        };
    }
}
