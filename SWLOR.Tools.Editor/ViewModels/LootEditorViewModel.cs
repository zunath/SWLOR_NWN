using System.Collections.ObjectModel;
using System.IO;
using Caliburn.Micro;
using SWLOR.Tools.Editor.Messages;
using SWLOR.Tools.Editor.ValueObjects;
using SWLOR.Tools.Editor.ViewModels.Contracts;

namespace SWLOR.Tools.Editor.ViewModels
{
    public class LootEditorViewModel: PropertyChangedBase, 
        ILootEditorViewModel, 
        IHandle<ApplicationStartedMessage>
    {
        public LootEditorViewModel(IEventAggregator eventAggregator)
        {
            ObjectListVM = new ObjectListViewModel();
            
            eventAggregator.Subscribe(this);
        }
        
        private ObjectListViewModel _objListVM;

        public ObjectListViewModel ObjectListVM
        {
            get => _objListVM;
            set
            {
                _objListVM = value;
                NotifyOfPropertyChange(() => ObjectListVM);
            }
        }

        public void Handle(ApplicationStartedMessage message)
        {
            if (!Directory.Exists("./Data/LootTables"))
            {
                Directory.CreateDirectory("./Data/LootTables");
            }
            if (!Directory.Exists("./Data/LootTableItems"))
            {
                Directory.CreateDirectory("./Data/LootTableItems");
            }
        }
    }
}
