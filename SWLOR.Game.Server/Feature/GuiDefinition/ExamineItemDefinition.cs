using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class ExamineItemDefinition: IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<ExamineItemViewModel> _builder = new();
        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.ExamineItem)
                .BindOnOpened(model => model.OnLoadWindow())
                .BindOnClosed(model => model.OnCloseWindow())
                .SetIsResizable(true)
                .SetInitialGeometry(0, 0, 385f, 379f)
                .BindTitle(model => model.WindowTitle)

                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddGroup(group =>
                        {
                            group.AddLabel()
                                .SetText("Description")
                                .SetHorizontalAlign(NuiHorizontalAlign.Center)
                                .SetVerticalAlign(NuiVerticalAlign.Middle);
                        })
                            .SetHeight(26f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddText()
                            .BindText(model => model.Description)
                            .SetHeight(160f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddGroup(group =>
                        {
                            group.AddLabel()
                                .SetText("Item Properties")
                                .SetHorizontalAlign(NuiHorizontalAlign.Center)
                                .SetVerticalAlign(NuiVerticalAlign.Middle);
                        })
                            .SetHeight(26f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddText()
                            .BindText(model => model.ItemProperties)
                            .SetHeight(105f);
                    });
                });
                ;

            return _builder.Build();
        }
    }
}
