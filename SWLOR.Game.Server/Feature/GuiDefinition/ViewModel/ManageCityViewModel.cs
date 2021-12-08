using System;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.PropertyService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class ManageCityViewModel : GuiViewModelBase<ManageCityViewModel, GuiPayloadBase>
    {
        private string _cityId;

        public string Instructions
        {
            get => Get<string>();
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

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            var area = GetArea(TetherObject);
            var propertyId = Property.GetPropertyId(area);
            var dbProperty = DB.Get<WorldProperty>(propertyId);
            var dbBuilding = DB.Get<WorldProperty>(dbProperty.ParentPropertyId);
            _cityId = dbBuilding.ParentPropertyId;
            var dbCity = DB.Get<WorldProperty>(_cityId);

            CityName = dbCity.CustomName;
            RefreshUpgradeLevels();



            WatchOnClient(model => model.CityName);
        }

        private void RefreshUpgradeLevels()
        {
            var dbCity = DB.Get<WorldProperty>(_cityId);

            BankUpgradeLevel = $"Bank: Lvl {dbCity.Upgrades[PropertyUpgradeType.BankLevel]}";
            MedicalCenterLevel = $"Medical Center: Lvl {dbCity.Upgrades[PropertyUpgradeType.MedicalCenterLevel]}";
            StarportLevel = $"Starport: Lvl {dbCity.Upgrades[PropertyUpgradeType.StarportLevel]}";
            CantinaLevel = $"Cantina: Lvl {dbCity.Upgrades[PropertyUpgradeType.CantinaLevel]}";
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
