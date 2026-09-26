using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.PlayerIntroductionService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class IntroductionsViewModel : GuiViewModelBase<IntroductionsViewModel, GuiPayloadBase>
    {
        public const string ContentElement = "introductions_content";
        public const string MainContentPartial = "INTRODUCTIONS_MAIN";
        private readonly List<(PlayerIntroductionOffer Offer, string KnownName)> _rows = new();

        public GuiBindingList<string> OfferDescriptions { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> RememberLabels { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> DismissLabels { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public string StatusText { get => Get<string>(); set => Set(value); }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            RefreshOffers();
            ChangePartialView(ContentElement, MainContentPartial);
        }

        protected override void OnModalClosedRestore() => ChangePartialView(ContentElement, MainContentPartial);

        public override Action OnWindowClosed() => () => _rows.Clear();

        public Action OnClickRefresh() => () => RefreshOffers();

        public Action OnClickRemember() => () =>
        {
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= _rows.Count)
                return;

            // Capture the displayed offer and label, not an index that a refresh could change.
            var (offer, knownName) = _rows[index];
            if (!string.IsNullOrEmpty(knownName) && knownName != offer.Name)
                ShowModal($"Replace '{knownName}' with '{offer.Name}'? This changes only your private label.",
                    () => Remember(offer, knownName), confirmText: "Replace", cancelText: "Keep label");
            else
                Remember(offer, knownName);
        };

        public Action OnClickDismiss() => () =>
        {
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= _rows.Count)
                return;

            PlayerIntroduction.Dismiss(Player, _rows[index].Offer.Id);
            RefreshOffers("Introduction dismissed. Your private labels are unchanged.");
        };

        private void Remember(PlayerIntroductionOffer offer, string knownName)
        {
            var error = PlayerIntroduction.Accept(Player, offer.Id, knownName);
            RefreshOffers(string.IsNullOrEmpty(error)
                ? $"Private label saved as '{offer.Name}'. Only you can see this label."
                : error);
        }

        private void RefreshOffers(string status = null)
        {
            var descriptions = new GuiBindingList<string>();
            var rememberLabels = new GuiBindingList<string>();
            var dismissLabels = new GuiBindingList<string>();
            _rows.Clear();

            foreach (var offer in PlayerIntroduction.GetPending(Player))
            {
                PlayerName.TryGetKnownName(Player, offer.Presenter, out var knownName);
                var currentLabel = string.IsNullOrEmpty(knownName) ? "Not yet remembered" : $"Your label: {knownName}";
                descriptions.Add($"{offer.DisplayName}\nIntroduced as: {offer.Name}\n{currentLabel}");
                rememberLabels.Add("Remember");
                dismissLabels.Add("Dismiss");
                _rows.Add((offer, knownName));
            }

            RememberLabels = rememberLabels;
            DismissLabels = dismissLabels;
            OfferDescriptions = descriptions;
            StatusText = status ?? (_rows.Count == 0 ? "No pending introductions." : "Choose which names you want to remember.");
        }
    }
}
