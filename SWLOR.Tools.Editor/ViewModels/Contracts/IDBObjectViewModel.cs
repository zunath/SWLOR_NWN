using System;

namespace SWLOR.Tools.Editor.ViewModels.Contracts
{
    public interface IDBObjectViewModel
    {
        string InternalEditorID { get; set; }
        string FileName { get; set; }
        string DisplayName { get; set; }
        bool IsDirty { get; set; }
        void DiscardChanges();
        void RefreshTrackedProperties();
        event EventHandler OnDirty;
    }
}
