using System;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class SkillsDefinition: IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<SkillsViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {

            _builder.CreateWindow(GuiWindowType.Skills)
                .BindGeometry(model => model.Geometry)
                .BindOnOpened(model => model.OnLoadWindow())
                .SetIsResizable(false)
                .SetGeometry(0, 0, 545f, 295.5f)
                .SetTitle("Skills")
                .AddColumn(column =>
                {
                    column.AddRow(row =>
                    {
                        row.AddList(template =>
                        {
                            template.AddCell(cell =>
                            {
                                cell.AddLabel()
                                    .BindText(model => model.SkillNames);
                            });
                            template.AddCell(cell =>
                            {
                                cell.AddLabel()
                                    .BindText(model => model.Levels);
                            });
                            template.AddCell(cell =>
                            {
                                cell.AddLabel()
                                    .BindText(model => model.Titles);
                            });
                            template.AddCell(cell =>
                            {
                                cell.AddProgressBar()
                                    .BindValue(model => model.Progresses);
                            });
                            template.AddCell(cell =>
                            {
                                cell.AddButton()
                                    .BindText(model => model.DecayLockTexts)
                                    .BindColor(model => model.DecayLockColors)
                                    .BindOnClicked(model => model.ToggleDecayLock());
                            });
                        });
                    });
                });

            return _builder.Build();
        }
    }
}
