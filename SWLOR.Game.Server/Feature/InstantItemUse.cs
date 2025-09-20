
using SWLOR.NWN.API.NWNX;
using SWLOR.Shared.Core.Event;

namespace SWLOR.Game.Server.Feature
{
    public static class InstantItemUse
    {
        /// <summary>
        /// Before an item is used, if the item has a script specified, it will be run instantly.
        /// This will bypass the "Use Item" animation items normally have.
        /// </summary>
        [ScriptHandler(ScriptName.OnItemUseBefore)]
        public static void OnUseItem()
        {
            var creature = OBJECT_SELF;
            var item = StringToObject(EventsPlugin.GetEventData("ITEM_OBJECT_ID"));
            var script = GetLocalString(item, "SCRIPT");

            // No script associated. Let it run the normal execution process.
            if (string.IsNullOrWhiteSpace(script)) return;

            EventsPlugin.SkipEvent();
            EventsPlugin.SetEventResult("0"); // Prevents the "You cannot use that item" error message from being sent.
            ExecuteScript(script, creature);
        }
    }
}
