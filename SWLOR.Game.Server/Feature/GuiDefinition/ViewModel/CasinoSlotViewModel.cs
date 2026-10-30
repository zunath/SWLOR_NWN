using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class CasinoSlotViewModel : GuiViewModelBase<CasinoSlotViewModel, CasinoSlotPayload>
    {
        public const string BodyElement = "casino_slot_body";
        public const string BodyPartial = "CASINO_SLOT_BODY";
        private uint _machine;
        private int _animationVersion;
        private bool _isAnimating;

        public string LeftReel { get => Get<string>(); set => Set(value); }
        public string MiddleReel { get => Get<string>(); set => Set(value); }
        public string RightReel { get => Get<string>(); set => Set(value); }
        public string HostImage { get => Get<string>(); set => Set(value); }
        public string Headline { get => Get<string>(); set => Set(value); }
        public string ResultText { get => Get<string>(); set => Set(value); }
        public string CreditsText { get => Get<string>(); set => Set(value); }
        public string PlayText { get => Get<string>(); set => Set(value); }
        public bool CanSpin { get => Get<bool>(); set => Set(value); }
        public GuiColor ResultColor { get => Get<GuiColor>(); set => Set(value); }

        protected override void Initialize(CasinoSlotPayload initialPayload)
        {
            _machine = initialPayload?.Machine ?? OBJECT_INVALID;
            _animationVersion++;
            _isAnimating = false;
            LeftReel = CasinoSlotPresentation.HuttImage;
            MiddleReel = CasinoSlotPresentation.StarImage;
            RightReel = CasinoSlotPresentation.CreditImage;
            HostImage = CasinoSlotPresentation.HuttImage;
            Headline = "SMUGGLER'S MOON SLOTS";
            ResultText = "Your Hutt host is feeling generous. Spin for 100 credits.";
            ResultColor = new GuiColor(245, 200, 75);
            RefreshStatus();
            ChangePartialView(BodyElement, BodyPartial);
        }

        private void RefreshStatus()
        {
            PlayText = CasinoSlot.GetPlayStatus(Player, out var canPlay);
            CreditsText = $"CREDITS: {GetGold(Player):N0}     STAKE: 100     MAX PAYOUT: 10,000";
            CanSpin = canPlay && !_isAnimating && GetIsObjectValid(_machine);
        }

        public Action OnClickRefresh() => () => RefreshStatus();
        public Action OnClickLeave() => () => Gui.CloseWindow(Player, GuiWindowType.CasinoSlot, Player);

        public Action OnClickSpin() => () =>
        {
            if (_isAnimating)
                return;
            var result = CasinoSlot.Spin(Player, _machine);
            if (!result.Succeeded)
            {
                ResultText = result.Message;
                ResultColor = new GuiColor(255, 160, 100);
                RefreshStatus();
                return;
            }

            _isAnimating = true;
            var version = ++_animationVersion;
            Headline = "SPINNING...";
            ResultText = "The reels are turning...";
            HostImage = CasinoSlotPresentation.HuttImage;
            ResultColor = new GuiColor(245, 200, 75);
            RefreshStatus();
            AnimateReels(result, version, 0);
        };

        private bool IsCurrentAnimation(int version) =>
            version == _animationVersion && GetIsObjectValid(Player) &&
            Gui.IsWindowOpen(Player, GuiWindowType.CasinoSlot);

        private void AnimateReels(CasinoSpinResult result, int version, int frame)
        {
            if (!IsCurrentAnimation(version))
                return;
            var reels = CasinoSlotPresentation.GetReels(result.Payout);
            var symbols = new[] { CasinoSlotPresentation.CreditImage, CasinoSlotPresentation.StarImage, CasinoSlotPresentation.HuttImage };
            LeftReel = frame >= 4 ? reels[0] : symbols[frame % 3];
            MiddleReel = frame >= 6 ? reels[1] : symbols[(frame + 1) % 3];
            RightReel = frame >= 8 ? reels[2] : symbols[(frame + 2) % 3];
            if (frame < 8)
            {
                DelayCommand(0.15f, () => AnimateReels(result, version, frame + 1));
                return;
            }

            Headline = CasinoSlotPresentation.GetHeadline(result.Payout);
            ResultText = result.Message;
            ResultColor = result.Payout > CasinoSlot.SpinCost
                ? new GuiColor(100, 240, 150) : new GuiColor(245, 200, 75);
            _isAnimating = false;
            RefreshStatus();
            if (CasinoSlotPresentation.IsBigWin(result.Payout))
                AnimateCelebration(version, 0);
        }

        private void AnimateCelebration(int version, int frame)
        {
            if (!IsCurrentAnimation(version))
                return;
            HostImage = frame % 2 == 0 || frame == 8
                ? CasinoSlotPresentation.BigWinImage : CasinoSlotPresentation.HuttImage;
            if (frame < 8)
                DelayCommand(0.25f, () => AnimateCelebration(version, frame + 1));
        }

        public override Action OnWindowClosed() => () =>
        {
            _animationVersion++;
            _isAnimating = false;
        };
    }
}
