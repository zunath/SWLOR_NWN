using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class NotesViewModel: GuiViewModelBase<NotesViewModel, GuiPayloadBase>
    {
        public const int MaxNumberOfNotes = 25;
        public const int MaxNoteLength = 1000;

        private readonly List<string> _noteIds = new();

        private bool _isLoadingNote;
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

        public int SelectedNoteIndex
        {
            get => Get<int>();
            set => Set(value);
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            var playerId = GetObjectUUID(Player);
            var query = new DBQuery<PlayerNote>()
                .AddFieldSearch(nameof(PlayerNote.PlayerId), playerId, false)
                .AddFieldSearch(nameof(PlayerNote.IsDMNote), false)
                .OrderBy(nameof(PlayerNote.Name));
            var notes = DB.Search(query)
                .ToList();

            _noteIds.Clear();
            var noteNames = new GuiBindingList<string>();
            var noteToggled = new GuiBindingList<bool>();

            foreach (var note in notes)
            {
                _noteIds.Add(note.Id);
                noteNames.Add(note.Name);
                noteToggled.Add(false);
            }

            SelectedNoteIndex = -1;
            IsNewEnabled = notes.Count < MaxNumberOfNotes;
            NoteNames = noteNames;
            NoteToggled = noteToggled;
            ActiveNoteName = string.Empty;
            ActiveNoteText = string.Empty;
            IsNoteSelected = false;
            IsSaveEnabled = false;

            WatchOnClient(model => model.ActiveNoteName);
            WatchOnClient(model => model.ActiveNoteText);
        }

        private void LoadNote()
        {
            if (SelectedNoteIndex <= -1)
                return;

            _isLoadingNote = true;
            var noteId = _noteIds[SelectedNoteIndex];
            var dbNote = DB.Get<PlayerNote>(noteId);

            ActiveNoteName = dbNote.Name;
            ActiveNoteText = dbNote.Text;
            _isLoadingNote = false;
        }

        private void SaveNote()
        {
            if (SelectedNoteIndex <= -1)
                return;

            var noteId = _noteIds[SelectedNoteIndex];
            var dbNote = DB.Get<PlayerNote>(noteId);

            dbNote.Name = ActiveNoteName;
            dbNote.Text = ActiveNoteText;

            DB.Set(dbNote);

            IsSaveEnabled = false;
            NoteNames[SelectedNoteIndex] = ActiveNoteName;
        }

        public Action OnCloseWindow() => SaveNote;

        public Action OnClickNewNote() => () =>
        {
            if (_noteIds.Count >= MaxNumberOfNotes)
                return;

            var playerId = GetObjectUUID(Player);
            var note = new PlayerNote
            {
                PlayerId = playerId,
                Name = "New Note",
                Text = string.Empty,
            };

            _noteIds.Add(note.Id);
            NoteNames.Add(note.Name);
            NoteToggled.Add(false);
            IsNewEnabled = _noteIds.Count < MaxNumberOfNotes;

            DB.Set(note);
        };

        public Action OnClickDeleteNote() => () =>
        {
            if (SelectedNoteIndex < 0)
                return;

            var noteId = _noteIds[SelectedNoteIndex];
            var noteName = NoteNames[SelectedNoteIndex];

            ShowModal($"Are you sure you want to delete the note '{noteName}'?", () =>
            {
                _noteIds.RemoveAt(SelectedNoteIndex);
                NoteNames.RemoveAt(SelectedNoteIndex);
                NoteToggled.RemoveAt(SelectedNoteIndex);
                SelectedNoteIndex = -1;

                _isLoadingNote = true;
                IsNoteSelected = false;
                IsDeleteEnabled = false;
                IsNewEnabled = _noteIds.Count < MaxNumberOfNotes;
                ActiveNoteName = string.Empty;
                ActiveNoteText = string.Empty;
                IsSaveEnabled = false;
                _isLoadingNote = false;

                DB.Delete<PlayerNote>(noteId);
            });
        };

        public Action OnSelectNote() => () =>
        {
            if (SelectedNoteIndex > -1)
                NoteToggled[SelectedNoteIndex] = false;

            var index = NuiGetEventArrayIndex();
            SelectedNoteIndex = index;

            LoadNote();

            IsDeleteEnabled = true;
            NoteToggled[index] = true;
            IsNoteSelected = true;

            IsSaveEnabled = false;
        };

        public Action OnClickSave() => SaveNote;

        public Action OnClickDiscardChanges() => () =>
        {
            LoadNote();
            IsSaveEnabled = false;
        };
    }
}
