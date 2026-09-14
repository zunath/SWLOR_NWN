using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class CreatureManagerViewModel: GuiViewModelBase<CreatureManagerViewModel, GuiPayloadBase>
    {
        private readonly List<string> _creatureIds = new();
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

        public int SelectedCreatureIndex
        {
            get => Get<int>();
            set => Set(value);
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            SearchText = string.Empty;
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

                 DeleteLocalObject(Player, "DMCM_CREATURE_TO_SPAWN");

                 Search();
             });

        };

        public Action OnSelectCreature() => () =>
        {
            var index = NuiGetEventArrayIndex();
            SelectedCreatureIndex = index;

            var dbCreature = DB.Get<DMCreature>(_creatureIds[SelectedCreatureIndex]);
            var deserialized = ObjectPlugin.Deserialize(dbCreature.Data);

            SetLocalObject(Player, "DMCM_CREATURE_TO_SPAWN", deserialized);

            EnterTargetingMode(Player);
            SendMessageToPC(Player, "Please click on a location to spawn " + CreatureNames[SelectedCreatureIndex]);
        };

        public Action OnCreateCreature() => () =>
        {
            var index = NuiGetEventArrayIndex();
            SelectedCreatureIndex = index;

            var dbCreature = DB.Get<DMCreature>(_creatureIds[SelectedCreatureIndex]);
            var deserialized = ObjectPlugin.Deserialize(dbCreature.Data);

            SetLocalObject(Player, "DMCM_CREATURE_TO_SPAWN", deserialized);

            EnterTargetingMode(Player, ObjectType.Tile);
            SendMessageToPC(Player, "Please click on a location to spawn " + CreatureNames[SelectedCreatureIndex]);
        };

        public Action OnDeleteCreature() => () =>
        {
            var index = NuiGetEventArrayIndex();
            SelectedCreatureIndex = index;

            DB.Delete<DMCreature>(_creatureIds[SelectedCreatureIndex]);
            Search();
        };

        [NWNEventHandler(ScriptName.OnModulePlayerTarget)]
        public static void RunTargetedLocationAction()
        {
            var player = GetLastPlayerToSelectTarget();
            var target = GetTargetingModeSelectedObject();
            var targetedLocation = GetTargetingModeSelectedPosition();

            if (!GetIsObjectValid(target) && targetedLocation == Vector3())
            {
                return;
            }

            if (!GetIsObjectValid(GetLocalObject(player, "DMCM_CREATURE_TO_SPAWN")))
            {
                return;
            }

            var location = Location(GetArea(player), targetedLocation, 0.0f);
            var deserialized = GetLocalObject(player, "DMCM_CREATURE_TO_SPAWN");
            CopyObject(deserialized, location);
            DeleteLocalObject(player, "DMCM_CREATURE_TO_SPAWN");
        }

        private void Search()
        {
            var query = new DBQuery<DMCreature>()
                .OrderBy(nameof(DMCreature.Name));

            if (!string.IsNullOrWhiteSpace(SearchText)) query.AddFieldSearch(nameof(DMCreature.Name), SearchText, true);

            query.AddPaging(ListingsPerPage, ListingsPerPage * SelectedPageIndex);

            var totalRecordCount = DB.SearchCount(query);
            UpdatePagination(totalRecordCount);

            var results = DB.Search(query);

            _creatureIds.Clear();
            var creatureNames = new GuiBindingList<string>();

            foreach (var record in results)
            {
                _creatureIds.Add(record.Id);
                creatureNames.Add(record.Name);
            }

            CreatureNames = creatureNames;
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
