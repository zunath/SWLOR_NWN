﻿using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class ChangePortraitDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<ChangePortraitViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.ChangePortrait)
                .SetIsResizable(true)
                .SetInitialGeometry(0, 0, 340f, 360f)
                .SetTitle("Change Portrait")

                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddImage()
                            .BindResref(model => model.ActivePortrait)
                            .SetVerticalAlign(NuiVerticalAlign.Top)
                            .SetHorizontalAlign(NuiHorizontalAlign.Center)
                            .SetAspect(NuiAspect.ExactScaled)
                            .SetWidth(128f)
                            .SetHeight(200f);
                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        row.AddColumn(col2 =>
                        {
                            col2.AddRow(row2 =>
                            {
                                row2.AddTextEdit()
                                    .BindValue(model => model.ActivePortraitInternalId);
                            });
                        });

                        row.AddColumn(col2 =>
                        {
                            col2.AddRow(row2 =>
                            {
                                row2.AddLabel()
                                    .BindText(model => model.MaxPortraitsText)
                                    .SetHeight(35f)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                    .SetVerticalAlign(NuiVerticalAlign.Middle);
                            });
                        });

                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddButton()
                            .SetText("Previous")
                            .BindOnClicked(model => model.OnPreviousClick())
                            .SetHeight(32f);

                        row.AddButton()
                            .SetText("Next")
                            .BindOnClicked(model => model.OnNextClick())
                            .SetHeight(32f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();

                        row.AddButton()
                            .SetText("Revert")
                            .BindOnClicked(model => model.OnRevertClick())
                            .SetHeight(32f);

                        row.AddButton()
                            .SetText("Save")
                            .BindOnClicked(model => model.OnSaveClick())
                            .SetHeight(32f);

                        row.AddSpacer();
                    });
                });
                


            return _builder.Build();
        }
    }
}