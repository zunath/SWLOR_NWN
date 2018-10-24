using Caliburn.Micro;
using SWLOR.Game.Server.Extension;
using SWLOR.Tools.Editor.Messages;
using SWLOR.Tools.Editor.ViewModels.Contracts;

namespace SWLOR.Tools.Editor.ViewModels
{
    public class ErrorViewModel: 
        Screen, 
        IErrorViewModel,
        IHandle<DatabaseConnectionFailedMessage>
    {
        private readonly IEventAggregator _eventAggregator;

        public ErrorViewModel(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;

            ErrorDetails = "my error now";

            _eventAggregator.Subscribe(this);
        }

        private string _errorDetails;

        public string ErrorDetails
        {
            get => _errorDetails;
            set
            {
                _errorDetails = value;
                NotifyOfPropertyChange(() => _errorDetails);
            }
        }

        public void OK()
        {
            TryClose();
        }

        public void Handle(DatabaseConnectionFailedMessage message)
        {
            ErrorDetails = message.Exception.ToMessageAndCompleteStacktrace();
        }
    }
}
