using System;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class ChangePortraitViewModel: GuiViewModelBase<ChangePortraitViewModel, GuiPayloadBase>
    {
        public string ActivePortrait
        {
            get => Get<string>();
            set => Set(value);
        }

        private int _activePortraitInternalId;

        public string ActivePortraitInternalId
        {
            get => Get<string>();
            set
            {
                if (int.TryParse(value, out var parsed))
                {
                    if (parsed > Cache.PortraitCount)
                        parsed = Cache.PortraitCount;
                    else if (parsed < 1)
                        parsed = 1;

                    _activePortraitInternalId = parsed;

                    Set(parsed.ToString());
                    ActivePortrait = Cache.GetPortraitResrefByInternalId(_activePortraitInternalId) + "l";
                }
                else
                {
                    Set(_activePortraitInternalId.ToString());
                    ActivePortrait = Cache.GetPortraitResrefByInternalId(_activePortraitInternalId) + "l";
                }
            }
        }

        public int MaximumPortraits
        {
            get => Get<int>();
            set => Set(value);
        }

        public string MaxPortraitsText
        {
            get => Get<string>();
            set => Set(value);
        }

        private void LoadCurrentPortrait()
        {
            var resref = GetPortraitResRef(Player);
            var internalId = Cache.GetPortraitInternalIdByResref(resref);
            var portraitId = internalId == -1
                ? Cache.GetPortraitInternalId(1)
                : Cache.GetPortraitByInternalId(internalId);

            ActivePortraitInternalId = Cache.GetPortraitInternalId(portraitId).ToString();
            ActivePortrait = Cache.GetPortraitResrefByInternalId(_activePortraitInternalId) + "l";
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            MaximumPortraits = Cache.PortraitCount;
            MaxPortraitsText = $"/ {MaximumPortraits}";
            
            LoadCurrentPortrait();
            WatchOnClient(model => model.ActivePortraitInternalId);
        }

        public Action OnPreviousClick() => () =>
        {
            var newId = _activePortraitInternalId - 1;

            if (newId < 1)
                newId = Cache.PortraitCount;

            ActivePortraitInternalId = newId.ToString();
            ActivePortrait = Cache.GetPortraitResrefByInternalId(_activePortraitInternalId) + "l";
        };

        public Action OnNextClick() => () =>
        {
            var newId = _activePortraitInternalId + 1;

            if (newId > Cache.PortraitCount)
                newId = 1;

            ActivePortraitInternalId = newId.ToString();
            ActivePortrait = Cache.GetPortraitResrefByInternalId(_activePortraitInternalId) + "l";
        };

        public Action OnRevertClick() => () =>
        {
            LoadCurrentPortrait();
        };

        public Action OnSaveClick() => () =>
        {
            var portraitId = Cache.GetPortraitByInternalId(_activePortraitInternalId);
            SetPortraitId(Player, portraitId);
            Gui.PublishRefreshEvent(Player, new ChangePortraitRefreshEvent());
        };

    }
}
