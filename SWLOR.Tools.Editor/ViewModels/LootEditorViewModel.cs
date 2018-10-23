using System.Collections.ObjectModel;
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
            LootTables = new ObservableCollection<ObjectListItem>();

            eventAggregator.Subscribe(this);
        }

        private ObservableCollection<ObjectListItem> _lootTables;
        public ObservableCollection<ObjectListItem> LootTables
        {
            get => _lootTables;
            set
            {
                _lootTables = value;
                NotifyOfPropertyChange(()=> LootTables);
            }
        }

        public void Handle(ApplicationStartedMessage message)
        {
            for(int x = 0; x <= 10; x++)
            {
                LootTables.Add(new ObjectListItem("test", "My test x = " + x));
            }

            NotifyOfPropertyChange(() => LootTables);


        }
    }
}
