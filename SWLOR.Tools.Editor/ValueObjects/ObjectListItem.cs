using Caliburn.Micro;

namespace SWLOR.Tools.Editor.ValueObjects
{
    public class ObjectListItem: PropertyChangedBase
    {
        public ObjectListItem(object dataObject, string name)
        {
            DataObject = dataObject;
            Name = name;
        }


        private object _dataObject;
        public object DataObject
        {
            get => _dataObject;
            set
            {
                _dataObject = value;
                NotifyOfPropertyChange(() => DataObject);
                NotifyOfPropertyChange(() => Name);
            }
        }

        private string _name;

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                NotifyOfPropertyChange(() => Name);
            }
        }

    }
}
