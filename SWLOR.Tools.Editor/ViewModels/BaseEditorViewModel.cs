using System;
using System.IO;
using Caliburn.Micro;
using Newtonsoft.Json;
using SWLOR.Tools.Editor.Attributes;
using SWLOR.Tools.Editor.Extensions;
using SWLOR.Tools.Editor.Messages;
using SWLOR.Tools.Editor.ViewModels.Contracts;

namespace SWLOR.Tools.Editor.ViewModels
{
    public abstract class BaseEditorViewModel<T> : 
        PropertyChangedBase,
        IHandle<EditorObjectSelected<T>>,
        IHandle<DeleteEditorObject<T>>,
        IHandle<RequestNewEditorObject<T>>,
        IHandle<DataObjectImported> 
        where T: IDBObjectViewModel
    {
        private readonly IEventAggregator _eventAggregator;

        protected BaseEditorViewModel(
            IEventAggregator eventAggregator,
            IObjectListViewModel<T> objListVM)
        {
            ObjectListVM = objListVM;

            _eventAggregator = eventAggregator;
            _eventAggregator.Subscribe(this);
        }


        private IObjectListViewModel<T> _objListVM;
        public IObjectListViewModel<T> ObjectListVM
        {
            get => _objListVM;
            set
            {
                _objListVM = value;
                NotifyOfPropertyChange(() => ObjectListVM);
            }
        }

        private T _activeObject;
        public T ActiveObject
        {
            get => _activeObject;
            set
            {
                _activeObject = value;

                if (ActiveObject != null)
                {
                    ActiveObject.OnDirty += (sender, args) =>
                    {
                        NotifyOfPropertyChange(() => IsObjectSelected);
                        NotifyOfPropertyChange(() => CanSaveOrDiscardChanges);
                    };
                }

                NotifyOfPropertyChange(() => ActiveObject);
            }
        }

        public bool IsObjectSelected => ActiveObject != null;
        public bool CanSaveOrDiscardChanges => IsObjectSelected && ActiveObject.IsDirty;

        public void SaveChanges()
        {
            ActiveObject.IsDirty = false;

            string json = JsonConvert.SerializeObject(ActiveObject);
            FolderAttribute folderAttribute = ActiveObject.GetAttributeByType<FolderAttribute>();

            string path = "./Data/" + folderAttribute.Folder + "/" + ActiveObject.FileName;
            File.WriteAllText(path, json);

            ActiveObject.RefreshTrackedProperties();
            _eventAggregator.PublishOnUIThread(new DataObjectSaved<T>(ActiveObject));
        }

        public void DiscardChanges()
        {
            ActiveObject.DiscardChanges();
        }

        // Selected a new object
        public void Handle(EditorObjectSelected<T> message)
        {
            message.OldObject?.DiscardChanges();

            if (message.SelectedObject == null) return;

            ActiveObject = message.SelectedObject;
            ActiveObject.RefreshTrackedProperties();

            NotifyOfPropertyChange(() => IsObjectSelected);
        }

        // Deleted an object
        public void Handle(DeleteEditorObject<T> message)
        {
            ActiveObject.IsDirty = false;
            ActiveObject = default(T);

            NotifyOfPropertyChange(() => IsObjectSelected);
        }

        // Request for a new object
        public void Handle(RequestNewEditorObject<T> message)
        {
            T vm = CreateNew();
            ActiveObject = vm;

            NotifyOfPropertyChange(() => IsObjectSelected);
            _eventAggregator.PublishOnUIThread(new CreatedNewEditorObject<T>(vm));
        }

        protected abstract T CreateNew();
        protected virtual void BeforeSaveChanges() { }
        protected virtual void AfterSaveChanges() { }

        public void Handle(DataObjectImported message)
        {
            if (message.DataObject.GetType() != typeof(T)) return;

            T data = (T)message.DataObject;
            data.FileName = Guid.NewGuid() + ".json";

            string json = JsonConvert.SerializeObject(data);
            FolderAttribute folderAttribute = data.GetAttributeByType<FolderAttribute>();

            string path = "./Data/" + folderAttribute.Folder + "/" + data.FileName;
            File.WriteAllText(path, json);
            ObjectListVM.DataObjects.Add(data);
        }
    }
}
