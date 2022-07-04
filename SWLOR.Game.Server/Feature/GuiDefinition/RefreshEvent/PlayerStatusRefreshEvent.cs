using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent
{
    internal class PlayerStatusRefreshEvent: IGuiRefreshEvent
    {
        internal enum StatType
        {
            HP = 1,
            FP = 2,
            STM = 3,

            Shield = 4,
            Hull = 5,
            Capacitor = 6,
        }

        public StatType Type { get; set; }

        public PlayerStatusRefreshEvent(StatType type)
        {
            Type = type;
        }
    }
}
