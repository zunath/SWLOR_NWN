using SWLOR.Shared.Dialog.Model;

namespace SWLOR.Shared.Dialog.Service;

public interface IDialogBuilder
{
    IDialogBuilder AddInitializationAction(Action initializationAction);
    IDialogBuilder WithDataModel(object dataModel);
    IDialogBuilder AddBackAction(Action<string, string> backAction);
    IDialogBuilder AddEndAction(Action endAction);
    IDialogBuilder AddPage(string pageId, Action<DialogPage> initAction);
    PlayerDialog Build();
}