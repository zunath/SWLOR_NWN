using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.DMToolService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class CreatureManagerViewModel: GuiViewModelBase<CreatureManagerViewModel, GuiPayloadBase>
    {
        private const string FavoriteMarker = "* ";
        private const string FavoriteText = "Favorite";
        private const string UnfavoriteText = "Unfavorite";
        private const string ShowFavoritesText = "Show Favorites";
        private const string ShowAllText = "Show All";

        private readonly List<string> _creatureIds = new();
        private readonly List<string> _rowNames = new();
        private List<DMSavedCreatureFavorite> _favorites = new();
        private bool _showFavoritesOnly;
        private const int ListingsPerPage = 20;
        private bool _skipPaginationSearch;

        public GuiBindingList<GuiComboEntry> PageNumbers
        {
            get => Get<GuiBindingList<GuiComboEntry>>();
            set => Set(value);
        }

        public int SelectedPageIndex
        {
            get => Get<int>();
            set
            {
                Set(value);

                if (!_skipPaginationSearch)
                    Search();
            }
        }

        public string SearchText
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiBindingList<string> CreatureNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> FavoriteTexts
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        /// <summary>
        /// False for favorites whose saved creature was deleted: there is nothing left to create or delete.
        /// </summary>
        public GuiBindingList<bool> CreatureExists
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public string FavoritesFilterText
        {
            get => Get<string>();
            set => Set(value);
        }

        public int SelectedCreatureIndex
        {
            get => Get<int>();
            set => Set(value);
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            SearchText = string.Empty;
            FavoritesFilterText = ShowFavoritesText;
            _favorites = DMCreatureManagerFavorite.Get(Player);
            Search();

            WatchOnClient(model => model.SearchText);
        }

        public Action OnClickAddNew() => () =>
        {
            Targeting.EnterTargetingMode(Player, ObjectType.Creature, "Please click on a creature to save.",
             creature =>
             {
                 if (!GetIsObjectValid(creature) || GetIsDM(creature) || GetIsPC(creature))
                 {
                     return;
                 }

                 if (GetObjectType(creature) != ObjectType.Creature)
                 {
                     return;
                 }

                 var serialized = ObjectPlugin.Serialize(creature);
                 var dbCreature = new DMCreature(GetName(creature), GetTag(creature), serialized);
                 DB.Set(dbCreature);

                 Search();
             });

        };

        public Action OnSelectCreature() => () =>
        {
            var index = NuiGetEventArrayIndex();
            SelectedCreatureIndex = index;

            EnterSpawnTargeting(ObjectType.All);
        };

        public Action OnCreateCreature() => () =>
        {
            var index = NuiGetEventArrayIndex();
            SelectedCreatureIndex = index;

            EnterSpawnTargeting(ObjectType.Tile);
        };

        // Goes through the Targeting service, so a later targeting request (such as the DM Creator's) replaces this
        // one instead of both answering the same click.
        private void EnterSpawnTargeting(ObjectType objectType)
        {
            if (SelectedCreatureIndex < 0 || SelectedCreatureIndex >= _creatureIds.Count)
                return;

            // Another DM can delete a saved creature while this list is open.
            var dbCreature = DB.Get<DMCreature>(_creatureIds[SelectedCreatureIndex]);
            if (dbCreature == null)
            {
                DMPresence.SendMessage(Player, "That saved creature no longer exists.");
                Search();
                return;
            }

            var deserialized = ObjectPlugin.Deserialize(dbCreature.Data);
            var player = Player;

            Targeting.EnterTargetingMode(player, objectType,
                "Please click on a location to spawn " + _rowNames[SelectedCreatureIndex],
                _ => CopyObject(deserialized, Location(GetArea(player), GetTargetingModeSelectedPosition(), 0.0f)),
                allowsLocationTarget: true);
        }

        public Action OnDeleteCreature() => () =>
        {
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= _creatureIds.Count)
                return;

            SelectedCreatureIndex = index;
            var creatureId = _creatureIds[index];
            ShowModal($"Delete {_rowNames[index]}? This cannot be undone.", () =>
            {
                DB.Delete<DMCreature>(creatureId);
                _favorites = DMCreatureManagerFavorite.Remove(Player, creatureId);
                Search();
            });
        };

        public Action OnToggleFavorite() => () =>
        {
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= _creatureIds.Count)
                return;

            // A saved creature another DM deleted after this list loaded can still be removed, but not added.
            var creatureId = _creatureIds[index];
            if (!DMSavedCreatureFavoriteList.Contains(_favorites, creatureId) && !DB.Exists<DMCreature>(creatureId))
            {
                DMPresence.SendMessage(Player, "That saved creature no longer exists.");
                Search();
                return;
            }

            var change = DMCreatureManagerFavorite.Toggle(Player, creatureId, _rowNames[index], out var favorites);
            if (change == DMFavoriteChange.NotSaved)
            {
                DMPresence.SendMessage(Player, "Your favorites could not be saved.");
                return;
            }

            if (change == DMFavoriteChange.Full)
            {
                DMPresence.SendMessage(Player, $"You already have {DMSavedCreatureFavoriteList.MaxFavorites} favorite creatures. Remove one to add another.");
                return;
            }

            // The search and page stay where they were; a removed favorite leaves the favorites list.
            _favorites = favorites;
            Search();
        };

        public Action OnClickToggleFavoritesFilter() => () =>
        {
            _showFavoritesOnly = !_showFavoritesOnly;
            FavoritesFilterText = _showFavoritesOnly ? ShowAllText : ShowFavoritesText;

            _skipPaginationSearch = true;
            SelectedPageIndex = 0;
            _skipPaginationSearch = false;
            Search();
        };

        private void Search()
        {
            if (_showFavoritesOnly)
            {
                SearchFavorites();
                return;
            }

            var query = new DBQuery<DMCreature>()
                .OrderBy(nameof(DMCreature.Name));

            if (!string.IsNullOrWhiteSpace(SearchText)) query.AddFieldSearch(nameof(DMCreature.Name), SearchText, true);

            query.AddPaging(ListingsPerPage, ListingsPerPage * SelectedPageIndex);

            var totalRecordCount = DB.SearchCount(query);
            UpdatePagination(totalRecordCount);

            var results = DB.Search(query);
            ShowRows(results.Select(record => (record.Id, record.Name, true)));
        }

        // Lists the DM's favorites through the same name search and paging, then the favorites whose saved creature
        // was deleted, so they can still be removed.
        private void SearchFavorites()
        {
            var savedIds = GetSavedFavoriteIds();
            // Deleted favorites follow the database's word-start matching, so a search finds them like saved ones.
            var matchingDeleted = _favorites
                .Where(favorite => !savedIds.Contains(favorite.CreatureId) &&
                                   DMSearchText.MatchesWordStarts(SearchText, favorite.Name))
                .OrderBy(favorite => favorite.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            DBQuery<DMCreature> query = null;
            long savedTotal = 0;
            if (savedIds.Count > 0)
            {
                query = new DBQuery<DMCreature>()
                    .OrderBy(nameof(DMCreature.Name))
                    .AddFieldSearch(nameof(DMCreature.Id), savedIds.ToList());
                if (!string.IsNullOrWhiteSpace(SearchText)) query.AddFieldSearch(nameof(DMCreature.Name), SearchText, true);
                savedTotal = DB.SearchCount(query);
            }

            UpdatePagination(savedTotal + matchingDeleted.Count);
            var page = DMSavedCreatureFavoriteList.GetPage(savedTotal, matchingDeleted.Count, SelectedPageIndex, ListingsPerPage);

            var rows = new List<(string Id, string Name, bool Exists)>();
            if (query != null && page.SavedCount > 0)
            {
                query.AddPaging(page.SavedCount, page.SavedOffset);
                rows.AddRange(DB.Search(query).Select(record => (record.Id, record.Name, true)));
            }

            rows.AddRange(matchingDeleted
                .Skip(page.DeletedOffset)
                .Take(page.DeletedCount)
                .Select(favorite => (favorite.CreatureId, favorite.Name, false)));
            ShowRows(rows);
        }

        // The favorites whose saved creature still exists, from one query that reads no records.
        private HashSet<string> GetSavedFavoriteIds()
        {
            var ids = _favorites.Select(favorite => favorite.CreatureId).ToList();
            if (ids.Count == 0)
                return new HashSet<string>();

            var query = new DBQuery<DMCreature>()
                .AddFieldSearch(nameof(DMCreature.Id), ids)
                .AddPaging(ids.Count, 0);
            return DB.SearchIds(query).ToHashSet();
        }

        private void ShowRows(IEnumerable<(string Id, string Name, bool Exists)> rows)
        {
            _creatureIds.Clear();
            _rowNames.Clear();
            var creatureNames = new GuiBindingList<string>();
            var favoriteTexts = new GuiBindingList<string>();
            var creatureExists = new GuiBindingList<bool>();

            foreach (var (id, name, exists) in rows)
            {
                var isFavorite = DMSavedCreatureFavoriteList.Contains(_favorites, id);
                _creatureIds.Add(id);
                _rowNames.Add(name);
                creatureNames.Add(!exists
                    ? $"{name} (deleted)"
                    : isFavorite ? FavoriteMarker + name : name);
                favoriteTexts.Add(isFavorite ? UnfavoriteText : FavoriteText);
                creatureExists.Add(exists);
            }

            CreatureNames = creatureNames;
            FavoriteTexts = favoriteTexts;
            CreatureExists = creatureExists;
            SelectedCreatureIndex = -1;
        }

        public Action OnClickSearch() => Search;

        public Action OnClickClearSearch() => () =>
        {
            SearchText = string.Empty;
            Search();
        };

        private void UpdatePagination(long totalRecordCount)
        {
            _skipPaginationSearch = true;
            var pagination = GuiPaginationState.Create(
                totalRecordCount,
                ListingsPerPage,
                SelectedPageIndex);
            PageNumbers = pagination.PageNumbers;
            SelectedPageIndex = pagination.SelectedPageIndex;

            _skipPaginationSearch = false;
        }
        public Action OnClickPreviousPage() => () =>
        {
            _skipPaginationSearch = true;
            var newPage = SelectedPageIndex - 1;
            if (newPage < 0)
                newPage = 0;

            SelectedPageIndex = newPage;
            _skipPaginationSearch = false;
            Search();
        };

        public Action OnClickNextPage() => () =>
        {
            _skipPaginationSearch = true;
            var newPage = SelectedPageIndex + 1;
            if (newPage > PageNumbers.Count - 1)
                newPage = PageNumbers.Count - 1;

            SelectedPageIndex = newPage;
            _skipPaginationSearch = false;
            Search();
        };
    }
}
