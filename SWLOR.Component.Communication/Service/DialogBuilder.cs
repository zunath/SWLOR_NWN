using SWLOR.Shared.Dialog.Model;

namespace SWLOR.Shared.Dialog.Service
{
    public class DialogBuilder : IDialogBuilder
    {
        private string _defaultPageName;
        private readonly Dictionary<string, DialogPage> _pages = new();
        private readonly List<Action> _initializationActions = new();
        private readonly List<Action<string, string>> _backActions = new();
        private readonly List<Action> _endActions = new();
        private object _dataModel;

        public IDialogBuilder AddInitializationAction(Action initializationAction)
        {
            _initializationActions.Add(initializationAction);

            return this;
        }

        public IDialogBuilder WithDataModel(object dataModel)
        {
            _dataModel = dataModel;

            return this;
        }

        public IDialogBuilder AddBackAction(Action<string, string> backAction)
        {
            _backActions.Add(backAction);

            return this;
        }

        public IDialogBuilder AddEndAction(Action endAction)
        {
            _endActions.Add(endAction);

            return this;
        }

        public IDialogBuilder AddPage(string pageId, Action<DialogPage> initAction)
        {
            var newPage = new DialogPage(initAction);
            _pages.Add(pageId, newPage);

            if (string.IsNullOrWhiteSpace(_defaultPageName))
                _defaultPageName = pageId;

            return this;
        }

        public PlayerDialog Build()
        {
            var dialog = new PlayerDialog(_defaultPageName)
            {
                InitializationActions = _initializationActions,
                BackActions = _backActions,
                EndActions = _endActions,
                DataModel = _dataModel
            };

            foreach (var (pageId, page) in _pages)
            {
                var dialogPage = dialog.AddPage(page, pageId);
                dialogPage.Responses = page.Responses;
            }

            return dialog;
        }

    }
}
