using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Creature;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Feature.AppearanceDefinition.RacialAppearance;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class AppearanceEditorViewModel: GuiViewModelBase<AppearanceEditorViewModel, GuiPayloadBase>
    {
        private const int ColorWidthCells = 16;
        private const int ColorHeightCells = 11;

        private static readonly ArmorAppearanceDefinition _armorAppearances = new();
        private static readonly Dictionary<RacialType, IAppearanceDefinition> _racialAppearances = new();
        private Dictionary<int, int> _partIdToIndex = new();

        [NWNEventHandler("mod_load")]
        public static void LoadAppearances()
        {
            _racialAppearances[RacialType.Human] = new HumanRacialAppearanceDefinition();
            _racialAppearances[RacialType.Bothan] = new BothanRacialAppearanceDefinition();
            _racialAppearances[RacialType.Chiss] = new ChissRacialAppearanceDefinition();
            _racialAppearances[RacialType.Zabrak] = new ZabrakRacialAppearanceDefinition();
            _racialAppearances[RacialType.Twilek] = new TwilekRacialAppearanceDefinition();
            _racialAppearances[RacialType.Mirialan] = new MirialanRacialAppearanceDefinition();
            _racialAppearances[RacialType.Echani] = new EchaniRacialAppearanceDefinition();
            _racialAppearances[RacialType.KelDor] = new KelDorRacialAppearanceDefinition();
            _racialAppearances[RacialType.Cyborg] = new CyborgRacialAppearanceDefinition();
            _racialAppearances[RacialType.Cathar] = new CatharRacialAppearanceDefinition();
            _racialAppearances[RacialType.Rodian] = new RodianRacialAppearanceDefinition();
            _racialAppearances[RacialType.Trandoshan] = new TrandoshanRacialAppearanceDefinition();
            _racialAppearances[RacialType.Togruta] = new TogrutaRacialAppearanceDefinition();
            _racialAppearances[RacialType.Wookiee] = new WookieeRacialAppearanceDefinition();
            _racialAppearances[RacialType.MonCalamari] = new MonCalamariRacialAppearanceDefinition();
            _racialAppearances[RacialType.Ugnaught] = new UgnaughtRacialAppearanceDefinition();
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

        public string ColorSheetResref
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool HasItemEquipped
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool DoesNotHaveItemEquipped
        {
            get => Get<bool>();
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

                if (IsAppearanceSelected)
                {
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
                else if (IsEquipmentSelected)
                {
                    if (value == 0) // 0 = Leather 1
                    {
                        ColorSheetResref = "gui_pal_tattoo";
                    }
                    else if (value == 1) // 1 = Leather 2
                    {
                        ColorSheetResref = "gui_pal_tattoo";
                    }
                    else if (value == 2) // 2 = Cloth 1
                    {
                        ColorSheetResref = "gui_pal_tattoo";
                    }
                    else if (value == 3) // 3 = Cloth 2
                    {
                        ColorSheetResref = "gui_pal_tattoo";
                    }
                    else if (value == 4) // 4 = Metal 1
                    {
                        ColorSheetResref = "gui_pal_armor01";
                    }
                    else if (value == 5) // 5 = Metal 2
                    {
                        ColorSheetResref = "gui_pal_armor01";
                    }
                }
            }
        }

        public int SelectedItemTypeIndex
        {
            get => Get<int>();
            set
            {
                Set(value);
                ToggleItemEquippedFlags();
                LoadColorCategoryOptions();
                LoadPartCategoryOptions();
                LoadItemParts();
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

        private void ToggleItemEquippedFlags()
        {
            var hasItemEquipped = IsAppearanceSelected || GetIsObjectValid(GetItem());
            HasItemEquipped = hasItemEquipped;
            DoesNotHaveItemEquipped = !hasItemEquipped;
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            IsAppearanceSelected = true;
            IsEquipmentSelected = false;
            ToggleItemEquippedFlags();
            LoadColorCategoryOptions();
            LoadPartCategoryOptions();
            SelectedColorCategoryIndex = 0;
            SelectedPartCategoryIndex = 0;
            SelectedPartIndex = 0;
            SelectedItemTypeIndex = 0;
            ColorCategorySelected[0] = true;
            PartCategorySelected[0] = true;
            LoadBodyParts();

            WatchOnClient(model => model.SelectedColorCategoryIndex);
            WatchOnClient(model => model.SelectedPartCategoryIndex);
            WatchOnClient(model => model.SelectedPartIndex);
            WatchOnClient(model => model.SelectedItemTypeIndex);
        }

        private void LoadColorCategoryOptions()
        {
            if (DoesNotHaveItemEquipped)
                return;

            var colorCategoryOptions = new GuiBindingList<string>();

            if (IsAppearanceSelected)
            {
                colorCategoryOptions.Add("Skin Color");
                colorCategoryOptions.Add("Hair Color");
                colorCategoryOptions.Add("Tattoo 1 Color");
                colorCategoryOptions.Add("Tattoo 2 Color");
            }
            else if (IsEquipmentSelected)
            {
                colorCategoryOptions.Add("Leather 1");
                colorCategoryOptions.Add("Leather 2");
                colorCategoryOptions.Add("Cloth 1");
                colorCategoryOptions.Add("Cloth 2");
                colorCategoryOptions.Add("Metal 1");
                colorCategoryOptions.Add("Metal 2");
            }

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
            if (DoesNotHaveItemEquipped)
                return;

            var partCategoryOptions = new GuiBindingList<string>();

            if (IsAppearanceSelected)
            {
                partCategoryOptions.Add("Head");
                partCategoryOptions.Add("Torso");
                partCategoryOptions.Add("Pelvis");
                partCategoryOptions.Add("Right Bicep");
                partCategoryOptions.Add("Right Forearm");
                partCategoryOptions.Add("Right Hand");
                partCategoryOptions.Add("Right Thigh");
                partCategoryOptions.Add("Right Shin");
                partCategoryOptions.Add("Right Foot");
                partCategoryOptions.Add("Left Bicep");
                partCategoryOptions.Add("Left Forearm");
                partCategoryOptions.Add("Left Hand");
                partCategoryOptions.Add("Left Thigh");
                partCategoryOptions.Add("Left Shin");
                partCategoryOptions.Add("Left Foot");
            }
            else if(IsEquipmentSelected)
            {
                if (SelectedItemTypeIndex == 0) // 0 = Armor
                {
                    partCategoryOptions.Add("Neck");
                    partCategoryOptions.Add("Torso");
                    partCategoryOptions.Add("Belt");
                    partCategoryOptions.Add("Pelvis");

                    partCategoryOptions.Add("Right Shoulder");
                    partCategoryOptions.Add("Right Bicep");
                    partCategoryOptions.Add("Right Forearm");
                    partCategoryOptions.Add("Right Hand");
                    partCategoryOptions.Add("Right Thigh");
                    partCategoryOptions.Add("Right Shin");
                    partCategoryOptions.Add("Right Foot");

                    partCategoryOptions.Add("Left Shoulder");
                    partCategoryOptions.Add("Left Bicep");
                    partCategoryOptions.Add("Left Forearm");
                    partCategoryOptions.Add("Left Hand");
                    partCategoryOptions.Add("Left Thigh");
                    partCategoryOptions.Add("Left Shin");
                    partCategoryOptions.Add("Left Foot");

                    partCategoryOptions.Add("Robe");
                }
                else if(SelectedItemTypeIndex == 1) // 1 = Helmet
                {
                    partCategoryOptions.Add("Helmet");
                }
            }

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

        private uint GetItem()
        {
            if (SelectedItemTypeIndex == 0) // 0 = Armor
            {
                return GetItemInSlot(InventorySlot.Chest, Player);
            }
            else if(SelectedItemTypeIndex == 1) // 1 = Helmet
            {
                return GetItemInSlot(InventorySlot.Head, Player);
            }

            return OBJECT_INVALID;
        }

        private void LoadBodyParts()
        {
            var race = GetRacialType(Player);
            var gender = GetGender(Player);
            var appearance = _racialAppearances[race];
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
                case 8: // Right Foot
                    partIds = appearance.RightFoot;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.RightFoot, Player);
                    break;
                case 9: // Left Bicep
                    partIds = appearance.LeftBicep;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.LeftBicep, Player);
                    break;
                case 10: // Left Forearm
                    partIds = appearance.LeftForearm;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.LeftForearm, Player);
                    break;
                case 11: // Left Hand
                    partIds = appearance.LeftHand;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.LeftHand, Player);
                    break;
                case 12: // Left Thigh
                    partIds = appearance.LeftThigh;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.LeftThigh, Player);
                    break;
                case 13: // Left Shin
                    partIds = appearance.LeftShin;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.LeftShin, Player);
                    break;
                case 14: // Left Foot
                    partIds = appearance.LeftFoot;
                    selectedPartId = GetCreatureBodyPart(CreaturePart.LeftFoot, Player);
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

        private void LoadItemParts()
        {
            if (DoesNotHaveItemEquipped)
                return;

            int[] partIds;
            int selectedPartId;
            var item = GetItem();

            if (SelectedItemTypeIndex == 0) // 0 = Armor
            {
                switch (SelectedPartCategoryIndex)
                {
                    case 0: // Neck
                        partIds = _armorAppearances.Neck;
                        selectedPartId = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Neck);
                        break;
                    case 1: // Torso
                        partIds = _armorAppearances.Torso;
                        selectedPartId = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Torso);
                        break;
                    case 2: // Belt
                        partIds = _armorAppearances.Belt;
                        selectedPartId = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Belt);
                        break;
                    case 3: // Pelvis
                        partIds = _armorAppearances.Pelvis;
                        selectedPartId = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Pelvis);
                        break;
                    case 4: // Right Shoulder
                        partIds = _armorAppearances.Shoulder;
                        selectedPartId = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightShoulder);
                        break;
                    case 5: // Right Bicep
                        partIds = _armorAppearances.Bicep;
                        selectedPartId = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightBicep);
                        break;
                    case 6: // Right Forearm
                        partIds = _armorAppearances.Forearm;
                        selectedPartId = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightForearm);
                        break;
                    case 7: // Right Hand
                        partIds = _armorAppearances.Hand;
                        selectedPartId = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightHand);
                        break;
                    case 8: // Right Thigh
                        partIds = _armorAppearances.Thigh;
                        selectedPartId = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightThigh);
                        break;
                    case 9: // Right Shin
                        partIds = _armorAppearances.Shin;
                        selectedPartId = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightShin);
                        break;
                    case 10: // Right Foot
                        partIds = _armorAppearances.Foot;
                        selectedPartId = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightFoot);
                        break;
                    case 11: // Left Shoulder
                        partIds = _armorAppearances.Shoulder;
                        selectedPartId = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftShoulder);
                        break;
                    case 12: // Left Bicep
                        partIds = _armorAppearances.Bicep;
                        selectedPartId = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftBicep);
                        break;
                    case 13: // Left Forearm
                        partIds = _armorAppearances.Forearm;
                        selectedPartId = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftForearm);
                        break;
                    case 14: // Left Hand
                        partIds = _armorAppearances.Hand;
                        selectedPartId = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftHand);
                        break;
                    case 15: // Left Thigh
                        partIds = _armorAppearances.Thigh;
                        selectedPartId = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftThigh);
                        break;
                    case 16: // Left Shin
                        partIds = _armorAppearances.Shin;
                        selectedPartId = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftShin);
                        break;
                    case 17: // Left Foot
                        partIds = _armorAppearances.Foot;
                        selectedPartId = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftFoot);
                        break;
                    case 18: // Robe
                        partIds = _armorAppearances.Robe;
                        selectedPartId = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Robe);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(SelectedPartIndex));
                }
            }
            else if (SelectedItemTypeIndex == 1) // 1 = Helmet
            {
                partIds = _armorAppearances.Helmet;
                selectedPartId = GetItemAppearance(item, ItemAppearanceType.SimpleModel, -1);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(SelectedItemTypeIndex));
            }

            if (selectedPartId <= -1)
                return;

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
            ToggleItemEquippedFlags();

            LoadColorCategoryOptions();
            LoadPartCategoryOptions();
            SelectedColorCategoryIndex = 0;
        };

        public Action OnSelectEquipment() => () =>
        {
            IsAppearanceSelected = false;
            IsEquipmentSelected = true;
            ToggleItemEquippedFlags();

            LoadColorCategoryOptions();
            LoadPartCategoryOptions();
            LoadItemParts();
            SelectedColorCategoryIndex = 0;
        };
        public Action OnDecreaseAppearanceScale() => () =>
        {
            var scale = GetObjectVisualTransform(Player, ObjectVisualTransform.Scale);
            const float Increment = 0.01f;
            const float MinimumScale = 0.85f;
            
            if (scale - Increment < MinimumScale)
            {
                SendMessageToPC(Player, "You cannot decrease your height any further.");
            }
            else
            {
                SetObjectVisualTransform(Player, ObjectVisualTransform.Scale, scale - Increment);
            }
        };
        public Action OnIncreaseAppearanceScale() => () =>
        {
            var scale = GetObjectVisualTransform(Player, ObjectVisualTransform.Scale);
            const float Increment = 0.01f;
            const float MaximumScale = 1.15f;

            if (scale + Increment > MaximumScale)
            {
                SendMessageToPC(Player, "You cannot increase your height any further.");
            }
            else
            {
                SetObjectVisualTransform(Player, ObjectVisualTransform.Scale, scale + Increment);
            }
        };

        public Action OnSaveAppearanceScale() => () =>
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);

            dbPlayer.AppearanceScale = GetObjectVisualTransform(Player, ObjectVisualTransform.Scale);

            DB.Set(dbPlayer);

            SendMessageToPC(Player, "Height saved successfully.");
        };

        public Action OnSelectColorCategory() => () =>
        {
            ToggleItemEquippedFlags();
            if (DoesNotHaveItemEquipped)
                return;

            var index = NuiGetEventArrayIndex();
            ColorCategorySelected[SelectedColorCategoryIndex] = false;

            SelectedColorCategoryIndex = index;
            ColorCategorySelected[index] = true;
        };

        public Action OnSelectPartCategory() => () =>
        {
            ToggleItemEquippedFlags();
            if (DoesNotHaveItemEquipped)
                return;

            var index = NuiGetEventArrayIndex();
            PartCategorySelected[SelectedPartCategoryIndex] = false;

            SelectedPartCategoryIndex = index;
            PartCategorySelected[index] = true;

            if (IsAppearanceSelected)
            {
                LoadBodyParts();
            }
            else if (IsEquipmentSelected)
            {
                LoadItemParts();
            }
        };

        private void ModifyItemColor(AppearanceArmorColor colorChannel, int colorId)
        {
            ToggleItemEquippedFlags();
            if (DoesNotHaveItemEquipped)
                return;

            var slot = SelectedItemTypeIndex == 0
                ? InventorySlot.Chest
                : InventorySlot.Head;
            var item = GetItem();
            var copy = CopyItemAndModify(item, ItemAppearanceType.ArmorColor, (int)colorChannel, colorId, true);
            DestroyObject(item);
            AssignCommand(Player, () =>
            {
                ClearAllActions();
                ActionEquipItem(copy, slot);
            });
        }

        private void ModifyItemPart(AppearanceArmor part, int partId)
        {
            ToggleItemEquippedFlags();
            if (DoesNotHaveItemEquipped)
                return;

            var slot = SelectedItemTypeIndex == 0
                ? InventorySlot.Chest
                : InventorySlot.Head;
            var item = GetItem();
            var modelType = SelectedItemTypeIndex == 0
                ? ItemAppearanceType.ArmorModel
                : ItemAppearanceType.SimpleModel;
            var copy = CopyItemAndModify(item, modelType, (int) part, partId);
            DestroyObject(item);
            AssignCommand(Player, () =>
            {
                ClearAllActions();
                ActionEquipItem(copy, slot);
            });
        }

        public Action OnSelectColor() => () =>
        {
            ToggleItemEquippedFlags();
            if (DoesNotHaveItemEquipped)
                return;

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
                switch (SelectedColorCategoryIndex)
                {
                    case 0: // 0 = Skin
                        SetColor(Player, ColorChannel.Skin, colorId);
                        break;
                    case 1: //  1 = Hair
                        SetColor(Player, ColorChannel.Hair, colorId);
                        break;
                    case 2: // 2 = Tattoo 1
                        SetColor(Player, ColorChannel.Tattoo1, colorId);
                        break;
                    case 3: // 3 = Tattoo 2
                        SetColor(Player, ColorChannel.Tattoo2, colorId);
                        break;
                }
            }
            // Armor - Cloth 1, Cloth 2, Leather 1, Leather 2, Metal 1, Metal 2
            else if (IsEquipmentSelected)
            {
                switch (SelectedColorCategoryIndex)
                {
                    case 0: // 0 = Leather 1
                        ModifyItemColor(AppearanceArmorColor.Leather1, colorId);
                        break;
                    case 1: // 1 = Leather 2
                        ModifyItemColor(AppearanceArmorColor.Leather2, colorId);
                        break;
                    case 2: // 2 = Cloth 1
                        ModifyItemColor(AppearanceArmorColor.Cloth1, colorId);
                        break;
                    case 3: // 3 = Cloth 2
                        ModifyItemColor(AppearanceArmorColor.Cloth2, colorId);
                        break;
                    case 4: // 4 = Metal 1
                        ModifyItemColor(AppearanceArmorColor.Metal1, colorId);
                        break;
                    case 5: // 5 = Metal 2
                        ModifyItemColor(AppearanceArmorColor.Metal2, colorId);
                        break;
                }
            }
        };

        private void LoadBodyPart()
        {
            var race = GetRacialType(Player);
            var gender = GetGender(Player);
            var appearance = _racialAppearances[race];

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
                case 8: // Right Foot
                    SetCreatureBodyPart(CreaturePart.RightFoot, appearance.RightFoot[SelectedPartIndex], Player);
                    break;
                case 9: // Left Bicep
                    SetCreatureBodyPart(CreaturePart.LeftBicep, appearance.LeftBicep[SelectedPartIndex], Player);
                    break;
                case 10: // Left Forearm
                    SetCreatureBodyPart(CreaturePart.LeftForearm, appearance.LeftForearm[SelectedPartIndex], Player);
                    break;
                case 11: // Left Hand
                    SetCreatureBodyPart(CreaturePart.LeftHand, appearance.LeftHand[SelectedPartIndex], Player);
                    break;
                case 12: // Left Thigh
                    SetCreatureBodyPart(CreaturePart.LeftThigh, appearance.LeftThigh[SelectedPartIndex], Player);
                    break;
                case 13: // Left Shin
                    SetCreatureBodyPart(CreaturePart.LeftShin, appearance.LeftShin[SelectedPartIndex], Player);
                    break;
                case 14: // Left Foot
                    SetCreatureBodyPart(CreaturePart.LeftFoot, appearance.LeftFoot[SelectedPartIndex], Player);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(SelectedPartIndex));
            }
        }

        private void LoadArmorPart()
        {
            ToggleItemEquippedFlags();
            if (DoesNotHaveItemEquipped)
                return;

            if (SelectedItemTypeIndex == 0) // 0 = Armor
            {
                switch (SelectedPartCategoryIndex)
                {
                    case 0: // Neck
                        ModifyItemPart(AppearanceArmor.Neck, _armorAppearances.Neck[SelectedPartIndex]);
                        break;
                    case 1: // Torso
                        ModifyItemPart(AppearanceArmor.Torso, _armorAppearances.Torso[SelectedPartIndex]);
                        break;
                    case 2: // Belt
                        ModifyItemPart(AppearanceArmor.Belt, _armorAppearances.Belt[SelectedPartIndex]);
                        break;
                    case 3: // Pelvis
                        ModifyItemPart(AppearanceArmor.Pelvis, _armorAppearances.Pelvis[SelectedPartIndex]);
                        break;
                    case 4: // Right Shoulder
                        ModifyItemPart(AppearanceArmor.RightShoulder, _armorAppearances.Shoulder[SelectedPartIndex]);
                        break;
                    case 5: // Right Bicep
                        ModifyItemPart(AppearanceArmor.RightBicep, _armorAppearances.Bicep[SelectedPartIndex]);
                        break;
                    case 6: // Right Forearm
                        ModifyItemPart(AppearanceArmor.RightForearm, _armorAppearances.Forearm[SelectedPartIndex]);
                        break;
                    case 7: // Right Hand
                        ModifyItemPart(AppearanceArmor.RightHand, _armorAppearances.Hand[SelectedPartIndex]);
                        break;
                    case 8: // Right Thigh
                        ModifyItemPart(AppearanceArmor.RightThigh, _armorAppearances.Thigh[SelectedPartIndex]);
                        break;
                    case 9: // Right Shin
                        ModifyItemPart(AppearanceArmor.RightShin, _armorAppearances.Shin[SelectedPartIndex]);
                        break;
                    case 10: // Right Foot
                        ModifyItemPart(AppearanceArmor.RightFoot, _armorAppearances.Foot[SelectedPartIndex]);
                        break;
                    case 11: // Left Shoulder
                        ModifyItemPart(AppearanceArmor.LeftShoulder, _armorAppearances.Shoulder[SelectedPartIndex]);
                        break;
                    case 12: // Left Bicep
                        ModifyItemPart(AppearanceArmor.LeftBicep, _armorAppearances.Bicep[SelectedPartIndex]);
                        break;
                    case 13: // Left Forearm
                        ModifyItemPart(AppearanceArmor.LeftForearm, _armorAppearances.Forearm[SelectedPartIndex]);
                        break;
                    case 14: // Left Hand
                        ModifyItemPart(AppearanceArmor.LeftHand, _armorAppearances.Hand[SelectedPartIndex]);
                        break;
                    case 15: // Left Thigh
                        ModifyItemPart(AppearanceArmor.LeftThigh, _armorAppearances.Thigh[SelectedPartIndex]);
                        break;
                    case 16: // Left Shin
                        ModifyItemPart(AppearanceArmor.LeftShin, _armorAppearances.Shin[SelectedPartIndex]);
                        break;
                    case 17: // Left Foot
                        ModifyItemPart(AppearanceArmor.LeftFoot, _armorAppearances.Foot[SelectedPartIndex]);
                        break;
                    case 18: // Robe
                        ModifyItemPart(AppearanceArmor.Robe, _armorAppearances.Robe[SelectedPartIndex]);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(SelectedPartIndex));
                }
            }
            else if (SelectedItemTypeIndex == 1) // 1 = Helmet
            {
                ModifyItemPart(AppearanceArmor.Invalid, _armorAppearances.Helmet[SelectedPartIndex]);
            }
        }

        private void LoadPart()
        {
            if (SelectedPartIndex <= -1)
                return;

            if (IsAppearanceSelected)
            {
                LoadBodyPart();
            }
            else if (IsEquipmentSelected)
            {
                LoadArmorPart();
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

        public Action OnClickOutfits() => () =>
        {
            Gui.TogglePlayerWindow(Player, GuiWindowType.Outfits);
        };

        public Action OnCloseWindow() => () =>
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);

            SetObjectVisualTransform(Player, ObjectVisualTransform.Scale, dbPlayer.AppearanceScale);
        };
    }
}
