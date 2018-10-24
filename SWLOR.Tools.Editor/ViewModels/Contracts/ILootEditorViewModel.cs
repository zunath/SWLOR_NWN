using System.Collections.ObjectModel;
using SWLOR.Game.Server.Data;

namespace SWLOR.Tools.Editor.ViewModels.Contracts
{
    public interface ILootEditorViewModel
    {
        IObjectListViewModel<LootTable> ObjectListVM { get; set; }
    }
}
