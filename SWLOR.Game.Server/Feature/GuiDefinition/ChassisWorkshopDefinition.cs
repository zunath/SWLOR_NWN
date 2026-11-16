using System;
using System.Linq.Expressions;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.AstromechService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>
    /// The Chassis Workshop: an astromech's chassis, paint and size. Every change applies at once.
    /// </summary>
    public class ChassisWorkshopDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<ChassisWorkshopViewModel> _builder = new();

        private const float ContentPanelWidth = 380f;
        private const float ChassisRowHeight = 24f;
        private const float ListRowPadding = 4f;
        private const float ChassisButtonWidth = 80f;
        private const float PaintLabelWidth = 76f;
        private const float SwatchWidth = 52f;
        private const float SwatchHeight = 24f;
        private const float PaintButtonWidth = 64f;
        private const float PaintButtonHeight = 26f;
        private const float StepButtonWidth = 36f;
        private const float PickerWidth = 240f;
        private const float PickerHeight = 110f;

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.ChassisWorkshop)
                .SetInitialGeometry(0, 0, 430f, 660f)
                .SetTitle("Chassis Workshop")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .BindOnClosed(model => model.OnWindowClosed())
                .DefinePartialView(ChassisWorkshopViewModel.MainPartial, AddMainContent);

            // Rule R5: the standard root, with no tabs and one partial holding the whole body.
            window.AddStandardLayout(layout =>
            {
                layout.SetContentPartialElement(ChassisWorkshopViewModel.ContentElement);
            });

            return _builder.Build();
        }

        private static void AddMainContent(GuiGroup<ChassisWorkshopViewModel> host)
        {
            host.AddColumn(col =>
            {
                col.AddRow(row =>
                {
                    // The page can be taller than a small window, so its panel scrolls (rule R5).
                    row.AddGroup(panel =>
                    {
                        panel.SetShowBorder(false);
                        panel.SetScrollbars(NuiScrollbars.Auto);
                        panel.AddColumn(content =>
                        {
                            // The color picker opens under the paint rows, so paint goes last and the
                            // size control never moves.
                            AddChassisSection(content);
                            AddSizeSection(content);
                            AddPaintSection(content);
                        });
                    })
                        .SetWidth(ContentPanelWidth);
                });
            });
        }

        private static void AddSectionLabel(GuiColumn<ChassisWorkshopViewModel> col, string text)
        {
            col.AddRow(row => row.AddLabel()
                .SetText(text)
                .SetHeight(20f)
                .SetHorizontalAlign(NuiHorizontalAlign.Left));
        }

        private static void AddChassisSection(GuiColumn<ChassisWorkshopViewModel> col)
        {
            AddSectionLabel(col, "Chassis");
            col.AddRow(row =>
            {
                // The client draws each list row 4 px taller than its row height and frames the list, so the
                // row needs that much more or the last chassis scrolls out of view.
                row.SetHeight((ChassisRowHeight + ListRowPadding) * AstromechChassisCatalog.All.Count + 12f);
                row.AddList(template =>
                {
                    template.AddCell(cell => cell.AddLabel()
                        .BindText(model => model.ChassisNames)
                        .SetHorizontalAlign(NuiHorizontalAlign.Left));
                    template.AddCell(cell =>
                    {
                        cell.SetWidth(ChassisButtonWidth);
                        cell.SetIsVariable(false);
                        cell.AddButton()
                            .BindText(model => model.ChassisButtonText)
                            .SetWidth(ChassisButtonWidth)
                            .BindOnClicked(model => model.OnClickChassis());
                    });
                })
                    .BindRowCount(model => model.ChassisNames)
                    .SetRowHeight(ChassisRowHeight);
            });
        }

        private static void AddPaintSection(GuiColumn<ChassisWorkshopViewModel> col)
        {
            AddSectionLabel(col, "Paint");
            AddPaintRow(col, AstromechPaintZone.Primary, "Primary",
                model => model.PrimaryTint, model => model.PrimaryPickText);
            AddPaintRow(col, AstromechPaintZone.Secondary, "Secondary",
                model => model.SecondaryTint, model => model.SecondaryPickText);
            AddPaintRow(col, AstromechPaintZone.Trim, "Trim",
                model => model.TrimTint, model => model.TrimPickText);

            col.AddRow(row =>
            {
                row.BindIsVisible(model => model.IsPickerVisible);
                row.AddLabel()
                    .BindText(model => model.PickerLabel)
                    .SetHeight(20f)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
            });
            col.AddRow(row =>
            {
                row.BindIsVisible(model => model.IsPickerVisible);
                row.AddColorPicker()
                    .BindSelectedColor(model => model.SelectedColor)
                    .SetWidth(PickerWidth)
                    .SetHeight(PickerHeight);
                row.AddSpacer();
            });
        }

        private static void AddPaintRow(
            GuiColumn<ChassisWorkshopViewModel> col,
            AstromechPaintZone zone,
            string name,
            Expression<Func<ChassisWorkshopViewModel, GuiColor>> tint,
            Expression<Func<ChassisWorkshopViewModel, string>> pickText)
        {
            col.AddRow(row =>
            {
                row.AddLabel()
                    .SetText(name)
                    .SetWidth(PaintLabelWidth)
                    .SetHeight(SwatchHeight)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
                row.AddButton()
                    .SetText("")
                    .SetWidth(SwatchWidth)
                    .SetHeight(SwatchHeight)
                    .AddDrawList(list => list.AddPolyLine(fill => fill
                        .BindColor(tint)
                        .SetIsFilled(true)
                        .AddPoint(3f, 3f)
                        .AddPoint(SwatchWidth - 3f, 3f)
                        .AddPoint(SwatchWidth - 3f, SwatchHeight - 3f)
                        .AddPoint(3f, SwatchHeight - 3f)))
                    .BindOnClicked(model => model.OnClickPick(zone));
                row.AddButton()
                    .BindText(pickText)
                    .SetWidth(PaintButtonWidth)
                    .SetHeight(PaintButtonHeight)
                    .BindOnClicked(model => model.OnClickPick(zone));
                row.AddButton()
                    .SetText("Reset")
                    .SetWidth(PaintButtonWidth)
                    .SetHeight(PaintButtonHeight)
                    .BindOnClicked(model => model.OnClickReset(zone));
                row.AddSpacer();
            });
        }

        private static void AddSizeSection(GuiColumn<ChassisWorkshopViewModel> col)
        {
            col.AddRow(row =>
            {
                row.AddLabel()
                    .SetText("Size")
                    .SetWidth(PaintLabelWidth)
                    .SetHeight(20f)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
                row.AddLabel()
                    .BindText(model => model.ScaleText)
                    .SetHeight(20f)
                    .SetHorizontalAlign(NuiHorizontalAlign.Left);
            });
            col.AddRow(row =>
            {
                row.AddButton()
                    .SetText("-")
                    .SetWidth(StepButtonWidth)
                    .SetHeight(PaintButtonHeight)
                    .BindOnClicked(model => model.OnClickSmaller());
                row.AddSliderFloat()
                    .BindValue(model => model.ScaleValue)
                    .SetMinimum(AstromechChassisCatalog.MinimumScale)
                    .SetMaximum(AstromechChassisCatalog.MaximumScale)
                    .SetStepSize(AstromechWorkshop.ScaleStep);
                row.AddButton()
                    .SetText("+")
                    .SetWidth(StepButtonWidth)
                    .SetHeight(PaintButtonHeight)
                    .BindOnClicked(model => model.OnClickLarger());
            });
        }
    }
}
