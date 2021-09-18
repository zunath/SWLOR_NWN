using System.Collections.Generic;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class TestWindowGuiDefinition: IGuiWindowListDefinition
    {
        private readonly GuiWindowBuilder _builder = new GuiWindowBuilder();

        public Dictionary<GuiWindowType, GuiWindow> BuildWindows()
        {
            _builder.CreateWindow(GuiWindowType.TestWindow)

                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddButton()
                            .SetText("regular button");

                        row.AddButtonImage()
                            .SetResref("ife_animal");

                        row.AddToggleButton()
                            .SetText("toggle button");

                        row.AddLabel()
                            .SetText("label");
                    });
                    col.AddRow(row =>
                    {
                        row.AddComboBox()
                            .BindSelectedIndex("selected_option")
                            .AddOption("item 1", 1)
                            .AddOption("item 2", 2)
                            .AddOption("item 3", 3);

                        row.AddColorPicker();

                        row.AddCheckBox()
                            .SetText("checkbox");

                        row.AddImage()
                            .SetResref("ife_defarrow");
                    });

                    col.AddRow(row =>
                    {
                        row.AddOptions()
                            .AddOption("option 1")
                            .AddOption("option 2");

                        row.AddProgressBar()
                            .SetValue(0.4f);

                        row.AddSliderFloat()
                            .SetMaximum(100f)
                            .SetMinimum(0f)
                            .SetValue(30f);

                        row.AddSliderInt()
                            .SetMaximum(50)
                            .SetMinimum(0)
                            .SetValue(15);
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();

                        row.AddText()
                            .SetText("text component");

                        row.AddTextEdit()
                            .SetPlaceholder("text edit");
                    });
                });
                

            return _builder.Build();
        }
    }
}
