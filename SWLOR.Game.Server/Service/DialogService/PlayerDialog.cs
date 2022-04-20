using System;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.DialogService
{
    public class PlayerDialog
    {
        public List<Action> InitializationActions { get; set; }
        public List<Action<string, string>> BackActions { get; set; }
        public List<Action> EndActions { get; set; }
        public Dictionary<string, DialogPage> Pages { get; private set; }
        public string CurrentPageName { get; set; }
        public Stack<DialogNavigation> NavigationStack { get; set; }
        public int PageOffset { get; set; }
        public string ActiveDialogName { get; set; }
        public uint DialogTarget { get; set; }
        public object DataModel { get; set; }
        public bool IsEnding { get; set; }
        public string DefaultPageName { get; set; }
        public int DialogNumber { get; set; }
        public bool EnableBackButton { get; set; }

        public PlayerDialog(string defaultPageName)
        {
            InitializationActions = new List<Action>();
            BackActions = new List<Action<string, string>>();
            EndActions = new List<Action>();
            Pages = new Dictionary<string, DialogPage>();
            CurrentPageName = string.Empty;
            NavigationStack = new Stack<DialogNavigation>();
            PageOffset = 0;
            ActiveDialogName = string.Empty;
            DefaultPageName = defaultPageName;
            DataModel = null;
            EnableBackButton = true;
        }

        public DialogPage AddPage(DialogPage page, string pageName = "MainPage")
        {
            Pages.Add(pageName, page);
            if (Pages.Count == 1)
            {
                CurrentPageName = pageName;
            }

            return page;
        }

        public DialogPage CurrentPage => Pages[CurrentPageName];

        public DialogPage GetPageByName(string pageName)
        {
            return Pages[pageName];
        }

        public void ResetPage()
        {
            CurrentPageName = DefaultPageName;
        }

    }
}
