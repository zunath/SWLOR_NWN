using SWLOR.Tools.Editor.ViewModels.Contracts;

namespace SWLOR.Tools.Editor
{
    public interface IShellViewModel
    {
        IMenuBarViewModel MenuBarVM { get; set; }
        IEditorListViewModel EditorListVM { get; set; }
    }
}