using System;
using System.Globalization;
using SWLOR.Game.Server.Feature.AppearanceDefinition;
using SWLOR.Game.Server.Feature.AppearanceDefinition.RacialAppearance;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public partial class AppearanceEditorViewModel
    {
        public string HeadSizeText { get => Get<string>(); set => Set(value); }
        public string HeadPositionXText { get => Get<string>(); set => Set(value); }
        public string HeadPositionYText { get => Get<string>(); set => Set(value); }
        public string HeadPositionZText { get => Get<string>(); set => Set(value); }

        private void RefreshHeadTransformSettings()
        {
            HeadSizeText = HelmetModelRenderer.GetHeadScale(_target).ToString("0.00", CultureInfo.InvariantCulture);
            HeadPositionXText = HeadPosition.Get(_target, ObjectVisualTransform.TranslateX).ToString("0.00", CultureInfo.InvariantCulture);
            HeadPositionYText = HeadPosition.Get(_target, ObjectVisualTransform.TranslateY).ToString("0.00", CultureInfo.InvariantCulture);
            HeadPositionZText = HeadPosition.Get(_target, ObjectVisualTransform.TranslateZ).ToString("0.00", CultureInfo.InvariantCulture);
        }

        public Action OnResetHead() => () =>
        {
            if (!RacialAppearanceRegistry.TryGet(GetAppearanceType(_target), out _))
            {
                CloseForUnsupportedAppearance();
                return;
            }

            HelmetModelRenderer.SetHeadScale(_target, 1f);
            HeadPosition.Set(_target, ObjectVisualTransform.TranslateX, 0f);
            HeadPosition.Set(_target, ObjectVisualTransform.TranslateY, 0f);
            HeadPosition.Set(_target, ObjectVisualTransform.TranslateZ, 0f);
            RefreshHeadTransformSettings();
        };

        public Action OnAdjustHeadPosition(ObjectVisualTransform axis, bool increase) => () =>
        {
            if (!RacialAppearanceRegistry.TryGet(GetAppearanceType(_target), out _))
            {
                CloseForUnsupportedAppearance();
                return;
            }

            var current = HeadPosition.Get(_target, axis);
            var next = HeadPosition.Normalize(current + (increase ? HeadPosition.Increment : -HeadPosition.Increment));
            if (current == next)
                SendScaleMessage("You cannot move your head any further in that direction.");
            else
                HeadPosition.Set(_target, axis, next);

            RefreshHeadTransformSettings();
        };
    }
}
