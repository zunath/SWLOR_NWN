using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Creature;
using SWLOR.Game.Server.Feature.AppearanceDefinition;
using SWLOR.Game.Server.Service.GuiService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class AppearanceEditorViewModel: GuiViewModelBase<AppearanceEditorViewModel>
    {
        private const int ColorWidthCells = 16;
        private const int ColorHeightCells = 11;

        private static readonly Dictionary<RacialType, IAppearanceDefinition> _appearances = new();
        private Dictionary<int, int> _partIdToIndex = new();

        [NWNEventHandler("mod_load")]
        public static void LoadAppearances()
        {
            _appearances[RacialType.Human] = new HumanAppearanceDefinition();
            _appearances[RacialType.Bothan] = new BothanAppearanceDefinition();
            _appearances[RacialType.Chiss] = new ChissAppearanceDefinition();
            _appearances[RacialType.Zabrak] = new ZabrakAppearanceDefinition();
            _appearances[RacialType.Twilek] = new TwilekAppearanceDefinition();
            _appearances[RacialType.Mirialan] = new MirialanAppearanceDefinition();
            _appearances[RacialType.Echani] = new EchaniAppearanceDefinition();
            _appearances[RacialType.Cyborg] = new CyborgAppearanceDefinition();
            _appearances[RacialType.Cathar] = new CatharAppearanceDefinition();
            _appearances[RacialType.Trandoshan] = new TrandoshanAppearanceDefinition();
            _appearances[RacialType.Wookiee] = new WookieeAppearanceDefinition();
            _appearances[RacialType.MonCalamari] = new MonCalamariAppearanceDefinition();
            _appearances[RacialType.Ugnaught] = new UgnaughtAppearanceDefinition();
        }

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

        public GuiBindingList<string> ColorCategoryOptions
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> PartCategoryOptions
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> ColorCategorySelected
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public GuiBindingList<bool> PartCategorySelected
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public int SelectedColorCategoryIndex
        {
            get => Get<int>();
            set
            {
                Set(value);

                if (value == 0) // 0 = Skin Color
                {
                    ColorSheetResref = "gui_pal_skin";
                }
                else if (value == 1) // 1 = Hair Color
                {
                    ColorSheetResref = "gui_pal_hair01";
                }
                else if (value == 2) // 2 = Tattoo Color 1
                {
                    ColorSheetResref = "gui_pal_tattoo";
                }
                else if (value == 3) // 3 = Tattoo Color 2
                {
                    ColorSheetResref = "gui_pal_tattoo";
                }
            }
        }

        public int SelectedPartCategoryIndex
        {
            get => Get<int>();
            set => Set(value);
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
            LoadColorCategoryOptions();
            LoadPartCategoryOptions();
            SelectedColorCategoryIndex = 0;
            SelectedPartCategoryIndex = 0;
            SelectedPartIndex = 0;
            ColorCategorySelected[0] = true;
            PartCategorySelected[0] = true;
            LoadBodyParts();

            WatchOnClient(model => model.SelectedColorCategoryIndex);
            WatchOnClient(model => model.SelectedPartCategoryIndex);
            WatchOnClient(model => model.SelectedPartIndex);
        };

        private void LoadColorCategoryOptions()
        {
            var colorCategoryOptions = new GuiBindingList<string>
            {
                "Skin Color",
                "Hair Color",
                "Tattoo 1 Color",
                "Tattoo 2 Color",
            };
            var colorCategorySelected = new GuiBindingList<bool>();

            foreach (var unused in colorCategoryOptions)
            {
                colorCategorySelected.Add(false);
            }

            ColorCategoryOptions = colorCategoryOptions;
            ColorCategorySelected = colorCategorySelected;

            SelectedColorCategoryIndex = 0;
            ColorCategorySelected[SelectedColorCategoryIndex] = true;
        }

        private void LoadPartCategoryOptions()
        {
            var partCategoryOptions = new GuiBindingList<string>
            {
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
            var partCategorySelected = new GuiBindingList<bool>();

            foreach (var unused in partCategoryOptions)
            {
                partCategorySelected.Add(false);
            }

            PartCategoryOptions = partCategoryOptions;
            PartCategorySelected = partCategorySelected;

            SelectedPartCategoryIndex = 0;
            PartCategorySelected[SelectedPartCategoryIndex] = true;
        }

        private (GuiBindingList<string>, GuiBindingList<bool>) GetPartLists(int[] partIds)
        {
            var partNames = new GuiBindingList<string>();
            var partSelected = new GuiBindingList<bool>();
            var partIdToIndex = new Dictionary<int, int>();
            var index = 0;

            foreach (var partId in partIds)
            {
                partNames.Add($"Part #{partId}");
                partSelected.Add(false);
                partIdToIndex[partId] = index;
                index++;
            }

            _partIdToIndex = partIdToIndex;
            return (partNames, partSelected);
        }

        private void LoadBodyParts()
        {
            var race = GetRacialType(Player);
            var gender = GetGender(Player);
            var appearance = _appearances[race];
            int[] partIds;
            int selectedPartId;

            switch (SelectedPartCategoryIndex)
            {
                case 0: // Head
                    switch (gender)
                    {
                        case Gender.Male:
                            partIds = appearance.MaleHeads;
                            break;
                        default:
                            partIds = appearance.FemaleHeads;
                            break;
                    }

                    selectedPartId = GetCreatureBodyPart(CreaturePart.Head, Player);
                    break;
                case 1: // Torso
                    partIds = appearance.Torsos;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.Torso, Player);
                    break;
                case 2: // Pelvis
                    partIds = appearance.Pelvis;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.Pelvis, Player);
                    break;
                case 3: // Right Bicep
                    partIds = appearance.RightBicep;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.RightBicep, Player);
                    break;
                case 4: // Right Forearm
                    partIds = appearance.RightForearm;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.RightForearm, Player);
                    break;
                case 5: // Right Hand
                    partIds = appearance.RightHand;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.RightHand, Player);
                    break;
                case 6: // Right Thigh
                    partIds = appearance.RightThigh;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.RightThigh, Player);
                    break;
                case 7: // Right Shin
                    partIds = appearance.RightShin;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.RightShin, Player);
                    break;
                case 8: // Left Bicep
                    partIds = appearance.LeftBicep;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.LeftBicep, Player);
                    break;
                case 9: // Left Forearm
                    partIds = appearance.LeftForearm;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.LeftForearm, Player);
                    break;
                case 10: // Left Hand
                    partIds = appearance.LeftHand;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.LeftHand, Player);
                    break;
                case 11: // Left Thigh
                    partIds = appearance.LeftThigh;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.LeftThigh, Player);
                    break;
                case 12: // Left Shin
                    partIds = appearance.LeftShin;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.LeftShin, Player);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(SelectedPartIndex));
            }

            var (partNames, partSelected) = GetPartLists(partIds);

            PartOptions = partNames;
            PartSelected = partSelected;
            SelectedPartIndex = _partIdToIndex[selectedPartId];
            PartSelected[SelectedPartIndex] = true;
        }

        public Action OnSelectAppearance() => () =>
        {
            IsAppearanceSelected = true;
            IsEquipmentSelected = false;
            IsOutfitsSelected = false;

            SelectedColorCategoryIndex = 0;
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

        public Action OnSelectColorCategory() => () =>
        {
            var index = NuiGetEventArrayIndex();
            ColorCategorySelected[SelectedColorCategoryIndex] = false;

            SelectedColorCategoryIndex = index;
            ColorCategorySelected[index] = true;
        };

        public Action OnSelectPartCategory() => () =>
        {
            var index = NuiGetEventArrayIndex();
            PartCategorySelected[SelectedPartCategoryIndex] = false;

            SelectedPartCategoryIndex = index;
            PartCategorySelected[index] = true;

            LoadBodyParts();
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
                if (SelectedColorCategoryIndex == 0) // 0 = Skin
                {
                    SetColor(Player, ColorChannel.Skin, colorId);
                }
                else if (SelectedColorCategoryIndex == 1) //  1 = Hair
                {
                    SetColor(Player, ColorChannel.Hair, colorId);
                }
                else if (SelectedColorCategoryIndex == 2) // 2 = Tattoo 1
                {
                    SetColor(Player, ColorChannel.Tattoo1, colorId);
                }
                else if (SelectedColorCategoryIndex == 3) // 3 = Tattoo 2
                {
                    SetColor(Player, ColorChannel.Tattoo2, colorId);
                }
            }
            else if (IsEquipmentSelected)
            {

            }

        };

        private void LoadPart()
        {
            var race = GetRacialType(Player);
            var gender = GetGender(Player);
            var appearance = _appearances[race];

            switch (SelectedPartCategoryIndex)
            {
                case 0: // Head
                    switch (gender)
                    {
                        case Gender.Male:
                            SetCreatureBodyPart(CreaturePart.Head, appearance.MaleHeads[SelectedPartIndex], Player);
                            break;
                        default:
                            SetCreatureBodyPart(CreaturePart.Head, appearance.FemaleHeads[SelectedPartIndex], Player);
                            break;
                    }
                    break;
                case 1: // Torso
                    SetCreatureBodyPart(CreaturePart.Torso, appearance.Torsos[SelectedPartIndex], Player);
                    break;
                case 2: // Pelvis
                    SetCreatureBodyPart(CreaturePart.Pelvis, appearance.Pelvis[SelectedPartIndex], Player);
                    break;
                case 3: // Right Bicep
                    SetCreatureBodyPart(CreaturePart.RightBicep, appearance.RightBicep[SelectedPartIndex], Player);
                    break;
                case 4: // Right Forearm
                    SetCreatureBodyPart(CreaturePart.RightForearm, appearance.RightForearm[SelectedPartIndex], Player);
                    break;
                case 5: // Right Hand
                    SetCreatureBodyPart(CreaturePart.RightHand, appearance.RightHand[SelectedPartIndex], Player);
                    break;
                case 6: // Right Thigh
                    SetCreatureBodyPart(CreaturePart.RightThigh, appearance.RightThigh[SelectedPartIndex], Player);
                    break;
                case 7: // Right Shin
                    SetCreatureBodyPart(CreaturePart.RightShin, appearance.RightShin[SelectedPartIndex], Player);
                    break;
                case 8: // Left Bicep
                    SetCreatureBodyPart(CreaturePart.LeftBicep, appearance.LeftBicep[SelectedPartIndex], Player);
                    break;
                case 9: // Left Forearm
                    SetCreatureBodyPart(CreaturePart.LeftForearm, appearance.LeftForearm[SelectedPartIndex], Player);
                    break;
                case 10: // Left Hand
                    SetCreatureBodyPart(CreaturePart.LeftHand, appearance.LeftHand[SelectedPartIndex], Player);
                    break;
                case 11: // Left Thigh
                    SetCreatureBodyPart(CreaturePart.LeftThigh, appearance.LeftThigh[SelectedPartIndex], Player);
                    break;
                case 12: // Left Shin
                    SetCreatureBodyPart(CreaturePart.LeftShin, appearance.LeftShin[SelectedPartIndex], Player);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(SelectedPartIndex));
            }
        }

        public Action OnSelectPart() => () =>
        {
            var index = NuiGetEventArrayIndex();

            PartSelected[SelectedPartIndex] = false;
            SelectedPartIndex = index;
            PartSelected[index] = true;
            
            LoadPart();
        };

        public Action OnPreviousPart() => () =>
        {
            var newPartIndex = SelectedPartIndex - 1;
            if (newPartIndex < 0)
                newPartIndex = 0;

            PartSelected[SelectedPartIndex] = false;
            SelectedPartIndex = newPartIndex;
            PartSelected[SelectedPartIndex] = true;
            LoadPart();
        };

        public Action OnNextPart() => () =>
        {
            var newPartIndex = SelectedPartIndex + 1;
            if (newPartIndex > _partIdToIndex.Count - 1)
                newPartIndex = _partIdToIndex.Count - 1;

            PartSelected[SelectedPartIndex] = false;
            SelectedPartIndex = newPartIndex;
            PartSelected[SelectedPartIndex] = true;
            LoadPart();
        };
    }
}
