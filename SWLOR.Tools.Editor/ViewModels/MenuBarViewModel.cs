using System.Windows;
using Caliburn.Micro;
using SWLOR.Tools.Editor.ViewModels.Contracts;

namespace SWLOR.Tools.Editor.ViewModels
{
    public class MenuBarViewModel : PropertyChangedBase, IMenuBarViewModel
    {
        public MenuBarViewModel()
        {
            
        }

        private string _importName;
        public string ImportName
        {
            get => _importName;
            set
            {
                _importName = value;
                NotifyOfPropertyChange(() => ImportName);
            }
        }

        public void Exit()
        {
            Application.Current.Shutdown();
        }

    }
}
