using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class NotesViewModel: GuiViewModelBase<NotesViewModel, GuiPayloadBase>
    {
        public const string CategoriesTabPartial = "NOTES_CATEGORIES_TAB_VIEW";
        private const string MainWindowElement = "_window_";
        private const string MainWindowPartial = "%%WINDOW_MAIN%%";

        private const string UntitledNoteName = "Untitled Note";

        private readonly List<string> _pageNoteIds = new();
        private readonly List<PlayerNoteCategory> _categories = new();

        private bool _isLoadingNote;
        private bool _suppressReload;
        private int _totalNoteCount;

        public bool IsSaveEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public GuiBindingList<string> NoteNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> NoteToggled
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public bool IsNoteSelected
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsNewEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsDeleteEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string ActiveNoteName
        {
            get => Get<string>();
            set
            {
                Set(value);

                if(!_isLoadingNote)
                    IsSaveEnabled = true;
            }
        }

        public string ActiveNoteText
        {
            get => Get<string>();
            set
            {
                Set(value);

                if(!_isLoadingNote)
                    IsSaveEnabled = true;
            }
        }

        public int ActiveNoteCategoryIndex
        {
            get => Get<int>();
            set
            {
                Set(value);

                if (!_isLoadingNote)
                    IsSaveEnabled = true;
            }
        }

        public GuiBindingList<GuiComboEntry> NoteCategoryOptions
        {
            get => Get<GuiBindingList<GuiComboEntry>>();
            set => Set(value);
        }

        public int SelectedNoteIndex
        {
            get => Get<int>();
            set => Set(value);
        }

        public float NoteUsageProgress
        {
            get => Get<float>();
            set => Set(value);
        }

        public string NoteUsageText
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiColor NoteUsageColor
        {
            get => Get<GuiColor>();
            set => Set(value);
        }

        public string SearchText
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiBindingList<GuiComboEntry> CategoryFilterOptions
        {
            get => Get<GuiBindingList<GuiComboEntry>>();
            set => Set(value);
        }

        public int SelectedCategoryFilterIndex
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (_suppressReload)
                    return;

                _suppressReload = true;
                SelectedPageIndex = 0;
                _suppressReload = false;
                ReloadIfBound();
            }
        }

        public int SelectedPageIndex
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (!_suppressReload)
                    ReloadIfBound();
            }
        }

        public GuiBindingList<GuiComboEntry> PageNumbers
        {
            get => Get<GuiBindingList<GuiComboEntry>>();
            set => Set(value);
        }

        public bool IsNotesTabToggled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsCategoriesTabToggled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public GuiBindingList<string> CategoryNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> CategoryToggled
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public int SelectedCategoryIndex
        {
            get => Get<int>();
            set => Set(value);
        }

        public string NewCategoryName
        {
            get => Get<string>();
            set
            {
                Set(value);
                IsAddCategoryEnabled = CanAddCategory();
            }
        }

        public bool IsAddCategoryEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsDeleteCategoryEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public float CategoryUsageProgress
        {
            get => Get<float>();
            set => Set(value);
        }

        public string CategoryUsageText
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiColor CategoryUsageColor
        {
            get => Get<GuiColor>();
            set => Set(value);
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            _suppressReload = true;
            SearchText = string.Empty;
            NewCategoryName = string.Empty;
            SelectedCategoryFilterIndex = 0;
            SelectedPageIndex = 0;
            SelectedNoteIndex = -1;
            SelectedCategoryIndex = -1;
            _suppressReload = false;

            _isLoadingNote = true;
            ActiveNoteName = string.Empty;
            ActiveNoteText = string.Empty;
            ActiveNoteCategoryIndex = 0;
            _isLoadingNote = false;

            IsNoteSelected = false;
            IsDeleteEnabled = false;
            IsSaveEnabled = false;
            IsDeleteCategoryEnabled = false;

            LoadCategories();
            LoadNotesList();

            // The notes tab is the base window layout, already shown and free to reflow. Just set
            // the tab state - do not swap, or the freshly-built layout loses its ability to resize.
            IsNotesTabToggled = true;
            IsCategoriesTabToggled = false;

            WatchOnClient(model => model.ActiveNoteName);
            WatchOnClient(model => model.ActiveNoteText);
            WatchOnClient(model => model.ActiveNoteCategoryIndex);
            WatchOnClient(model => model.SearchText);
            WatchOnClient(model => model.NewCategoryName);
            WatchOnClient(model => model.SelectedCategoryFilterIndex);
            WatchOnClient(model => model.SelectedPageIndex);
        }

        private void ReloadIfBound()
        {
            if (Player != 0 && WindowToken > 0)
                LoadNotesList();
        }

        private List<PlayerNote> GetAllNotes()
        {
            var playerId = GetObjectUUID(Player);

            // Paging is mandatory, not an optimization: an unpaged DBQuery falls back to a 50 record
            // limit, which would silently truncate a player's list well below the note cap.
            var query = new DBQuery<PlayerNote>()
                .AddFieldSearch(nameof(PlayerNote.PlayerId), playerId, false)
                .AddFieldSearch(nameof(PlayerNote.IsDMNote), false)
                .OrderBy(nameof(PlayerNote.Name))
                .AddPaging(Notes.MaxNumberOfNotes, 0);

            return DB.Search(query).ToList();
        }

        private void LoadCategories()
        {
            var playerId = GetObjectUUID(Player);

            _categories.Clear();
            _categories.AddRange(Notes.GetCategories(playerId));

            var categoryNames = new GuiBindingList<string>();
            var categoryToggled = new GuiBindingList<bool>();
            var filterOptions = new GuiBindingList<GuiComboEntry>
            {
                new(Notes.AllCategoriesLabel, 0)
            };
            var noteOptions = new GuiBindingList<GuiComboEntry>
            {
                new(Notes.UncategorizedLabel, 0)
            };

            for (var index = 0; index < _categories.Count; index++)
            {
                var category = _categories[index];

                categoryNames.Add(category.Name);
                categoryToggled.Add(false);
                filterOptions.Add(new GuiComboEntry(category.Name, index + 1));
                noteOptions.Add(new GuiComboEntry(category.Name, index + 1));
            }

            CategoryNames = categoryNames;
            CategoryToggled = categoryToggled;
            CategoryFilterOptions = filterOptions;
            NoteCategoryOptions = noteOptions;

            SelectedCategoryIndex = -1;
            IsDeleteCategoryEnabled = false;
            IsAddCategoryEnabled = CanAddCategory();

            CategoryUsageProgress = Notes.GetCategoryUsagePercentage(_categories.Count);
            CategoryUsageText = Notes.GetCategoryUsageText(_categories.Count);
            CategoryUsageColor = Notes.GetCategoryUsageColor(_categories.Count);

            // Replacing the option lists clears the client's selection on both combos.
            RefreshComboSelections();
        }

        private void LoadNotesList()
        {
            // Rebuilding the list clears the editor, so anything the player typed has to be written
            // out first. Every reload path (search, filter, paging, new note, category changes) goes
            // through here, and the window already auto-saves on close - silently discarding edits
            // because the player changed page would be inconsistent with that.
            SaveDirtyNote();

            var notes = GetAllNotes();
            _totalNoteCount = notes.Count;

            var filtered = ApplyFilters(notes);

            UpdatePagination(filtered.Count);

            var pageNotes = filtered
                .Skip(SelectedPageIndex * Notes.EntriesPerPage)
                .Take(Notes.EntriesPerPage)
                .ToList();

            var noteNames = new GuiBindingList<string>();
            var noteToggled = new GuiBindingList<bool>();
            _pageNoteIds.Clear();

            foreach (var note in pageNotes)
            {
                _pageNoteIds.Add(note.Id);
                noteNames.Add(BuildNoteLabel(note));
                noteToggled.Add(false);
            }

            NoteNames = noteNames;
            NoteToggled = noteToggled;

            SelectedNoteIndex = -1;
            IsNoteSelected = false;
            IsDeleteEnabled = false;
            IsSaveEnabled = false;

            _isLoadingNote = true;
            ActiveNoteName = string.Empty;
            ActiveNoteText = string.Empty;
            ActiveNoteCategoryIndex = 0;
            _isLoadingNote = false;

            RefreshNoteUsage();
        }

        private List<PlayerNote> ApplyFilters(List<PlayerNote> notes)
        {
            var search = (SearchText ?? string.Empty).Trim();
            var categoryId = GetCategoryIdByOptionIndex(SelectedCategoryFilterIndex);

            return notes
                .Where(note => string.IsNullOrEmpty(categoryId) || note.CategoryId == categoryId)
                .Where(note => search.Length == 0 ||
                               (note.Name ?? string.Empty).Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        private void UpdatePagination(int totalRecordCount)
        {
            var wasSuppressingReload = _suppressReload;
            _suppressReload = true;

            var pagination = GuiPaginationState.Create(
                totalRecordCount,
                Notes.EntriesPerPage,
                SelectedPageIndex);
            PageNumbers = pagination.PageNumbers;
            SelectedPageIndex = pagination.SelectedPageIndex;

            _suppressReload = wasSuppressingReload;
        }

        private void RefreshNoteUsage()
        {
            NoteUsageProgress = Notes.GetNoteUsagePercentage(_totalNoteCount);
            NoteUsageText = Notes.GetNoteUsageText(_totalNoteCount);
            NoteUsageColor = Notes.GetNoteUsageColor(_totalNoteCount);
            IsNewEnabled = !Notes.IsNoteListFull(_totalNoteCount);
        }

        private string BuildNoteLabel(PlayerNote note)
        {
            var categoryName = GetCategoryNameById(note.CategoryId);

            return string.IsNullOrEmpty(categoryName)
                ? note.Name
                : $"{note.Name} [{categoryName}]";
        }

        private string GetCategoryNameById(string categoryId)
        {
            if (string.IsNullOrEmpty(categoryId))
                return string.Empty;

            return _categories.FirstOrDefault(x => x.Id == categoryId)?.Name ?? string.Empty;
        }

        private string GetCategoryIdByOptionIndex(int optionIndex)
        {
            var categoryIndex = optionIndex - 1;

            return categoryIndex >= 0 && categoryIndex < _categories.Count
                ? _categories[categoryIndex].Id
                : string.Empty;
        }

        /// <summary>
        /// Re-asserts both category combo selections. Assigning the option list clears the client's
        /// selection, so without this the boxes render blank - most visibly with no categories at
        /// all, where the filter would show nothing instead of its "all categories" entry.
        /// </summary>
        private void RefreshComboSelections()
        {
            var wasSuppressingReload = _suppressReload;
            var wasLoadingNote = _isLoadingNote;

            // Re-asserting must not look like a player edit or trigger another list reload.
            _suppressReload = true;
            _isLoadingNote = true;

            SelectedCategoryFilterIndex = ClampCategoryOptionIndex(SelectedCategoryFilterIndex);
            ActiveNoteCategoryIndex = ClampCategoryOptionIndex(ActiveNoteCategoryIndex);

            _isLoadingNote = wasLoadingNote;
            _suppressReload = wasSuppressingReload;
        }

        /// <summary>
        /// Snaps an out of range option index back onto the synthetic first entry, which is always
        /// present. Deleting a category or a stale client value can otherwise leave the combo
        /// pointing at an option that no longer exists.
        /// </summary>
        private int ClampCategoryOptionIndex(int optionIndex)
        {
            return optionIndex >= 0 && optionIndex <= _categories.Count
                ? optionIndex
                : 0;
        }

        private int GetOptionIndexByCategoryId(string categoryId)
        {
            if (string.IsNullOrEmpty(categoryId))
                return 0;

            var categoryIndex = _categories.FindIndex(x => x.Id == categoryId);

            return categoryIndex < 0 ? 0 : categoryIndex + 1;
        }

        private bool CanAddCategory()
        {
            return !Notes.IsCategoryListFull(_categories.Count) &&
                   !string.IsNullOrWhiteSpace(NewCategoryName);
        }

        private void LoadNote()
        {
            if (SelectedNoteIndex <= -1)
                return;

            _isLoadingNote = true;
            var noteId = _pageNoteIds[SelectedNoteIndex];
            var dbNote = DB.Get<PlayerNote>(noteId);

            ActiveNoteName = dbNote.Name;
            ActiveNoteText = dbNote.Text;
            ActiveNoteCategoryIndex = GetOptionIndexByCategoryId(dbNote.CategoryId);
            _isLoadingNote = false;
        }

        private void SaveNote()
        {
            if (SelectedNoteIndex <= -1)
                return;

            var noteId = _pageNoteIds[SelectedNoteIndex];
            ApplyEditsToNote(noteId);

            IsSaveEnabled = false;

            // A rename or category change can move the note between pages or out of the active
            // filter, so rebuild the list and reselect the note wherever it landed.
            ReloadAndSelectNote(noteId);
        }

        /// <summary>
        /// Writes the editor's current contents onto the given note. The name doubles as the list
        /// label and the search key, so a blank one falls back to a placeholder rather than leaving
        /// an unclickable empty row behind.
        /// </summary>
        private void ApplyEditsToNote(string noteId)
        {
            var dbNote = DB.Get<PlayerNote>(noteId);

            // The note can be gone if it was deleted out from under the editor.
            if (dbNote == null)
                return;

            dbNote.Name = string.IsNullOrWhiteSpace(ActiveNoteName)
                ? UntitledNoteName
                : ActiveNoteName.Trim();

            // The text edit caps length client-side, but enforce it here too so a crafted client
            // cannot persist an oversized note. This is the single funnel for every note write.
            var text = ActiveNoteText ?? string.Empty;
            dbNote.Text = text.Length > Notes.MaxNoteLength
                ? text[..Notes.MaxNoteLength]
                : text;

            dbNote.CategoryId = GetCategoryIdByOptionIndex(ActiveNoteCategoryIndex);

            DB.Set(dbNote);
        }

        private void ReloadAndSelectNote(string noteId)
        {
            // Flush before computing the target's position. A pending rename can move the selected
            // note across a page boundary, and the page must be derived from the post-save order or
            // the note lands on a different page than the one we jump to and is never reselected.
            SaveDirtyNote();

            var filtered = ApplyFilters(GetAllNotes());
            var index = filtered.FindIndex(x => x.Id == noteId);

            if (index >= 0)
            {
                _suppressReload = true;
                SelectedPageIndex = index / Notes.EntriesPerPage;
                _suppressReload = false;
            }

            LoadNotesList();

            var pageIndex = _pageNoteIds.IndexOf(noteId);
            if (pageIndex < 0)
                return;

            SelectNote(pageIndex);
        }

        private void SelectNote(int index)
        {
            if (SelectedNoteIndex > -1 && SelectedNoteIndex < NoteToggled.Count)
                NoteToggled[SelectedNoteIndex] = false;

            SelectedNoteIndex = index;
            NoteToggled[index] = true;

            LoadNote();

            IsDeleteEnabled = true;
            IsNoteSelected = true;
            IsSaveEnabled = false;
        }

        private void ShowNotesTab()
        {
            IsNotesTabToggled = true;
            IsCategoriesTabToggled = false;
            RestoreSelectedTabPartial();
        }

        private void ShowCategoriesTab()
        {
            IsNotesTabToggled = false;
            IsCategoriesTabToggled = true;
            RestoreSelectedTabPartial();
        }

        /// <summary>
        /// Swaps the active tab into the window root. The notes tab is the base window layout
        /// (MainWindowPartial), which reflows on resize; the categories tab is a separate partial.
        /// </summary>
        private void RestoreSelectedTabPartial()
        {
            ChangePartialView(
                MainWindowElement,
                IsCategoriesTabToggled ? CategoriesTabPartial : MainWindowPartial);

            // Re-rendering rebuilds that tab's lists and combos, which come back empty until their
            // bound values are pushed again.
            RefreshActiveTabData();
            // NUI can drop a layout while its parent is still being redrawn, so push once more on
            // the next tick.
            DelayCommand(0.0f, RefreshActiveTabData);
        }

        /// <summary>
        /// Re-pushes the bound data for whichever tab is showing.
        /// </summary>
        private void RefreshActiveTabData()
        {
            if (IsCategoriesTabToggled)
            {
                LoadCategories();
            }
            else
            {
                LoadNotesList();
                RefreshComboSelections();
            }
        }

        protected override void OnMainViewRestored()
        {
            RestoreSelectedTabPartial();
        }

        public Action OnCloseWindow() => SaveDirtyNote;

        /// <summary>
        /// Flushes pending editor changes to the database without rebuilding the list. Used both on
        /// window close and ahead of any reload which would otherwise discard them.
        /// </summary>
        private void SaveDirtyNote()
        {
            if (SelectedNoteIndex <= -1 || SelectedNoteIndex >= _pageNoteIds.Count || !IsSaveEnabled)
                return;

            ApplyEditsToNote(_pageNoteIds[SelectedNoteIndex]);
            IsSaveEnabled = false;
        }

        public Action OnClickNotesTab() => ShowNotesTab;

        public Action OnClickCategoriesTab() => ShowCategoriesTab;

        public Action OnClickSearch() => () =>
        {
            _suppressReload = true;
            SelectedPageIndex = 0;
            _suppressReload = false;

            LoadNotesList();
        };

        public Action OnClickClearSearch() => () =>
        {
            SearchText = string.Empty;

            _suppressReload = true;
            SelectedPageIndex = 0;
            _suppressReload = false;

            LoadNotesList();
        };

        public Action OnClickPreviousPage() => () =>
        {
            SelectedPageIndex = Math.Max(0, SelectedPageIndex - 1);
        };

        public Action OnClickNextPage() => () =>
        {
            SelectedPageIndex = Math.Min(PageNumbers.Count - 1, SelectedPageIndex + 1);
        };

        public Action OnClickNewNote() => () =>
        {
            if (Notes.IsNoteListFull(_totalNoteCount))
                return;

            var playerId = GetObjectUUID(Player);
            var note = new PlayerNote
            {
                PlayerId = playerId,
                Name = "New Note",
                Text = string.Empty,
            };

            DB.Set(note);

            // A new note is uncategorized and named "New Note", so an active filter or search would
            // hide it and the button would look like it did nothing. Clear both so it is visible.
            SearchText = string.Empty;

            _suppressReload = true;
            SelectedCategoryFilterIndex = 0;
            _suppressReload = false;

            ReloadAndSelectNote(note.Id);
        };

        public Action OnClickDeleteNote() => () =>
        {
            if (SelectedNoteIndex < 0)
                return;

            var noteId = _pageNoteIds[SelectedNoteIndex];
            var noteName = ActiveNoteName;

            ShowModal($"Are you sure you want to delete the note '{noteName}'?", () =>
            {
                // Drop any pending edits first - the reload below flushes dirty notes, and the
                // player explicitly asked for this one to go away.
                IsSaveEnabled = false;
                SelectedNoteIndex = -1;

                DB.Delete<PlayerNote>(noteId);

                LoadNotesList();
            });
        };

        public Action OnSelectNote() => () =>
        {
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= _pageNoteIds.Count)
                return;

            SelectNote(index);
        };

        public Action OnClickSave() => SaveNote;

        public Action OnClickDiscardChanges() => () =>
        {
            LoadNote();
            IsSaveEnabled = false;
        };

        public Action OnSelectCategory() => () =>
        {
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= _categories.Count)
                return;

            if (SelectedCategoryIndex > -1 && SelectedCategoryIndex < CategoryToggled.Count)
                CategoryToggled[SelectedCategoryIndex] = false;

            SelectedCategoryIndex = index;
            CategoryToggled[index] = true;
            IsDeleteCategoryEnabled = true;
        };

        public Action OnClickAddCategory() => () =>
        {
            var name = (NewCategoryName ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(name))
                return;

            if (Notes.IsCategoryListFull(_categories.Count))
            {
                SendMessageToPC(Player, ColorToken.Red("You have reached the maximum number of note categories."));
                return;
            }

            if (name.Length > Notes.MaxCategoryNameLength)
                name = name[..Notes.MaxCategoryNameLength];

            // The combo boxes prepend synthetic entries for "no category" and "no filter". A real
            // category sharing either label would render as two identical options with different
            // meanings, so those names are reserved.
            if (Notes.IsReservedCategoryName(name))
            {
                SendMessageToPC(Player, ColorToken.Red($"'{name}' is a reserved name. Please choose another."));
                return;
            }

            if (_categories.Any(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                SendMessageToPC(Player, ColorToken.Red($"A category named '{name}' already exists."));
                return;
            }

            // Flush before the category list changes: the editor's combo index is positional, so
            // saving after a re-sort would file the note under the wrong category.
            SaveDirtyNote();

            var playerId = GetObjectUUID(Player);
            Notes.CreateCategory(playerId, name);

            NewCategoryName = string.Empty;

            RefreshAfterCategoryChange();
        };

        public Action OnClickDeleteCategory() => () =>
        {
            if (SelectedCategoryIndex < 0 || SelectedCategoryIndex >= _categories.Count)
                return;

            var category = _categories[SelectedCategoryIndex];

            ShowModal($"Are you sure you want to delete the category '{category.Name}'? Any notes using it will become uncategorized.", () =>
            {
                // Flush before the category list changes: the editor's combo index is positional, so
                // saving after the deletion shifts indices would file the note under the wrong one.
                SaveDirtyNote();

                var playerId = GetObjectUUID(Player);
                Notes.DeleteCategory(playerId, category.Id);

                RefreshAfterCategoryChange();
            });
        };

        private void RefreshAfterCategoryChange()
        {
            var activeFilterId = GetCategoryIdByOptionIndex(SelectedCategoryFilterIndex);

            LoadCategories();

            // The category the note list was filtered on may no longer exist. Fall back to showing
            // every note rather than leaving the player on an empty, unexplained list.
            _suppressReload = true;
            SelectedCategoryFilterIndex = GetOptionIndexByCategoryId(activeFilterId);
            SelectedPageIndex = 0;
            _suppressReload = false;

            LoadNotesList();
        }
    }
}
