using System.Windows.Forms;

namespace SWLOR.Tools.Editor.ViewModels.Contracts
{
    public interface IRenameObjectViewModel
    {
        string Name { get; set; }
        DialogResult Result { get; set; }
        void OK();
        void Cancel();
    }
}
