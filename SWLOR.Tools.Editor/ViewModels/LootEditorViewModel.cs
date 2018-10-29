using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Forms;
using Caliburn.Micro;
using SWLOR.Game.Server.Data;
using SWLOR.Tools.Editor.Messages;
using SWLOR.Tools.Editor.ViewModels.Contracts;
using SWLOR.Tools.Editor.ViewModels.Data;

namespace SWLOR.Tools.Editor.ViewModels
{
    public class LootEditorViewModel: PropertyChangedBase, 
        ILootEditorViewModel, 
        IHandle<EditorObjectSelected<LootTableViewModel>>
    {
        private readonly IWindowManager _windowManager;
        private readonly IYesNoViewModel _yesNo;
        private readonly IEventAggregator _eventAggregator;

        public LootEditorViewModel(
            IEventAggregator eventAggregator, 
            IObjectListViewModel<LootTableViewModel> objListVM,
            IWindowManager windowManager,
            IYesNoViewModel yesNo)
        {
            ObjectListVM = objListVM;
            _windowManager = windowManager;
            _yesNo = yesNo;

            _eventAggregator = eventAggregator;
            _eventAggregator.Subscribe(this);
        }

        private IObjectListViewModel<LootTableViewModel> _objListVM;

        public IObjectListViewModel<LootTableViewModel> ObjectListVM
        {
            get => _objListVM;
            set
            {
                _objListVM = value;
                NotifyOfPropertyChange(() => ObjectListVM);
            }
        }


        private LootTableViewModel _activeLootTable;
        public LootTableViewModel ActiveLootTable
        {
            get => _activeLootTable;
            set
            {
                if (_activeLootTable != null && _activeLootTable.IsDirty)
                {
                    _yesNo.Prompt = "You have modified this object. Would you like to save changes?";
                    _windowManager.ShowDialog(_yesNo);

                    if (_yesNo.Result == DialogResult.Yes)
                    {
                        SaveChanges();
                    }
                    else if(_yesNo.Result == DialogResult.Cancel)
                    {
                        NotifyOfPropertyChange(() => ActiveLootTable);
                        return;
                    }
                }

                _activeLootTable = value;
                NotifyOfPropertyChange(() => ActiveLootTable);


                if (ActiveLootTable != null)
                {
                    ActiveLootTable.SetTrackedValues();
                    ActiveLootTable.OnDirty += OnDirty;
                }
            }
        }
        
        private LootTableItemViewModel _selectedLootTableItem;
        public LootTableItemViewModel SelectedLootTableItem
        {
            get => _selectedLootTableItem;
            set
            {
                _selectedLootTableItem = value;
                NotifyOfPropertyChange(() => SelectedLootTableItem);
                NotifyOfPropertyChange(() => IsTableSelected);
                
                if(SelectedLootTableItem != null)
                {
                    SelectedLootTableItem.OnDirty += OnDirty;
                }
            }
        }

        private void OnDirty(object sender, EventArgs e)
        {
            NotifyOfPropertyChange(() => IsTableSelected);
            NotifyOfPropertyChange(() => CanSaveOrDiscardChanges);
        }

        public bool IsTableSelected => ActiveLootTable != null;
        public bool CanSaveOrDiscardChanges => IsTableSelected && (ActiveLootTable.IsDirty || SelectedLootTableItem.IsDirty);

        public void SaveChanges()
        {
            ActiveLootTable.IsDirty = false;
            SelectedLootTableItem.IsDirty = false;
            
            _eventAggregator.PublishOnUIThread(new DataObjectSaved<LootTableViewModel>(ActiveLootTable));
        }

        public void DiscardChanges()
        {
            ActiveLootTable.DiscardChanges();

            ActiveLootTable.IsDirty = false;
            SelectedLootTableItem.IsDirty = false;
        }

        public void Handle(EditorObjectSelected<LootTableViewModel> message)
        {
            ActiveLootTable = message.SelectedObject;

            if (ActiveLootTable.LootTableItems.Count > 0)
            {
                SelectedLootTableItem = ActiveLootTable.LootTableItems.First();
            }
        }
    }
}
