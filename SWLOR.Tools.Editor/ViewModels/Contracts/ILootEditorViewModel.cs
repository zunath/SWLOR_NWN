using System.Collections.ObjectModel;
using SWLOR.Tools.Editor.Messages;
using SWLOR.Tools.Editor.ValueObjects;

namespace SWLOR.Tools.Editor.ViewModels.Contracts
{
    public interface ILootEditorViewModel
    {
        void Handle(ApplicationStartedMessage message);
    }
}
