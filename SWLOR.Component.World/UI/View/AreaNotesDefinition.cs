using SWLOR.Component.World.UI.ViewModel;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Enums;
using SWLOR.Shared.UI.Model;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.World.UI.View
{
    public class AreaNotesDefinition : IGuiWindowDefinition
    {
        private readonly IGuiService _guiService;
        private readonly GuiWindowBuilder<AreaNotesViewModel> _builder;

        public AreaNotesDefinition(IGuiService guiService)
        {
            _guiService = guiService;
            _builder = new GuiWindowBuilder<AreaNotesViewModel>(_guiService);
        }

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.AreaNotes)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 1000f, 600f)
                .SetTitle("Area Notes")

                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddTextEdit()
                            .SetPlaceholder("Search")
                            .BindValue(model => model.SearchText);

                        row.AddButton()
                            .SetText("X")
                            .SetHeight(35f)
                            .SetWidth(35f)
                            .BindOnClicked(model => model.OnClickClearSearch());

                        row.AddButton()
                            .SetText("Search")
                            .SetHeight(35f)
                            .BindOnClicked(model => model.OnClickSearch());
                    });

                    col.AddRow(row =>
                    {
                        row.AddColumn(colAreas =>
                        {
                            colAreas.SetHeight(450f);
                            colAreas.AddRow(row =>
                            {
                                row.AddList(template =>
                                {
                                    template.AddCell(cell =>
                                    {
                                        cell.AddToggleButton()
                                            .BindText(model => model.AreaNames)
                                            .BindIsToggled(model => model.AreaToggled)
                                            .BindOnClicked(model => model.OnSelectNote());
                                    });
                                })
                                    .BindRowCount(model => model.AreaNames);
                            });

                        });

                        row.AddColumn(colAreas =>
                        {
                            colAreas.SetHeight(300f);
                            colAreas.AddRow(row =>
                            {
                                row.AddTextEdit()
                                    .SetIsMultiline(true)
                                    .SetMaxLength(AreaNotesViewModel.MaxNoteLength)
                                    .BindValue(model => model.PrivateText)
                                    .SetHeight(205f)
                                    .SetWidth(600f)
                                    .SetPlaceholder("DM Only Notes");
                            });

                            colAreas.AddRow(row =>
                            {
                                row.AddTextEdit()
                                    .SetIsMultiline(true)
                                    .SetMaxLength(AreaNotesViewModel.MaxNoteLength)
                                    .BindValue(model => model.PublicText)
                                    .SetHeight(205f)
                                    .SetWidth(600f)
                                    .SetPlaceholder("Public Notes");
                            });

                            colAreas.AddRow(row =>
                            {
                                row.AddSpacer();
                            });

                            colAreas.AddRow(row =>
                            {
                                row.AddButton()
                                    .BindOnClicked(model => model.OnClickSave())
                                    .SetText("Save")
                                    .SetHeight(35f)
                                    .BindIsEnabled(model => model.IsSaveEnabled);

                                row.AddButton()
                                    .BindOnClicked(model => model.OnClickDiscardChanges())
                                    .SetText("Discard Changes")
                                    .SetHeight(35f)
                                    .BindIsEnabled(model => model.IsSaveEnabled);

                                row.AddButton()
                                    .SetText("Delete Note")
                                    .BindOnClicked(model => model.OnClickDeleteNote())
                                    .BindIsEnabled(model => model.IsDeleteEnabled)
                                    .SetHeight(35f);
                            });
                        });
                    });
                });

            return _builder.Build();
        }       
    }
}
