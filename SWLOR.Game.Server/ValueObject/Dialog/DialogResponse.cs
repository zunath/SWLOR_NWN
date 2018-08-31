using System;

namespace SWLOR.Game.Server.ValueObject.Dialog
{
    public class DialogResponse
    {
        public string Text { get; set; }
        public bool IsActive { get; set; }
        public CustomData CustomData { get; set; }

        public DialogResponse(string text, bool isVisible = true, params Tuple<string, dynamic>[] customData)
        {
            Text = text;
            IsActive = isVisible;
            CustomData = new CustomData();

            if (customData == null) return;
            foreach (var data in customData)
            {
                CustomData[data.Item1] = data.Item2;
            }
        }

        public bool HasCustomData => CustomData.Count > 0;

    }
}
