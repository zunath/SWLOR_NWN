using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Component;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class RentApartmentViewModel: GuiViewModelBase<RentApartmentViewModel, GuiPayloadBase>
    {
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

        // One row DTO per apartment layout, replacing the two hand-synced parallel
        // GuiBindingList instances Initialize used to build in lockstep.
        private sealed class LayoutEntry
        {
            public PropertyLayoutType LayoutType { get; }
            public string Name { get; }

            public LayoutEntry(PropertyLayoutType layoutType, string name)
            {
                LayoutType = layoutType;
                Name = name;
            }
        }

        private static readonly GuiTableSource<RentApartmentViewModel, LayoutEntry> LayoutTable =
            new GuiTableSource<RentApartmentViewModel, LayoutEntry>()
                .Column((m, v) => m.LayoutNames = v, r => r.Name)
                .Column((m, v) => m.LayoutToggles = v, r => false);

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
            var dbApartment = DB.Search(query).FirstOrDefault();

            return dbApartment == null;
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            SelectedLayout = -1;
            IsRentApartmentEnabled = CanRentApartment();

            var rows = new List<LayoutEntry>();

            foreach (var layoutType in Property.GetAllLayoutsByPropertyType(PropertyType.Apartment))
            {
                var layout = Property.GetLayoutByType(layoutType);

                rows.Add(new LayoutEntry(layoutType, layout.Name));
            }

            // OnSelectLayout/OnBuyApartment/OnPreviewApartment index into this in
            // lockstep with the bound lists.
            _layoutTypes.Clear();
            foreach (var row in rows)
                _layoutTypes.Add(row.LayoutType);

            Name = $"Please select a layout.";
            FurnitureLimit = string.Empty;
            InitialPrice = string.Empty;
            PricePerDay = string.Empty;

            LayoutTable.Refresh(this, rows);
        }

        private void LoadLayout()
        {
            var layoutType = _layoutTypes[SelectedLayout];
            var layout = Property.GetLayoutByType(layoutType);

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
            var layout = Property.GetLayoutByType(layoutType);

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
                    var apartments = DB.Search(query).ToList();

                    if (apartments.Count > 0)
                    {
                        Instructions = "Apartment already owned!";
                        InstructionsColor = GuiColor.Red;
                        return;
                    }

                    AssignCommand(Player, () => TakeGoldFromCreature(layout.InitialPrice, Player, true));

                    var property = Property.CreateApartment(Player, layoutType);
                    Property.EnterProperty(Player, property.Id);

                    Gui.TogglePlayerWindow(Player, GuiWindowType.RentApartment);

                    if (Gui.IsWindowOpen(Player, GuiWindowType.ManageApartment))
                    {
                        Gui.TogglePlayerWindow(Player, GuiWindowType.ManageApartment);
                    }
                });
        };

        public Action OnPreviewApartment() => () =>
        {
            var layoutType = _layoutTypes[SelectedLayout];

            Property.PreviewProperty(Player, layoutType);
            Gui.TogglePlayerWindow(Player, GuiWindowType.RentApartment);
        };
    }
}
