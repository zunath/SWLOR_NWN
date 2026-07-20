using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.NWN.API.Engine;

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
        /// Space taken by the window frame, element margins, and the gap between the two panes,
        /// subtracted from the window width before the panes split what is left. Deliberately
        /// generous: a combo slightly narrower than its pane leaves a small gap, while one that
        /// overshoots gets clipped.
        /// </summary>
        private const float WindowChromeWidth = 140f;

        private const float MinimumComboWidth = 140f;

        public const float DefaultWindowWidth = 720f;
        public const float DefaultWindowHeight = 460f;

        private readonly GuiWindowBuilder<NotesViewModel> _builder = new();

        /// <summary>
        /// Converts a window width into the width a combo box should occupy so it spans its pane.
        /// The notes tab is two evenly flexing columns, so each pane gets half the content width.
        /// </summary>
        /// <param name="windowWidth">The current width of the window.</param>
        public static float CalculateComboWidth(float windowWidth)
        {
            var paneWidth = (windowWidth - WindowChromeWidth) / 2f;

            return paneWidth < MinimumComboWidth ? MinimumComboWidth : paneWidth;
        }

        /// <summary>
        /// Builds the category filter combo sized for a specific width, ready to be swapped into
        /// its placeholder via NuiSetGroupLayout.
        /// </summary>
        public static Json BuildFilterComboLayout(float comboWidth)
        {
            return BuildComboLayout(combo => combo
                .BindOptions(model => model.CategoryFilterOptions)
                .BindSelectedIndex(model => model.SelectedCategoryFilterIndex)
                .SetWidth(comboWidth));
        }

        /// <summary>
        /// Builds the note editor's category combo sized for a specific width, ready to be swapped
        /// into its placeholder via NuiSetGroupLayout.
        /// </summary>
        public static Json BuildNoteCategoryComboLayout(float comboWidth)
        {
            return BuildComboLayout(combo => combo
                .BindOptions(model => model.NoteCategoryOptions)
                .BindSelectedIndex(model => model.ActiveNoteCategoryIndex)
                .BindIsEnabled(model => model.IsNoteSelected)
                .SetWidth(comboWidth));
        }

        private static Json BuildComboLayout(Action<GuiComboBox<NotesViewModel>> configure)
        {
            var host = new GuiGroup<NotesViewModel>();
            host.SetShowBorder(false);
            host.SetScrollbars(NuiScrollbars.None);

            host.AddColumn(col =>
            {
                col.AddRow(row =>
                {
                    configure(row.AddComboBox());
                });
            });

            return host.ToJson();
        }

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.Notes)
                .SetInitialGeometry(0, 0, DefaultWindowWidth, DefaultWindowHeight)
                .SetTitle("Notes")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .BindOnClosed(model => model.OnCloseWindow())

                .DefinePartialView(NotesViewModel.NotesTabPartial, BuildNotesTab)
                .DefinePartialView(NotesViewModel.CategoriesTabPartial, BuildCategoriesTab)

                .AddColumn(col =>
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

                    // The two tabs are swapped into this placeholder rather than stacked and toggled
                    // with BindIsVisible - a hidden row still reserves its flex space, which would
                    // leave half the window empty on whichever tab is active.
                    col.AddRow(row =>
                    {
                        row.AddPartialView(NotesViewModel.TabContentPartialElement);
                    });
                });

            return _builder.Build();
        }

        private static void BuildNotesTab(GuiGroup<NotesViewModel> group)
        {
            group.AddColumn(shell =>
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

                        // NUI cannot bind an element's width, and a combo box does not stretch to
                        // fill its row the way a text edit does. The combo therefore lives in its
                        // own placeholder whose layout the view model regenerates at the current
                        // column width whenever the window is resized.
                        browser.AddRow(row =>
                        {
                            row.SetHeight(RowHeight);

                            row.AddPartialView(NotesViewModel.FilterComboElement);
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
                            row.SetHeight(RowHeight);

                            row.AddPartialView(NotesViewModel.NoteCategoryComboElement);
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
            });
        }

        private static void BuildCategoriesTab(GuiGroup<NotesViewModel> group)
        {
            group.AddColumn(col =>
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
            });
        }
    }
}
