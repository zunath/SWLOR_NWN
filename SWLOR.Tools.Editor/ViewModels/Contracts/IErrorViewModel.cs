namespace SWLOR.Tools.Editor.ViewModels.Contracts
{
    public interface IErrorViewModel
    {
        string ErrorDetails { get; set; }
        void OK();
    }
}
