using System;
using Caliburn.Micro;
using SWLOR.Tools.Editor.Enumeration;

namespace SWLOR.Tools.Editor.ViewModels.Data
{
    public class ResourceGroup: PropertyChangedBase
    {
        public ResourceGroup(string name, ResourceType resourceType, string folderName, Type type)
        {
            Name = name;
            ResourceType = resourceType;
            FolderName = folderName;
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
    }
}
