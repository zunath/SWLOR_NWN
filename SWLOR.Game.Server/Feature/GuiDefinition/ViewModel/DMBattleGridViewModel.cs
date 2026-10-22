using System.Globalization;
using System.Numerics;
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
        /// The grid belongs to the area of whatever the user controls: a DM's possessed creature, or the player
        /// object of a staff member using /battlegrid. Only DM tools and that command open this window.
        /// </summary>
        private uint GetGridArea()
        {
            return GetArea(DMPresence.GetControlledCreature(Player));
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
            var sections = settings.SectionsWide * settings.SectionsLong;
            var drawn = IsPlaced ? BattleGrid.GetDrawnSections(area) : 0;
            StatusText = !IsPlaced
                ? "No grid in this area."
                : drawn < sections
                    ? $"Grid shown: {drawn} of {sections} sections fit on the map."
                    : $"Grid shown: {squaresWide} x {squaresLong} squares.";
            SquareSizeText = $"{FormatMeters(settings.SquareSize)} per square";
            WidthText = $"{squaresWide} squares ({FormatMeters(squaresWide * settings.SquareSize)})";
            LengthText = $"{squaresLong} squares ({FormatMeters(squaresLong * settings.SquareSize)})";
            HeightText = $"{(settings.Height >= 0f ? "+" : string.Empty)}{FormatMeters(settings.Height)}";
        }

        private void ReportPlacement(int drawn)
        {
            Refresh();
            if (drawn <= 0)
                DMPresence.SendMessage(Player, ColorToken.Red("The grid could not be drawn there. Pick a spot inside the map."));
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
            if (!IsPlaced)
                DMPresence.SendMessage(Player, ColorToken.Red("The grid moved off the map and was removed. Place it again."));
        }

        public Action OnPlaceAtTarget() => () =>
        {
            var controlled = DMPresence.GetControlledCreature(Player);
            if (!GetIsObjectValid(GetGridArea()))
                return;

            Targeting.EnterTargetingMode(controlled, ObjectType.All, "Click where to center the battle grid.", target =>
            {
                // A ground pick arrives as the area object, so this is an object-only targeting request: Targeting's
                // location fallback, which ignores a (0, 0, 0) position, is never needed, and a cancelled pick never
                // calls back. A clicked object centers the grid on the object, but only one standing in this area:
                // inventory items and objects elsewhere have no position here.
                var area = GetArea(controlled);
                Vector3 position;
                if (target == area)
                    position = GetTargetingModeSelectedPosition();
                else if (GetIsObjectValid(target) && GetArea(target) == area)
                    position = GetPosition(target);
                else
                {
                    DMPresence.SendMessage(Player, ColorToken.Red("Pick the ground or an object in this area to center the grid."));
                    return;
                }

                ReportPlacement(BattleGrid.Place(area, position));
            }, false);
        };

        public Action OnPlaceHere() => () =>
        {
            var area = GetGridArea();
            if (!GetIsObjectValid(area))
                return;

            ReportPlacement(BattleGrid.Place(area, GetPosition(DMPresence.GetControlledCreature(Player))));
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
