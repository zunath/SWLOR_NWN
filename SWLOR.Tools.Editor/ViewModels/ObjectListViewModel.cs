using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Caliburn.Micro;
using Newtonsoft.Json;
using SWLOR.Tools.Editor.Attributes;
using SWLOR.Tools.Editor.Messages;
using SWLOR.Tools.Editor.ViewModels.Contracts;

namespace SWLOR.Tools.Editor.ViewModels
{
    public class ObjectListViewModel<T>:
        PropertyChangedBase,
        IObjectListViewModel<T>,
        IHandle<ApplicationStarted>
        where T: IDBObjectViewModel
    {
        private readonly IEventAggregator _eventAggregator;

        public ObjectListViewModel(
            IEventAggregator eventAggregator)
        {
            DataObjects = new ObservableCollection<T>();
            _eventAggregator = eventAggregator;
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
                _selectedDataObject = value;
                NotifyOfPropertyChange(() => SelectedDataObject);
                _eventAggregator.PublishOnUIThread(new EditorObjectSelected<T>(SelectedDataObject));
            }
        }
        
        public void New()
        {

        }

        public void Rename()
        {

        }

        public void Delete()
        {

        }

        public void Handle(ApplicationStarted message)
        {
            FolderAttribute folderAttribute = typeof(T).GetCustomAttributes(typeof(FolderAttribute), false).FirstOrDefault() as FolderAttribute;

            if (folderAttribute == null)
            {
                throw new NullReferenceException("Unable to find " + nameof(FolderAttribute) + " attribute on data object.");
            }

            foreach (var file in Directory.GetFiles("./Data/" + folderAttribute.Folder))
            {
                var json = File.ReadAllText(file);
                T data = JsonConvert.DeserializeObject<T>(json);
                data.FileName = Path.GetFileName(file);
                
                DataObjects.Add(data);
            }
        }
    }
}
