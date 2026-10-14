using System.Linq.Expressions;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>
    /// The selected-object panel shared by the DM chooser and creator: a portrait or icon, a title and the details text.
    /// </summary>
    public static class DMDetailsRail
    {
        public static void AddHeader<T>(
            GuiColumn<T> col,
            Expression<Func<T, string>> icon,
            Expression<Func<T, string>> title)
            where T : IGuiViewModel
        {
            col.AddRow(header =>
            {
                header.SetHeight(112f);

                header.AddGroup(group =>
                {
                    group.AddImage()
                        .BindResref(icon)
                        .SetHorizontalAlign(NuiHorizontalAlign.Center)
                        .SetVerticalAlign(NuiVerticalAlign.Middle)
                        .SetAspect(NuiAspect.Fit);
                })
                    .SetScrollbars(NuiScrollbars.None)
                    .SetWidth(72f)
                    .SetHeight(104f);

                header.AddColumn(text =>
                {
                    text.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("SELECTED")
                            .SetColor(221, 181, 93)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetHeight(20f);
                    });

                    text.AddRow(row =>
                    {
                        row.AddText()
                            .BindText(title)
                            .SetShowBorder(false)
                            .SetScrollbars(NuiScrollbars.None)
                            .SetHeight(72f);
                    });
                });
            });
        }

        public static void AddBody<T>(
            GuiColumn<T> col,
            Expression<Func<T, string>> body,
            float height)
            where T : IGuiViewModel
        {
            col.AddRow(row =>
            {
                row.AddText()
                    .BindText(body)
                    .SetShowBorder(true)
                    .SetScrollbars(NuiScrollbars.Auto)
                    .SetHeight(height);
            });
        }
    }
}
