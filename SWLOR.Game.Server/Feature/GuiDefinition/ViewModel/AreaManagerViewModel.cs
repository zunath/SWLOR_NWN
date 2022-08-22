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
            var areaObjectList = new GuiBindingList<string>();
            var areaObjectToggled = new GuiBindingList<bool>();

            _areas.Clear();
            _objects.Clear();

            SelectedAreaIndex = -1;
            AreaResrefs = areaResrefs;
            AreaNames = areaNames;
            AreaToggled = areaToggled;
            AreaObjectList = areaObjectList;
            AreaObjectToggled = areaObjectToggled;
            IsAreaSelected = false;
                        
            SearchText = string.Empty;
            Search();

            WatchOnClient(model => model.SearchText);
        }

        private void LoadAreaObjectList()
        {
            if (SelectedAreaIndex <= -1)
                return;
            
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
            if (SelectedAreaObjectIndex > -1)
                AreaObjectToggled[SelectedAreaObjectIndex] = false;

            var index = NuiGetEventArrayIndex();
            SelectedAreaObjectIndex = index;

            IsDeleteObjectEnabled = true;
            AreaObjectToggled[index] = true;
        };

        public Action OnClickDeleteObject() => () =>
        {
            if (SelectedAreaIndex < 0)
                return;

            var areaObject = _objects[SelectedAreaObjectIndex];
            var areaObjectId = GetLocalString(areaObject, "DBID");

            // Can enable modal for this but it seems like it would be a hassle to have for every single object in practice.
            //ShowModal($"Are you sure you want to permanently remove this DM spawned object? '{AreaObjectList[SelectedAreaObjectIndex]}'", () =>
            //{
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
            //});
        };

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
                foreach (var area in AreaTemplate.GetTemplateAreas())
                {
                    if (AreaTemplate.GetTemplateAreaCustomObjectsByArea(area.Value).Count() > 0)
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
                foreach (var area in AreaTemplate.GetTemplateAreas())
                {
                    if (AreaTemplate.GetTemplateAreaCustomObjectsByArea(area.Value).Count() > 0)
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
