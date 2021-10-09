using System;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.GuiService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class AppearanceEditorViewModel: GuiViewModelBase<AppearanceEditorViewModel>
    {
        private const int ColorWidthCells = 16;
        private const int ColorHeightCells = 11;

        public bool IsAppearanceSelected
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsEquipmentSelected
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsOutfitsSelected
        {
            get => Get<bool>();
            set => Set(value);
        }


        public string ColorSheetResref
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsColorSheetPartSelected
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsRegularPartSelected
        {
            get => Get<bool>();
            set => Set(value);
        }
        
        public GuiBindingList<string> CategoryOptions
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> CategorySelected
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public int SelectedCategoryIndex
        {
            get => Get<int>();
            set
            {
                Set(value);

                if (value == 0) // 0 = Skin Color
                {
                    ColorSheetResref = "gui_pal_skin";
                    IsColorSheetPartSelected = true;
                    IsRegularPartSelected = false;
                }
                else if (value == 1) // 1 = Hair Color
                {
                    ColorSheetResref = "gui_pal_hair01";
                    IsColorSheetPartSelected = true;
                    IsRegularPartSelected = false;
                }
                else if (value == 2) // 2 = Tattoo Color 1
                {
                    ColorSheetResref = "gui_pal_tattoo";
                    IsColorSheetPartSelected = true;
                    IsRegularPartSelected = false;
                }
                else if (value == 3) // 3 = Tattoo Color 2
                {
                    ColorSheetResref = "gui_pal_tattoo";
                    IsColorSheetPartSelected = true;
                    IsRegularPartSelected = false;
                }
                else // All others = Body parts
                {
                    IsColorSheetPartSelected = false;
                    IsRegularPartSelected = true;
                }
            }
        }
        public GuiBindingList<string> PartOptions
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> PartSelected
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public int SelectedPartIndex
        {
            get => Get<int>();
            set => Set(value);
        }

        public Action OnLoadWindow() => () =>
        {
            IsAppearanceSelected = true;
            IsEquipmentSelected = false;
            IsOutfitsSelected = false;
            LoadCategoryOptions();
            SelectedCategoryIndex = 0;
            CategorySelected[0] = true;

            WatchOnClient(model => model.SelectedCategoryIndex);
        };

        private void LoadCategoryOptions()
        {
            var partOptions = new GuiBindingList<string>
            {
                "Skin Color",
                "Hair Color",
                "Tattoo 1 Color",
                "Tattoo 2 Color",
                "Tattoo 1",
                "Tattoo 2",
                "Head",
                "Torso",
                "Pelvis",
                "Right Bicep",
                "Right Forearm",
                "Right Hand",
                "Right Thigh",
                "Right Shin",
                "Left Bicep",
                "Left Forearm",
                "Left Hand",
                "Left Thigh",
                "Left Shin"
            };
            var partOptionsSelected = new GuiBindingList<bool>();

            foreach (var unused in partOptions)
            {
                partOptionsSelected.Add(false);
            }

            CategoryOptions = partOptions;
            CategorySelected = partOptionsSelected;

            CategorySelected[SelectedCategoryIndex] = true;
            SelectedCategoryIndex = 0;
        }

        public Action OnCloseWindow() => () =>
        {

        };

        public Action OnSelectAppearance() => () =>
        {
            IsAppearanceSelected = true;
            IsEquipmentSelected = false;
            IsOutfitsSelected = false;

            SelectedCategoryIndex = 0;
        };

        public Action OnSelectEquipment() => () =>
        {
            IsAppearanceSelected = false;
            IsEquipmentSelected = true;
            IsOutfitsSelected = false;
        };

        public Action OnSelectOutfits() => () =>
        {
            IsAppearanceSelected = false;
            IsEquipmentSelected = false;
            IsOutfitsSelected = true;
        };

        public Action OnSelectCategory() => () =>
        {
            var index = NuiGetEventArrayIndex();
            CategorySelected[SelectedCategoryIndex] = false;

            SelectedCategoryIndex = index;
            CategorySelected[index] = true;
        };

        public Action OnSelectColor() => () =>
        {
            var scale = GetPlayerDeviceProperty(Player, PlayerDevicePropertyType.GuiScale) / 100.0f;
            var payload = NuiGetEventPayload();
            var mousePosition = JsonObjectGet(payload, "mouse_pos");
            var jsonX = JsonObjectGet(mousePosition, "x");
            var jsonY = JsonObjectGet(mousePosition, "y");
            var x = (float)Convert.ToDouble(JsonDump(jsonX)) / scale;
            var y = (float)Convert.ToDouble(JsonDump(jsonY)) / scale;
            var tileWidth = 16f * scale;
            var tileHeight = 16f * scale;
            var cellX = (int)(x * scale / tileWidth);
            var cellY = (int) (y * scale / tileHeight);
            
            if (cellX < 0)
                cellX = 0;
            else if (cellX > ColorWidthCells)
                cellX = ColorWidthCells;

            if (cellY < 0)
                cellY = 0;
            else if (cellY > ColorHeightCells)
                cellY = ColorHeightCells;

            var colorId = cellX + cellY * ColorWidthCells;

            // Appearance - Skin, Hair, or Tattoo
            if (IsAppearanceSelected)
            {
                if (SelectedCategoryIndex == 0) // 0 = Skin
                {
                    SetColor(Player, ColorChannel.Skin, colorId);
                }
                else if (SelectedCategoryIndex == 1) //  1 = Hair
                {
                    SetColor(Player, ColorChannel.Hair, colorId);
                }
                else if (SelectedCategoryIndex == 2) // 2 = Tattoo 1
                {
                    SetColor(Player, ColorChannel.Tattoo1, colorId);
                }
                else if (SelectedCategoryIndex == 3) // 3 = Tattoo 2
                {
                    SetColor(Player, ColorChannel.Tattoo2, colorId);
                }
            }
            else if (IsEquipmentSelected)
            {

            }

        };

        public Action OnSelectPart() => () =>
        {

        };

        public Action OnApplyChanges() => () =>
        {

        };

        public Action OnCancelChanges() => () =>
        {

        };
    }
}
