using SWLOR.Component.Properties.Service;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Abstractions.Enums;
using SWLOR.Shared.Abstractions.Models;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Domain.Entity;
using SWLOR.Shared.Domain.Enums;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Model;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.Properties.UI.ViewModel
{
    public class RentApartmentViewModel: GuiViewModelBase<RentApartmentViewModel, IGuiPayload>
    {
        private readonly IDatabaseService _db;
        private readonly PropertyService _property;

        public RentApartmentViewModel(IGuiService guiService, IDatabaseService db, PropertyService property) : base(guiService)
        {
            _db = db;
            _property = property;
        }
        
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

        private readonly List<PropertyLayoutType> _layoutTypes = new();

        public GuiBindingList<string> LayoutNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> LayoutToggles
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        private int SelectedLayout { get; set; }

        public string Name
        {
            get => Get<string>();
            set => Set(value);
        }

        public string FurnitureLimit
        {
            get => Get<string>();
            set => Set(value);
        }

        public string InitialPrice
        {
            get => Get<string>();
            set => Set(value);
        }

        public string PricePerDay
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsRentApartmentEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        private bool CanRentApartment()
        {
            var playerId = GetObjectUUID(Player);
            var query = new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.OwnerPlayerId), playerId, false)
                .AddFieldSearch(nameof(WorldProperty.PropertyType), (int)PropertyType.Apartment)
                .AddFieldSearch(nameof(WorldProperty.IsQueuedForDeletion), false);
            var dbApartment = _db.Search(query).FirstOrDefault();

            return dbApartment == null;
        }

        protected override void Initialize(IGuiPayload initialPayload)
        {
            SelectedLayout = -1;
            IsRentApartmentEnabled = CanRentApartment();
            var layoutNames = new GuiBindingList<string>();
            var layoutToggles = new GuiBindingList<bool>();
            _layoutTypes.Clear();

            foreach (var layoutType in _property.GetAllLayoutsByPropertyType(PropertyType.Apartment))
            {
                var layout = _property.GetLayoutByType(layoutType);

                layoutNames.Add(layout.Name);
                layoutToggles.Add(false);
                _layoutTypes.Add(layoutType);
            }

            Name = $"Please select a layout.";
            FurnitureLimit = string.Empty;
            InitialPrice = string.Empty;
            PricePerDay = string.Empty;

            LayoutNames = layoutNames;
            LayoutToggles = layoutToggles;
        }

        private void LoadLayout()
        {
            var layoutType = _layoutTypes[SelectedLayout];
            var layout = _property.GetLayoutByType(layoutType);

            Name = layout.Name;
            FurnitureLimit = $"Structure Limit: {layout.StructureLimit} items";
            InitialPrice = $"Initial Price: {layout.InitialPrice} cr";
            PricePerDay = $"Price Per Day: {layout.PricePerDay} cr";
        }

        public Action OnSelectLayout() => () =>
        {
            if (SelectedLayout > -1)
                LayoutToggles[SelectedLayout] = false;

            SelectedLayout = NuiGetEventArrayIndex();
            LayoutToggles[SelectedLayout] = true;

            LoadLayout();
        };

        public Action OnBuyApartment() => () =>
        {
            var layoutType = _layoutTypes[SelectedLayout];
            var layout = _property.GetLayoutByType(layoutType);

            ShowModal($"Are you sure you want to buy this apartment layout for {layout.InitialPrice} credits? " +
                      $"Your lease will last for seven days and can be extended up to thirty days for {layout.PricePerDay} credits per day.",
                () =>
                {
                    var credits = GetGold(Player);

                    if (credits < layout.InitialPrice)
                    {
                        Instructions = "Not enough credits!";
                        InstructionsColor = GuiColor.Red;
                        return;
                    }

                    var playerId = GetObjectUUID(Player);
                    var query = new DBQuery<WorldProperty>()
                        .AddFieldSearch(nameof(WorldProperty.OwnerPlayerId), playerId, false)
                        .AddFieldSearch(nameof(WorldProperty.PropertyType), (int)PropertyType.Apartment)
                        .AddFieldSearch(nameof(WorldProperty.IsQueuedForDeletion), false);
                    var apartments = _db.Search(query).ToList();

                    if (apartments.Count > 0)
                    {
                        Instructions = "Apartment already owned!";
                        InstructionsColor = GuiColor.Red;
                        return;
                    }

                    AssignCommand(Player, () => TakeGoldFromCreature(layout.InitialPrice, Player, true));

                    var property = _property.CreateApartment(Player, layoutType);
                    _property.EnterProperty(Player, property.Id);

                    _guiService.TogglePlayerWindow(Player, GuiWindowType.RentApartment);

                    if (_guiService.IsWindowOpen(Player, GuiWindowType.ManageApartment))
                    {
                        _guiService.TogglePlayerWindow(Player, GuiWindowType.ManageApartment);
                    }
                });
        };

        public Action OnPreviewApartment() => () =>
        {
            var layoutType = _layoutTypes[SelectedLayout];

            _property.PreviewProperty(Player, layoutType);
            _guiService.TogglePlayerWindow(Player, GuiWindowType.RentApartment);
        };
    }
}
