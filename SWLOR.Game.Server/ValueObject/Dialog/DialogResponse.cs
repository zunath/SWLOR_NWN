namespace SWLOR.Game.Server.ValueObject.Dialog
{
    public class DialogResponse
    {
        public string Text { get; set; }
        public bool IsActive { get; set; }
        public object CustomData { get; set; }

        public DialogResponse(string text, bool isVisible = true, object customData = null)
        {
            Text = text;
            IsActive = isVisible;
            CustomData = customData;
        }

        public bool HasCustomData => CustomData != null;

    }
}
