using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Threading;
using Caliburn.Micro;
using Newtonsoft.Json;
using SWLOR.Game.Server.Data;
using SWLOR.Tools.Editor.Attributes;
using SWLOR.Tools.Editor.Extensions;
using SWLOR.Tools.Editor.Messages;
using SWLOR.Tools.Editor.ViewModels.Contracts;
using SWLOR.Tools.Editor.ViewModels.Data;

namespace SWLOR.Tools.Editor.ViewModels
{
    public class LootEditorViewModel: PropertyChangedBase, 
        ILootEditorViewModel, 
        IHandle<EditorObjectSelected<LootTableViewModel>>,
        IHandle<DeleteEditorObject<LootTableViewModel>>,
        IHandle<RequestNewEditorObject<LootTableViewModel>>
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
            _deletedItems = new List<Tuple<int, LootTableItemViewModel>>();
            _addedItems = new List<LootTableItemViewModel>();

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
                _activeLootTable = value;
                
                if (ActiveLootTable != null)
                {
                    ActiveLootTable.OnDirty += (sender, args) =>
                    {
                        NotifyOfPropertyChange(() => IsTableSelected);
                        NotifyOfPropertyChange(() => IsLootItemSelected);
                        NotifyOfPropertyChange(() => CanSaveOrDiscardChanges);
                    };
                }

                NotifyOfPropertyChange(() => ActiveLootTable);
            }
        }
        
        private LootTableItemViewModel _selectedLootTableItem;
        public LootTableItemViewModel SelectedLootTableItem
        {
            get => _selectedLootTableItem;
            set
            {
                _selectedLootTableItem = value;

                if (SelectedLootTableItem != null)
                {
                    SelectedLootTableItem.OnDirty += (sender, args) =>
                    {
                        NotifyOfPropertyChange(() => IsTableSelected);
                        NotifyOfPropertyChange(() => IsLootItemSelected);
                        NotifyOfPropertyChange(() => CanSaveOrDiscardChanges);
                    };
                }

                NotifyOfPropertyChange(() => SelectedLootTableItem);
                NotifyOfPropertyChange(() => IsTableSelected);
                NotifyOfPropertyChange(() => IsLootItemSelected);
            }
        }

        private readonly List<Tuple<int, LootTableItemViewModel>> _deletedItems;
        private readonly List<LootTableItemViewModel> _addedItems;

        public bool IsTableSelected => ActiveLootTable != null;
        public bool IsLootItemSelected => SelectedLootTableItem != null;
        public bool CanSaveOrDiscardChanges => IsTableSelected && (ActiveLootTable.IsDirty || IsLootItemSelected && SelectedLootTableItem.IsDirty);


        public void SaveChanges()
        {
            ActiveLootTable.IsDirty = false;

            if(SelectedLootTableItem != null)
            {
                SelectedLootTableItem.IsDirty = false;
            }
            
            string json = JsonConvert.SerializeObject(ActiveLootTable);
            FolderAttribute folderAttribute = ActiveLootTable.GetAttributeByType<FolderAttribute>();
            if (folderAttribute == null)
            {
                throw new NullReferenceException("Ensure the " + nameof(FolderAttribute) + " attribute is specified on all data view model objects.");
            }

            string path = "./Data/" + folderAttribute.Folder + "/" + ActiveLootTable.FileName;
            File.WriteAllText(path, json);

            ActiveLootTable.RefreshTrackedProperties();
            SelectedLootTableItem?.RefreshTrackedProperties();
            _eventAggregator.PublishOnUIThread(new DataObjectSaved<LootTableViewModel>(ActiveLootTable));
        }

        public void DiscardChanges()
        {
            ActiveLootTable.DiscardChanges();

            foreach (var added in _addedItems)
            {
                ActiveLootTable.LootTableItems.Remove(added);
            }

            foreach (var deleted in _deletedItems)
            {
                ActiveLootTable.LootTableItems.Insert(deleted.Item1, deleted.Item2);
            }

            _addedItems.Clear();
            _deletedItems.Clear();
        }

        public void NewItem()
        {
            var newItem = new LootTableItemViewModel();
            ActiveLootTable.LootTableItems.Add(newItem);
            _addedItems.Add(newItem);

            SelectedLootTableItem = newItem;
            ActiveLootTable.IsDirty = true;
        }

        public void DeleteItem()
        {
            _yesNo.Prompt = "Are you sure you want to delete this item?";

            _windowManager.ShowDialog(_yesNo);

            if (_yesNo.Result == DialogResult.Yes)
            {
                int index = ActiveLootTable.LootTableItems.IndexOf(SelectedLootTableItem);
                _deletedItems.Add(new Tuple<int, LootTableItemViewModel>(index, SelectedLootTableItem));

                ActiveLootTable.LootTableItems.Remove(SelectedLootTableItem);
                SelectedLootTableItem = ActiveLootTable.LootTableItems.FirstOrDefault();
                ActiveLootTable.IsDirty = true;
            }
        }

        // Selected a new loot table
        public void Handle(EditorObjectSelected<LootTableViewModel> message)
        {
            message.OldObject?.DiscardChanges();

            if (message.SelectedObject == null) return;

            ActiveLootTable = message.SelectedObject;
            ActiveLootTable.RefreshTrackedProperties();

            if (ActiveLootTable.LootTableItems.Count > 0)
            {
                SelectedLootTableItem = ActiveLootTable.LootTableItems.First();
            }
        }

        // Renamed a loot table
        public void Handle(RenamedEditorObject<LootTableViewModel> message)
        {
            if (ActiveLootTable == message.Object)
            {
                ActiveLootTable.Name = message.Object.DisplayName;
            }
        }
        
        // Deleted Loot Table
        public void Handle(DeleteEditorObject<LootTableViewModel> message)
        {
            if (ActiveLootTable == message.DeletedEditorObject)
            {
                ActiveLootTable.IsDirty = false;
                ActiveLootTable = null;
            }
        }

        // Request for a new loot table
        public void Handle(RequestNewEditorObject<LootTableViewModel> message)
        {
            LootTableViewModel table = new LootTableViewModel();
            ActiveLootTable = table;
            _eventAggregator.PublishOnUIThread(new CreatedNewEditorObject<LootTableViewModel>(table));
        }
    }
}
