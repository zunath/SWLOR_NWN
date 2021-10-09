using System;
using SWLOR.Game.Server.Service.GuiService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class AppearanceEditorViewModel: GuiViewModelBase<AppearanceEditorViewModel>
    {
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

        public int SelectedPartIndex
        {
            get => Get<int>();
            set
            {
                Set(value);

                if (value == 1) // 1 = Skin Color
                {
                    ColorSheetResref = "gui_pal_skin";
                    IsColorSheetPartSelected = true;
                }
                else if (value == 2) // 2 = Hair Color
                {
                    ColorSheetResref = "gui_pal_hair01";
                    IsColorSheetPartSelected = true;
                }
                else if (value == 3) // 3 = Tattoo Color
                {
                    ColorSheetResref = "gui_pal_tattoo";
                    IsColorSheetPartSelected = true;
                }
                else
                {
                    IsColorSheetPartSelected = false;
                }
            }
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

        public Action OnLoadWindow() => () =>
        {
            IsAppearanceSelected = true;
            IsEquipmentSelected = false;
            IsOutfitsSelected = false;
            SelectedPartIndex = 1;

            WatchOnClient(model => model.SelectedPartIndex);
        };

        public Action OnCloseWindow() => () =>
        {

        };

        public Action OnSelectAppearance() => () =>
        {
            IsAppearanceSelected = true;
            IsEquipmentSelected = false;
            IsOutfitsSelected = false;

            SelectedPartIndex = 1;
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

        public Action OnSelectColor() => () =>
        {
            var payload = NuiGetEventPayload();
            var x = JsonObjectGet(payload, "x");
            var y = JsonObjectGet(payload, "y");

            Console.WriteLine(JsonDump(payload));
        };

        public Action OnApplyChanges() => () =>
        {

        };

        public Action OnCancelChanges() => () =>
        {

        };
    }
}
