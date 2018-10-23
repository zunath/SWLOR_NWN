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
        
        public void Exit()
        {
            Application.Current.Shutdown();
        }

    }
}
