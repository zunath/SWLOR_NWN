using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class NotesDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<NotesViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.Notes)
                .SetInitialGeometry(0, 0, 638f, 336f)
                .SetTitle("Notes")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .BindOnClosed(model => model.OnCloseWindow())

                .AddColumn(col =>
                {
                    col.SetHeight(300f);
                    col.AddRow(row =>
                    {
                        row.AddList(template =>
                        {
                            template.AddCell(cell =>
                            {
                                cell.AddToggleButton()
                                    .BindText(model => model.NoteNames)
                                    .BindIsToggled(model => model.NoteToggled)
                                    .BindOnClicked(model => model.OnSelectNote());
                            });
                        })
                            .BindRowCount(model => model.NoteNames);
                    });

                    col.AddRow(row =>
                    {
                        row.AddButton()
                            .SetText("New Note")
                            .BindOnClicked(model => model.OnClickNewNote())
                            .BindIsEnabled(model => model.IsNewEnabled)
                            .SetHeight(35f);

                        row.AddButton()
                            .SetText("Delete Note")
                            .BindOnClicked(model => model.OnClickDeleteNote())
                            .BindIsEnabled(model => model.IsDeleteEnabled)
                            .SetHeight(35f);
                    });
                })

                .AddColumn(col =>
                {
                    col.SetHeight(300f);
                    col.AddRow(row =>
                    {
                        row.AddTextEdit()
                            .BindValue(model => model.ActiveNoteName)
                            .BindIsEnabled(model => model.IsNoteSelected)
                            .SetPlaceholder("Note Name");

                        row.SetHeight(32f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddTextEdit()
                            .SetIsMultiline(true)
                            .SetMaxLength(NotesViewModel.MaxNoteLength)
                            .BindValue(model => model.ActiveNoteText)
                            .BindIsEnabled(model => model.IsNoteSelected)
                            .SetHeight(205f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                    });
                    
                    col.AddRow(row =>
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
                    });
                });

            return _builder.Build();
        }
    }
}
