using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;

using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Service;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class AreaNotesViewModel: GuiViewModelBase<AreaNotesViewModel, GuiPayloadBase>
    {
        private static readonly IDatabaseService _db = ServiceContainer.GetService<IDatabaseService>();
        
        public const int MaxNoteLength = 10000;

        private readonly List<uint> _areas = new();
        private bool _isLoadingNote;

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
            var areaResrefs = new GuiBindingList<string>();
            var areaNames = new GuiBindingList<string>();
            var areaToggled = new GuiBindingList<bool>();

            _areas.Clear();

            foreach (var area in Area.GetAreas())
            {
                _areas.Add(area.Value);
                areaResrefs.Add(area.Key);
                areaNames.Add(GetName(area.Value));
                areaToggled.Add(false);
            }

            SelectedAreaIndex = -1;
            AreaResrefs = areaResrefs;
            AreaNames = areaNames;
            AreaToggled = areaToggled;
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
            var notes = _db.Search(query)
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
                _db.Set<AreaNote>(dbNote);
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
            var notes = _db.Search(query)
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

            _db.Set(notes[0]);
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
            var areaResrefs = new GuiBindingList<string>();
            var areaNames = new GuiBindingList<string>();
            var areaToggled = new GuiBindingList<bool>();

            _areas.Clear();
            AreaToggled.Clear();
            AreaNames.Clear();
            AreaResrefs.Clear();

            if (string.IsNullOrWhiteSpace(SearchText)) 
            {
                foreach (var area in Area.GetAreas())
                {
                    _areas.Add(area.Value);
                    areaResrefs.Add(area.Key);
                    areaNames.Add(GetName(area.Value));
                    areaToggled.Add(false);
                }
            }
            else
            {
                foreach (var area in Area.GetAreas())
                {
                    if (GetStringUpperCase(GetName(area.Value)).Contains(GetStringUpperCase(SearchText)))
                    {
                        _areas.Add(area.Value);
                        areaResrefs.Add(area.Key);
                        areaNames.Add(GetName(area.Value));
                        areaToggled.Add(false);
                    }
                }
            }

            SelectedAreaIndex = -1;
            AreaResrefs = areaResrefs;
            AreaNames = areaNames;
            AreaToggled = areaToggled;
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
