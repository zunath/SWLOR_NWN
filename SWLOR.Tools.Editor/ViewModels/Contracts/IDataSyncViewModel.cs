namespace SWLOR.Tools.Editor.ViewModels.Contracts
{
    public interface IDataSyncViewModel
    {
        IDatabaseConnectionViewModel DatabaseConnectionVM { get; set; }
        bool DatabaseControlsEnabled { get; set; }
    }
}
