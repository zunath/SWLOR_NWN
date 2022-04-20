using System;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.DialogService
{
    public class DialogPage
    {
        public string Header { get; set; }
        public Action<DialogPage> PageInit { get; set; }
        public List<DialogResponse> Responses { get; set; }

        public DialogPage(Action<DialogPage> pageInit)
        {
            PageInit = pageInit;
            Responses = new List<DialogResponse>();
            Header = string.Empty;
        }

        public DialogPage AddResponse(string text, Action action)
        {
            Responses.Add(new DialogResponse(text, action));
            return this;
        }

        public int NumberOfResponses => Responses.Count;
    }
}
