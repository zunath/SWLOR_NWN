﻿using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Caliburn.Micro;
using Newtonsoft.Json;
using SWLOR.Tools.Editor.Attributes;
using SWLOR.Tools.Editor.Extensions;
using SWLOR.Tools.Editor.Messages;
using SWLOR.Tools.Editor.ViewModels.Contracts;

namespace SWLOR.Tools.Editor.ViewModels
{
    public class ObjectListViewModel<T>:
        PropertyChangedBase,
        IObjectListViewModel<T>,
        IHandle<ApplicationStarted>,
        IHandle<DataObjectsLoadedFromDisk>,
        IHandle<RenamedEditorObject<T>>,
        IHandle<CreatedNewEditorObject<T>>
        where T: IDBObjectViewModel
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IYesNoViewModel _yesNo;
        private readonly IWindowManager _windowManager;
        private readonly SynchronizationContext _sync;

        public ObjectListViewModel(
            IEventAggregator eventAggregator,
            IYesNoViewModel yesNo,
            IWindowManager windowManager)
        {
            DataObjects = new ObservableCollection<T>();
            _eventAggregator = eventAggregator;
            _yesNo = yesNo;
            _windowManager = windowManager;
            _sync = SynchronizationContext.Current;

            MaxRenameLength = 32;
            eventAggregator.Subscribe(this);
        }

        private ObservableCollection<T> _dataObjects;
        public ObservableCollection<T> DataObjects
        {
            get => _dataObjects;
            set
            {
                _dataObjects = value;
                NotifyOfPropertyChange(() => DataObjects);
            }
        }
        
        private T _selectedDataObject;

        public T SelectedDataObject
        {
            get => _selectedDataObject;
            set
            {
                var oldObject = _selectedDataObject;
                _selectedDataObject = value;
                NotifyOfPropertyChange(() => SelectedDataObject);
                NotifyOfPropertyChange(() => IsObjectSelected);

                _eventAggregator.PublishOnUIThread(new EditorObjectSelected<T>(oldObject, SelectedDataObject));
            }
        }

        private int _maxRenameLength;

        public int MaxRenameLength
        {
            get => _maxRenameLength;
            set
            {
                _maxRenameLength = value;
                NotifyOfPropertyChange(() => MaxRenameLength);
            }
        }

        public bool IsObjectSelected => SelectedDataObject != null;

        public void New()
        {
            _eventAggregator.PublishOnUIThread(new RequestNewEditorObject<T>());
        }
        
        public void Delete()
        {
            _yesNo.Prompt = "Are you sure you want to delete this object?";
            _windowManager.ShowDialog(_yesNo);

            if (_yesNo.Result == DialogResult.Yes)
            {
                var folderAttribute = SelectedDataObject.GetAttributeByType<FolderAttribute>();
                string path = "./Data/" + folderAttribute.Folder + "/" + SelectedDataObject.FileName;
                File.Delete(path);
                _eventAggregator.PublishOnUIThread(new DeleteEditorObject<T>(SelectedDataObject));
                DataObjects.Remove(SelectedDataObject);
            }
        }

        public void Handle(ApplicationStarted message)
        {
            LoadFiles();
        }

        private string GetFolderName()
        {
            FolderAttribute folderAttribute = typeof(T).GetCustomAttributes(typeof(FolderAttribute), false).FirstOrDefault() as FolderAttribute;

            if (folderAttribute == null)
            {
                throw new NullReferenceException("Unable to find " + nameof(FolderAttribute) + " attribute on data object.");
            }

            return folderAttribute.Folder;
        }

        private void LoadFiles()
        {
            string path = "./Data/" + GetFolderName();
            var files = Directory.GetFiles(path);
            foreach (var file in files)
            {
                var json = File.ReadAllText(file);
                T data = JsonConvert.DeserializeObject<T>(json);
                data.FileName = Path.GetFileName(file);

                DataObjects.Add(data);
            }

        }
        
        public void Handle(DataObjectsLoadedFromDisk message)
        {
            if (message.Folder != GetFolderName()) return;

            _sync.Post(op =>
            {
                DataObjects.Clear();
                LoadFiles();
            }, null);
        }

        public void Handle(RenamedEditorObject<T> message)
        {
            if (SelectedDataObject == null) return;

            SelectedDataObject.DisplayName = message.Object.DisplayName;
        }

        public void Handle(CreatedNewEditorObject<T> message)
        {
            message.Object.FileName = Guid.NewGuid() + ".json";
            DataObjects.Add(message.Object);
            SelectedDataObject = message.Object;

            string path = "./Data/" + GetFolderName() + "/" + message.Object.FileName;
            string json = JsonConvert.SerializeObject(message.Object);
            File.WriteAllText(path, json);
        }
    }
}
