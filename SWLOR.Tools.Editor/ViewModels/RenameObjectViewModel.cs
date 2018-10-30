using System.Windows.Forms;
using SWLOR.Game.Server.Extension;
using SWLOR.Tools.Editor.Messages;
using SWLOR.Tools.Editor.ViewModels.Contracts;
using Screen = Caliburn.Micro.Screen;

namespace SWLOR.Tools.Editor.ViewModels
{
    public class RenameObjectViewModel :
        Screen,
        IRenameObjectViewModel
    {
        public RenameObjectViewModel()
        {
            MaxLength = 32;
        }

        private string _name;

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                NotifyOfPropertyChange(() => Name);
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

        private int _maxLength;

        public int MaxLength
        {
            get => _maxLength;
            set
            {
                _maxLength = value;
                NotifyOfPropertyChange(() => MaxLength);
            }
        }

        public void OK()
        {
            Result = DialogResult.OK;
            TryClose();
        }

        public void Cancel()
        {
            Result = DialogResult.Cancel;
            TryClose();
        }
    }
}
