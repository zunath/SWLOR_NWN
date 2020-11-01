using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Legacy.Conversation.Contracts;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Service;
using SWLOR.Game.Server.Legacy.ValueObject;
using SWLOR.Game.Server.Legacy.ValueObject.Dialog;

namespace SWLOR.Game.Server.Legacy.Conversation
{
    public abstract class ConversationBase: IConversation
    {
        protected NWPlayer GetPC()
        {
            return (NWScript.GetPCSpeaker());
        }

        protected NWObject GetDialogTarget()
        {
            var dialog = DialogService.LoadPlayerDialog(GetPC().GlobalID);
            return dialog.DialogTarget;
        }

        private CustomData GetDialogCustomData()
        {
            var dialog = DialogService.LoadPlayerDialog(GetPC().GlobalID);
            return dialog.CustomData;
        }

        protected T GetDialogCustomData<T>(string key = "")
        {
            var data = GetDialogCustomData();
            if (!data.ContainsKey(key))
            {
                var instance = (T)Activator.CreateInstance(typeof(T));
                SetDialogCustomData(key, instance);
            }
            return (T)GetDialogCustomData()[key];
        }

        protected void SetDialogCustomData(dynamic value)
        {
            var data = GetDialogCustomData();
            data[""] = value;
        }

        protected void SetDialogCustomData(string key, dynamic value)
        {
            var data = GetDialogCustomData();
            data[key] = value;
        }

        protected void ChangePage(string pageName, bool updateNavigationStack = true)
        {
            var dialog = DialogService.LoadPlayerDialog(GetPC().GlobalID);

            if(updateNavigationStack && dialog.EnableBackButton)
                dialog.NavigationStack.Push(new DialogNavigation(dialog.CurrentPageName, dialog.ActiveDialogName));
            dialog.CurrentPageName = pageName;
            dialog.PageOffset = 0;
        }

        protected void SetPageHeader(string pageName, string header)
        {
            var dialog = DialogService.LoadPlayerDialog(GetPC().GlobalID);
            var page = dialog.GetPageByName(pageName);
            page.Header = header;
        }

        protected DialogPage GetPageByName(string pageName)
        {
            var dialog = DialogService.LoadPlayerDialog(GetPC().GlobalID);
            return dialog.GetPageByName(pageName);
        }

        protected DialogPage GetCurrentPage()
        {
            return GetPageByName(GetCurrentPageName());
        }

        protected string GetCurrentPageName()
        {
            var dialog = DialogService.LoadPlayerDialog(GetPC().GlobalID);
            return dialog.CurrentPageName;
        }

        protected DialogResponse GetResponseByID(string pageName, int responseID)
        {
            var dialog = DialogService.LoadPlayerDialog(GetPC().GlobalID);
            var page = dialog.GetPageByName(pageName);
            return page.Responses[responseID - 1];
        }

        protected void SetResponseText(string pageName, int responseID, string responseText)
        {
            var dialog = DialogService.LoadPlayerDialog(GetPC().GlobalID);
            var page = dialog.GetPageByName(pageName);
            page.Responses[responseID - 1].Text = responseText;
        }

        protected void SetResponseVisible(string pageName, int responseID, bool isVisible)
        {
            var dialog = DialogService.LoadPlayerDialog(GetPC().GlobalID);
            var page = dialog.GetPageByName(pageName);
            page.Responses[responseID - 1].IsActive = isVisible;
        }

        protected void AddResponseToPage(string pageName, string text, bool isVisible = true, object customData = null)
        {
            var dialog = DialogService.LoadPlayerDialog(GetPC().GlobalID);
            var page = dialog.GetPageByName(pageName);
            page.Responses.Add(new DialogResponse(text, isVisible, customData));
        }

        protected void AddResponseToPage(string pageName, DialogResponse response)
        {
            var dialog = DialogService.LoadPlayerDialog(GetPC().GlobalID);
            var page = dialog.GetPageByName(pageName);
            page.Responses.Add(response);
        }

        protected void ClearPageResponses(string pageName)
        {
            var dialog = DialogService.LoadPlayerDialog(GetPC().GlobalID);
            var page = dialog.GetPageByName(pageName);
            page.Responses.Clear();
        }

        protected void SwitchConversation(string conversationName)
        {
            var dialog = DialogService.LoadPlayerDialog(GetPC().GlobalID);
            Stack<DialogNavigation> navigationStack = null;

            if (dialog.EnableBackButton)
            {
                navigationStack = dialog.NavigationStack;
                navigationStack.Push(new DialogNavigation(dialog.CurrentPageName, dialog.ActiveDialogName));
            }
            DialogService.LoadConversation(GetPC(), dialog.DialogTarget, conversationName, dialog.DialogNumber);
            dialog = DialogService.LoadPlayerDialog(GetPC().GlobalID);
            
            if(dialog.EnableBackButton && navigationStack != null)
                dialog.NavigationStack = navigationStack;

            dialog.ResetPage();
            ChangePage(dialog.CurrentPageName, false);

            var conversation = DialogService.GetConversation(dialog.ActiveDialogName);
            conversation.Initialize();
            GetPC().SetLocalInt("DIALOG_SYSTEM_INITIALIZE_RAN", 1);
        }

        protected void ToggleBackButton(bool isOn)
        {
            var dialog = DialogService.LoadPlayerDialog(GetPC().GlobalID);
            dialog.EnableBackButton = isOn;
            dialog.NavigationStack.Clear();
        }

        protected Stack<DialogNavigation> NavigationStack
        {
            get
            {
                var dialog = DialogService.LoadPlayerDialog(GetPC().GlobalID);
                return dialog.NavigationStack;
            }
            set
            {
                var dialog = DialogService.LoadPlayerDialog(GetPC().GlobalID);
                dialog.NavigationStack = value;
            }
        }

        protected void ClearNavigationStack()
        {
            var dialog = DialogService.LoadPlayerDialog(GetPC().GlobalID);
            dialog.NavigationStack.Clear();
        }

        protected void EndConversation()
        {
            DialogService.EndConversation(GetPC());
        }

        public abstract PlayerDialog SetUp(NWPlayer player);

        public abstract void Initialize();

        public abstract void DoAction(NWPlayer player, string pageName, int responseID);
        
        public abstract void Back(NWPlayer player, string beforeMovePage, string afterMovePage);

        public abstract void EndDialog();
    }
}
