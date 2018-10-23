using System.Collections.ObjectModel;
using SWLOR.Game.Server.Data;
using SWLOR.Tools.Editor.Messages;
using SWLOR.Tools.Editor.ValueObjects;

namespace SWLOR.Tools.Editor.ViewModels.Contracts
{
    public interface ILootEditorViewModel
    {
        IObjectListViewModel<LootTable> ObjectListVM { get; set; }
    }
}
