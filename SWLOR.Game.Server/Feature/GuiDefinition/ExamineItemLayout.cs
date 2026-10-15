using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>Builds the item inspection content: the icon beside its facts, then the sectioned body, in one scrolling group.</summary>
    public static class ExamineItemLayout
    {
        private const float BodyWidth = 360f;
        private const float IconBoxSize = 64f;
        private const float HeaderGap = 12f;
        private const float FactsWidth = BodyWidth - IconBoxSize - HeaderGap;
        private const float SlotSize = 32f;
        private const float MaxTextureHeight = 128f;
        private const float SectionGap = 10f;
        // The glyph table runs slightly narrow for the client font, so wrap short of the label edge.
        private const float WrapMargin = 12f;

        private static float TextHeight(IReadOnlyCollection<ExamineItemTextRun> runs) =>
            runs.Count == 0 ? 0f : runs.Max(run => run.Y) + ExamineItemText.LineHeight;

        public static Json Build(string iconResref, int slotWidth, int slotHeight, string facts, string body)
        {
            var factsRuns = ExamineItemText.Wrap(facts, FactsWidth - WrapMargin);
            var bodyRuns = ExamineItemText.Wrap(body, BodyWidth - WrapMargin);
            var iconWidth = Math.Clamp(slotWidth, 1, 2) * SlotSize;
            var iconHeight = Math.Clamp(slotHeight, 1, 4) * SlotSize;

            var host = new GuiGroup<ExamineItemViewModel>();
            host.SetShowBorder(false)
                .SetScrollbars(NuiScrollbars.Y);
            host.AddColumn(content =>
            {
                content.AddRow(header =>
                {
                    header.AddLabel()
                        .SetText(string.Empty)
                        .SetWidth(IconBoxSize)
                        .SetHeight(iconHeight)
                        .SetMargin(0f)
                        .AddDrawList(draw =>
                        {
                            draw.SetIsConstrainedToTargetBounds(true);
                            // Icon textures are padded to powers of two. Draw them at their own size from the top of
                            // the slot frame like the native inventory; the label clips the transparent padding.
                            if (!string.IsNullOrEmpty(iconResref))
                                draw.AddImage(image => image
                                    .SetResref(iconResref)
                                    .SetPosition((IconBoxSize - iconWidth) / 2f, 0f, iconWidth, MaxTextureHeight)
                                    .SetAspect(NuiAspect.ExactScaled)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                    .SetVerticalAlign(NuiVerticalAlign.Top));
                        });
                    header.AddSpacer()
                        .SetWidth(HeaderGap);
                    AddText(header, factsRuns, FactsWidth);
                });
                if (bodyRuns.Count > 0)
                {
                    content.AddRow(gap => gap.AddLabel().SetText(string.Empty).SetHeight(SectionGap).SetMargin(0f));
                    content.AddRow(row => AddText(row, bodyRuns, BodyWidth));
                }
                // Spacers flex, so this one takes the leftover height instead of the rows above.
                content.AddRow(rest => rest.AddSpacer());
            });
            return host.ToJson();
        }

        private static void AddText(GuiRow<ExamineItemViewModel> row, List<ExamineItemTextRun> runs, float width)
        {
            row.AddLabel()
                .SetText(string.Empty)
                .SetWidth(width)
                .SetHeight(TextHeight(runs))
                .SetMargin(0f)
                .AddDrawList(draw =>
                {
                    draw.SetIsConstrainedToTargetBounds(true);
                    foreach (var run in runs)
                        draw.AddText(item => item
                            .SetText(run.Text)
                            .SetColor(run.Color)
                            .SetBounds(run.X, run.Y, width - run.X, ExamineItemText.LineHeight));
                });
        }
    }
}
