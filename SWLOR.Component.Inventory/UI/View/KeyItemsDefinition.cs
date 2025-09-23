using SWLOR.Component.Inventory.Contracts;
using SWLOR.Component.Inventory.UI.ViewModel;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Enums;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Model;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.Inventory.UI.View
{
    public class KeyItemsDefinition : IGuiWindowDefinition
    {
        private readonly IGuiService _guiService;
        private readonly IKeyItemService _keyItemService;
        private readonly GuiWindowBuilder<KeyItemsViewModel> _builder;

        public KeyItemsDefinition(IGuiService guiService, IKeyItemService keyItemService)
        {
            _guiService = guiService;
            _keyItemService = keyItemService;
            _builder = new GuiWindowBuilder<KeyItemsViewModel>(_guiService);
        }

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.KeyItems)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
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
                        foreach (var (type, detail) in _keyItemService.GetActiveCategories())
                        {
                            comboBox.AddOption(detail.Name, (int)type);
                        }

                        row.AddSpacer();
                    });

                    column.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Name")
                            .SetHorizontalAlign(NuiHorizontalAlign.Center)
                            .SetVerticalAlign(NuiVerticalAlign.Top);

                        row.AddLabel()
                            .SetText("Type")
                            .SetHorizontalAlign(NuiHorizontalAlign.Center)
                            .SetVerticalAlign(NuiVerticalAlign.Top);

                        row.SetHeight(20f);
                    });

                    column.AddRow(row =>
                    {
                        row.AddList(template =>
                        {
                            template.AddCell(cell =>
                            {
                                cell.AddLabel()
                                    .BindText(model => model.Names)
                                    .BindTooltip(model => model.Descriptions);
                            });

                            template.AddCell(cell =>
                            {
                                cell.AddLabel()
                                    .BindText(model => model.Types)
                                    .BindTooltip(model => model.Descriptions);
                            });
                        })
                            .BindRowCount(model => model.Names);
                    });
                });

            return _builder.Build();
        }
    }
}
