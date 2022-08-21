using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class AreaManagerViewModel: GuiViewModelBase<AreaManagerViewModel, GuiPayloadBase>
    {
        private readonly List<uint> _areas = new();

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

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            var areaResrefs = new GuiBindingList<string>();
            var areaNames = new GuiBindingList<string>();
            var areaToggled = new GuiBindingList<bool>();

            _areas.Clear();

            foreach (var area in Area.GetTemplateAreas())
            {
                if(Area.GetTemplateAreaCustomObjectsByArea(area.Value).Count() > 0)
                {
                    _areas.Add(area.Value);
                    areaResrefs.Add(area.Key);
                    areaNames.Add(GetName(area.Value));
                    areaToggled.Add(false);
                }
            }

            SelectedAreaIndex = -1;
            AreaResrefs = areaResrefs;
            AreaNames = areaNames;
            AreaToggled = areaToggled;
            IsAreaSelected = false;
                        
            SearchText = string.Empty;
            Search();

            WatchOnClient(model => model.SearchText);
            //WatchOnClient(model => model.AreaObjectList);
        }

        public Action OnClickTest() => () =>
        {
            Targeting.EnterTargetingMode(Player, ObjectType.Tile, "Please click on a location to create instanced area.",
            area =>
            {
                if (Area.GetIsTemplateArea(area))
                {
                    foreach (var x in Area.GetTemplateAreaCustomObjectsByArea(area))
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

            foreach (var areaObject in Area.GetTemplateAreaCustomObjectsByArea(_areas[SelectedAreaIndex]))
            {
                areaObjectList.Add(GetName(areaObject));
            }

            if (areaObjectList.Count == 0) areaObjectList.Add("No Objects.");

            AreaObjectList = areaObjectList;
        }

        public Action OnSelectArea() => () =>
        {
            if (SelectedAreaIndex > -1)
                AreaToggled[SelectedAreaIndex] = false;

            var index = NuiGetEventArrayIndex();
            SelectedAreaIndex = index;

            LoadAreaObjectList();

            IsDeleteEnabled = true;
            AreaToggled[index] = true;
            IsAreaSelected = true;

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
                foreach (var area in Area.GetTemplateAreas())
                {
                    if (Area.GetTemplateAreaCustomObjectsByArea(area.Value).Count() > 0)
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
                foreach (var area in Area.GetTemplateAreas())
                {
                    if (Area.GetTemplateAreaCustomObjectsByArea(area.Value).Count() > 0)
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
            AreaObjectList.Clear();
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
