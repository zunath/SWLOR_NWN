using System;
using System.Collections.ObjectModel;
using Caliburn.Micro;
using SWLOR.Tools.Editor.Enumeration;

namespace SWLOR.Tools.Editor.ViewModels.Data
{
    public class ResourceGroup: PropertyChangedBase
    {
        public ResourceGroup(string name, ResourceType resourceType, string folderName, Type type, string displayName = "Name")
        {
            Name = name;
            ResourceType = resourceType;
            FolderName = folderName;
            Type = type;
            TargetCollection = new ObservableCollection<dynamic>();
            DisplayName = displayName;
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
        
        private ResourceType _resourceType;
        public ResourceType ResourceType
        {
            get => _resourceType;
            set
            {
                _resourceType = value;
                NotifyOfPropertyChange(() => ResourceType);
            }
        }

        private string _folderName;
        public string FolderName
        {
            get => _folderName;
            set
            {
                _folderName = value;
                NotifyOfPropertyChange(() => FolderName);
            }
        }

        public Type Type { get; set; }

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

        private ObservableCollection<dynamic> _targetCollection;
        public ObservableCollection<dynamic> TargetCollection
        {
            get => _targetCollection;
            set
            {
                _targetCollection = value;
                NotifyOfPropertyChange(() => TargetCollection);
            }
        }
    }
}
