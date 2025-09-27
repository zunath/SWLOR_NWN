using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class CustomizeCharacterViewModel: GuiViewModelBase<CustomizeCharacterViewModel, CustomizeCharacterPayload>
    {
        private uint _target;

        public const string PartialElement = "PARTIAL_VIEW";
        public const string PortraitPartial = "PORTRAIT_PARTIAL";
        public const string VoicePartial = "VOICE_PARTIAL";

        public bool IsPortraitSelected
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsVoiceSelected
        {
            get => Get<bool>();
            set => Set(value);
        }
        
        public string ActivePortrait
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsCustomPortraitVisible
        {
            get => Get<bool>();
            set => Set(value);
        }
        
        public string CustomPortraitFile
        {
            get => Get<string>();
            set => Set(value);
        }
        
        private int _selectedSoundSetIndex;
        private List<int> _soundSetIds;
        public GuiBindingList<string> SoundSetNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> SoundSetToggles
        {
            get => Get<GuiBindingList<bool>>();
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

        private void LoadPortraitView()
        {
            IsPortraitSelected = true;
            IsVoiceSelected = false;
            IsCustomPortraitVisible = _target == Player;
            
            ChangePartialView(PartialElement, PortraitPartial);
            LoadCurrentPortrait();
            
            WatchOnClient(model => model.CustomPortraitFile);
        }

        private void LoadVoiceView()
        {
            IsPortraitSelected = false;
            IsVoiceSelected = true;
            
            ChangePartialView(PartialElement, VoicePartial);
            LoadSoundSets();
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

        private void LoadSoundSets()
        {
            _selectedSoundSetIndex = -1;
            var activeSoundSetId = GetSoundset(_target);
            var soundSetNames = new GuiBindingList<string>();
            var soundSetToggles = new GuiBindingList<bool>();
            _soundSetIds = new List<int>();

            foreach (var (soundSet, label) in Cache.GetSoundSets())
            {
                soundSetNames.Add(label);
                soundSetToggles.Add(activeSoundSetId == soundSet);
                _soundSetIds.Add(soundSet);
            }
            
            SoundSetNames = soundSetNames;
            SoundSetToggles = soundSetToggles;
        }
        
        protected override void Initialize(CustomizeCharacterPayload initialPayload)
        {
            _target = GetIsObjectValid(initialPayload.Target) ? initialPayload.Target : Player;

            CustomPortraitFile = string.Empty;
            MaximumPortraits = Cache.PortraitCount;
            MaxPortraitsText = $"/ {MaximumPortraits}";
            
            LoadPortraitView();
            
            WatchOnClient(model => model.ActivePortraitInternalId);
        }

        public Action OnPortraitClick() => () =>
        {
            LoadPortraitView();
        };

        public Action OnVoiceClick() => () =>
        {
            LoadVoiceView();
        };
        
        public Action OnPreviousPortraitClick() => () =>
        {
            var newId = _activePortraitInternalId - 1;

            if (newId < 1)
                newId = Cache.PortraitCount;

            ActivePortraitInternalId = newId.ToString();
            ActivePortrait = Cache.GetPortraitResrefByInternalId(_activePortraitInternalId) + "l";
        };

        public Action OnNextPortraitClick() => () =>
        {
            var newId = _activePortraitInternalId + 1;

            if (newId > Cache.PortraitCount)
                newId = 1;

            ActivePortraitInternalId = newId.ToString();
            ActivePortrait = Cache.GetPortraitResrefByInternalId(_activePortraitInternalId) + "l";
        };

        public Action OnRevertPortraitClick() => () =>
        {
            LoadCurrentPortrait();
        };

        public Action OnSavePortraitClick() => () =>
        {
            var isDroid = Droid.IsDroid(_target);
            var isBeast = BeastMastery.IsPlayerBeast(_target);
            var portraitId = Cache.GetPortraitByInternalId(_activePortraitInternalId);

            if (isDroid || isBeast || string.IsNullOrWhiteSpace(CustomPortraitFile))
            {
                SetPortraitId(_target, portraitId);
            }
            else if(!string.IsNullOrWhiteSpace(CustomPortraitFile))
            {
                SetPortraitResRef(_target, CustomPortraitFile);
                ActivePortrait = CustomPortraitFile + "l";
            }
            
            if (isDroid)
            {
                var controller = Droid.GetControllerItem(_target);
                var constructedDroid = Droid.LoadConstructedDroid(controller);

                constructedDroid.PortraitId = portraitId;

                Droid.SaveConstructedDroid(controller, constructedDroid);
            }
            else if (isBeast)
            {
                var beastId = BeastMastery.GetBeastId(_target);
                var dbBeast = DB.Get<Beast>(beastId);

                dbBeast.PortraitId = portraitId;
                DB.Set(dbBeast);
            }

            Gui.PublishRefreshEvent(Player, new ChangePortraitRefreshEvent());
        };

        public Action OnSoundSetClick() => () =>
        {
            if (_selectedSoundSetIndex > -1)
            {
                SoundSetToggles[_selectedSoundSetIndex] = false;
            }

            _selectedSoundSetIndex = NuiGetEventArrayIndex();
            SoundSetToggles[_selectedSoundSetIndex] = true;

            var soundSetId = _soundSetIds[_selectedSoundSetIndex];
            SetSoundset(_target, soundSetId);

            if (Droid.IsDroid(_target))
            {
                var controller = Droid.GetControllerItem(_target);
                var constructedDroid = Droid.LoadConstructedDroid(controller);

                constructedDroid.SoundSetId = soundSetId;

                Droid.SaveConstructedDroid(controller, constructedDroid);
            }
            else if (BeastMastery.IsPlayerBeast(_target))
            {
                var beastId = BeastMastery.GetBeastId(_target);
                var dbBeast = DB.Get<Beast>(beastId);

                dbBeast.SoundSetId = soundSetId;
                DB.Set(dbBeast);
            }
        };
    }
}
