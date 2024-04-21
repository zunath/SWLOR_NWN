using SWLOR.Core.Beamdog;
using SWLOR.Core.Feature.GuiDefinition.ViewModel;
using SWLOR.Core.Service.GuiService;

namespace SWLOR.Core.Feature.GuiDefinition
{
    public class ExamineItemDefinition: IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<ExamineItemViewModel> _builder = new();
        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.ExamineItem)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
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
