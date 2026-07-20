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
        /// Width of the two category combo boxes. A combo does not stretch to fill its row the way
        /// a text edit does, so it needs an explicit width - the same approach Perks and Key Items
        /// take. It must sit in a row with no pinned height or the window's lists render empty.
        /// </summary>
        private const float ComboWidth = 300f;

        private readonly GuiWindowBuilder<NotesViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.Notes)
                .SetInitialGeometry(0, 0, 720f, 460f)
                .SetTitle("Notes")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .BindOnClosed(model => model.OnCloseWindow())

                // Both tabs are registered as partial views and swapped into the window root, so
                // they render through the same clean group -> column path. The base window layout
                // that GuiWindowBuilder generates wraps its content in an extra row, and that
                // wrapper stops the two side-by-side panes from filling the window width. A swapped
                // partial has no such wrapper, which is why the categories tab always filled while
                // the notes tab, when left as the base layout, did not.
                .DefinePartialView(NotesViewModel.NotesTabPartial, BuildNotesTab)
                .DefinePartialView(NotesViewModel.CategoriesTabPartial, BuildCategoriesTab)

                // The base layout is a bare placeholder; Initialize swaps the notes tab in over it.
                .AddColumn(col => col.AddRow(row => row.AddSpacer()));

            return _builder.Build();
        }

        private static void BuildNotesTab(GuiGroup<NotesViewModel> group)
        {
            group.AddColumn(col =>
            {
                AddTabRow(col);
                AddNotesContent(col);
            });
        }

        /// <summary>
        /// The tab selector, repeated at the top of both tab layouts so it survives a swap.
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

        private static void AddNotesContent(GuiColumn<NotesViewModel> shell)
        {
            {
                shell.AddRow(root =>
                {
                    // Neither column is given a width, so they flex and share the window as it is
                    // resized. Every row below is pinned except the note list and the note body,
                    // which absorb the remaining vertical space.
                    root.AddColumn(browser =>
                    {
                        browser.AddRow(row =>
                        {
                            row.SetHeight(ProgressBarRowHeight);

                            row.AddProgressBar()
                                .BindValue(model => model.NoteUsageProgress)
                                .BindColor(model => model.NoteUsageColor)
                                .BindTooltip(model => model.NoteUsageText)
                                .SetHeight(ProgressBarHeight)
                                .AddDrawList(drawList =>
                                {
                                    drawList.AddText(text =>
                                    {
                                        text.BindText(model => model.NoteUsageText);
                                        text.SetBounds(6, 2, ProgressBarTextWidth, ProgressBarHeight);
                                        text.SetColor(255, 255, 255);
                                    });
                                });
                        });

                        // Combo rows follow Perks and Key Items: spacers either side and an explicit
                        // width, in a row with no pinned height. A width-pinned combo inside a
                        // fixed-height row makes every list in the window render empty.
                        browser.AddRow(row =>
                        {
                            row.AddSpacer();

                            row.AddComboBox()
                                .BindOptions(model => model.CategoryFilterOptions)
                                .BindSelectedIndex(model => model.SelectedCategoryFilterIndex)
                                .SetWidth(ComboWidth);

                            row.AddSpacer();
                        });

                        browser.AddRow(row =>
                        {
                            row.SetHeight(RowHeight);

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
                                .BindOnClicked(model => model.OnClickSearch());
                        });

                        browser.AddRow(row =>
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

                        browser.AddPagination(
                            model => model.PageNumbers,
                            model => model.SelectedPageIndex,
                            model => model.OnClickPreviousPage(),
                            model => model.OnClickNextPage());

                        browser.AddRow(row =>
                        {
                            row.SetHeight(RowHeight);

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
                        });
                    });

                    root.AddColumn(editor =>
                    {
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
                            row.AddSpacer();

                            row.AddComboBox()
                                .BindOptions(model => model.NoteCategoryOptions)
                                .BindSelectedIndex(model => model.ActiveNoteCategoryIndex)
                                .BindIsEnabled(model => model.IsNoteSelected)
                                .SetWidth(ComboWidth);

                            row.AddSpacer();
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
            }
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
            {
                col.AddRow(row =>
                {
                    row.SetHeight(ProgressBarRowHeight);

                    row.AddProgressBar()
                        .BindValue(model => model.CategoryUsageProgress)
                        .BindColor(model => model.CategoryUsageColor)
                        .BindTooltip(model => model.CategoryUsageText)
                        .SetHeight(ProgressBarHeight)
                        .AddDrawList(drawList =>
                        {
                            drawList.AddText(text =>
                            {
                                text.BindText(model => model.CategoryUsageText);
                                text.SetBounds(6, 2, ProgressBarTextWidth, ProgressBarHeight);
                                text.SetColor(255, 255, 255);
                            });
                        });
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
}
