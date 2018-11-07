using System.Collections.ObjectModel;
using Newtonsoft.Json;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Tools.Editor.Attributes;

namespace SWLOR.Tools.Editor.ViewModels.Data
{
    [Folder(nameof(CustomEffect))]
    public class CustomEffectViewModel : DBObjectViewModelBase
    {
        public CustomEffectViewModel()
        {
            TrackProperty(this, x => x.Name);
            TrackProperty(this, x => x.ScriptHandler);
            TrackProperty(this, x => x.StartMessage);
            TrackProperty(this, x => x.ContinueMessage);
            TrackProperty(this, x => x.WornOffMessage);
            TrackProperty(this, x => x.CustomEffectCategoryID);
        }

        private string _displayName;
        public override string DisplayName
        {
            get => _displayName;
            set
            {
                _name = value;
                _displayName = value;
                NotifyOfPropertyChange(() => Name);
                NotifyOfPropertyChange(() => DisplayName);
            }
        }

        private string _name;
        [JsonProperty(nameof(Name))]
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                _displayName = value;
                NotifyOfPropertyChange(() => Name);
                NotifyOfPropertyChange(() => DisplayName);
            }
        }


        private string _scriptHandler;
        [JsonProperty(nameof(ScriptHandler))]
        public string ScriptHandler
        {
            get => _scriptHandler;
            set
            {
                _scriptHandler = value;
                NotifyOfPropertyChange(() => ScriptHandler);
            }
        }

        private string _startMessage;

        [JsonProperty(nameof(StartMessage))]
        public string StartMessage
        {
            get => _startMessage;
            set
            {
                _startMessage = value;
                NotifyOfPropertyChange(() => StartMessage);
            }
        }

        private string _continueMessage;

        [JsonProperty(nameof(ContinueMessage))]
        public string ContinueMessage
        {
            get => _continueMessage;
            set
            {
                _continueMessage = value;
                NotifyOfPropertyChange(() => ContinueMessage);
            }
        }

        private string _wornOffMessage;

        [JsonProperty(nameof(WornOffMessage))]
        public string WornOffMessage
        {
            get => _wornOffMessage;
            set
            {
                _wornOffMessage = value;
                NotifyOfPropertyChange(()=> WornOffMessage);
            }
        }

        private int _customEffectCategoryID;

        [JsonProperty(nameof(CustomEffectCategoryID))]
        public int CustomEffectCategoryID
        {
            get => _customEffectCategoryID;
            set
            {
                _customEffectCategoryID = value;
                NotifyOfPropertyChange(() => CustomEffectCategoryID);
            }
        }
        
    }
}
