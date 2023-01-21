using System;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class ChangePortraitViewModel: GuiViewModelBase<ChangePortraitViewModel, ChangePortraitPayload>
    {
        private uint _target;

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
            var resref = GetPortraitResRef(_target);
            var internalId = Cache.GetPortraitInternalIdByResref(resref);
            var portraitId = internalId == -1
                ? Cache.GetPortraitInternalId(1)
                : Cache.GetPortraitByInternalId(internalId);

            ActivePortraitInternalId = Cache.GetPortraitInternalId(portraitId).ToString();
            ActivePortrait = Cache.GetPortraitResrefByInternalId(_activePortraitInternalId) + "l";
        }

        protected override void Initialize(ChangePortraitPayload initialPayload)
        {
            _target = GetIsObjectValid(initialPayload.Target) ? initialPayload.Target : Player;

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
            SetPortraitId(_target, portraitId);

            if (Droid.IsDroid(_target))
            {
                var controller = Droid.GetControllerItem(_target);
                var constructedDroid = Droid.LoadConstructedDroid(controller);

                constructedDroid.PortraitId = portraitId;

                Droid.SaveConstructedDroid(controller, constructedDroid);
            }
            else if (BeastMastery.IsPlayerBeast(_target))
            {
                var beastId = BeastMastery.GetBeastId(_target);
                var dbBeast = DB.Get<Beast>(beastId);

                dbBeast.PortraitId = portraitId;
                DB.Set(dbBeast);
            }


            Gui.PublishRefreshEvent(Player, new ChangePortraitRefreshEvent());
        };

    }
}
