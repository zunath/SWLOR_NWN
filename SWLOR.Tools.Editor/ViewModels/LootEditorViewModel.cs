using System.Collections.ObjectModel;
using System.IO;
using Caliburn.Micro;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Tools.Editor.Messages;
using SWLOR.Tools.Editor.ValueObjects;
using SWLOR.Tools.Editor.ViewModels.Contracts;

namespace SWLOR.Tools.Editor.ViewModels
{
    public class LootEditorViewModel: PropertyChangedBase, 
        ILootEditorViewModel, 
        IHandle<EditorObjectSelectedMessage<LootTable>>
    {
        public LootEditorViewModel(IEventAggregator eventAggregator, IObjectListViewModel<LootTable> objListVM)
        {
            ObjectListVM = objListVM;
            ObjectListVM.DisplayName = "Name";
            
            eventAggregator.Subscribe(this);
        }
        
        private IObjectListViewModel<LootTable> _objListVM;

        public IObjectListViewModel<LootTable> ObjectListVM
        {
            get => _objListVM;
            set
            {
                _objListVM = value;
                NotifyOfPropertyChange(() => ObjectListVM);
            }
        }

        private LootTable _activeRecord;

        public LootTable ActiveRecord
        {
            get => _activeRecord;
            set
            {
                _activeRecord = value;
                NotifyOfPropertyChange(() => ActiveRecord);
            }
        }

        public void Handle(EditorObjectSelectedMessage<LootTable> message)
        {
            ActiveRecord = message.SelectedObject;

        }
    }
}
