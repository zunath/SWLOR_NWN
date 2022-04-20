using System;

namespace SWLOR.Game.Server.Service.DialogService
{
    public class DialogResponse
    {
        public string Text { get; set; }
        public bool IsActive { get; set; }
        public Action Action { get; set; }
        public object Data { get; set; }
        
        public DialogResponse(
            string text, 
            Action action, 
            bool isVisible = true,
            object data = null)
        {
            Text = text;
            IsActive = isVisible;
            Action = action;
            Data = data;
        }
    }
}
