using System.Collections.ObjectModel;
using SWLOR.Game.Server.Data;
using SWLOR.Tools.Editor.ViewModels.Data;

namespace SWLOR.Tools.Editor.ViewModels.Contracts
{
    public interface ILootEditorViewModel
    {
        IObjectListViewModel<LootTableViewModel> ObjectListVM { get; set; }
    }
}
