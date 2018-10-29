using System.Windows.Forms;

namespace SWLOR.Tools.Editor.ViewModels.Contracts
{
    public interface IYesNoViewModel
    {
        DialogResult Result { get; set; }
        void Yes();
        void No();
        void Cancel();
        string Prompt { get; set; }
    }
}
