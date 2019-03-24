using System;
using System.Collections.Generic;
using NWN;
using SWLOR.Game.Server.Conversation.Contracts;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

using SWLOR.Game.Server.ValueObject;
using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Conversation
{
    public abstract class ConversationBase: IConversation
    {
        protected NWPlayer GetPC()
        {
            return (_.GetPCSpeaker());
        }

        protected NWObject GetDialogTarget()
        {
            PlayerDialog dialog = DialogService.LoadPlayerDialog(GetPC().GlobalID);
            return dialog.DialogTarget;
        }

        private CustomData GetDialogCustomData()
        {
            PlayerDialog dialog = DialogService.LoadPlayerDialog(GetPC().GlobalID);
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

        protected void ChangePage(string pageName, bool updateNavigationStack = true)
        {
            PlayerDialog dialog = DialogService.LoadPlayerDialog(GetPC().GlobalID);

            if(updateNavigationStack && dialog.EnableBackButton)
                dialog.NavigationStack.Push(new DialogNavigation(dialog.CurrentPageName, dialog.ActiveDialogName));
            dialog.CurrentPageName = pageName;
            dialog.PageOffset = 0;
        }

        protected void SetPageHeader(string pageName, string header)
        {
            PlayerDialog dialog = DialogService.LoadPlayerDialog(GetPC().GlobalID);
            DialogPage page = dialog.GetPageByName(pageName);
            page.Header = header;
        }

        protected DialogPage GetPageByName(string pageName)
        {
            PlayerDialog dialog = DialogService.LoadPlayerDialog(GetPC().GlobalID);
            return dialog.GetPageByName(pageName);
        }

        protected DialogPage GetCurrentPage()
        {
            return GetPageByName(GetCurrentPageName());
        }

        protected string GetCurrentPageName()
        {
            PlayerDialog dialog = DialogService.LoadPlayerDialog(GetPC().GlobalID);
            return dialog.CurrentPageName;
        }

        protected DialogResponse GetResponseByID(string pageName, int responseID)
        {
            PlayerDialog dialog = DialogService.LoadPlayerDialog(GetPC().GlobalID);
            DialogPage page = dialog.GetPageByName(pageName);
            return page.Responses[responseID - 1];
        }

        protected void SetResponseText(string pageName, int responseID, string responseText)
        {
            PlayerDialog dialog = DialogService.LoadPlayerDialog(GetPC().GlobalID);
            DialogPage page = dialog.GetPageByName(pageName);
            page.Responses[responseID - 1].Text = responseText;
        }

        protected void SetResponseVisible(string pageName, int responseID, bool isVisible)
        {
            PlayerDialog dialog = DialogService.LoadPlayerDialog(GetPC().GlobalID);
            DialogPage page = dialog.GetPageByName(pageName);
            page.Responses[responseID - 1].IsActive = isVisible;
        }

        protected void AddResponseToPage(string pageName, string text, bool isVisible = true, object customData = null)
        {
            PlayerDialog dialog = DialogService.LoadPlayerDialog(GetPC().GlobalID);
            DialogPage page = dialog.GetPageByName(pageName);
            page.Responses.Add(new DialogResponse(text, isVisible, customData));
        }

        protected void AddResponseToPage(string pageName, DialogResponse response)
        {
            PlayerDialog dialog = DialogService.LoadPlayerDialog(GetPC().GlobalID);
            DialogPage page = dialog.GetPageByName(pageName);
            page.Responses.Add(response);
        }

        protected void ClearPageResponses(string pageName)
        {
            PlayerDialog dialog = DialogService.LoadPlayerDialog(GetPC().GlobalID);
            DialogPage page = dialog.GetPageByName(pageName);
            page.Responses.Clear();
        }

        protected void SwitchConversation(string conversationName)
        {
            PlayerDialog dialog = DialogService.LoadPlayerDialog(GetPC().GlobalID);
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
            PlayerDialog dialog = DialogService.LoadPlayerDialog(GetPC().GlobalID);
            dialog.EnableBackButton = isOn;
            dialog.NavigationStack.Clear();
        }

        protected Stack<DialogNavigation> NavigationStack
        {
            get
            {
                PlayerDialog dialog = DialogService.LoadPlayerDialog(GetPC().GlobalID);
                return dialog.NavigationStack;
            }
            set
            {
                PlayerDialog dialog = DialogService.LoadPlayerDialog(GetPC().GlobalID);
                dialog.NavigationStack = value;
            }
        }

        protected void ClearNavigationStack()
        {
            PlayerDialog dialog = DialogService.LoadPlayerDialog(GetPC().GlobalID);
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
