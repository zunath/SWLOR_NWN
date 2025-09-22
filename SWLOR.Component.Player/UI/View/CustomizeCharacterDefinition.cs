using SWLOR.Component.Player.UI.ViewModel;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Enums;
using SWLOR.Shared.UI.Model;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.Player.UI.View
{
    public class CustomizeCharacterDefinition : IGuiWindowDefinition
    {
        private readonly IGuiService _guiService;
        private readonly GuiWindowBuilder<CustomizeCharacterViewModel> _builder;

        public CustomizeCharacterDefinition(IGuiService guiService)
        {
            _guiService = guiService;
            _builder = new GuiWindowBuilder<CustomizeCharacterViewModel>(_guiService);
        }

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.CustomizeCharacter)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 340f, 360f)
                .SetTitle("Customize")

                .DefinePartialView(CustomizeCharacterViewModel.PortraitPartial, partial =>
                {
                    partial.AddColumn(col =>
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
                            row.AddTextEdit()
                                .SetPlaceholder("Custom File ( ex: po_me_ )")
                                .BindValue(model => model.CustomPortraitFile)
                                .BindIsVisible(model => model.IsCustomPortraitVisible);
                        });
                        
                        col.AddRow(row =>
                        {
                            row.AddButton()
                                .SetText("Previous")
                                .BindOnClicked(model => model.OnPreviousPortraitClick())
                                .SetHeight(32f);

                            row.AddButton()
                                .SetText("Next")
                                .BindOnClicked(model => model.OnNextPortraitClick())
                                .SetHeight(32f);
                        });

                        col.AddRow(row =>
                        {
                            row.AddSpacer();

                            row.AddButton()
                                .SetText("Revert")
                                .BindOnClicked(model => model.OnRevertPortraitClick())
                                .SetHeight(32f);

                            row.AddButton()
                                .SetText("Save")
                                .BindOnClicked(model => model.OnSavePortraitClick())
                                .SetHeight(32f);

                            row.AddSpacer();
                        });
                    });
                })
                
                .DefinePartialView(CustomizeCharacterViewModel.VoicePartial, partial =>
                {
                    partial.AddColumn(col =>
                    {
                        col.AddRow(row =>
                        {
                            row.AddList(template =>
                            {
                                template.AddCell(cell =>
                                {
                                    cell.AddToggleButton()
                                        .BindText(model => model.SoundSetNames)
                                        .BindIsToggled(model => model.SoundSetToggles)
                                        .BindOnClicked(model => model.OnSoundSetClick());
                                });
                            })
                                .BindRowCount(model => model.SoundSetNames);
                        });
                    });
                })
                
                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddToggleButton()
                            .SetText("Portrait")
                            .BindIsToggled(model => model.IsPortraitSelected)
                            .BindOnClicked(model => model.OnPortraitClick())
                            .SetHeight(32f);

                        row.AddToggleButton()
                            .SetText("Voice")
                            .BindIsToggled(model => model.IsVoiceSelected)
                            .BindOnClicked(model => model.OnVoiceClick())
                            .SetHeight(32f);
                        row.AddSpacer();
                    });
                    
                    col.AddRow(row =>
                    {
                        row.AddPartialView(CustomizeCharacterViewModel.PartialElement);
                    });
                })
                ;



            return _builder.Build();
        }
    }
}
