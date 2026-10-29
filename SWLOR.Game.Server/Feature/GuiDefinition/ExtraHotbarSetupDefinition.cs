using System;
using System.Linq.Expressions;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ExtraHotbarService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>
    /// Turns the extra hotbars on and off and sets their size and direction. Bars can be dragged while it is open.
    /// </summary>
    public class ExtraHotbarSetupDefinition : IGuiWindowDefinition
    {
        private const float PanelWidth = 500f;
        private const float RowHeight = 30f;

        private readonly GuiWindowBuilder<ExtraHotbarSetupViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.ExtraHotbarSetup)
                .SetInitialGeometry(0, 0, 540f, 380f)
                .SetTitle("Hotbars")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .BindOnClosed(model => model.OnWindowClosed())
                .DefinePartialView(ExtraHotbarSetupViewModel.MainPartial, AddBody);

            window.AddStandardLayout(layout =>
            {
                layout.SetContentPartialElement(ExtraHotbarSetupViewModel.ContentElement);
            });

            return _builder.Build();
        }

        private static void AddBody(GuiGroup<ExtraHotbarSetupViewModel> host)
        {
            host.AddColumn(col =>
            {
                col.AddRow(row =>
                {
                    row.AddGroup(panel =>
                    {
                        panel.SetShowBorder(false);
                        panel.SetScrollbars(NuiScrollbars.None);
                        panel.AddColumn(content =>
                        {
                            for (var bar = 0; bar < ExtraHotbars.BarCount; bar++)
                                AddBarRow(content, bar);

                            content.AddRow(r => r.AddText()
                                .SetText("Left-click a slot to use it. Right-click a slot to choose an ability, item or chat command for it.\n\nWhile this window is open, drag a hotbar by its title bar to move it.")
                                .SetHeight(130f));
                        });
                    })
                        .SetWidth(PanelWidth);
                });
            });
        }

        private static void AddBarRow(GuiColumn<ExtraHotbarSetupViewModel> content, int bar)
        {
            content.AddRow(r =>
            {
                // Every control needs its own id: the three rows share labels, and controls with the same label clash.
                r.AddCheckBox()
                    .SetId($"ehb_setup_visible_{bar}")
                    .SetText($"Hotbar {bar + 1}")
                    .BindIsChecked(Bind<bool>(ExtraHotbarSetupViewModel.VisibleProperty(bar)))
                    .SetWidth(110f)
                    .SetHeight(RowHeight);
                r.AddSliderInt()
                    .SetId($"ehb_setup_slots_{bar}")
                    .BindValue(Bind<int>(ExtraHotbarSetupViewModel.SlotCountProperty(bar)))
                    .SetMinimum(1)
                    .SetMaximum(ExtraHotbars.MaxSlots)
                    .SetStepSize(1)
                    .SetWidth(150f)
                    .SetHeight(RowHeight);
                r.AddLabel()
                    .BindText(Bind<string>(ExtraHotbarSetupViewModel.SlotCountTextProperty(bar)))
                    .SetWidth(70f)
                    .SetHeight(RowHeight);
                r.AddComboBox()
                    .SetId($"ehb_setup_layout_{bar}")
                    .AddOption("Horizontal", (int)ExtraHotbarLayout.Horizontal)
                    .AddOption("Vertical", (int)ExtraHotbarLayout.Vertical)
                    .AddOption("Stacked", (int)ExtraHotbarLayout.Stacked)
                    .BindSelectedIndex(Bind<int>(ExtraHotbarSetupViewModel.LayoutProperty(bar)))
                    .SetWidth(130f)
                    .SetHeight(RowHeight);
            });
        }

        private static Expression<Func<ExtraHotbarSetupViewModel, TValue>> Bind<TValue>(string propertyName)
        {
            var model = Expression.Parameter(typeof(ExtraHotbarSetupViewModel), "model");
            return Expression.Lambda<Func<ExtraHotbarSetupViewModel, TValue>>(
                Expression.Property(model, propertyName), model);
        }
    }
}
