using System;
using NWN;
using SWLOR.Game.Server.Conversation.Contracts;
using SWLOR.Game.Server.GameObject;

using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;
using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Conversation
{
    public abstract class ConversationBase: IConversation
    {
        protected readonly INWScript _;
        private readonly IDialogService _dialog;

        protected ConversationBase(INWScript script, IDialogService dialog)
        {
            _ = script;
            _dialog = dialog;
        }

        protected NWPlayer GetPC()
        {
            return (_.GetPCSpeaker());
        }

        protected NWObject GetDialogTarget()
        {
            PlayerDialog dialog = _dialog.LoadPlayerDialog(GetPC().GlobalID);
            return dialog.DialogTarget;
        }

        private CustomData GetDialogCustomData()
        {
            PlayerDialog dialog = _dialog.LoadPlayerDialog(GetPC().GlobalID);
            return dialog.CustomData;
        }

        protected T GetDialogCustomData<T>(string key = "")
        {
            CustomData data = GetDialogCustomData();
            if (!data.ContainsKey(key))
            {
                var instance = (T)Activator.CreateInstance(typeof(T));
                SetDialogCustomData(key, instance);
            }
            return (T)GetDialogCustomData()[key];
        }

        protected void SetDialogCustomData(dynamic value)
        {
            CustomData data = GetDialogCustomData();
            data[""] = value;
        }

        protected void SetDialogCustomData(string key, dynamic value)
        {
            CustomData data = GetDialogCustomData();
            data[key] = value;
        }

        protected void ChangePage(string pageName)
        {
            PlayerDialog dialog = _dialog.LoadPlayerDialog(GetPC().GlobalID);
            dialog.CurrentPageName = pageName;
            dialog.PageOffset = 0;
        }

        protected void SetPageHeader(string pageName, string header)
        {
            PlayerDialog dialog = _dialog.LoadPlayerDialog(GetPC().GlobalID);
            DialogPage page = dialog.GetPageByName(pageName);
            page.Header = header;
        }

        protected DialogPage GetPageByName(string pageName)
        {
            PlayerDialog dialog = _dialog.LoadPlayerDialog(GetPC().GlobalID);
            return dialog.GetPageByName(pageName);
        }

        protected DialogPage GetCurrentPage()
        {
            return GetPageByName(GetCurrentPageName());
        }

        protected string GetCurrentPageName()
        {
            PlayerDialog dialog = _dialog.LoadPlayerDialog(GetPC().GlobalID);
            return dialog.CurrentPageName;
        }

        protected DialogResponse GetResponseByID(string pageName, int responseID)
        {
            PlayerDialog dialog = _dialog.LoadPlayerDialog(GetPC().GlobalID);
            DialogPage page = dialog.GetPageByName(pageName);
            return page.Responses[responseID - 1];
        }

        protected void SetResponseText(string pageName, int responseID, string responseText)
        {
            PlayerDialog dialog = _dialog.LoadPlayerDialog(GetPC().GlobalID);
            DialogPage page = dialog.GetPageByName(pageName);
            page.Responses[responseID - 1].Text = responseText;
        }

        protected void SetResponseVisible(string pageName, int responseID, bool isVisible)
        {
            PlayerDialog dialog = _dialog.LoadPlayerDialog(GetPC().GlobalID);
            DialogPage page = dialog.GetPageByName(pageName);
            page.Responses[responseID - 1].IsActive = isVisible;
        }

        protected void AddResponseToPage(string pageName, string text, bool isVisible = true,
            params Tuple<string, dynamic>[] customData)
        {
            PlayerDialog dialog = _dialog.LoadPlayerDialog(GetPC().GlobalID);
            DialogPage page = dialog.GetPageByName(pageName);
            page.Responses.Add(new DialogResponse(text, isVisible, customData));
        }

        protected void AddResponseToPage(string pageName, string text, bool isVisible = true, dynamic customData = null)
        {
            AddResponseToPage(pageName, text, isVisible, new Tuple<string, dynamic>(string.Empty, customData));
        }

        protected void AddResponseToPage(string pageName, DialogResponse response)
        {
            PlayerDialog dialog = _dialog.LoadPlayerDialog(GetPC().GlobalID);
            DialogPage page = dialog.GetPageByName(pageName);
            page.Responses.Add(response);
        }

        protected void ClearPageResponses(string pageName)
        {
            PlayerDialog dialog = _dialog.LoadPlayerDialog(GetPC().GlobalID);
            DialogPage page = dialog.GetPageByName(pageName);
            page.Responses.Clear();
        }

        protected void SwitchConversation(string conversationName)
        {
            PlayerDialog dialog = _dialog.LoadPlayerDialog(GetPC().GlobalID);
            _dialog.LoadConversation(GetPC(), dialog.DialogTarget, conversationName, dialog.DialogNumber);
            dialog = _dialog.LoadPlayerDialog(GetPC().GlobalID);
            
            dialog.ResetPage();
            ChangePage(dialog.CurrentPageName);
            
            App.ResolveByInterface<IConversation>("Conversation." + dialog.ActiveDialogName, convo =>
            {
                convo.Initialize();
                GetPC().SetLocalInt("DIALOG_SYSTEM_INITIALIZE_RAN", 1);
            });
            
        }

        protected void EndConversation()
        {
            _dialog.EndConversation(GetPC());
        }

        public abstract PlayerDialog SetUp(NWPlayer player);

        public abstract void Initialize();

        public abstract void DoAction(NWPlayer player, string pageName, int responseID);

        public abstract void EndDialog();
    }
}
