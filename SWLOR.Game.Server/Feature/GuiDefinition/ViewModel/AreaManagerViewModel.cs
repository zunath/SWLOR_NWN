using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;
using Pipelines.Sockets.Unofficial.Arenas;
using SWLOR.Game.Server.Core.NWScript.Enum.Area;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class AreaManagerViewModel: GuiViewModelBase<AreaManagerViewModel, GuiPayloadBase>
    {
        private readonly List<uint> _areas = new();
        private readonly List<uint> _objects = new();
        private readonly List<int> _objectAppearanceIds = new();
        private readonly List<string> _objectAppearanceNames = new();

        private static Dictionary<int, Dictionary<int, string>> _placeableAppearanceByPage { get; } = new();
        private static Dictionary<int, Dictionary<int, string>> _creatureAppearanceByPage { get; } = new();
        private static int _totalCountAppearancePlaceables;
        private static int _totalCountAppearanceCreatures;

        // Despite being greyed out, this private bool is actually used - if deleted, it'll cause a lot of errors.
        private bool _skipPaginationSearch;

        public string SearchText
        {
            get => Get<string>();
            set => Set(value);
        }
        public string SearchAppearanceText
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsDeleteObjectEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }
        public bool IsResetAreaEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }
        public bool IsResaveAllObjectsEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }
        public bool IsSelectedObjectPlaceableOrCreature
        {
            get => Get<bool>();
            set => Set(value);
        }

        public int SelectedAreaIndex
        {
            get => Get<int>();
            set
            {
                Set(value);
                _objectAppearanceIds.Clear();
                _objectAppearanceNames.Clear();
                _placeableAppearanceByPage.Clear();
                _creatureAppearanceByPage.Clear();
                ObjectAppearanceList = new GuiBindingList<string>();
                ObjectAppearanceToggled = new GuiBindingList<bool>();
                SelectedAreaObjectIndex = -1;
            }
        }
        public int SelectedAreaObjectIndex
        {
            get => Get<int>();
            set 
            { 
                Set(value);
                _objectAppearanceIds.Clear();
                _objectAppearanceNames.Clear();
                _placeableAppearanceByPage.Clear();
                _creatureAppearanceByPage.Clear();
                ObjectAppearanceList = new GuiBindingList<string>();
                ObjectAppearanceToggled = new GuiBindingList<bool>();
            }
        }
        public int SelectedAppearanceIndex
        {
            get => Get<int>();
            set => Set(value);
        }

        public string SelectedAreaObjectName
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiBindingList<bool> AreaToggled
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }
        public GuiBindingList<bool> AreaObjectToggled
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }
        public GuiBindingList<bool> ObjectAppearanceToggled
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
        public GuiBindingList<string> AreaObjectList
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }
        public GuiBindingList<string> ObjectAppearanceList
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<GuiComboEntry> PageNumbersAppearances
        {
            get => Get<GuiBindingList<GuiComboEntry>>();
            set => Set(value);
        }

        public int SelectedPageIndexAppearances
        {
            get => Get<int>();
            set => Set(value);           
        }

        public static void CacheTemplateArea(string resref, uint area)
        {
            Service.AreaTemplate.TemplateAreaByResRef[resref] = area;
        }

        public static bool GetIsTemplateArea(uint area)
        {
            return GetLocalBool(area, "IS_TEMPLATE_AREA");
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            Init();            
            Search();
            WatchOnClient(model => model.SearchText);
            WatchOnClient(model => model.SearchAppearanceText); 
            WatchOnClient(model => model.SelectedAreaObjectName);
            WatchOnClient(model => model.SelectedPageIndexAppearances);
        }

        public Action OnSelectArea() => () =>
        {
            _skipPaginationSearch = true;
            ClearObjectHighlight();

            if (SelectedAreaIndex > -1)
                AreaToggled[SelectedAreaIndex] = false;

            if (SelectedAreaObjectIndex > -1)
            {
                AreaObjectToggled[SelectedAreaObjectIndex] = false;
                PlayerPlugin.ApplyLoopingVisualEffectToObject(Player, _objects[SelectedAreaObjectIndex], VisualEffect.None);
            }

            var index = NuiGetEventArrayIndex();
            SelectedAreaIndex = index;

            LoadAreaObjectList();

            SelectedAreaObjectName = String.Empty;

            IsDeleteObjectEnabled = false;
            IsResetAreaEnabled = true;
            IsResaveAllObjectsEnabled = true;

            AreaToggled[index] = true;
            _skipPaginationSearch = false;
        };

        public Action OnSelectAreaObject() => () =>
        {
            _skipPaginationSearch = true;
            ClearObjectHighlight();

            if (SelectedAreaObjectIndex > -1)
                AreaObjectToggled[SelectedAreaObjectIndex] = false;                

            var index = NuiGetEventArrayIndex();
            SelectedAreaObjectIndex = index;

            if (GetObjectType(_objects[SelectedAreaObjectIndex]) == ObjectType.Placeable ||
                GetObjectType(_objects[SelectedAreaObjectIndex]) == ObjectType.Creature)
            {
                IsSelectedObjectPlaceableOrCreature = true;
                PlayerPlugin.ApplyLoopingVisualEffectToObject(Player, _objects[SelectedAreaObjectIndex], VisualEffect.Vfx_Dur_Aura_Green);
                SelectedAreaObjectName = GetName(_objects[SelectedAreaObjectIndex]);
            }
            else
            {
                IsSelectedObjectPlaceableOrCreature = false;
            }

            SearchAppearances(GetObjectType(_objects[SelectedAreaObjectIndex]));

            IsDeleteObjectEnabled = true;
            AreaObjectToggled[index] = true;
            _skipPaginationSearch = false;
        };

        public Action OnClickDeleteObject() => () =>
        {
            if (SelectedAreaIndex < 0)
                return;

            var areaObject = _objects[SelectedAreaObjectIndex];
            var areaObjectId = GetLocalString(areaObject, "DBID");

            var query = new DBQuery<AreaTemplateObject>()
                .AddFieldSearch(nameof(AreaTemplateObject.AreaResref), GetResRef(_areas[SelectedAreaIndex]), false)
                .AddFieldSearch(nameof(AreaTemplateObject.Id), areaObjectId, false)
                .OrderBy(nameof(AreaTemplateObject.AreaResref));
            var areaTemplates = DB.Search(query)
                .ToList();

            foreach (var dbRecord in areaTemplates)
            {
                DB.Delete<AreaTemplateObject>(dbRecord.Id);
            }

            AreaTemplate.RemoveTemplateAreaCustomObjectByArea(_areas[SelectedAreaIndex], areaObject);
            DestroyObject(areaObject);

            _objects.RemoveAt(SelectedAreaObjectIndex);
            AreaObjectList.RemoveAt(SelectedAreaObjectIndex);
            AreaObjectToggled.RemoveAt(SelectedAreaObjectIndex);
            SelectedAreaObjectIndex = -1;
            IsDeleteObjectEnabled = false;
        };

        /// <summary>
        /// Click this to delete all objects currently in the selected Template Area to reset it.
        /// </summary>
        /// <returns></returns>
        public Action OnClickResetArea() => () =>
        {
            if (!(SelectedAreaIndex > -1))
                return;

            ShowModal($"Are you sure you want to permanently remove all DM spawned objects from '{AreaNames[SelectedAreaIndex]}'", () =>
            {
                var query = new DBQuery<AreaTemplateObject>()
                    .AddFieldSearch(nameof(AreaTemplateObject.AreaResref), GetResRef(_areas[SelectedAreaIndex]), false)
                    .OrderBy(nameof(AreaTemplateObject.AreaResref));
                var areaTemplates = DB.Search(query)
                    .ToList();

                foreach (var dbRecord in areaTemplates)
                {
                    DB.Delete<AreaTemplateObject>(dbRecord.Id);
                }

                foreach (var areaObject in AreaTemplate.GetTemplateAreaCustomObjectsByArea(_areas[SelectedAreaIndex]))
                {
                    AreaTemplate.RemoveTemplateAreaCustomObjectByArea(_areas[SelectedAreaIndex], areaObject);
                    DestroyObject(areaObject);
                }
                Search();
            });
        };

        public Action OnClickResaveAllObjects() => () =>
        {
            ClearObjectHighlight();

            if (!(SelectedAreaIndex > -1))
                return;

            ShowModal($"Are you sure you want to permanently resave all DM spawned objects in '{AreaNames[SelectedAreaIndex]}'?", () =>
            {
                AreaTemplate.ResaveAllAreaTemplateObjectsByArea(_areas[SelectedAreaIndex]);
                Search();
            });
        };

        /// <summary>
        /// Click this to create a new Template Area. It opens up another GuiWindow that names said new Template Area before creating it.
        /// </summary>
        /// <returns></returns>

        public Action CreateNewAreaTemplate() => () =>
        {
            ShowModal($"Are you sure you want to create a new template area? Try not to flood the module with too many of those!", () =>
            {
                Gui.TogglePlayerWindow(Player, GuiWindowType.AreaManagerNaming);
            });
        };

        /// <summary>
        /// Click this to delete the selected Template Area and all objects saved to it. There's a modal to prevent DMs from accidental deletion, but otherwise, DMs are expected not to abuse this.
        /// </summary>
        /// <returns></returns>
        public Action OnClickDeleteAreaTemplate() => () =>
        {
            if (!(SelectedAreaIndex > -1))
                return;

            ShowModal($"Are you sure you want to permanently delete '{AreaNames[SelectedAreaIndex]}'? The template area cannot be recovered. This action cannot be undone. Please ensure that you are deleting the right area and not another DM's work!", () =>
            {
                var query = new DBQuery<AreaTemplateObject>()
                    .AddFieldSearch(nameof(AreaTemplateObject.AreaResref), GetResRef(_areas[SelectedAreaIndex]), false)
                    .OrderBy(nameof(AreaTemplateObject.AreaResref));
                var areaTemplates = DB.Search(query)
                    .ToList();

                foreach (var dbRecord in areaTemplates)
                {
                    DB.Delete<AreaTemplateObject>(dbRecord.Id);
                }

                foreach (var areaObject in AreaTemplate.GetTemplateAreaCustomObjectsByArea(_areas[SelectedAreaIndex]))
                {
                    AreaTemplate.RemoveTemplateAreaCustomObjectByArea(_areas[SelectedAreaIndex], areaObject);
                    DestroyObject(areaObject);
                }

                var areaTemplateQuery = new DBQuery<TemplateArea>()
                    .AddFieldSearch(nameof(TemplateArea.TemplateAreaTag), GetTag(_areas[SelectedAreaIndex]), false);
                var templateArea = DB.Search(areaTemplateQuery)
                    .ToList();

                foreach (var dbRecord in templateArea)
                {
                    var playerName = GetName(Player);
                    
                    DB.Delete<TemplateArea>(dbRecord.Id);
                    Console.WriteLine($"{playerName} has deleted {dbRecord.TemplateAreaName}");
                }

                DestroyArea(_areas[SelectedAreaIndex]);
                Search();
            });
        };

        public Action OnClickSearch() => Search;

        public Action OnClickClearSearch() => () =>
        {
            SearchText = string.Empty;
            Search();
        };

        public Action OnClickAppearanceSearch() => () =>
        {
            SelectedPageIndexAppearances = 0;
            SearchAppearances(GetObjectType(_objects[SelectedAreaObjectIndex]));
        };

        public Action OnClickClearAppearanceSearch() => () =>
        {
            SelectedPageIndexAppearances = 0;
            SearchAppearanceText = string.Empty;
            SearchAppearances(GetObjectType(_objects[SelectedAreaObjectIndex]));
        };

        public Action OnWindowClose() => () =>
        {
            ClearObjectHighlight();
        };

        private void Init()
        {
            _skipPaginationSearch = true;

            var areaResrefs = new GuiBindingList<string>();
            var areaNames = new GuiBindingList<string>();
            var areaToggled = new GuiBindingList<bool>();
            var areaObjectList = new GuiBindingList<string>();
            var areaObjectToggled = new GuiBindingList<bool>();

            _areas.Clear();
            _objects.Clear();

            AreaResrefs = areaResrefs;
            AreaNames = areaNames;
            AreaToggled = areaToggled;
            AreaObjectList = areaObjectList;
            AreaObjectToggled = areaObjectToggled;

            SelectedAreaIndex = -1;
            SelectedAreaObjectIndex = -1;
            SearchText = string.Empty;
            SearchAppearanceText = String.Empty;

            _totalCountAppearancePlaceables = 0;
            _totalCountAppearanceCreatures = 0;

            _skipPaginationSearch = false;
        }

        private void Search()
        {
            _skipPaginationSearch = true;

            ClearObjectHighlight();

            var areaResrefs = new GuiBindingList<string>();
            var areaNames = new GuiBindingList<string>();
            var areaToggled = new GuiBindingList<bool>();
            var areaObjectList = new GuiBindingList<string>();
            var areaObjectToggled = new GuiBindingList<bool>();
            var objectAppearanceToggled = new GuiBindingList<bool>();

            _areas.Clear();
            _objects.Clear();

            AreaToggled.Clear();
            AreaNames.Clear();
            AreaResrefs.Clear();
            AreaObjectList.Clear();
            AreaObjectToggled.Clear();

            if (string.IsNullOrWhiteSpace(SearchText))
            {
                for (var area = GetFirstArea(); GetIsObjectValid(area); area = GetNextArea())
                {
                    if (GetIsTemplateArea(area))
                    {
                        _areas.Add(area);
                        areaNames.Add(GetName(area));
                        areaToggled.Add(false);
                    }
                }
            }
            else
            {
                for (var area = GetFirstArea(); GetIsObjectValid(area); area = GetNextArea())
                {
                    if (GetStringUpperCase(GetName(area)).Contains(GetStringUpperCase(SearchText)) && GetIsTemplateArea(area))
                    {
                        _areas.Add(area);
                        areaNames.Add(GetName(area));
                        areaToggled.Add(false);
                    }
                }
            }

            SelectedAreaIndex = -1;
            SelectedAreaObjectIndex = -1;
            SelectedAppearanceIndex = -1;

            SelectedPageIndexAppearances = 0;

            AreaResrefs = areaResrefs;
            AreaNames = areaNames;
            AreaToggled = areaToggled;
            AreaObjectList = areaObjectList;
            AreaObjectToggled = areaObjectToggled;
            ObjectAppearanceToggled = objectAppearanceToggled;

            IsDeleteObjectEnabled = false;
            IsResetAreaEnabled = false;
            IsResaveAllObjectsEnabled = false;

            SelectedAreaObjectName = String.Empty;

            IsSelectedObjectPlaceableOrCreature = false;

            _skipPaginationSearch = false;
        }

        public void SearchAppearances(ObjectType objectType)
        {
            _objectAppearanceIds.Clear();
            _objectAppearanceNames.Clear();
            _placeableAppearanceByPage.Clear();
            _creatureAppearanceByPage.Clear();
            ObjectAppearanceToggled.Clear();

            var objectAppearanceList = new GuiBindingList<string>();
            var objectAppearanceToggled = new GuiBindingList<bool>();

            var pageNumber = 0;
            var actualRowCount = 0;            

            switch (objectType)
            {
                case ObjectType.Placeable:
                    for (var page = 0; 
                         page <AreaTemplate.GetPlaceableAppearancePageCount(); 
                         page++)
                    {
                        foreach (var appearance in AreaTemplate.GetPlaceableAppearances(page))
                        {
                            if (string.IsNullOrWhiteSpace(SearchAppearanceText))
                            {
                                if (actualRowCount > 0)
                                    if (actualRowCount % AreaTemplate.PageSize == 0) pageNumber++;
                                    
                                if (!_placeableAppearanceByPage.ContainsKey(pageNumber))
                                    _placeableAppearanceByPage[pageNumber] = new Dictionary<int, string>();

                                _placeableAppearanceByPage[pageNumber].Add(appearance.Key, appearance.Value);
                                actualRowCount++;
                            }
                            else
                            {
                                if (GetStringUpperCase(appearance.Value).Contains(GetStringUpperCase(SearchAppearanceText)))
                                {
                                    if (actualRowCount > 0)
                                        if (actualRowCount % AreaTemplate.PageSize == 0) pageNumber++;
                                        
                                    if (!_placeableAppearanceByPage.ContainsKey(pageNumber))
                                        _placeableAppearanceByPage[pageNumber] = new Dictionary<int, string>();

                                    _placeableAppearanceByPage[pageNumber].Add(appearance.Key, appearance.Value);
                                    actualRowCount++;
                                }
                            }
                        }
                    }
                    _totalCountAppearancePlaceables = actualRowCount;

                    foreach (var appearance in _placeableAppearanceByPage[SelectedPageIndexAppearances])
                    {
                        _objectAppearanceIds.Add(appearance.Key);
                        _objectAppearanceNames.Add(appearance.Value);
                        objectAppearanceList.Add(appearance.Value);
                        objectAppearanceToggled.Add(false);
                    }

                    UpdatePaginationAppearances(GetObjectType(_objects[SelectedAreaObjectIndex]));
                    break;
                case ObjectType.Creature:
                    for (var page = 0;
                         page <AreaTemplate.GetCreatureAppearancePageCount();
                         page++)
                    {
                        foreach (var appearance in AreaTemplate.GetCreatureAppearances(page))
                        {
                            if (string.IsNullOrWhiteSpace(SearchAppearanceText))
                            {
                                if (actualRowCount > 0)
                                    if (actualRowCount % AreaTemplate.PageSize == 0) pageNumber++;

                                if (!_creatureAppearanceByPage.ContainsKey(pageNumber))
                                    _creatureAppearanceByPage[pageNumber] = new Dictionary<int, string>();

                                _creatureAppearanceByPage[pageNumber].Add(appearance.Key, appearance.Value);
                                actualRowCount++;
                            }
                            else
                            {
                                if (GetStringUpperCase(appearance.Value).Contains(GetStringUpperCase(SearchAppearanceText)))
                                {
                                    if (actualRowCount > 0)
                                        if (actualRowCount % AreaTemplate.PageSize == 0) pageNumber++;
                                        
                                    if (!_creatureAppearanceByPage.ContainsKey(pageNumber))
                                        _creatureAppearanceByPage[pageNumber] = new Dictionary<int, string>();

                                    _creatureAppearanceByPage[pageNumber].Add(appearance.Key, appearance.Value);
                                    actualRowCount++;
                                }
                            }
                        }
                    }
                    _totalCountAppearanceCreatures = actualRowCount;

                    if (_creatureAppearanceByPage[SelectedPageIndexAppearances].Count > 0)
                    {
                        foreach (var appearance in _creatureAppearanceByPage[SelectedPageIndexAppearances])
                        {
                            _objectAppearanceIds.Add(appearance.Key);
                            _objectAppearanceNames.Add(appearance.Value);
                            objectAppearanceList.Add(appearance.Value);
                            objectAppearanceToggled.Add(false);
                        }
                    }

                    UpdatePaginationAppearances(GetObjectType(_objects[SelectedAreaObjectIndex]));
                    break;
                default:
                    break;
            }

            ObjectAppearanceList = objectAppearanceList;
            ObjectAppearanceToggled = objectAppearanceToggled;
        }

        public void UpdatePaginationAppearances(ObjectType objectType)
        {
            var pageNumbers = new GuiBindingList<GuiComboEntry>();
            var totalCount = 0;

            switch (objectType)
            {
                case ObjectType.Placeable:
                    totalCount = _totalCountAppearancePlaceables;
                    break;
                case ObjectType.Creature:
                    totalCount = _totalCountAppearanceCreatures;
                    break;
                default:
                    break;
            }

            var pages = (int)(totalCount / AreaTemplate.PageSize + (totalCount % AreaTemplate.PageSize == 0 ? 0 : 1));

            // Always add page 1. In the event no structures are available,
            // it still needs to be displayed.
            pageNumbers.Add(new GuiComboEntry($"Page 1", 0));
            for (var x = 2; x <= pages; x++)
            {
                pageNumbers.Add(new GuiComboEntry($"Page {x}", x - 1));
            }

            PageNumbersAppearances = pageNumbers;

            // In the event no results are found, default the index to zero
            if (pages <= 0)
                SelectedPageIndexAppearances = 0;
            // Otherwise, if current page is outside the new page bounds,
            // set it to the last page in the list.
            else if (SelectedPageIndexAppearances > pages - 1)
                SelectedPageIndexAppearances = pages - 1;

            _skipPaginationSearch = false;
        }

        private void ClearObjectHighlight()
        {
            if (SelectedAreaObjectIndex > -1)
            {
                PlayerPlugin.ApplyLoopingVisualEffectToObject(Player, _objects[SelectedAreaObjectIndex], VisualEffect.None);
            }
        }

        private void LoadAreaObjectList()
        {
            _skipPaginationSearch = true;

            if (SelectedAreaIndex < 0)
                return;

            SelectedPageIndexAppearances = 0;

            var areaObjectList = new GuiBindingList<string>();
            var areaObjectToggled = new GuiBindingList<bool>();           

            _objects.Clear();
            foreach (var areaObject in AreaTemplate.GetTemplateAreaCustomObjectsByArea(_areas[SelectedAreaIndex]))
            {
                _objects.Add(areaObject);
                areaObjectList.Add(GetName(areaObject));
                areaObjectToggled.Add(false);
            }

            AreaObjectList = areaObjectList;
            AreaObjectToggled = areaObjectToggled;

            IsDeleteObjectEnabled = false;
            _skipPaginationSearch = true;
        }

        public Action OnClickGoToArea() => () =>
        {
            if (!(SelectedAreaIndex > -1))
                return;

            var area = _areas[SelectedAreaIndex];
            var centerHeightOfTemplateArea = GetAreaSize(Dimension.Height, area) / 2f;
            var centerWidthOfTemplateArea = GetAreaSize(Dimension.Width, area) / 2f;
            var centerHeightOfTemplateAreaInMeters = centerHeightOfTemplateArea * 10f;
            var centerWidthOfTemplateAreaInMeters = centerWidthOfTemplateArea * 10f;
            var vectorOfTemplateArea = Vector3(centerHeightOfTemplateAreaInMeters, centerWidthOfTemplateAreaInMeters, 0.0f);
            var position = Location(area, vectorOfTemplateArea, 0.0f);

            AssignCommand(Player, () => ActionJumpToLocation(position));
        };

        public Action OnClickGoToObject() => () =>
        {
            if (SelectedAreaObjectIndex < 0)
                return;

            var obj = _objects[SelectedAreaObjectIndex];
            var position = GetLocation(obj);

            AssignCommand(Player, () => ActionJumpToLocation(position));
        };

        public Action OnRotateClockwise() => () =>
        {
            if (SelectedAreaObjectIndex < 0)
                return;

            var placeable = _objects[SelectedAreaObjectIndex];
            var facing = GetFacing(placeable) + 20f;

            while (facing > 360f)
                facing -= 360f;

            AssignCommand(placeable, () => SetFacing(facing));
        };

        public Action OnRotateCounterClockwise() => () =>
        {
            if (SelectedAreaObjectIndex < 0)
                return;

            var placeable = _objects[SelectedAreaObjectIndex];
            var facing = GetFacing(placeable) - 20f;

            while (facing > 360f)
                facing -= 360f;

            AssignCommand(placeable, () => SetFacing(facing));
        };

        private void AdjustFacing(float facing)
        {
            if (SelectedAreaObjectIndex < 0)
                return;

            var placeable = _objects[SelectedAreaObjectIndex];
            AssignCommand(placeable, () => SetFacing(facing));
        }

        public Action OnSetFacingNorth() => () =>
        {
            AdjustFacing(90f);
        };

        public Action OnSetFacingSouth() => () =>
        {
            AdjustFacing(270f);
        };

        public Action OnSetFacingEast() => () =>
        {
            AdjustFacing(0f);
        };

        public Action OnSetFacingWest() => () =>
        {
            AdjustFacing(180f);
        };

        private void AdjustPosition(float x, float y, float z)
        {
            if (SelectedAreaObjectIndex < 0)
                return;

            var placeable = _objects[SelectedAreaObjectIndex];
            var position = GetPosition(placeable);
            position.X += x;
            position.Y += y;
            position.Z += z;

            ObjectPlugin.SetPosition(placeable, position);
        }

        public Action OnYAxisUp() => () =>
        {
            AdjustPosition(0f, 0.1f, 0f);
        };

        public Action OnYAxisDown() => () =>
        {
            AdjustPosition(0f, -0.1f, 0f);
        };

        public Action OnXAxisUp() => () =>
        {
            AdjustPosition(0.1f, 0f, 0f);
        };

        public Action OnXAxisDown() => () =>
        {
            AdjustPosition(-0.1f, 0f, 0f);
        };

        public Action OnZAxisUp() => () =>
        {
            AdjustPosition(0f, 0f, 0.1f);
        };

        public Action OnZAxisDown() => () =>
        {
            AdjustPosition(0f, 0f, -0.1f);
        };

        public Action OnZAxisReset() => () =>
        {
            if (SelectedAreaObjectIndex < 0)
                return;

            var placeable = _objects[SelectedAreaObjectIndex];
            var location = GetLocation(placeable);
            var position = GetPosition(placeable);
            var z = GetGroundHeight(location);

            if (z == -6f)
                return;

            position.Z = z;
            ObjectPlugin.SetPosition(placeable, position);
        };

        public Action OnSaveObjectChanges() => () =>
        {
            if (SelectedAreaObjectIndex < 0)
                return;

            var areaObject = _objects[SelectedAreaObjectIndex];

            var areaObjectId = GetLocalString(areaObject, "DBID");
            var dbObject = DB.Get<AreaTemplateObject>(areaObjectId);

            SetName(areaObject, SelectedAreaObjectName);

            dbObject.ObjectName = GetName(areaObject);
            dbObject.ObjectData = ObjectPlugin.Serialize(areaObject);
            dbObject.LocationX = GetPosition(areaObject).X;
            dbObject.LocationY = GetPosition(areaObject).Y;
            dbObject.LocationZ = GetPosition(areaObject).Z;
            dbObject.LocationOrientation = GetFacing(areaObject);

            DB.Set<AreaTemplateObject>(dbObject);
        };

        public Action OnPreviousPageAppearance() => () =>
        {
            _skipPaginationSearch = true;
            var newPage = SelectedPageIndexAppearances - 1;
            if (newPage < 0)
                newPage = 0;

            SelectedPageIndexAppearances = newPage;

            SearchAppearances(GetObjectType(_objects[SelectedAreaObjectIndex]));

            _skipPaginationSearch = false;
        };

        public Action OnNextPageAppearance() => () =>
        {
            _skipPaginationSearch = true;
            var newPage = SelectedPageIndexAppearances + 1;
            if (newPage > PageNumbersAppearances.Count - 1)
                newPage = PageNumbersAppearances.Count - 1;

            SelectedPageIndexAppearances = newPage;

            SearchAppearances(GetObjectType(_objects[SelectedAreaObjectIndex]));

            _skipPaginationSearch = false;
        };

        public Action OnSelectObjectAppearance() => () =>
        {
            if (SelectedAppearanceIndex > -1)
                ObjectAppearanceToggled[SelectedAppearanceIndex] = false;

            var index = NuiGetEventArrayIndex();
            SelectedAppearanceIndex = index;

            if (index < 0)
                return;

            var areaObject = _objects[SelectedAreaObjectIndex];
            var areaObjectId = GetLocalString(areaObject, "DBID");
            var dbObject = DB.Get<AreaTemplateObject>(areaObjectId);

            switch (GetObjectType(areaObject))
            {
                case ObjectType.Creature:
                    {
                        SetCreatureAppearanceType(areaObject, (AppearanceType)_objectAppearanceIds[SelectedAppearanceIndex]);
                    }
                    break;
                case ObjectType.Placeable:
                    {
                        ObjectPlugin.SetAppearance(areaObject, _objectAppearanceIds[SelectedAppearanceIndex]);
                        SendMessageToPC(Player, "Placeable appearance change will only display after exiting and re-entering the area.");
                    }
                    break;
                default:
                    break;
            }

            dbObject.ObjectData = ObjectPlugin.Serialize(areaObject);

            DB.Set<AreaTemplateObject>(dbObject);

            ObjectAppearanceToggled[SelectedAppearanceIndex] = true;
        };
    }
}
