using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.PerformanceService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>The rows the Footwork and Session windows share: the audience summary and the Roleplay mode note.</summary>
    public static class PerformanceCueWindowParts
    {
        private const float AudienceGetsHeight = 96f;
        private const float RoleplayNoteHeight = 92f;

        public static readonly string RoleplayNote =
            "Roleplay mode: no cues to follow. Your audience banks Inspired at the Rough rate. " +
            $"Talk or emote at least once every {(int)(PerformanceMath.RoleplayIdleTimeoutSeconds / 60)} minutes to keep performing. " +
            "Options on your hot bar switches back to active play.";

        /// <summary>What a follower leaves with and how fast it banks, in a bordered box under the header.</summary>
        public static void AddAudienceGets<TModel>(GuiColumn<TModel> content)
            where TModel : PerformanceCueViewModelBase<TModel>
        {
            content.AddRow(row => row.AddText()
                .BindText(model => model.AudienceGetsText)
                .SetShowBorder(true)
                .SetScrollbars(NuiScrollbars.None)
                .SetHeight(AudienceGetsHeight));
        }

        public static void AddRoleplayNote<TModel>(GuiColumn<TModel> content)
            where TModel : PerformanceCueViewModelBase<TModel>
        {
            content.AddRow(row => row.AddText()
                .SetText(RoleplayNote)
                .SetShowBorder(false)
                .SetScrollbars(NuiScrollbars.None)
                .SetHeight(RoleplayNoteHeight));
        }

        public static void AddInspiring<TModel>(GuiColumn<TModel> content)
            where TModel : PerformanceCueViewModelBase<TModel>
        {
            content.AddRow(row => row.AddLabel()
                .BindText(model => model.InspiringText)
                .SetHeight(22f)
                .SetHorizontalAlign(NuiHorizontalAlign.Left));
        }
    }
}
