using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Component;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class AreaNotesViewModel: GuiViewModelBase<AreaNotesViewModel, GuiPayloadBase>
    {
        public const int MaxNoteLength = 10000;

        private readonly List<uint> _areas = new();
        private bool _isLoadingNote;

        // Row DTO replacing the three hand-synced parallel GuiBindingList instances
        // (and the parallel _areas list) that Initialize/Search used to build in lockstep.
        private sealed class AreaEntry
        {
            public uint Area { get; }
            public string Resref { get; }
            public string Name { get; }

            public AreaEntry(uint area, string resref, string name)
            {
                Area = area;
                Resref = resref;
                Name = name;
            }
        }

        private static readonly GuiTableSource<AreaNotesViewModel, AreaEntry> AreaTable =
            new GuiTableSource<AreaNotesViewModel, AreaEntry>()
                .Column((m, v) => m.AreaResrefs = v, r => r.Resref)
                .Column((m, v) => m.AreaNames = v, r => r.Name)
                .Column((m, v) => m.AreaToggled = v, r => false);

        public string SearchText
        {
            get => Get<string>();
            set => Set(value);
        }
        public bool IsSaveEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }
        public bool IsDeleteEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public GuiBindingList<bool> AreaToggled
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }
        public GuiBindingList<string> AreaResrefs
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }
        public GuiBindingList<string> AreaNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public bool IsAreaSelected
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string PrivateText
        {
            get => Get<string>();
            set
            {
                Set(value);

                if(!_isLoadingNote)
                    IsSaveEnabled = true;
            }
        }

        public string PublicText
        {
            get => Get<string>();
            set
            {
                Set(value);

                if(!_isLoadingNote)
                    IsSaveEnabled = true;
            }
        }

        public int SelectedAreaIndex
        {
            get => Get<int>();
            set => Set(value);
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            var rows = new List<AreaEntry>();

            _areas.Clear();

            foreach (var area in Area.GetAreas())
            {
                rows.Add(new AreaEntry(area.Value, area.Key, GetName(area.Value)));
            }

            foreach (var row in rows)
                _areas.Add(row.Area);

            SelectedAreaIndex = -1;
            AreaTable.Refresh(this, rows);
            PrivateText = string.Empty;
            PublicText = string.Empty;
            IsAreaSelected = false;
            IsSaveEnabled = false;

            SearchText = string.Empty;
            Search();

            WatchOnClient(model => model.PrivateText);
            WatchOnClient(model => model.PublicText);
            WatchOnClient(model => model.SearchText);
        }

        private void LoadNote()
        {
            if (SelectedAreaIndex <= -1)
                return;

            _isLoadingNote = true;

            var query = new DBQuery<AreaNote>()
                .AddFieldSearch(nameof(AreaNote.AreaResref), AreaResrefs[SelectedAreaIndex], false)
                .OrderBy(nameof(AreaNote.AreaResref));
            var notes = DB.Search(query)
                .ToList();

            foreach (var note in notes)
            {
                PrivateText = note.PrivateText;
                PublicText = note.PublicText;
                _isLoadingNote = false;
            }

            if (_isLoadingNote)
            {
                var dbNote = new AreaNote
                {
                    AreaResref = AreaResrefs[SelectedAreaIndex]
                };
                DB.Set<AreaNote>(dbNote);
            }

            _isLoadingNote = false;
            IsSaveEnabled = false;
        }

        private void SaveNote()
        {
            if (SelectedAreaIndex <= -1)
                return;

            var query = new DBQuery<AreaNote>()
                .AddFieldSearch(nameof(AreaNote.AreaResref), AreaResrefs[SelectedAreaIndex], false)
                .OrderBy(nameof(AreaNote.AreaResref));
            var notes = DB.Search(query)
                .ToList();

            foreach (var note in notes)
            {
                note.PrivateText = PrivateText;
                note.PublicText = PublicText;
                _isLoadingNote = false;
            }

            var message = AreaNames[SelectedAreaIndex] + ": " + notes[0].PublicText;
            foreach (var player in Area.GetPlayersInArea(_areas[SelectedAreaIndex]))
            {
                SendMessageToPC(player, ColorToken.Purple(message));
            }

            DB.Set(notes[0]);
            IsSaveEnabled = false;
        }

        public Action OnCloseWindow() => SaveNote;

        public Action OnClickDeleteNote() => () =>
        {
            if (SelectedAreaIndex < 0)
                return;

            ShowModal($"Are you sure you want to delete the note for this area? '{AreaNames[SelectedAreaIndex]}'", () =>
            {
                PrivateText = string.Empty;
                PublicText = string.Empty;
                _isLoadingNote = true;
                IsAreaSelected = false;
                IsDeleteEnabled = false;
                IsSaveEnabled = false;
                _isLoadingNote = false;

                SaveNote();
            });

            IsSaveEnabled = false;
        };

        public Action OnSelectNote() => () =>
        {
            if (SelectedAreaIndex > -1)
                AreaToggled[SelectedAreaIndex] = false;

            var index = NuiGetEventArrayIndex();
            SelectedAreaIndex = index;

            LoadNote();

            IsDeleteEnabled = true;
            AreaToggled[index] = true;
            IsAreaSelected = true;

            IsSaveEnabled = false;
        };

        public Action OnClickSave() => SaveNote;

        public Action OnClickDiscardChanges() => () =>
        {
            LoadNote();
            IsSaveEnabled = false;
        };

        private void Search()
        {
            _areas.Clear();
            AreaToggled.Clear();
            AreaNames.Clear();
            AreaResrefs.Clear();

            var rows = new List<AreaEntry>();

            if (string.IsNullOrWhiteSpace(SearchText))
            {
                foreach (var area in Area.GetAreas())
                {
                    rows.Add(new AreaEntry(area.Value, area.Key, GetName(area.Value)));
                }
            }
            else
            {
                foreach (var area in Area.GetAreas())
                {
                    if (GetStringUpperCase(GetName(area.Value)).Contains(GetStringUpperCase(SearchText)))
                    {
                        rows.Add(new AreaEntry(area.Value, area.Key, GetName(area.Value)));
                    }
                }
            }

            foreach (var row in rows)
                _areas.Add(row.Area);

            SelectedAreaIndex = -1;
            AreaTable.Refresh(this, rows);
            PrivateText = string.Empty;
            PublicText = string.Empty;
            IsAreaSelected = false;
            IsSaveEnabled = false;
        }

        public Action OnClickSearch() => Search;

        public Action OnClickClearSearch() => () =>
        {
            SearchText = string.Empty;
            Search();
        };
    }
}
