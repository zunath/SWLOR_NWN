using System;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class TestWindowGuiDefinition:  IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<TestWindowViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.TestWindow)
                .OnOpened((player, token, id, index) =>
                {
                    Console.WriteLine("Hello from OnOpen event.");
                })
                .OnClosed((player, token, id, index) =>
                {
                    Console.WriteLine("Hello from OnClosed event.");
                })

                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddButton()
                            .SetId(Guid.NewGuid().ToString())
                            .BindText(model => model.ButtonText)
                            .OnMouseUp((player, token, id, index) =>
                            {
                                Console.WriteLine("Hello from MouseUp event.");
                            })
                            .OnMouseDown((player, token, id, index) =>
                            {
                                Console.WriteLine("Hello from MouseDown event.");
                            })
                            .OnClicked((player, token, id, index) =>
                            {
                                Console.WriteLine("Hello from Click event.");
                            });

                        row.AddButtonImage()
                            .SetId(Guid.NewGuid().ToString())
                            .SetResref("ife_animal");

                        row.AddToggleButton()
                            .SetId(Guid.NewGuid().ToString())
                            .SetText("toggle button")
                            .BindIsToggled(model => model.IsToggled);

                        row.AddLabel()
                            .SetId(Guid.NewGuid().ToString())
                            .SetText("label");
                    });

                    col.AddRow(row =>
                    {
                        row.AddComboBox()
                            .SetId(Guid.NewGuid().ToString())
                            .BindSelectedIndex(model => model.SelectedComboBoxValue)
                            .AddOption("item 1", 1)
                            .AddOption("item 2", 2)
                            .AddOption("item 3", 3);

                        row.AddColorPicker()
                            .SetId(Guid.NewGuid().ToString())
                            .BindSelectedColor(model => model.SelectedColor);

                        row.AddCheckBox()
                            .SetId(Guid.NewGuid().ToString())
                            .SetText("checkbox")
                            .BindIsChecked(model => model.IsChecked);

                        row.AddImage()
                            .SetId(Guid.NewGuid().ToString())
                            .SetResref("ife_defarrow");
                    });

                    col.AddRow(row =>
                    {
                        row.AddOptions()
                            .SetId(Guid.NewGuid().ToString())
                            .BindSelectedValue(model => model.SelectedOption)
                            .AddOption("option 1")
                            .AddOption("option 2");

                        row.AddProgressBar()
                            .SetId(Guid.NewGuid().ToString())
                            .SetValue(0.4f);

                        row.AddSliderFloat()
                            .SetId(Guid.NewGuid().ToString())
                            .SetMaximum(100f)
                            .SetMinimum(0f)
                            .SetValue(30f);

                        row.AddSliderInt()
                            .SetId(Guid.NewGuid().ToString())
                            .SetMaximum(50)
                            .SetMinimum(0)
                            .SetValue(15);
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer()
                            .SetId(Guid.NewGuid().ToString());

                        row.AddText()
                            .SetId(Guid.NewGuid().ToString())
                            .SetText("text component");

                        row.AddTextEdit()
                            .SetId(Guid.NewGuid().ToString())
                            .SetPlaceholder("text edit")
                            .BindValue(model => model.EnteredText);
                    });
                });


            return _builder.Build();
        }
    }
}
