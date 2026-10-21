using System.Globalization;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class DMBattleGridViewModel : GuiViewModelBase<DMBattleGridViewModel, GuiPayloadBase>
    {
        public const string MainContentElement = "dmbattlegrid_content";
        public const string MainContentPartial = "DMBATTLEGRID_MAIN";

        public string StatusText
        {
            get => Get<string>();
            set => Set(value);
        }

        public string SquareSizeText
        {
            get => Get<string>();
            set => Set(value);
        }

        public string WidthText
        {
            get => Get<string>();
            set => Set(value);
        }

        public string LengthText
        {
            get => Get<string>();
            set => Set(value);
        }

        public string HeightText
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsPlaced
        {
            get => Get<bool>();
            set => Set(value);
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            Geometry = DMToolbarViewModel.PlaceBelowToolbar(Player, Geometry);
            Refresh();
            ChangePartialView(MainContentElement, MainContentPartial);
        }

        /// <summary>
        /// The grid belongs to the area of whatever the DM controls, which can be a possessed creature.
        /// </summary>
        private uint GetGridArea()
        {
            var controlled = DMPresence.GetControlledCreature(Player);
            if (!GetIsDM(controlled) && !GetIsDMPossessed(controlled))
                return OBJECT_INVALID;

            return GetArea(controlled);
        }

        private void Refresh()
        {
            var area = GetGridArea();
            var settings = GetIsObjectValid(area)
                ? BattleGrid.Clamp(BattleGrid.GetSettings(area))
                : BattleGrid.DefaultSettings;
            IsPlaced = GetIsObjectValid(area) && BattleGrid.IsPlaced(area);

            var squaresWide = settings.SectionsWide * BattleGrid.SquaresPerSection;
            var squaresLong = settings.SectionsLong * BattleGrid.SquaresPerSection;
            StatusText = IsPlaced
                ? $"Grid shown: {squaresWide} x {squaresLong} squares."
                : "No grid in this area.";
            SquareSizeText = $"{FormatMeters(settings.SquareSize)} per square";
            WidthText = $"{squaresWide} squares ({FormatMeters(squaresWide * settings.SquareSize)})";
            LengthText = $"{squaresLong} squares ({FormatMeters(squaresLong * settings.SquareSize)})";
            HeightText = $"{(settings.Height >= 0f ? "+" : string.Empty)}{FormatMeters(settings.Height)}";
        }

        private static string FormatMeters(float meters)
        {
            return meters.ToString("0.0", CultureInfo.InvariantCulture) + " m";
        }

        private void ChangeSettings(Func<BattleGridSettings, BattleGridSettings> change)
        {
            var area = GetGridArea();
            if (!GetIsObjectValid(area))
                return;

            BattleGrid.SetSettings(area, change(BattleGrid.Clamp(BattleGrid.GetSettings(area))));
            Refresh();
        }

        private void Move(int squaresEast, int squaresNorth)
        {
            var area = GetGridArea();
            if (!GetIsObjectValid(area))
                return;

            BattleGrid.Move(area, squaresEast, squaresNorth);
            Refresh();
        }

        public Action OnPlaceAtTarget() => () =>
        {
            var controlled = DMPresence.GetControlledCreature(Player);
            if (!GetIsObjectValid(GetGridArea()))
                return;

            Targeting.EnterTargetingMode(controlled, ObjectType.All, "Click where to center the battle grid.", target =>
            {
                var area = GetArea(controlled);
                var position = GetTargetingModeSelectedPosition();
                if (position == Vector3() && GetIsObjectValid(target) && target != area)
                    position = GetPosition(target);

                if (position == Vector3())
                    return;

                BattleGrid.Place(area, position);
                Refresh();
            }, true);
        };

        public Action OnPlaceHere() => () =>
        {
            var area = GetGridArea();
            if (!GetIsObjectValid(area))
                return;

            BattleGrid.Place(area, GetPosition(DMPresence.GetControlledCreature(Player)));
            Refresh();
        };

        public Action OnRemove() => () =>
        {
            var area = GetGridArea();
            if (!GetIsObjectValid(area))
                return;

            BattleGrid.Remove(area);
            Refresh();
        };

        public Action OnSquareSizeDown() => () =>
            ChangeSettings(settings => settings with { SquareSize = settings.SquareSize - BattleGrid.SquareSizeStep });

        public Action OnSquareSizeUp() => () =>
            ChangeSettings(settings => settings with { SquareSize = settings.SquareSize + BattleGrid.SquareSizeStep });

        public Action OnWidthDown() => () =>
            ChangeSettings(settings => settings with { SectionsWide = settings.SectionsWide - 1 });

        public Action OnWidthUp() => () =>
            ChangeSettings(settings => settings with { SectionsWide = settings.SectionsWide + 1 });

        public Action OnLengthDown() => () =>
            ChangeSettings(settings => settings with { SectionsLong = settings.SectionsLong - 1 });

        public Action OnLengthUp() => () =>
            ChangeSettings(settings => settings with { SectionsLong = settings.SectionsLong + 1 });

        public Action OnHeightDown() => () =>
            ChangeSettings(settings => settings with { Height = settings.Height - BattleGrid.HeightStep });

        public Action OnHeightUp() => () =>
            ChangeSettings(settings => settings with { Height = settings.Height + BattleGrid.HeightStep });

        public Action OnMoveNorth() => () => Move(0, 1);

        public Action OnMoveSouth() => () => Move(0, -1);

        public Action OnMoveEast() => () => Move(1, 0);

        public Action OnMoveWest() => () => Move(-1, 0);
    }
}
