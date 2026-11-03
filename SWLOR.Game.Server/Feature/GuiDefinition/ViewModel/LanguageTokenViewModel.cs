using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CurrencyService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class LanguageTokenViewModel : GuiViewModelBase<LanguageTokenViewModel, GuiPayloadBase>,
        IGuiRefreshable<CurrencyRefreshEvent>
    {
        public const string ContentElement = "language_token_content";
        public const string MainPartial = "LANGUAGE_TOKEN_MAIN";

        private readonly List<SkillType> _languages = new();

        public string TokenCountText { get => Get<string>(); set => Set(value); }
        public GuiBindingList<string> LanguageNames { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> LanguageDescriptions { get => Get<GuiBindingList<string>>(); set => Set(value); }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            TokenCountText = string.Empty;
            LanguageNames = new GuiBindingList<string>();
            LanguageDescriptions = new GuiBindingList<string>();

            ChangePartialView(ContentElement, MainPartial);
            LoadData();
        }

        public void Refresh(CurrencyRefreshEvent payload)
        {
            LoadData();
        }

        private void LoadData()
        {
            var dbPlayer = DB.Get<Player>(GetObjectUUID(Player));
            dbPlayer.Currencies.TryGetValue(CurrencyType.LanguageToken, out var tokens);

            _languages.Clear();
            _languages.AddRange(Language.GetUnmasteredLanguages(dbPlayer));

            var names = new GuiBindingList<string>();
            var descriptions = new GuiBindingList<string>();
            foreach (var language in _languages)
            {
                var detail = Skill.GetSkillDetails(language);
                names.Add(detail.Name);
                descriptions.Add(detail.Description);
            }

            TokenCountText = $"Language Tokens: {tokens}";
            LanguageNames = names;
            LanguageDescriptions = descriptions;
        }

        public Action OnClickLanguage() => () =>
        {
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= _languages.Count)
                return;

            var language = _languages[index];
            var name = Skill.GetSkillDetails(language).Name;

            ShowModal($"Spend 1 Language Token to master {name}?", () =>
            {
                var dbPlayer = DB.Get<Player>(GetObjectUUID(Player));
                if (!Language.TryMasterWithToken(dbPlayer, language))
                {
                    FloatingTextStringOnCreature("You cannot master that language right now.", Player, false);
                    LoadData();
                    return;
                }

                DB.Set(dbPlayer);
                FloatingTextStringOnCreature($"You have mastered {name}.", Player, false);
                Gui.PublishRefreshEvent(Player, new SkillXPRefreshEvent(new List<SkillType> { language }));
                Gui.PublishRefreshEvent(Player, new CurrencyRefreshEvent());

                if (Currency.GetCurrency(Player, CurrencyType.LanguageToken) <= 0)
                    Gui.ClosePlayerWindow(Player, GuiWindowType.LanguageToken);
            });
        };
    }
}
