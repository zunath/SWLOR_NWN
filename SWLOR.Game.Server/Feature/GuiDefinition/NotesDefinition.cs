using System.Linq.Expressions;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class NotesDefinition : IGuiWindowDefinition
    {
        private const float RowHeight = 40f;
        private const float ControlHeight = 32f;
        private const float ProgressBarRowHeight = 28f;
        private const float ProgressBarHeight = 20f;
        private const float ProgressBarTextWidth = 440f;

        /// <summary>
        /// A pinned width on the Search button keeps it compact so the flexing search box beside it
        /// gets the extra room. Without a width the button sizes to its label and steals it.
        /// </summary>
        private const float SearchButtonWidth = 64f;

        /// <summary>
        /// Width of a combo box. A combo does not stretch to fill its row the way a text edit does,
        /// so it needs an explicit width. It must sit in a row with no pinned height, or the window's
        /// lists render empty.
        /// </summary>
        private const float ComboWidth = 300f;

        /// <summary>
        /// Width of the fixed right-hand editor pane. The note list to its left is a greedy element,
        /// so it - and only it - grows as the window is resized, which is what lets the window
        /// reflow. This mirrors Perks and the Character Sheet, the windows that resize correctly: a
        /// row fills only when one of its direct children is a greedy element such as a list.
        /// </summary>
        private const float EditorWidth = 360f;

        private readonly GuiWindowBuilder<NotesViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.Notes)
                .SetInitialGeometry(0, 0, 720f, 460f)
                .SetTitle("Notes")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .BindOnClosed(model => model.OnCloseWindow())

                // The notes tab is the base window layout so it reflows natively on resize (the same
                // as Perks). The categories tab is a partial swapped in over it on demand.
                .DefinePartialView(NotesViewModel.CategoriesTabPartial, BuildCategoriesTab)

                .AddColumn(col =>
                {
                    AddTabRow(col);
                    AddNotesContent(col);
                });

            return _builder.Build();
        }

        /// <summary>
        /// A Bank-style usage bar with its "used / limit" text drawn over it. Shared by both tabs,
        /// which differ only in the bound properties.
        /// </summary>
        private static void AddUsageProgressBar(
            GuiRow<NotesViewModel> row,
            Expression<Func<NotesViewModel, float>> progress,
            Expression<Func<NotesViewModel, GuiColor>> color,
            Expression<Func<NotesViewModel, string>> text)
        {
            row.AddProgressBar()
                .BindValue(progress)
                .BindColor(color)
                .BindTooltip(text)
                .SetHeight(ProgressBarHeight)
                .AddDrawList(drawList =>
                {
                    drawList.AddText(drawText =>
                    {
                        drawText.BindText(text);
                        drawText.SetBounds(6, 2, ProgressBarTextWidth, ProgressBarHeight);
                        drawText.SetColor(255, 255, 255);
                    });
                });
        }

        /// <summary>
        /// The tab selector, at the top of both tab layouts so it survives a swap.
        /// </summary>
        private static void AddTabRow(GuiColumn<NotesViewModel> col)
        {
            col.AddRow(row =>
            {
                row.SetHeight(RowHeight);

                row.AddSpacer();

                row.AddToggleButton()
                    .SetText("Notes")
                    .SetHeight(ControlHeight)
                    .SetWidth(180f)
                    .BindIsToggled(model => model.IsNotesTabToggled)
                    .BindOnClicked(model => model.OnClickNotesTab());

                row.AddToggleButton()
                    .SetText("Manage Categories")
                    .SetHeight(ControlHeight)
                    .SetWidth(180f)
                    .BindIsToggled(model => model.IsCategoriesTabToggled)
                    .BindOnClicked(model => model.OnClickCategoriesTab());

                row.AddSpacer();
            });
        }

        // Full-width bands (progress, filter/search, pagination, actions) stacked above and below a
        // single content row of [greedy note list | fixed-width editor]. The greedy list is what
        // makes the row fill and reflow; a row of two plain columns would size to content instead.
        private static void AddNotesContent(GuiColumn<NotesViewModel> col)
        {
            col.AddRow(row =>
            {
                row.SetHeight(ProgressBarRowHeight);

                AddUsageProgressBar(
                    row,
                    model => model.NoteUsageProgress,
                    model => model.NoteUsageColor,
                    model => model.NoteUsageText);
            });

            // No pinned height: a width-pinned combo inside a fixed-height row blanks the lists.
            col.AddRow(row =>
            {
                row.AddComboBox()
                    .BindOptions(model => model.CategoryFilterOptions)
                    .BindSelectedIndex(model => model.SelectedCategoryFilterIndex)
                    .SetWidth(ComboWidth);

                row.AddTextEdit()
                    .SetPlaceholder("Search")
                    .BindValue(model => model.SearchText);

                row.AddButton()
                    .SetText("X")
                    .SetHeight(ControlHeight)
                    .SetWidth(35f)
                    .BindOnClicked(model => model.OnClickClearSearch());

                row.AddButton()
                    .SetText("Search")
                    .SetHeight(ControlHeight)
                    .SetWidth(SearchButtonWidth)
                    .BindOnClicked(model => model.OnClickSearch());
            });

            col.AddRow(content =>
            {
                // Left pane: the note list, a greedy element, so it takes all width left by the
                // fixed editor and grows on resize.
                content.AddList(template =>
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

                // Right pane: the editor, a fixed width so the list beside it stays greedy.
                content.AddColumn(editor =>
                {
                    editor.SetWidth(EditorWidth);

                    editor.AddRow(row =>
                    {
                        row.SetHeight(RowHeight);

                        row.AddTextEdit()
                            .BindValue(model => model.ActiveNoteName)
                            .BindIsEnabled(model => model.IsNoteSelected)
                            .SetPlaceholder("Note Name");
                    });

                    editor.AddRow(row =>
                    {
                        row.AddComboBox()
                            .BindOptions(model => model.NoteCategoryOptions)
                            .BindSelectedIndex(model => model.ActiveNoteCategoryIndex)
                            .BindIsEnabled(model => model.IsNoteSelected)
                            .SetWidth(ComboWidth);
                    });

                    editor.AddRow(row =>
                    {
                        row.AddTextEdit()
                            .SetIsMultiline(true)
                            .SetMaxLength(Notes.MaxNoteLength)
                            .BindValue(model => model.ActiveNoteText)
                            .BindIsEnabled(model => model.IsNoteSelected);
                    });

                    editor.AddRow(row =>
                    {
                        row.SetHeight(RowHeight);

                        row.AddButton()
                            .BindOnClicked(model => model.OnClickSave())
                            .SetText("Save")
                            .SetHeight(ControlHeight)
                            .BindIsEnabled(model => model.IsSaveEnabled);

                        row.AddButton()
                            .BindOnClicked(model => model.OnClickDiscardChanges())
                            .SetText("Discard Changes")
                            .SetHeight(ControlHeight)
                            .BindIsEnabled(model => model.IsSaveEnabled);
                    });
                });
            });

            col.AddPagination(
                model => model.PageNumbers,
                model => model.SelectedPageIndex,
                model => model.OnClickPreviousPage(),
                model => model.OnClickNextPage());

            col.AddRow(row =>
            {
                row.SetHeight(RowHeight);

                row.AddSpacer();

                row.AddButton()
                    .SetText("New Note")
                    .BindOnClicked(model => model.OnClickNewNote())
                    .BindIsEnabled(model => model.IsNewEnabled)
                    .SetHeight(ControlHeight);

                row.AddButton()
                    .SetText("Delete Note")
                    .BindOnClicked(model => model.OnClickDeleteNote())
                    .BindIsEnabled(model => model.IsDeleteEnabled)
                    .SetHeight(ControlHeight);

                row.AddSpacer();
            });
        }

        private static void BuildCategoriesTab(GuiGroup<NotesViewModel> group)
        {
            group.AddColumn(col =>
            {
                AddTabRow(col);
                AddCategoriesContent(col);
            });
        }

        private static void AddCategoriesContent(GuiColumn<NotesViewModel> col)
        {
            col.AddRow(row =>
            {
                row.SetHeight(ProgressBarRowHeight);

                AddUsageProgressBar(
                    row,
                    model => model.CategoryUsageProgress,
                    model => model.CategoryUsageColor,
                    model => model.CategoryUsageText);
            });

            col.AddRow(row =>
            {
                row.SetHeight(RowHeight);

                row.AddTextEdit()
                    .SetPlaceholder("New Category Name")
                    .SetMaxLength(Notes.MaxCategoryNameLength)
                    .BindValue(model => model.NewCategoryName);

                row.AddButton()
                    .SetText("Add Category")
                    .SetHeight(ControlHeight)
                    .BindOnClicked(model => model.OnClickAddCategory())
                    .BindIsEnabled(model => model.IsAddCategoryEnabled);
            });

            col.AddRow(row =>
            {
                row.AddList(template =>
                {
                    template.AddCell(cell =>
                    {
                        cell.AddToggleButton()
                            .BindText(model => model.CategoryNames)
                            .BindIsToggled(model => model.CategoryToggled)
                            .BindOnClicked(model => model.OnSelectCategory());
                    });
                })
                    .BindRowCount(model => model.CategoryNames);
            });

            col.AddRow(row =>
            {
                row.SetHeight(RowHeight);

                row.AddSpacer();

                row.AddButton()
                    .SetText("Delete Category")
                    .SetHeight(ControlHeight)
                    .BindOnClicked(model => model.OnClickDeleteCategory())
                    .BindIsEnabled(model => model.IsDeleteCategoryEnabled);

                row.AddSpacer();
            });
        }
    }
}
