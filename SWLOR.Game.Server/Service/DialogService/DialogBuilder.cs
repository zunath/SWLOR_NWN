using System;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.DialogService
{
    public class DialogBuilder
    {
        private string _defaultPageName;
        private readonly Dictionary<string, DialogPage> _pages = new Dictionary<string, DialogPage>();
        private readonly List<Action> _initializationActions = new List<Action>();
        private readonly List<Action<string, string>> _backActions = new List<Action<string, string>>();
        private readonly List<Action> _endActions = new List<Action>();
        private object _dataModel;

        public DialogBuilder AddInitializationAction(Action initializationAction)
        {
            _initializationActions.Add(initializationAction);

            return this;
        }

        public DialogBuilder WithDataModel(object dataModel)
        {
            _dataModel = dataModel;

            return this;
        }

        public DialogBuilder AddBackAction(Action<string, string> backAction)
        {
            _backActions.Add(backAction);

            return this;
        }

        public DialogBuilder AddEndAction(Action endAction)
        {
            _endActions.Add(endAction);

            return this;
        }

        public DialogBuilder AddPage(string pageId, Action<DialogPage> initAction)
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
