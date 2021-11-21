using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.PropertyService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class ManageStructuresViewModel: GuiViewModelBase<ManageStructuresViewModel, GuiPayloadBase>
    {
        private const int StructuresPerPage = 25;
        private int SelectedStructureIndex { get; set; }

        private readonly List<string> _structurePropertyIds = new();

        public GuiBindingList<string> StructureNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> StructureToggles
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public GuiBindingList<GuiComboEntry> PageNumbers
        {
            get => Get<GuiBindingList<GuiComboEntry>>();
            set => Set(value);
        }

        public int SelectedPageIndex
        {
            get => Get<int>();
            set => Set(value);
        }

        public string StructureName
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsStructureNameEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string FurnitureCount
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsManagePropertyEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsOpenStorageEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsRetrieveStructureEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        private WorldProperty GetStructure()
        {
            var propertyId = _structurePropertyIds[SelectedStructureIndex];
            var structure = DB.Get<WorldProperty>(propertyId);

            return structure;
        }

        private void UpdatePagination(long totalRecordCount)
        {
            var pageNumbers = new GuiBindingList<GuiComboEntry>();
            var pages = (int)(totalRecordCount / StructuresPerPage + (totalRecordCount % StructuresPerPage == 0 ? 0 : 1));

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
        }

        private void Search()
        {
            var area = GetArea(Player);
            var propertyId = Property.GetPropertyId(area);

            SelectedStructureIndex = -1;

            var structureNames = new GuiBindingList<string>();
            var structureToggles = new GuiBindingList<bool>();
            var query = new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.ParentPropertyId), propertyId, false)
                .AddFieldSearch(nameof(WorldProperty.PropertyType), (int)PropertyType.Structure);
            var structureCount = DB.SearchCount(query);
            UpdatePagination(structureCount);

            query.AddPaging(StructuresPerPage, SelectedPageIndex * StructuresPerPage);
            var structures = DB.Search(query);
            _structurePropertyIds.Clear();

            foreach (var structure in structures)
            {
                _structurePropertyIds.Add(structure.Id);
                structureNames.Add(structure.CustomName);
                structureToggles.Add(false);
            }


            StructureNames = structureNames;
            StructureToggles = structureToggles;

        }

        private WorldPropertyPermission GetPermission()
        {
            var playerId = GetObjectUUID(Player);
            var area = GetArea(Player);
            var propertyId = Property.GetPropertyId(area);
            var query = new DBQuery<WorldPropertyPermission>()
                .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), propertyId, false)
                .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), playerId, false);
            return DB.Search(query).FirstOrDefault();
        }

        private void LoadPermissions()
        {
            var permission = GetPermission();

            if (permission == null)
            {
                IsStructureNameEnabled = false;
                IsManagePropertyEnabled = false;
                IsRetrieveStructureEnabled = false;
                IsOpenStorageEnabled = false;
            }
            else
            {
                IsManagePropertyEnabled = false; // todo: what determines this?
                IsRetrieveStructureEnabled = permission.Permissions[PropertyPermissionType.RetrieveStructures];
                IsOpenStorageEnabled = permission.Permissions[PropertyPermissionType.AccessStorage];
            }
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            var area = GetArea(Player);
            var propertyId = Property.GetPropertyId(area);

            // Safety check to ensure we've got a property Id.
            if (string.IsNullOrWhiteSpace(propertyId))
            {
                Gui.TogglePlayerWindow(Player, GuiWindowType.ManageStructures);
                return;
            }

            StructureName = string.Empty;
            SelectedPageIndex = 0;
            LoadPermissions();
            Search();

            WatchOnClient(model => model.SelectedPageIndex);
            WatchOnClient(model => model.StructureName);
        }

        private void LoadStructure()
        {
            if (SelectedStructureIndex == -1)
            {
                StructureName = string.Empty;
                IsStructureNameEnabled = false;

            }
            else
            {
                var propertyId = _structurePropertyIds[SelectedStructureIndex];
                var structure = GetStructure();
                var placeable = Property.GetPlaceableByPropertyId(propertyId);
                var permission = GetPermission();

                PlayerPlugin.ApplyLoopingVisualEffectToObject(Player, placeable, VisualEffect.Vfx_Dur_Aura_Green);

                StructureName = structure.CustomName;
                IsStructureNameEnabled = permission.Permissions[PropertyPermissionType.EditStructures];
            }
        }

        private void ClearStructureHighlight()
        {
            var propertyId = _structurePropertyIds[SelectedStructureIndex];
            var placeable = Property.GetPlaceableByPropertyId(propertyId);
            PlayerPlugin.ApplyLoopingVisualEffectToObject(Player, placeable, VisualEffect.None);
        }

        public Action OnCloseWindow() => () =>
        {
            ClearStructureHighlight();
        };

        public Action OnSelectStructure() => () =>
        {
            if (SelectedStructureIndex > -1)
            {
                StructureToggles[SelectedStructureIndex] = false;
                ClearStructureHighlight();
            }

            SelectedStructureIndex = NuiGetEventArrayIndex();
            StructureToggles[SelectedStructureIndex] = true;

            LoadStructure();
        };

        public Action OnPreviousPage() => () =>
        {
            var newPage = SelectedPageIndex - 1;
            if (newPage < 0)
                newPage = 0;

            SelectedPageIndex = newPage;
        };

        public Action OnNextPage() => () =>
        {
            var newPage = SelectedPageIndex + 1;
            if (newPage > PageNumbers.Count - 1)
                newPage = PageNumbers.Count - 1;

            SelectedPageIndex = newPage;
        };

        public Action OnManageProperty() => () =>
        {

        };

        public Action OnOpenStorage() => () =>
        {

        };

        public Action OnRetrieveStructure() => () =>
        {

        };

        public Action OnSaveChanges() => () =>
        {

        };

        public Action OnDiscardChanges() => () =>
        {

        };

        public Action OnRotateClockwise() => () =>
        {
            var structure = GetStructure();
            var placeable = Property.GetPlaceableByPropertyId(structure.Id);
            var facing = GetFacing(placeable) + 20f;

            while (facing > 360f)
                facing -= 360f;

            AssignCommand(placeable, () => SetFacing(facing));

            structure.Orientation = facing;
            DB.Set(structure);
        };

        public Action OnRotateCounterClockwise() => () =>
        {
            var structure = GetStructure();
            var placeable = Property.GetPlaceableByPropertyId(structure.Id);
            var facing = GetFacing(placeable) - 20f;

            while (facing > 360f)
                facing -= 360f;

            AssignCommand(placeable, () => SetFacing(facing));

            structure.Orientation = facing;
            DB.Set(structure);
        };

        private void AdjustFacing(float facing)
        {
            var structure = GetStructure();
            var placeable = Property.GetPlaceableByPropertyId(structure.Id);
            AssignCommand(placeable, () => SetFacing(facing));

            structure.Orientation = facing;
            DB.Set(structure);
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
            var structure = GetStructure();
            var placeable = Property.GetPlaceableByPropertyId(structure.Id);
            var position = GetPosition(placeable);
            position.X += x;
            position.Y += y;
            position.Z += z;

            ObjectPlugin.SetPosition(placeable, position);

            structure.Position = position;
            DB.Set(structure);
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
            var structure = GetStructure();
            var placeable = Property.GetPlaceableByPropertyId(structure.Id);
            var location = GetLocation(placeable);
            var position = GetPosition(placeable);
            var z = GetGroundHeight(location);

            if (z == -6f)
                return;

            position.Z = z;
            ObjectPlugin.SetPosition(placeable, position);

            structure.Position = position;
            DB.Set(structure);
        };
    }
}
