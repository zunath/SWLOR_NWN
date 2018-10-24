using System.Windows;
using System.Windows.Controls;

namespace SWLOR.Tools.Editor.Views
{
    /// <summary>
    /// Interaction logic for DatabaseConnectionView.xaml
    /// </summary>
    public partial class DatabaseConnectionView : UserControl
    {
        public DatabaseConnectionView()
        {
            InitializeComponent();
        }

        private void Password_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext != null)
            { ((dynamic)DataContext).Password = ((PasswordBox)sender).Password; }
        }
    }
}
