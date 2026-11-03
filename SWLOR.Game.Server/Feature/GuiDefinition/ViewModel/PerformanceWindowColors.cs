using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    /// <summary>The colors the Performance windows share, matching the gold, green and amber of the wireframes.</summary>
    public static class PerformanceWindowColors
    {
        public static readonly GuiColor Gold = new(232, 181, 80);
        public static readonly GuiColor Green = new(110, 200, 120);
        public static readonly GuiColor Amber = new(240, 170, 60);
        public static readonly GuiColor Dim = new(150, 150, 150);
        public static readonly GuiColor White = new(255, 255, 255);
    }
}
