using System;
using System.Linq;
using System.Text.RegularExpressions;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class ChangePortraitViewModel: GuiViewModelBase<ChangePortraitViewModel>
    {
        public string ActivePortrait
        {
            get => Get<string>();
            set => Set(value);
        }

        public int ActivePortraitInternalId
        {
            get => Get<int>();
            set
            {
                Set(value);
                ActivePortrait = Cache.GetPortraitResrefByInternalId(value) + "l";
            }
        }

        public int MaximumPortraits
        {
            get => Get<int>();
            set => Set(value);
        }

        private void LoadCurrentPortrait()
        {
            var portraitId = GetPortraitId(Player);
            if (portraitId == PORTRAIT_INVALID)
            {
                portraitId = Cache.GetPortraitInternalId(1);
            }

            ActivePortraitInternalId = Cache.GetPortraitInternalId(portraitId);
        }

        public Action OnLoadWindow() => () =>
        {
            MaximumPortraits = Cache.PortraitCount;
            LoadCurrentPortrait();

            WatchOnClient(model => model.ActivePortraitInternalId);
        };

        public Action OnPreviousClick() => () =>
        {
            var newId = ActivePortraitInternalId - 1;

            if (newId < 1)
                newId = Cache.PortraitCount;

            ActivePortraitInternalId = newId;
        };

        public Action OnNextClick() => () =>
        {
            var newId = ActivePortraitInternalId + 1;

            if (newId > Cache.PortraitCount)
                newId = 1;

            ActivePortraitInternalId = newId;
        };

        public Action OnRevertClick() => () =>
        {
            LoadCurrentPortrait();
        };

        public Action OnSaveClick() => () =>
        {
            var portraitId = Cache.GetPortraitByInternalId(ActivePortraitInternalId);
            SetPortraitId(Player, portraitId);
        };
    }
}
