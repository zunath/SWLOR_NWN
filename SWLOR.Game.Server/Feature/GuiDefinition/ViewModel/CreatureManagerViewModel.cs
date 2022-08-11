using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class CreatureManagerViewModel: GuiViewModelBase<CreatureManagerViewModel, GuiPayloadBase>
    {
        private readonly List<string> _CreatureIds = new();        
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
            Console.WriteLine("Initialization Complete.");
        }

        public Action OnClickAddNew() => () =>
        {
            Console.WriteLine("Add New Create, Entering Targeting Mode...");
            Targeting.EnterTargetingMode(Player, ObjectType.Creature, "Please click on a creature to save.",
             creature =>
             {
                 if (!GetIsObjectValid(creature) || GetIsDM(creature) || GetIsPC(creature))
                 {
                     Console.WriteLine("Invalid Creature, return.");
                     return;
                 }                     

                 if (GetObjectType(creature) != ObjectType.Creature)
                 {                     
                     Console.WriteLine("ObjectType is not Creature, return.");
                     return;
                 }

                 var serialized = ObjectPlugin.Serialize(creature);
                 var dbCreature = new Creature(GetName(creature), GetTag(creature), serialized);
                 DB.Set<Creature>(dbCreature);

                 Console.WriteLine("Creature Added to DB: " + dbCreature.Name);
                 DeleteLocalObject(Player, "DMCM_CREATURE_TO_SPAWN");
             });
            
            Search();
        };

        public Action OnSelectCreature() => () =>
        {
            var index = NuiGetEventArrayIndex();
            SelectedCreatureIndex = index;

            var dbCreature = DB.Get<Creature>(_CreatureIds[SelectedCreatureIndex]);
            var deserialized = ObjectPlugin.Deserialize(dbCreature.Data);

            SetLocalObject(Player, "DMCM_CREATURE_TO_SPAWN", deserialized);

            NWScript.EnterTargetingMode(Player, ObjectType.All);
            SendMessageToPC(Player, "Please click on a location to spawn " + CreatureNames[SelectedCreatureIndex]);
        };

        public Action OnCreateCreature() => () =>
        {
            var index = NuiGetEventArrayIndex();
            SelectedCreatureIndex = index;

            Console.WriteLine("Create Creature at Location, Selected Index = " + SelectedCreatureIndex);

            var dbCreature = DB.Get<Creature>(_CreatureIds[SelectedCreatureIndex]);
            var deserialized = ObjectPlugin.Deserialize(dbCreature.Data);

            SetLocalObject(Player, "DMCM_CREATURE_TO_SPAWN", deserialized);

            Console.WriteLine("Entering Location Targeting Mode...");
            NWScript.EnterTargetingMode(Player, ObjectType.Tile);
            SendMessageToPC(Player, "Please click on a location to spawn " + CreatureNames[SelectedCreatureIndex]);

            Console.WriteLine("OnCreateCreature complete.");
        };

        public Action OnDeleteCreature() => () =>
        {
            var index = NuiGetEventArrayIndex();
            SelectedCreatureIndex = index;

            Console.WriteLine("Deleting Creature at Index: " + SelectedCreatureIndex);

            DB.Delete<Creature>(_CreatureIds[SelectedCreatureIndex]);
            Search();

            Console.WriteLine("Creature Deleted.");
        };

        [NWNEventHandler("mod_p_target")]
        public static void RunTargetedLocationAction()
        {
            Console.WriteLine("Target Selected Event...");
            var player = GetLastPlayerToSelectTarget();
            var target = GetTargetingModeSelectedObject();
            var targetedLocation = GetTargetingModeSelectedPosition();

            Console.WriteLine("Target Object Name: " + GetName(target));
            Console.WriteLine("Stored Object Name: " + GetName(GetLocalObject(player, "DMCM_CREATURE_TO_SPAWN")));
            if (!GetIsObjectValid(target) && targetedLocation == Vector3())
            {
                Console.WriteLine("Targeting Mode Exited with no selection.");
                return;
            }
            
            if (!GetIsObjectValid(GetLocalObject(player, "DMCM_CREATURE_TO_SPAWN")))
            {
                Console.WriteLine("Stored object to spawn is invalid. Return.");
                return;
            }

            var location = Location(GetArea(player), targetedLocation, 0.0f);
            var deserialized = GetLocalObject(player, "DMCM_CREATURE_TO_SPAWN");
            CopyObject(deserialized, location);
            DeleteLocalObject(player, "DMCM_CREATURE_TO_SPAWN");
            Console.WriteLine("Target Selected Event Complete.");
        }

        private void Search()
        {
            var query = new DBQuery<Creature>()
                .OrderBy(nameof(Creature.Name));                

            if (!string.IsNullOrWhiteSpace(SearchText)) query.AddFieldSearch(nameof(Creature.Name), SearchText, true);

            query.AddPaging(ListingsPerPage, ListingsPerPage * SelectedPageIndex);

            var totalRecordCount = DB.SearchCount<Creature>(query);
            UpdatePagination(totalRecordCount);

            var results = DB.Search<Creature>(query);

            _CreatureIds.Clear();
            var creatureNames = new GuiBindingList<string>();

            foreach (var record in results)
            {                
                _CreatureIds.Add(record.Id);
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
            var pageNumbers = new GuiBindingList<GuiComboEntry>();
            var pages = (int)(totalRecordCount / ListingsPerPage + (totalRecordCount % ListingsPerPage == 0 ? 0 : 1));

            // Always add page 1. In the event no items are for sale,
            // it still needs to be displayed.
            pageNumbers.Add(new GuiComboEntry($"Page 1", 0));
            for (var x = 2; x <= pages; x++)
            {
                pageNumbers.Add(new GuiComboEntry($"Page {x}", x - 1));
            }

            PageNumbers = pageNumbers;

            // In the event no results are found, default the index to zero
            if (pages <= 0)
                SelectedPageIndex = 0;
            // Otherwise, if current page is outside the new page bounds,
            // set it to the last page in the list.
            else if (SelectedPageIndex > pages - 1)
                SelectedPageIndex = pages - 1;

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
