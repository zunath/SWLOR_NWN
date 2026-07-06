using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class CombatGuiDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<CombatGuiViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            var window = _builder.CreateWindow(GuiWindowType.CombatGui);
            window.SetInitialGeometry(0, 0, 140f, 460f)
                .SetTitle("Combat")
                .SetIsResizable(true)
                .SetIsCollapsible(true)

                .AddColumn(col =>
                {
                    // Row 1: style category toggles.
                    col.AddRow(row =>
                    {
                        row.AddToggleButton()
                            .SetText("Saber")
                            .BindIsToggled(model => model.IsSaberToggled)
                            .BindOnClicked(model => model.OnSelectSaber())
                            .SetHeight(28f);

                        row.AddToggleButton()
                            .SetText("Guns")
                            .BindIsToggled(model => model.IsGunsToggled)
                            .BindOnClicked(model => model.OnSelectGuns())
                            .SetHeight(28f);
                    });

                    // Row 2: selected style badge + X to change it.
                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .BindText(model => model.FormBadgeText)
                            .BindIsVisible(model => model.IsFormBadgeVisible)
                            .SetHeight(24f);

                        row.AddButton()
                            .SetText("X")
                            .BindIsVisible(model => model.IsFormBadgeVisible)
                            .BindOnClicked(model => model.OnClearForm())
                            .SetWidth(28f)
                            .SetHeight(24f);
                    });

                    // Row 3: role buttons (only when a saber form is chosen).
                    col.AddRow(row =>
                    {
                        row.AddButton()
                            .SetText("GRD")
                            .BindIsVisible(model => model.AreRolesVisible)
                            .BindOnClicked(model => model.OnSelectRole(0))
                            .SetHeight(24f);

                        row.AddButton()
                            .SetText("ATK")
                            .BindIsVisible(model => model.AreRolesVisible)
                            .BindOnClicked(model => model.OnSelectRole(1))
                            .SetHeight(24f);

                        row.AddButton()
                            .SetText("DEF")
                            .BindIsVisible(model => model.AreRolesVisible)
                            .BindOnClicked(model => model.OnSelectRole(2))
                            .SetHeight(24f);
                    });

                    // Row 4: results area.
                    col.AddRow(row =>
                    {
                        row.AddList(template =>
                        {
                            template.AddCell(cell =>
                            {
                                cell.AddToggleButton()
                                    .BindText(model => model.ResultNames)
                                    .BindIsToggled(model => model.ResultToggles)
                                    .BindOnClicked(model => model.OnSelectResult());
                            });
                        })
                        .BindRowCount(model => model.ResultNames)
                        .SetHeight(280f);
                    });

                    // Footer: emergency RESET, always visible.
                    col.AddRow(row =>
                    {
                        row.AddButton()
                            .SetText("RESET")
                            .BindOnClicked(model => model.OnClickReset())
                            .SetHeight(28f);
                    });
                });

            return _builder.Build();
        }
    }
}
