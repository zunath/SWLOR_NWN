using System;
using System.Collections.Generic;
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
    public class ApartmentManagementViewModel: GuiViewModelBase<ApartmentManagementViewModel, GuiPayloadBase>
    {
        private GuiColor _red = new GuiColor(255, 0, 0);
        private GuiColor _green = new GuiColor(0, 255, 0);

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

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            SelectedLayout = -1;
            var layoutNames = new GuiBindingList<string>();
            var layoutToggles = new GuiBindingList<bool>();
            _layoutTypes.Clear();

            foreach (var layoutType in Property.GetAllLayoutsByPropertyType(PropertyType.Apartment))
            {
                var layout = Property.GetLayoutByType(layoutType);

                layoutNames.Add(layout.Name);
                layoutToggles.Add(false);
                _layoutTypes.Add(layoutType);
            }

            LayoutNames = layoutNames;
            LayoutToggles = layoutToggles;
        }

        private void LoadLayout()
        {
            var layoutType = _layoutTypes[SelectedLayout];
            var layout = Property.GetLayoutByType(layoutType);

            Name = layout.Name;
            FurnitureLimit = $"Furniture Limit: {layout.FurnitureLimit} items";
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
                        InstructionsColor = _red;
                        return;
                    }

                    var playerId = GetObjectUUID(Player);
                    var query = new DBQuery<WorldProperty>()
                        .AddFieldSearch(nameof(WorldProperty.OwnerPlayerId), playerId, false);
                    var apartments = DB.Search(query).ToList();

                    if (apartments.Count > 0)
                    {
                        Instructions = "Apartment already owned!";
                        InstructionsColor = _red;
                        return;
                    }

                    AssignCommand(Player, () => TakeGoldFromCreature(layout.InitialPrice, Player, true));

                    Gui.TogglePlayerWindow(Player, GuiWindowType.ApartmentManagement);
                    Property.CreateApartment(Player, layoutType);
                    var position = Property.GetEntrancePosition(layoutType);
                    var area = Cache.GetAreaByResref(layout.AreaInstanceResref);
                    var location = Location(area, position, 0.0f);

                });
        };

        public Action OnPreviewApartment() => () =>
        {
            var layoutType = _layoutTypes[SelectedLayout];
            var layout = Property.GetLayoutByType(layoutType);
            var position = Property.GetEntrancePosition(layoutType);
            var area = Cache.GetAreaByResref(layout.AreaInstanceResref);
            var location = Location(area, position, 0.0f);

            AssignCommand(Player, () =>
            {
                JumpToLocation(location);
            });

            Gui.TogglePlayerWindow(Player, GuiWindowType.ApartmentManagement);
        };
    }
}
