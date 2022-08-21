using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class AreaManagerViewModel: GuiViewModelBase<AreaManagerViewModel, GuiPayloadBase>
    {
        private readonly List<uint> _areas = new();

        private readonly List<uint> _objects = new();

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
        public bool IsDeleteObjectEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }
        public bool IsAreaResetEnabled
        {
            get => Get<bool>();
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
        public bool IsAreaSelected
        {
            get => Get<bool>();
            set => Set(value);
        }

        public int SelectedAreaIndex
        {
            get => Get<int>();
            set => Set(value);
        }
        public int SelectedAreaObjectIndex
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
            _objects.Clear();
            /*
            foreach (var area in AreaTemplateService.GetTemplateAreas())
            {
                if(AreaTemplateService.GetTemplateAreaCustomObjectsByArea(area.Value).Count() > 0)
                {
                    _areas.Add(area.Value);
                    areaResrefs.Add(area.Key);
                    areaNames.Add(GetName(area.Value));
                    areaToggled.Add(false);
                }
            }
            */

            SelectedAreaIndex = -1;
            AreaResrefs = areaResrefs;
            AreaNames = areaNames;
            AreaToggled = areaToggled;
            IsAreaSelected = false;
                        
            SearchText = string.Empty;
            Search();

            WatchOnClient(model => model.SearchText);
        }

        public Action OnClickTest() => () =>
        {
            Targeting.EnterTargetingMode(Player, ObjectType.Tile, "Please click on a location to create instanced area.",
            area =>
            {
                if (AreaTemplateService.GetIsTemplateArea(area))
                {
                    foreach (var x in AreaTemplateService.GetTemplateAreaCustomObjectsByArea(area))
                    {
                        Console.WriteLine(GetName(x));
                    }
                }
            });
        };

        private void LoadAreaObjectList()
        {
            if (SelectedAreaIndex <= -1)
                return;
            
            var areaObjectList = new GuiBindingList<string>();
            var areaObjectToggled = new GuiBindingList<bool>();

            _objects.Clear();
            foreach (var areaObject in AreaTemplateService.GetTemplateAreaCustomObjectsByArea(_areas[SelectedAreaIndex]))
            {
                _objects.Add(areaObject);
                areaObjectList.Add(GetName(areaObject));
                areaObjectToggled.Add(false);
            }

            if (areaObjectList.Count == 0) areaObjectList.Add("No Objects.");

            AreaObjectList = areaObjectList;
            AreaObjectToggled = areaObjectToggled;
        }

        public Action OnSelectArea() => () =>
        {
            if (SelectedAreaIndex > -1)
                AreaToggled[SelectedAreaIndex] = false;

            var index = NuiGetEventArrayIndex();
            SelectedAreaIndex = index;

            LoadAreaObjectList();

            IsDeleteObjectEnabled = true;
            IsAreaResetEnabled = true;

            AreaToggled[index] = true;
            IsAreaSelected = true;
        };
        public Action OnSelectAreaObject() => () =>
        {
            if (SelectedAreaIndex > -1)
                AreaToggled[SelectedAreaIndex] = false;

            var index = NuiGetEventArrayIndex();
            SelectedAreaObjectIndex = index;

            IsDeleteObjectEnabled = true;

            AreaObjectToggled[index] = true;
           // IsAreaObjectSelected = true;
        };

        public Action OnClickDeleteObject() => () =>
        {
            if (!(SelectedAreaObjectIndex > -1))
                return;

            var index = NuiGetEventArrayIndex();
            SelectedAreaObjectIndex = index;
        };

        public Action OnClickResetArea() => () =>
        {
            if (!(SelectedAreaIndex > -1))
                return;
            Console.WriteLine("SelectedAreaIndex = " + SelectedAreaIndex);

            ShowModal($"Are you sure you want to permanently remove all DM spawned objects from '{AreaNames[SelectedAreaIndex]}'", () =>
            {
                Console.WriteLine("2-SelectedAreaIndex = " + SelectedAreaIndex);

                foreach (var areaObject in AreaTemplateService.GetTemplateAreaCustomObjectsByArea(_areas[SelectedAreaIndex]))
                {
                    Console.WriteLine("Removing object: " + GetName(areaObject));

                    var query = new DBQuery<AreaTemplate>()
                        .AddFieldSearch(nameof(AreaTemplate.AreaResref), GetResRef(_areas[SelectedAreaIndex]), false)
                        .OrderBy(nameof(AreaTemplate.AreaResref));
                    var areaTemplates = DB.Search(query)
                        .ToList();

                    foreach(var dbRecord in areaTemplates)
                    {
                        Console.WriteLine("Removing db record for object: " + dbRecord.ObjectName);
                        DB.Delete<AreaTemplate>(dbRecord.Id);                       
                    }
                    AreaTemplateService.RemoveTemplateAreaCustomObjectByArea(_areas[SelectedAreaIndex], areaObject);
                    DestroyObject(areaObject);
                }
                Search();
            });
        };

        private void Search()
        {
            var areaResrefs = new GuiBindingList<string>();
            var areaNames = new GuiBindingList<string>();
            var areaToggled = new GuiBindingList<bool>();
            var areaObjectList = new GuiBindingList<string>();
            var areaObjectToggled = new GuiBindingList<bool>();

            _areas.Clear();
            _objects.Clear();

            AreaToggled.Clear();
            AreaNames.Clear();
            AreaResrefs.Clear();
            AreaObjectList.Clear();
            AreaObjectToggled.Clear();

            if (string.IsNullOrWhiteSpace(SearchText)) 
            {
                foreach (var area in AreaTemplateService.GetTemplateAreas())
                {
                    if (AreaTemplateService.GetTemplateAreaCustomObjectsByArea(area.Value).Count() > 0)
                    {
                        _areas.Add(area.Value);
                        areaResrefs.Add(area.Key);
                        areaNames.Add(GetName(area.Value));
                        areaToggled.Add(false);
                    }
                }
            }
            else
            {
                foreach (var area in AreaTemplateService.GetTemplateAreas())
                {
                    if (AreaTemplateService.GetTemplateAreaCustomObjectsByArea(area.Value).Count() > 0)
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
            }

            SelectedAreaIndex = -1;
            AreaResrefs = areaResrefs;
            AreaNames = areaNames;
            AreaToggled = areaToggled;
            AreaObjectList = areaObjectList;
            AreaObjectToggled = areaObjectToggled;
            IsAreaSelected = false;
            IsDeleteObjectEnabled = false;
            IsAreaResetEnabled = false;
        }

        public Action OnClickSearch() => Search;

        public Action OnClickClearSearch() => () =>
        {
            SearchText = string.Empty;
            Search();
        };
    }
}
