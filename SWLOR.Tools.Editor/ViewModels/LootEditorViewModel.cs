using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
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
            LootTableItems = new ObservableCollection<LootTableItem>();
            
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

        private LootTable _activeLootTable;

        public LootTable ActiveLootTable
        {
            get => _activeLootTable;
            set
            {
                _activeLootTable = value;
                NotifyOfPropertyChange(() => ActiveLootTable);
            }
        }

        private ObservableCollection<LootTableItem> _lootTableItems;
        public ObservableCollection<LootTableItem> LootTableItems
        {
            get => _lootTableItems;
            set
            {
                _lootTableItems = value;
                NotifyOfPropertyChange(() => LootTableItems);
            }
        }

        private LootTableItem _selectedLootTableItem;
        public LootTableItem SelectedLootTableItem
        {
            get => _selectedLootTableItem;
            set
            {
                _selectedLootTableItem = value;
                NotifyOfPropertyChange(() => SelectedLootTableItem);

                if (value != null)
                {
                    Weight = value.Weight;
                    Resref = value.Resref;
                    IsActive = value.IsActive;
                    SpawnRule = value.SpawnRule;
                }
            }
        }
        

        private int _weight;

        public int Weight
        {
            get => _weight;
            set
            {
                _weight = value;
                NotifyOfPropertyChange(() => Weight);
            }
        }

        private string _resref;

        public string Resref
        {
            get => _resref;
            set
            {
                _resref = value;
                NotifyOfPropertyChange(() => Resref);
            }
        }

        private string _spawnRule;

        public string SpawnRule
        {
            get => _spawnRule;
            set
            {
                _spawnRule = value;
                NotifyOfPropertyChange(() => SpawnRule);
            }
        }

        private bool _isActive;

        public bool IsActive
        {
            get => _isActive;
            set
            {
                _isActive = value;
                NotifyOfPropertyChange(() => IsActive);
            }
        }

        public void Handle(EditorObjectSelectedMessage<LootTable> message)
        {
            ActiveLootTable = message.SelectedObject;
            LootTableItems.Clear();

            foreach (var lti in message.SelectedObject.LootTableItems)
            {
                LootTableItems.Add(lti);
            }

            NotifyOfPropertyChange(() => LootTableItems);
        }
    }
}
