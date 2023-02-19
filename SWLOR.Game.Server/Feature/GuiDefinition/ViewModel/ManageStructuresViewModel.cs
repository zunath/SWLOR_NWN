using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class ManageStructuresViewModel: GuiViewModelBase<ManageStructuresViewModel, GuiPayloadBase>
    {
        private const int StructuresPerPage = 20;
        private int SelectedStructureIndex { get; set; }
        private bool _skipPaginationSearch;

        private readonly List<string> _structurePropertyIds = new();

        private Vector3 _currentPosition;
        private float _currentFacing;

        public string Instructions
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiColor InstructionColor
        {
            get => Get<GuiColor>();
            set => Set(value);
        }

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
            set
            {
                Set(value);

                if (!_skipPaginationSearch)
                {
                    DiscardChanges();
                    ClearStructureHighlight();
                    Search();
                }
            }
        }

        public string StructureName
        {
            get => Get<string>();
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

        public bool IsEditStructureEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string ManageButtonText
        {
            get => Get<string>();
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
            _skipPaginationSearch = true;
            var pageNumbers = new GuiBindingList<GuiComboEntry>();
            var pages = (int)(totalRecordCount / StructuresPerPage + (totalRecordCount % StructuresPerPage == 0 ? 0 : 1));

            // Always add page 1. In the event no structures are available,
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

            LoadStructure();
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

        private void LoadPropertyPermissions(WorldProperty property)
        {
            var permission = GetPermission();
            IsEditStructureEnabled = false;
            IsManagePropertyEnabled = false;
            IsRetrieveStructureEnabled = false;

            if (permission != null)
            {
                // Has one of the permissions found on the Manage Property window or
                // can grant other players permissions.
                IsManagePropertyEnabled = permission.Permissions.ContainsKey(PropertyPermissionType.CancelLease) && permission.Permissions[PropertyPermissionType.CancelLease] ||
                                          permission.Permissions.ContainsKey(PropertyPermissionType.ExtendLease) && permission.Permissions[PropertyPermissionType.ExtendLease] ||
                                          permission.Permissions[PropertyPermissionType.RenameProperty] ||
                                          permission.Permissions[PropertyPermissionType.ChangeDescription] ||
                                          permission.GrantPermissions.Any(x => x.Value);
            }

            // Item storage permissions
            var area = GetArea(Player);
            var propertyId = Property.GetPropertyId(area);
            var playerId = GetObjectUUID(Player);
            var permissions = Property.GetCategoryPermissions(playerId, propertyId);
            var propertyTypeDetail = Property.GetPropertyDetail(property.PropertyType);

            IsOpenStorageEnabled = permissions.Count > 0 && propertyTypeDetail.HasStorage;
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            _skipPaginationSearch = true;
            Instructions = string.Empty;
            InstructionColor = GuiColor.Green;

            var area = GetArea(Player);
            var propertyId = Property.GetPropertyId(area);

            // Safety check to ensure we've got a property Id.
            if (string.IsNullOrWhiteSpace(propertyId))
            {
                Gui.TogglePlayerWindow(Player, GuiWindowType.ManageStructures);
                return;
            }
            
            var property = DB.Get<WorldProperty>(propertyId);
            ManageButtonText = property.PropertyType == PropertyType.Apartment
                ? "Manage Property"
                : "Permissions";
            StructureName = string.Empty;
            SelectedPageIndex = 0;
            LoadPropertyPermissions(property);
            Search();

            WatchOnClient(model => model.SelectedPageIndex);
            WatchOnClient(model => model.StructureName);
            _skipPaginationSearch = false;
        }

        private void LoadStructure()
        {
            if (SelectedStructureIndex == -1)
            {
                StructureName = string.Empty;
                IsEditStructureEnabled = false;
                IsRetrieveStructureEnabled = false;
                _currentPosition = new Vector3();
                _currentFacing = 0f;
            }
            else
            {
                var propertyId = _structurePropertyIds[SelectedStructureIndex];
                var structure = GetStructure();
                var placeable = Property.GetPlaceableByPropertyId(propertyId);
                var permission = GetPermission();
                var structureDetail = Property.GetStructureByType(structure.StructureType);

                PlayerPlugin.ApplyLoopingVisualEffectToObject(Player, placeable, VisualEffect.Vfx_Dur_Aura_Green);
                _currentPosition = GetPosition(placeable);
                _currentFacing = GetFacing(placeable);

                StructureName = structure.CustomName;
                IsEditStructureEnabled = permission.Permissions[PropertyPermissionType.EditStructures];
                IsRetrieveStructureEnabled = permission.Permissions[PropertyPermissionType.RetrieveStructures] && structureDetail.CanBeRetrieved;
            }
        }

        private void ClearStructureHighlight()
        {
            if (SelectedStructureIndex <= -1)
                return;

            var propertyId = _structurePropertyIds[SelectedStructureIndex];
            var placeable = Property.GetPlaceableByPropertyId(propertyId);
            PlayerPlugin.ApplyLoopingVisualEffectToObject(Player, placeable, VisualEffect.None);
        }

        public Action OnCloseWindow() => () =>
        {
            ClearStructureHighlight();
            DiscardChanges();
        };

        public Action OnSelectStructure() => () =>
        {
            if (SelectedStructureIndex > -1)
            {
                StructureToggles[SelectedStructureIndex] = false;
                ClearStructureHighlight();
                DiscardChanges();
            }

            SelectedStructureIndex = NuiGetEventArrayIndex();
            StructureToggles[SelectedStructureIndex] = true;

            LoadStructure();
        };

        public Action OnPreviousPage() => () =>
        {
            _skipPaginationSearch = true;
            var newPage = SelectedPageIndex - 1;
            if (newPage < 0)
                newPage = 0;

            SelectedPageIndex = newPage;

            DiscardChanges();
            ClearStructureHighlight();
            Search();
            _skipPaginationSearch = false;
        };

        public Action OnNextPage() => () =>
        {
            _skipPaginationSearch = true;
            var newPage = SelectedPageIndex + 1;
            if (newPage > PageNumbers.Count - 1)
                newPage = PageNumbers.Count - 1;

            SelectedPageIndex = newPage;

            DiscardChanges();
            ClearStructureHighlight();
            Search();
            _skipPaginationSearch = false;
        };

        public Action OnManageProperty() => () =>
        {
            var area = GetArea(Player);
            var propertyId = Property.GetPropertyId(area);
            var property = DB.Get<WorldProperty>(propertyId);

            // Apartments have their own management menu.
            if (property.PropertyType == PropertyType.Apartment)
            {
                var payload = new ManageApartmentPayload(propertyId);
                Gui.TogglePlayerWindow(Player, GuiWindowType.ManageApartment, payload);
            }
            // Cities use the same permissions menu as all other buildings,
            // but the city Id is located on themselves instead of the parent building's parent.
            else if (property.PropertyType == PropertyType.City)
            {
                var payload = new PropertyPermissionPayload(property.PropertyType, propertyId, propertyId, false);
                Gui.TogglePlayerWindow(Player, GuiWindowType.PermissionManagement, payload);
            }
            else if (property.PropertyType == PropertyType.Starship)
            {
                var payload = new PropertyPermissionPayload(property.PropertyType, propertyId, string.Empty, false);
                Gui.TogglePlayerWindow(Player, GuiWindowType.PermissionManagement, payload);
            }
            // Buildings look at their parent's parent to determine the city Id.
            else
            {
                var parentBuilding = DB.Get<WorldProperty>(property.ParentPropertyId);
                var cityId = parentBuilding.ParentPropertyId;
                var payload = new PropertyPermissionPayload(property.PropertyType, propertyId, cityId, false);
                Gui.TogglePlayerWindow(Player, GuiWindowType.PermissionManagement, payload);
            }
        };

        public Action OnOpenStorage() => () =>
        {
            Gui.TogglePlayerWindow(Player, GuiWindowType.PropertyItemStorage);
        };

        public Action OnRetrieveStructure() => () =>
        {
            ShowModal($"Are you sure you want to retrieve this structure?", () =>
            {
                var structure = GetStructure();
                var parentProperty = DB.Get<WorldProperty>(structure.ParentPropertyId);
                var placeable = Property.GetPlaceableByPropertyId(structure.Id);
                var permission = GetPermission();

                // Permissions are missing now.
                if (permission == null || !permission.Permissions[PropertyPermissionType.RetrieveStructures])
                {
                    Instructions = $"No permission!";
                    InstructionColor = GuiColor.Red;
                    return;
                }

                var structureDetail = Property.GetStructureByType(structure.StructureType);

                if (!structureDetail.CanBeRetrieved)
                {
                    Instructions = "This structure cannot be retrieved.";
                    InstructionColor = GuiColor.Red;
                    return;
                }

                // Removing this structure would reduce the item storage cap below the number of items actually
                // in storage.
                var query = new DBQuery<WorldPropertyCategory>()
                    .AddFieldSearch(nameof(WorldPropertyCategory.ParentPropertyId), structure.ParentPropertyId, false);
                var categories = DB.Search(query).ToList();
                var itemCount = categories.Sum(x => x.Items.Count);

                if (itemCount > parentProperty.ItemStorageCount - structure.ItemStorageCount)
                {
                    Instructions = $"Remove items from storage first.";
                    InstructionColor = GuiColor.Red;
                    return;
                }
                
                var item = ObjectPlugin.Deserialize(structure.SerializedItem);
                ObjectPlugin.AcquireItem(Player, item);
                
                // Remove the structure from the parent's child list.
                parentProperty.ChildPropertyIds[PropertyChildType.Structure].Remove(structure.Id);
                parentProperty.ItemStorageCount -= structure.ItemStorageCount;

                DB.Set(parentProperty);

                // Some structures have specific logic which must be run when they're picked up. Do that now.
                Property.RunStructureChangedEvent(structure.StructureType, StructureChangeType.Retrieved, structure, placeable);

                DB.Delete<WorldProperty>(structure.Id);

                // Remove any child instances this structure contains.
                if (structure.ChildPropertyIds.ContainsKey(PropertyChildType.Interior))
                {
                    foreach (var childId in structure.ChildPropertyIds[PropertyChildType.Interior])
                    {
                        DB.Delete<WorldProperty>(childId);
                    }
                }

                StructureNames.RemoveAt(SelectedStructureIndex);
                StructureToggles.RemoveAt(SelectedStructureIndex);
                _structurePropertyIds.RemoveAt(SelectedStructureIndex);
                SelectedStructureIndex = -1;

                DestroyObject(placeable);
                LoadStructure();
            });
        };

        public Action OnSaveChanges() => () =>
        {
            if (StructureName.Length <= 0)
            {
                Instructions = $"Name is required.";
                InstructionColor = GuiColor.Red;
                return;
            }

            var structure = GetStructure();
            var placeable = Property.GetPlaceableByPropertyId(structure.Id);
            _currentFacing = GetFacing(placeable);
            _currentPosition = GetPosition(placeable);

            structure.Positions[PropertyLocationType.StaticPosition] = new PropertyLocation
            {
                X = _currentPosition.X,
                Y = _currentPosition.Y,
                Z = _currentPosition.Z,
                Orientation = _currentFacing,
                AreaResref = string.Empty
            };

            structure.CustomName = StructureName;

            DB.Set(structure);

            StructureNames[SelectedStructureIndex] = StructureName;

            Instructions = $"Structure saved!";
            InstructionColor = GuiColor.Green;

            Property.RunStructureChangedEvent(structure.StructureType, StructureChangeType.PositionChanged, structure, placeable);
        };

        private void DiscardChanges()
        {
            if (SelectedStructureIndex <= -1)
                return;

            var structure = GetStructure();
            var placeable = Property.GetPlaceableByPropertyId(structure.Id);
            StructureName = structure.CustomName;

            // Copy the values since there is a delay before the action is fired and another
            // structure may have been selected by the user by that point.
            var facing = _currentFacing;
            var position = new Vector3(_currentPosition.X, _currentPosition.Y, _currentPosition.Z);
            AssignCommand(placeable, () =>
            {
                SetFacing(facing);
                ObjectPlugin.SetPosition(placeable, position);
            });
        }

        public Action OnDiscardChanges() => () =>
        {
            DiscardChanges();
        };

        public Action OnRotateClockwise() => () =>
        {
            var structure = GetStructure();
            var placeable = Property.GetPlaceableByPropertyId(structure.Id);
            var facing = GetFacing(placeable) + 20f;

            while (facing > 360f)
                facing -= 360f;

            AssignCommand(placeable, () => SetFacing(facing));
        };

        public Action OnRotateCounterClockwise() => () =>
        {
            var structure = GetStructure();
            var placeable = Property.GetPlaceableByPropertyId(structure.Id);
            var facing = GetFacing(placeable) - 20f;

            while (facing > 360f)
                facing -= 360f;

            AssignCommand(placeable, () => SetFacing(facing));
        };

        private void AdjustFacing(float facing)
        {
            var structure = GetStructure();
            var placeable = Property.GetPlaceableByPropertyId(structure.Id);
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
            var structure = GetStructure();
            var placeable = Property.GetPlaceableByPropertyId(structure.Id);
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
            var structure = GetStructure();
            var placeable = Property.GetPlaceableByPropertyId(structure.Id);
            var location = GetLocation(placeable);
            var position = GetPosition(placeable);
            var z = GetGroundHeight(location);

            if (z == -6f)
                return;

            position.Z = z;
            ObjectPlugin.SetPosition(placeable, position);
        };
    }
}
