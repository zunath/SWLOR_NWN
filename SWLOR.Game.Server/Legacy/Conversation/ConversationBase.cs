using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Legacy.Conversation.Contracts;
using SWLOR.Game.Server.Legacy.GameObject;
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
            return null;
        }

        private CustomData GetDialogCustomData()
        {
            return null;
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
            return;
        }

        protected void SetPageHeader(string pageName, string header)
        {
            return;
        }

        protected DialogPage GetPageByName(string pageName)
        {
            return null;
        }

        protected DialogPage GetCurrentPage()
        {
            return GetPageByName(GetCurrentPageName());
        }

        protected string GetCurrentPageName()
        {
            return null;
        }

        protected DialogResponse GetResponseByID(string pageName, int responseID)
        {
            return null;
        }

        protected void SetResponseText(string pageName, int responseID, string responseText)
        {
            return;
        }

        protected void SetResponseVisible(string pageName, int responseID, bool isVisible)
        {
            return;
        }

        protected void AddResponseToPage(string pageName, string text, bool isVisible = true, object customData = null)
        {
            return;
        }

        protected void AddResponseToPage(string pageName, DialogResponse response)
        {
            return;
        }

        protected void ClearPageResponses(string pageName)
        {
            return;
        }

        protected void SwitchConversation(string conversationName)
        {
            return;
        }

        protected void ToggleBackButton(bool isOn)
        {

        }

        protected void EndConversation()
        {
        }

        public abstract PlayerDialog SetUp(NWPlayer player);

        public abstract void Initialize();

        public abstract void DoAction(NWPlayer player, string pageName, int responseID);
        
        public abstract void Back(NWPlayer player, string beforeMovePage, string afterMovePage);

        public abstract void EndDialog();
    }
}
