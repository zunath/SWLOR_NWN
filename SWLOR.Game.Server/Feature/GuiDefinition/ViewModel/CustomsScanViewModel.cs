using System;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class CustomsScanViewModel : GuiViewModelBase<CustomsScanViewModel, CustomsScanPayload>
    {
        public const string MainContentElement = "customsscan_content";
        public const string MainContentPartial = "CUSTOMSSCAN_MAIN";

        private string _scanId = string.Empty;

        public string PatrolText { get => Get<string>(); set => Set(value); }
        public string FoundText { get => Get<string>(); set => Set(value); }
        public string FineText { get => Get<string>(); set => Set(value); }
        public string DeadlineText { get => Get<string>(); set => Set(value); }
        public bool CanPay { get => Get<bool>(); set => Set(value); }
        public bool IsPending { get => Get<bool>(); set => Set(value); }
        public bool CanSlice { get => Get<bool>(); set => Set(value); }

        protected override void Initialize(CustomsScanPayload initialPayload)
        {
            _scanId = initialPayload?.ScanId ?? string.Empty;
            var scan = GetScan();
            if (scan == null)
            {
                PatrolText = "Customs Scan";
                FoundText = "The patrol has no further business with you.";
                FineText = string.Empty;
                DeadlineText = string.Empty;
                CanPay = false;
                IsPending = false;
                CanSlice = false;
            }
            else
            {
                var authority = CustomsRules.GetAuthorityName(scan.Authority);
                var seconds = Math.Max(0, (int)Math.Ceiling((scan.AnswerBy - DateTime.UtcNow).TotalSeconds));
                PatrolText = $"{authority} customs patrol";
                FoundText = $"The patrol's scan found goods the {authority} bans: {CustomsRules.Describe(scan.Goods)}. " +
                            "Pay the fine and surrender them, or run.";
                FineText = $"Fine: {scan.Fine} credits. You carry {GetGold(Player)}.";
                DeadlineText = $"Answer within {seconds} seconds. No answer counts as running.";
                CanPay = GetGold(Player) >= scan.Fine;
                IsPending = true;
                CanSlice = Trade.CanSliceCustomsScan(Player);
            }

            ChangePartialView(MainContentElement, MainContentPartial);
        }

        private CustomsScan GetScan()
        {
            var scan = Trade.GetCustomsScan(Player);
            return scan != null && scan.Id == _scanId ? scan : null;
        }

        public Action OnClickPay() => () =>
        {
            if (GetScan() != null)
                Trade.PayCustomsFine(Player);
        };

        public Action OnClickRun() => () =>
        {
            if (GetScan() != null)
                Trade.RunFromCustoms(Player, false);
        };
    }
}
