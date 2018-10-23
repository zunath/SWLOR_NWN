using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Caliburn.Micro;
using Newtonsoft.Json;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Tools.Editor.Messages;
using SWLOR.Tools.Editor.ValueObjects;
using SWLOR.Tools.Editor.ViewModels.Contracts;

namespace SWLOR.Tools.Editor.ViewModels
{
    public class ObjectListViewModel<T>: 
        PropertyChangedBase, 
        IObjectListViewModel<T>, 
        IHandle<ApplicationStartedMessage>
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
        
        private string _displayName;

        public string DisplayName
        {
            get => _displayName;
            set
            {
                _displayName = value;
                NotifyOfPropertyChange(() => DisplayName);
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
                _eventAggregator.PublishOnUIThread(new EditorObjectSelectedMessage<T>(SelectedDataObject));
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

        public void Handle(ApplicationStartedMessage message)
        {
            foreach (var file in Directory.GetFiles("./Data/" + typeof(T).Name))
            {
                var json = File.ReadAllText(file);
                T data = JsonConvert.DeserializeObject<T>(json);
                DataObjects.Add(data);
            }

        }
    }
}
