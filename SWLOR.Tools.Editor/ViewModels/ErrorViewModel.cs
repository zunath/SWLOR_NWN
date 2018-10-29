using Caliburn.Micro;
using SWLOR.Game.Server.Extension;
using SWLOR.Tools.Editor.Messages;
using SWLOR.Tools.Editor.ViewModels.Contracts;

namespace SWLOR.Tools.Editor.ViewModels
{
    public class ErrorViewModel: 
        Screen, 
        IErrorViewModel
    {
        public ErrorViewModel(IEventAggregator eventAggregator)
        {
            eventAggregator.Subscribe(this);
        }

        private string _errorDetails;

        public string ErrorDetails
        {
            get => _errorDetails;
            set
            {
                _errorDetails = value;
                NotifyOfPropertyChange(() => ErrorDetails);
            }
        }

        public void OK()
        {
            TryClose();
        }
    }
}
