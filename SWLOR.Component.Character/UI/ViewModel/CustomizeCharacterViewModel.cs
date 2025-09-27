using Microsoft.Extensions.DependencyInjection;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Caching.Contracts;
using SWLOR.Component.Character.UI.Payload;
using SWLOR.Shared.Domain.Beasts.Contracts;
using SWLOR.Shared.Domain.Droids.Contracts;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.UI.Events;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Model;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.Character.UI.ViewModel
{
    public class CustomizeCharacterViewModel: GuiViewModelBase<CustomizeCharacterViewModel, CustomizeCharacterPayload>
    {
        private readonly IDatabaseService _db;
        private readonly IPortraitCacheService _portraitCache;
        private readonly ISoundSetCacheService _soundSetCache;
        private readonly IServiceProvider _serviceProvider;
        
        // Lazy-loaded services to break circular dependencies
        private IBeastMasteryService BeastMastery => _serviceProvider.GetRequiredService<IBeastMasteryService>();
        private IDroidService DroidService => _serviceProvider.GetRequiredService<IDroidService>();

        public CustomizeCharacterViewModel(
            IGuiService guiService, 
            IDatabaseService db, 
            IPortraitCacheService portraitCache, 
            ISoundSetCacheService soundSetCache, 
            IServiceProvider serviceProvider) 
            : base(guiService)
        {
            _db = db;
            _portraitCache = portraitCache;
            _soundSetCache = soundSetCache;
            _serviceProvider = serviceProvider;
            // Services are now lazy-loaded via IServiceProvider
        }
        
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
                    if (parsed > _portraitCache.PortraitCount)
                        parsed = _portraitCache.PortraitCount;
                    else if (parsed < 1)
                        parsed = 1;

                    _activePortraitInternalId = parsed;

                    Set(parsed.ToString());
                    ActivePortrait = _portraitCache.GetPortraitResrefByInternalId(_activePortraitInternalId) + "l";
                }
                else
                {
                    Set(_activePortraitInternalId.ToString());
                    ActivePortrait = _portraitCache.GetPortraitResrefByInternalId(_activePortraitInternalId) + "l";
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
            var internalId = _portraitCache.GetPortraitInternalIdByResref(resref);
            var portraitId = internalId == -1
                ? _portraitCache.GetPortraitInternalId(1)
                : _portraitCache.GetPortraitByInternalId(internalId);

            ActivePortraitInternalId = _portraitCache.GetPortraitInternalId(portraitId).ToString();
            ActivePortrait = _portraitCache.GetPortraitResrefByInternalId(_activePortraitInternalId) + "l";
        }

        private void LoadSoundSets()
        {
            _selectedSoundSetIndex = -1;
            var activeSoundSetId = GetSoundset(_target);
            var soundSetNames = new GuiBindingList<string>();
            var soundSetToggles = new GuiBindingList<bool>();
            _soundSetIds = new List<int>();

            foreach (var (soundSet, label) in _soundSetCache.GetSoundSets())
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
            MaximumPortraits = _portraitCache.PortraitCount;
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
                newId = _portraitCache.PortraitCount;

            ActivePortraitInternalId = newId.ToString();
            ActivePortrait = _portraitCache.GetPortraitResrefByInternalId(_activePortraitInternalId) + "l";
        };

        public Action OnNextPortraitClick() => () =>
        {
            var newId = _activePortraitInternalId + 1;

            if (newId > _portraitCache.PortraitCount)
                newId = 1;

            ActivePortraitInternalId = newId.ToString();
            ActivePortrait = _portraitCache.GetPortraitResrefByInternalId(_activePortraitInternalId) + "l";
        };

        public Action OnRevertPortraitClick() => () =>
        {
            LoadCurrentPortrait();
        };

        public Action OnSavePortraitClick() => () =>
        {
            var isDroid = DroidService.IsDroid(_target);
            var isBeast = BeastMastery.IsPlayerBeast(_target);
            var portraitId = _portraitCache.GetPortraitByInternalId(_activePortraitInternalId);

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
                var controller = DroidService.GetControllerItem(_target);
                var constructedDroid = DroidService.LoadConstructedDroid(controller);

                constructedDroid.PortraitId = portraitId;

                DroidService.SaveConstructedDroid(controller, constructedDroid);
            }
            else if (isBeast)
            {
                var beastId = BeastMastery.GetBeastId(_target);
                var dbBeast = _db.Get<Beast>(beastId);

                dbBeast.PortraitId = portraitId;
                _db.Set(dbBeast);
            }

            _guiService.PublishRefreshEvent(Player, new ChangePortraitRefreshEvent());
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

            if (DroidService.IsDroid(_target))
            {
                var controller = DroidService.GetControllerItem(_target);
                var constructedDroid = DroidService.LoadConstructedDroid(controller);

                constructedDroid.SoundSetId = soundSetId;

                DroidService.SaveConstructedDroid(controller, constructedDroid);
            }
            else if (BeastMastery.IsPlayerBeast(_target))
            {
                var beastId = BeastMastery.GetBeastId(_target);
                var dbBeast = _db.Get<Beast>(beastId);

                dbBeast.SoundSetId = soundSetId;
                _db.Set(dbBeast);
            }
        };
    }
}
