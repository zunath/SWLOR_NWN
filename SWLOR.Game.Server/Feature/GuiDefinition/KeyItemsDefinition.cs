using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class KeyItemsDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<KeyItemsViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.KeyItems)
                .BindOnOpened(model => model.OnLoadWindow())
                .SetIsResizable(true)
                .SetInitialGeometry(0, 0, 545f, 295.5f)
                .SetTitle("Key Items")
                .AddColumn(column =>
                {
                    column.AddRow(row =>
                    {
                        row.AddSpacer();
                        var comboBox = row.AddComboBox()
                            .BindSelectedIndex(model => model.SelectedCategoryId);

                        comboBox.AddOption("<All Types>", 0);
                        foreach (var (type, detail) in KeyItem.GetActiveCategories())
                        {
                            comboBox.AddOption(detail.Name, (int)type);
                        }

                        row.AddSpacer();
                    });

                    column.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Name")
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetVerticalAlign(NuiVerticalAlign.Top);

                        row.AddLabel()
                            .SetText("Type")
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetVerticalAlign(NuiVerticalAlign.Top);

                        row.SetHeight(20f);
                    });

                    column.AddRow(row =>
                    {
                        row.AddList(template =>
                        {
                            template.AddLabel()
                                .BindText(model => model.Names)
                                .BindTooltip(model => model.Descriptions);

                            template.AddLabel()
                                .BindText(model => model.Types)
                                .BindTooltip(model => model.Descriptions);
                        })
                            .BindRowCount(model => model.Names);
                    });
                });

            return _builder.Build();
        }
    }
}
