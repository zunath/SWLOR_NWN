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
        // NUI cannot bind a layout width, so the tab content is regenerated for the current window
        // width and swapped in. Element ids default to a fresh Guid, which would leave every
        // regenerated button unwired, so each event bearing element gets a stable id here. These
        // must match between the copies registered at boot and every runtime generated copy.
        private const string NotesTabButtonId = "notes_tab_notes";
        private const string CategoriesTabButtonId = "notes_tab_categories";
        private const string NoteRowButtonId = "notes_note_row";
        private const string ClearSearchButtonId = "notes_clear_search";
        private const string SearchButtonId = "notes_search";
        private const string PreviousPageButtonId = "notes_previous_page";
        private const string NextPageButtonId = "notes_next_page";
        private const string NewNoteButtonId = "notes_new_note";
        private const string DeleteNoteButtonId = "notes_delete_note";
        private const string SaveButtonId = "notes_save";
        private const string DiscardButtonId = "notes_discard";
        private const string AddCategoryButtonId = "notes_add_category";
        private const string CategoryRowButtonId = "notes_category_row";
        private const string DeleteCategoryButtonId = "notes_delete_category";

        private const float RowHeight = 40f;
        private const float ControlHeight = 32f;
        private const float ProgressBarRowHeight = 28f;
        private const float ProgressBarHeight = 20f;
        private const float ProgressBarTextWidth = 440f;
        private const float TabButtonWidth = 180f;

        /// <summary>Window frame and margins, subtracted before the panes split the remainder.</summary>
        private const float WindowChromeWidth = 44f;

        /// <summary>Gap between the two panes, plus their own margins.</summary>
        private const float PaneGutterWidth = 14f;

        private const float MinimumContentWidth = 560f;

        public const float DefaultWindowWidth = 720f;
        public const float DefaultWindowHeight = 460f;

        private readonly GuiWindowBuilder<NotesViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            var defaultWidth = CalculateContentWidth(DefaultWindowWidth);

            _builder.CreateWindow(GuiWindowType.Notes)
                .SetInitialGeometry(0, 0, DefaultWindowWidth, DefaultWindowHeight)
                .SetTitle("Notes")
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .BindOnClosed(model => model.OnCloseWindow())

                // Registered so both tabs' element events are hooked at boot. The layouts actually
                // shown are generated per current window width by the view model.
                .DefinePartialView(NotesViewModel.NotesTabPartial, group => BuildNotesTab(group, defaultWidth))
                .DefinePartialView(NotesViewModel.CategoriesTabPartial, group => BuildCategoriesTab(group, defaultWidth))

                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddPartialView(NotesViewModel.TabContentPartialElement);
                    });
                });

            return _builder.Build();
        }

        /// <summary>
        /// Converts a window width into the width available to the tab content. The generated layout
        /// pins this width, which is what lets the content grow with the window - nothing reflows on
        /// its own.
        /// </summary>
        public static float CalculateContentWidth(float windowWidth)
        {
            var contentWidth = windowWidth - WindowChromeWidth;

            return contentWidth < MinimumContentWidth ? MinimumContentWidth : contentWidth;
        }

        /// <summary>Width of one of the two side by side panes on the notes tab.</summary>
        public static float CalculatePaneWidth(float contentWidth)
        {
            return (contentWidth - PaneGutterWidth) / 2f;
        }

        public static Json BuildNotesTabLayout(float contentWidth)
        {
            var host = new GuiGroup<NotesViewModel>();
            BuildNotesTab(host, contentWidth);

            return host.ToJson();
        }

        public static Json BuildCategoriesTabLayout(float contentWidth)
        {
            var host = new GuiGroup<NotesViewModel>();
            BuildCategoriesTab(host, contentWidth);

            return host.ToJson();
        }

        private static void BuildNotesTab(GuiGroup<NotesViewModel> host, float contentWidth)
        {
            host.SetShowBorder(false);
            host.SetScrollbars(NuiScrollbars.None);

            var paneWidth = CalculatePaneWidth(contentWidth);

            host.AddColumn(outer =>
            {
                outer.SetWidth(contentWidth);

                AddTabRow(outer);

                outer.AddRow(root =>
                {
                    root.AddColumn(browser =>
                    {
                        browser.SetWidth(paneWidth);

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

                        // Combo rows follow the Key Items window: no pinned row height, spacers
                        // either side. A width pinned combo inside a fixed height row makes the
                        // whole tab render its lists empty.
                        browser.AddRow(row =>
                        {
                            row.AddSpacer();

                            row.AddComboBox()
                                .BindOptions(model => model.CategoryFilterOptions)
                                .BindSelectedIndex(model => model.SelectedCategoryFilterIndex)
                                .SetWidth(paneWidth);

                            row.AddSpacer();
                        });

                        browser.AddRow(row =>
                        {
                            row.SetHeight(RowHeight);

                            row.AddTextEdit()
                                .SetPlaceholder("Search")
                                .BindValue(model => model.SearchText);

                            row.AddButton()
                                .SetId(ClearSearchButtonId)
                                .SetText("X")
                                .SetHeight(ControlHeight)
                                .SetWidth(35f)
                                .BindOnClicked(model => model.OnClickClearSearch());

                            row.AddButton()
                                .SetId(SearchButtonId)
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
                                        .SetId(NoteRowButtonId)
                                        .BindText(model => model.NoteNames)
                                        .BindIsToggled(model => model.NoteToggled)
                                        .BindOnClicked(model => model.OnSelectNote());
                                });
                            })
                                .BindRowCount(model => model.NoteNames);
                        });

                        // Inlined rather than AddPagination: the shared control generates its own
                        // Guid ids, which do not survive a layout regeneration.
                        browser.AddRow(row =>
                        {
                            row.AddSpacer();

                            row.AddButton()
                                .SetId(PreviousPageButtonId)
                                .SetText("<")
                                .SetWidth(32f)
                                .SetHeight(ControlHeight)
                                .BindOnClicked(model => model.OnClickPreviousPage());

                            row.AddComboBox()
                                .BindOptions(model => model.PageNumbers)
                                .BindSelectedIndex(model => model.SelectedPageIndex);

                            row.AddButton()
                                .SetId(NextPageButtonId)
                                .SetText(">")
                                .SetWidth(32f)
                                .SetHeight(ControlHeight)
                                .BindOnClicked(model => model.OnClickNextPage());

                            row.AddSpacer();
                        });

                        browser.AddRow(row =>
                        {
                            row.SetHeight(RowHeight);

                            row.AddButton()
                                .SetId(NewNoteButtonId)
                                .SetText("New Note")
                                .BindOnClicked(model => model.OnClickNewNote())
                                .BindIsEnabled(model => model.IsNewEnabled)
                                .SetHeight(ControlHeight);

                            row.AddButton()
                                .SetId(DeleteNoteButtonId)
                                .SetText("Delete Note")
                                .BindOnClicked(model => model.OnClickDeleteNote())
                                .BindIsEnabled(model => model.IsDeleteEnabled)
                                .SetHeight(ControlHeight);
                        });
                    });

                    root.AddColumn(editor =>
                    {
                        editor.SetWidth(paneWidth);

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
                                .SetWidth(paneWidth);

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
                                .SetId(SaveButtonId)
                                .BindOnClicked(model => model.OnClickSave())
                                .SetText("Save")
                                .SetHeight(ControlHeight)
                                .BindIsEnabled(model => model.IsSaveEnabled);

                            row.AddButton()
                                .SetId(DiscardButtonId)
                                .BindOnClicked(model => model.OnClickDiscardChanges())
                                .SetText("Discard Changes")
                                .SetHeight(ControlHeight)
                                .BindIsEnabled(model => model.IsSaveEnabled);
                        });
                    });
                });
            });
        }

        private static void BuildCategoriesTab(GuiGroup<NotesViewModel> host, float contentWidth)
        {
            host.SetShowBorder(false);
            host.SetScrollbars(NuiScrollbars.None);

            host.AddColumn(outer =>
            {
                outer.SetWidth(contentWidth);

                AddTabRow(outer);

                outer.AddRow(row =>
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

                outer.AddRow(row =>
                {
                    row.SetHeight(RowHeight);

                    row.AddTextEdit()
                        .SetPlaceholder("New Category Name")
                        .SetMaxLength(Notes.MaxCategoryNameLength)
                        .BindValue(model => model.NewCategoryName);

                    row.AddButton()
                        .SetId(AddCategoryButtonId)
                        .SetText("Add Category")
                        .SetHeight(ControlHeight)
                        .BindOnClicked(model => model.OnClickAddCategory())
                        .BindIsEnabled(model => model.IsAddCategoryEnabled);
                });

                outer.AddRow(row =>
                {
                    row.AddList(template =>
                    {
                        template.AddCell(cell =>
                        {
                            cell.AddToggleButton()
                                .SetId(CategoryRowButtonId)
                                .BindText(model => model.CategoryNames)
                                .BindIsToggled(model => model.CategoryToggled)
                                .BindOnClicked(model => model.OnSelectCategory());
                        });
                    })
                        .BindRowCount(model => model.CategoryNames);
                });

                outer.AddRow(row =>
                {
                    row.SetHeight(RowHeight);

                    row.AddSpacer();

                    row.AddButton()
                        .SetId(DeleteCategoryButtonId)
                        .SetText("Delete Category")
                        .SetHeight(ControlHeight)
                        .BindOnClicked(model => model.OnClickDeleteCategory())
                        .BindIsEnabled(model => model.IsDeleteCategoryEnabled);

                    row.AddSpacer();
                });
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
                    .SetId(NotesTabButtonId)
                    .SetText("Notes")
                    .SetHeight(ControlHeight)
                    .SetWidth(TabButtonWidth)
                    .BindIsToggled(model => model.IsNotesTabToggled)
                    .BindOnClicked(model => model.OnClickNotesTab());

                row.AddToggleButton()
                    .SetId(CategoriesTabButtonId)
                    .SetText("Manage Categories")
                    .SetHeight(ControlHeight)
                    .SetWidth(TabButtonWidth)
                    .BindIsToggled(model => model.IsCategoriesTabToggled)
                    .BindOnClicked(model => model.OnClickCategoriesTab());

                row.AddSpacer();
            });
        }
    }
}
