using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using SWLOR.Toolset.Domain.Categories;

namespace SWLOR.Toolset.Shell.Panels
{
    /// <summary>
    /// One destination on the "Move to" submenu: a folder, labelled by its full path.
    /// </summary>
    /// <remarks>
    /// A submenu of folders rather than a modal picker, because moving one thing into a folder should
    /// cost one gesture, and the tree the builder is looking at is already the list of choices.
    /// <para>
    /// The label is the whole path ("Tatooine / Anchorhead") since folder names repeat between branches
    /// and bare names would leave two identical entries on the menu.
    /// </para>
    /// <para>
    /// It carries its own command rather than being handed to one on the panel. A styled
    /// <c>MenuItem</c> generated from an ItemsSource can bind Header and Command off the item, but has
    /// no route back to the panel's own commands, so the item is what has to know what clicking it does.
    /// </para>
    /// </remarks>
    public sealed class FolderTargetViewModel
    {
        public FolderTargetViewModel(CategoryFolder folder, string path, Action<CategoryFolder> move)
        {
            Folder = folder;
            Path = path;
            Command = new RelayCommand(() => move(folder));
        }

        public CategoryFolder Folder { get; }

        public string Path { get; }

        public ICommand Command { get; }
    }
}
