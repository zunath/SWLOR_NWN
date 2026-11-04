using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent
{
    /// <summary>
    /// A chat command showed or hid a creature's helmet or cloak, so an open Appearance Editor shows the new setting.
    /// </summary>
    public class ArmorDisplayChangedRefreshEvent : IGuiRefreshEvent
    {
        public uint Creature { get; }
        public bool Helmet { get; }
        public bool Shown { get; }

        public ArmorDisplayChangedRefreshEvent(uint creature, bool helmet, bool shown)
        {
            Creature = creature;
            Helmet = helmet;
            Shown = shown;
        }
    }
}
