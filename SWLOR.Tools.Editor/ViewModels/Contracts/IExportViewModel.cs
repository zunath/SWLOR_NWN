using System.Collections.ObjectModel;
using SWLOR.Tools.Editor.ViewModels.Data;

namespace SWLOR.Tools.Editor.ViewModels.Contracts
{
    public interface IExportViewModel
    {
        ObservableCollection<dynamic> AddedResources { get; set; }
        ObservableCollection<dynamic> AvailableResources { get; set; }
        bool IsAddEnabled { get; }
        bool IsExportEnabled { get; }
        bool IsRemoveEnabled { get; }
        string PackageName { get; set; }
        ObservableCollection<ResourceGroup> ResourceGroups { get; set; }
        object SelectedAddedResource { get; set; }
        ResourceGroup SelectedAddedResourceGroup { get; set; }
        object SelectedAvailableResource { get; set; }
        ResourceGroup SelectedAvailableResourceGroup { get; set; }

        void LoadAvailableResources();
        void AddResource();
        void Cancel();
        void Export();
        void RemoveResource();
    }
}
