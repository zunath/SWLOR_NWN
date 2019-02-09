using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Caliburn.Micro;
using Newtonsoft.Json;
using SWLOR.Tools.Editor.Attributes;
using SWLOR.Tools.Editor.Extensions;
using SWLOR.Tools.Editor.Messages;
using SWLOR.Tools.Editor.ViewModels.Contracts;
using SWLOR.Tools.Editor.ViewModels.Data;

namespace SWLOR.Tools.Editor.ViewModels
{
    public class KeyItemEditorViewModel: PropertyChangedBase, 
        IKeyItemEditorViewModel, 
        IHandle<EditorObjectSelected<KeyItemCategoryViewModel>>,
        IHandle<DeleteEditorObject<KeyItemCategoryViewModel>>,
        IHandle<RequestNewEditorObject<KeyItemCategoryViewModel>>
    {
        private readonly IWindowManager _windowManager;
        private readonly IYesNoViewModel _yesNo;
        private readonly IEventAggregator _eventAggregator;

        public KeyItemEditorViewModel(
            IEventAggregator eventAggregator, 
            IObjectListViewModel<KeyItemCategoryViewModel> objListVM,
            IWindowManager windowManager,
            IYesNoViewModel yesNo)
        {
            ObjectListVM = objListVM;
            _windowManager = windowManager;
            _yesNo = yesNo;
            _deletedItems = new List<Tuple<int, KeyItemViewModel>>();
            _addedItems = new List<KeyItemViewModel>();

            _eventAggregator = eventAggregator;
            _eventAggregator.Subscribe(this);
        }

        private IObjectListViewModel<KeyItemCategoryViewModel> _objListVM;
        public IObjectListViewModel<KeyItemCategoryViewModel> ObjectListVM
        {
            get => _objListVM;
            set
            {
                _objListVM = value;
                NotifyOfPropertyChange(() => ObjectListVM);
            }
        }
        
        private KeyItemCategoryViewModel _activeCategory;
        public KeyItemCategoryViewModel ActiveCategory
        {
            get => _activeCategory;
            set
            {
                _activeCategory = value;
                
                if (ActiveCategory != null)
                {
                    ActiveCategory.OnDirty += (sender, args) =>
                    {
                        NotifyOfPropertyChange(() => IsCategorySelected);
                        NotifyOfPropertyChange(() => IsKeyItemSelected);
                        NotifyOfPropertyChange(() => CanSaveOrDiscardChanges);
                    };
                }

                NotifyOfPropertyChange(() => ActiveCategory);
            }
        }
        
        private KeyItemViewModel _selectedKeyItem;
        public KeyItemViewModel SelectedKeyItem
        {
            get => _selectedKeyItem;
            set
            {
                _selectedKeyItem = value;

                if (SelectedKeyItem != null)
                {
                    SelectedKeyItem.OnDirty += (sender, args) =>
                    {
                        NotifyOfPropertyChange(() => IsCategorySelected);
                        NotifyOfPropertyChange(() => IsKeyItemSelected);
                        NotifyOfPropertyChange(() => CanSaveOrDiscardChanges);
                    };
                }

                NotifyOfPropertyChange(() => SelectedKeyItem);
                NotifyOfPropertyChange(() => IsCategorySelected);
                NotifyOfPropertyChange(() => IsKeyItemSelected);
            }
        }

        private readonly List<Tuple<int, KeyItemViewModel>> _deletedItems;
        private readonly List<KeyItemViewModel> _addedItems;

        public bool IsCategorySelected => ActiveCategory != null;
        public bool IsKeyItemSelected => SelectedKeyItem != null;
        public bool CanSaveOrDiscardChanges => IsCategorySelected && (ActiveCategory.IsDirty || IsKeyItemSelected && SelectedKeyItem.IsDirty);


        public void SaveChanges()
        {
            ActiveCategory.IsDirty = false;

            if(SelectedKeyItem != null)
            {
                SelectedKeyItem.IsDirty = false;
            }
            
            string json = JsonConvert.SerializeObject(ActiveCategory);
            FolderAttribute folderAttribute = ActiveCategory.GetAttributeByType<FolderAttribute>();
            if (folderAttribute == null)
            {
                throw new NullReferenceException("Ensure the " + nameof(FolderAttribute) + " attribute is specified on all data view model objects.");
            }

            string path = "./Data/" + folderAttribute.Folder + "/" + ActiveCategory.FileName;
            File.WriteAllText(path, json);

            ActiveCategory.RefreshTrackedProperties();
            SelectedKeyItem?.RefreshTrackedProperties();
            _eventAggregator.PublishOnUIThread(new DataObjectSaved<KeyItemCategoryViewModel>(ActiveCategory));
        }

        public void DiscardChanges()
        {
            ActiveCategory.DiscardChanges();

            foreach (var added in _addedItems)
            {
                ActiveCategory.KeyItems.Remove(added);
            }

            foreach (var deleted in _deletedItems)
            {
                ActiveCategory.KeyItems.Insert(deleted.Item1, deleted.Item2);
            }

            _addedItems.Clear();
            _deletedItems.Clear();
        }

        public void NewItem()
        {
            var newItem = new KeyItemViewModel();
            ActiveCategory.KeyItems.Add(newItem);
            _addedItems.Add(newItem);

            SelectedKeyItem = newItem;
            ActiveCategory.IsDirty = true;
        }

        public void DeleteItem()
        {
            _yesNo.Prompt = "Are you sure you want to delete this item?";

            _windowManager.ShowDialog(_yesNo);

            if (_yesNo.Result == DialogResult.Yes)
            {
                int index = ActiveCategory.KeyItems.IndexOf(SelectedKeyItem);
                _deletedItems.Add(new Tuple<int, KeyItemViewModel>(index, SelectedKeyItem));

                ActiveCategory.KeyItems.Remove(SelectedKeyItem);
                SelectedKeyItem = ActiveCategory.KeyItems.FirstOrDefault();
                ActiveCategory.IsDirty = true;
            }
        }

        // Selected a new Key Item Category
        public void Handle(EditorObjectSelected<KeyItemCategoryViewModel> message)
        {
            message.OldObject?.DiscardChanges();

            if (message.SelectedObject == null) return;

            ActiveCategory = message.SelectedObject;
            ActiveCategory.RefreshTrackedProperties();

            if (ActiveCategory.KeyItems.Count > 0)
            {
                SelectedKeyItem = ActiveCategory.KeyItems.First();
            }
        }
        
        // Deleted Key Item Category
        public void Handle(DeleteEditorObject<KeyItemCategoryViewModel> message)
        {
            if (ActiveCategory == message.DeletedEditorObject)
            {
                ActiveCategory.IsDirty = false;
                ActiveCategory = null;
            }
        }

        // Request for a new Key Item Category
        public void Handle(RequestNewEditorObject<KeyItemCategoryViewModel> message)
        {
            KeyItemCategoryViewModel table = new KeyItemCategoryViewModel();
            table.KeyItems.Add(new KeyItemViewModel());

            ActiveCategory = table;
            _eventAggregator.PublishOnUIThread(new CreatedNewEditorObject<KeyItemCategoryViewModel>(table));
        }
    }
}
