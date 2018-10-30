using System.ComponentModel;
using System.Windows.Controls;

namespace SWLOR.Tools.Editor.Views
{
    /// <summary>
    /// Interaction logic for ObjectListView.xaml
    /// </summary>
    public partial class ObjectListView : UserControl
    {
        public ObjectListView()
        {
            InitializeComponent();

            DataObjects.Items.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
        }
    }
}
