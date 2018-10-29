using System.Windows.Forms;
using Caliburn.Micro;
using SWLOR.Game.Server.Extension;
using SWLOR.Tools.Editor.Messages;
using SWLOR.Tools.Editor.ViewModels.Contracts;
using Screen = Caliburn.Micro.Screen;

namespace SWLOR.Tools.Editor.ViewModels
{
    public class YesNoViewModel :
        Screen,
        IYesNoViewModel
    {
        public YesNoViewModel(IEventAggregator eventAggregator)
        {
            eventAggregator.Subscribe(this);
        }

        private string _prompt;

        public string Prompt
        {
            get => _prompt;
            set
            {
                _prompt = value;
                NotifyOfPropertyChange(() => Prompt);
            }
        }

        private DialogResult _result;

        public DialogResult Result
        {
            get => _result;
            set
            {
                _result = value;
                NotifyOfPropertyChange(() => Result);
            }
        }

        public void Yes()
        {
            Result = DialogResult.Yes;
            TryClose();
        }
        
        public void No()
        {
            Result = DialogResult.No;
            TryClose();
        }

        public void Cancel()
        {
            Result = DialogResult.Cancel;
            TryClose();
        }

    }

}
