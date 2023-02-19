using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class PropertyItemStorageViewModel: GuiViewModelBase<PropertyItemStorageViewModel, GuiPayloadBase>
    {
        private const int MaxNumberOfCategories = 20;

        private readonly List<string> _categoryIds = new();

        public string Instructions
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiColor InstructionsColor
        {
            get => Get<GuiColor>();
            set => Set(value);
        }

        public string ItemCount
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiBindingList<string> CategoryNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> CategoryToggles
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public GuiBindingList<bool> CategoryEnables
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        private int SelectedCategoryIndex { get; set; }

        public string CategoryName
        {
            get => Get<string>();
            set => Set(value);
        }

        private readonly List<string> _itemIds = new();

        public GuiBindingList<string> ItemNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> ItemToggles
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public GuiBindingList<string> ItemResrefs
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        private int SelectedItemIndex { get; set; }

        public GuiBindingList<GuiComboEntry> PageNumbers
        {
            get => Get<GuiBindingList<GuiComboEntry>>();
            set => Set(value);
        }

        public bool IsCategorySelected
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool CanEditPermissions
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsItemSelected
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool CanAddCategory
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool CanEditCategory
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool CanDeleteCategory
        {
            get => Get<bool>();
            set => Set(value);
        }

        private void ClearInstructions()
        {
            Instructions = string.Empty;
            InstructionsColor = GuiColor.Green;
        }

        private int GetItemCount()
        {
            var area = GetArea(Player);
            var propertyId = Property.GetPropertyId(area);
            var query = new DBQuery<WorldPropertyCategory>()
                .AddFieldSearch(nameof(WorldPropertyCategory.ParentPropertyId), propertyId, false);
            var categories = DB.Search(query).ToList();
            var itemCount = categories.Sum(x => x.Items.Count);

            return itemCount;
        }

        private WorldPropertyPermission GetPropertyPermission(string playerId, string propertyId)
        {
            var property = DB.Get<WorldProperty>(propertyId);
            var propertyPermissionQuery = new DBQuery<WorldPropertyPermission>()
                .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), playerId, false)
                .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), propertyId, false);
            return DB.Search(propertyPermissionQuery).FirstOrDefault() ?? new WorldPropertyPermission
            {
                PropertyId = propertyId,
                PlayerId = playerId,
                Permissions = Property.GetPermissionsByPropertyType(property.PropertyType).ToDictionary(x => x, _ => false),
                GrantPermissions = Property.GetPermissionsByPropertyType(property.PropertyType).ToDictionary(x => x, _ => false)
            };
        }

        private WorldPropertyPermission GetCategoryPermission(string playerId, string categoryId)
        {
            var propertyPermissionQuery = new DBQuery<WorldPropertyPermission>()
                .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), playerId, false)
                .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), categoryId, false);
            return DB.Search(propertyPermissionQuery).FirstOrDefault() ?? new WorldPropertyPermission
            {
                PropertyId = categoryId,
                PlayerId = playerId,
                Permissions = Property.GetPermissionsByPropertyType(PropertyType.Category).ToDictionary(x => x, _ => false),
                GrantPermissions = Property.GetPermissionsByPropertyType(PropertyType.Category).ToDictionary(x => x, _ => false)
            };
        }

        private void RefreshItemCount(int current, int max)
        {
            ItemCount = $"Items: {current} / {max}";
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            Instructions = string.Empty;
            CategoryName = string.Empty;

            SelectedCategoryIndex = -1;
            SelectedItemIndex = -1;
            _categoryIds.Clear();

            var area = GetArea(Player);
            var playerId = GetObjectUUID(Player);
            var propertyId = Property.GetPropertyId(area);
            var property = DB.Get<WorldProperty>(propertyId);
            var propertyPermission = GetPropertyPermission(playerId, propertyId);

            var categoriesQuery = new DBQuery<WorldPropertyCategory>()
                .AddFieldSearch(nameof(WorldPropertyCategory.ParentPropertyId), propertyId, false);
            var categories = DB.Search(categoriesQuery).ToList();
            var categoryIds = categories.Select(s => s.Id).ToList();

            var permissionQuery = new DBQuery<WorldPropertyPermission>()
                .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), categoryIds)
                .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), playerId, false);
            var permissions = DB.Search(permissionQuery).ToList();

            var categoryNames = new GuiBindingList<string>();
            var categoryToggles = new GuiBindingList<bool>();
            var categoryEnables = new GuiBindingList<bool>();

            foreach (var category in categories)
            {
                var permission = permissions.SingleOrDefault(x => x.PropertyId == category.Id);
                
                _categoryIds.Add(category.Id);
                categoryNames.Add(category.Name);
                categoryToggles.Add(false);
                categoryEnables.Add(permission != null && permission.Permissions[PropertyPermissionType.AccessStorage]);
            }
            
            RefreshItemCount(
                categories.Sum(x => x.Items.Count), 
                property.ItemStorageCount);
            CategoryNames = categoryNames;
            CategoryToggles = categoryToggles;
            CategoryEnables = categoryEnables;
            CanAddCategory = propertyPermission.Permissions[PropertyPermissionType.EditCategories];

            LoadCategory();

            WatchOnClient(model => model.CategoryName);
        }

        private void LoadCategory()
        {
            var playerId = GetObjectUUID(Player);
            _itemIds.Clear();
            var itemNames = new GuiBindingList<string>();
            var itemToggles = new GuiBindingList<bool>();
            var itemResrefs = new GuiBindingList<string>();

            if (SelectedCategoryIndex > -1)
            {
                var area = GetArea(Player);
                var propertyId = Property.GetPropertyId(area);
                var propertyPermission = GetPropertyPermission(playerId, propertyId);
                var categoryId = _categoryIds[SelectedCategoryIndex];
                var category = DB.Get<WorldPropertyCategory>(categoryId);
                var categoryPermission = GetCategoryPermission(playerId, categoryId);

                foreach (var (itemId, item) in category.Items)
                {
                    _itemIds.Add(itemId);
                    itemNames.Add($"{item.Quantity}x {item.Name}");
                    itemToggles.Add(false);
                    itemResrefs.Add(item.IconResref);
                }

                IsCategorySelected = true;
                CategoryName = category.Name;
                CanEditCategory = propertyPermission.Permissions[PropertyPermissionType.EditCategories];
                CanDeleteCategory = propertyPermission.Permissions[PropertyPermissionType.EditCategories];
                CanEditPermissions = categoryPermission.GrantPermissions.Any(x => x.Value);
            }
            else
            {
                IsCategorySelected = false;
                CategoryName = string.Empty;
                CanEditCategory = false;
                CanDeleteCategory = false;
                CanEditPermissions = false;
            }

            ItemNames = itemNames;
            ItemToggles = itemToggles;
            ItemResrefs = itemResrefs;
        }

        public Action OnAddCategory() => () =>
        {
            ClearInstructions();

            var area = GetArea(Player);
            var propertyId = Property.GetPropertyId(area);
            var property = DB.Get<WorldProperty>(propertyId);
            var playerId = GetObjectUUID(Player);

            var propertyPermission = GetPropertyPermission(playerId, propertyId);
            if (!propertyPermission.Permissions[PropertyPermissionType.EditCategories])
                return;

            var query = new DBQuery<WorldPropertyCategory>()
                .AddFieldSearch(nameof(WorldPropertyCategory.ParentPropertyId), propertyId, false);
            var categoryCount = DB.SearchCount(query);

            if (categoryCount >= MaxNumberOfCategories)
            {
                Instructions = $"Maximum number of categories reached.";
                InstructionsColor = GuiColor.Red;
                return;
            }

            var category = new WorldPropertyCategory
            {
                ParentPropertyId = propertyId,
                Name = $"Category {CategoryNames.Count+1}"
            };

            var defaultPermissions = Property.GetPermissionsByPropertyType(PropertyType.Category);
            var ownerPermission = new WorldPropertyPermission
            {
                PropertyId = category.Id,
                PlayerId = property.OwnerPlayerId
            };
            var adderPermission = new WorldPropertyPermission
            {
                PropertyId = category.Id,
                PlayerId = playerId
            };

            foreach (var permission in defaultPermissions)
            {
                ownerPermission.Permissions[permission] = true;
                ownerPermission.GrantPermissions[permission] = true;

                adderPermission.Permissions[permission] = true;
                adderPermission.GrantPermissions[permission] = true;
            }

            DB.Set(category);
            DB.Set(ownerPermission);

            if(playerId != property.OwnerPlayerId)
                DB.Set(adderPermission);

            _categoryIds.Add(category.Id);
            CategoryNames.Add(category.Name);
            CategoryEnables.Add(true);
            CategoryToggles.Add(false);
        };

        public Action OnDeleteCategory() => () =>
        {
            ClearInstructions();

            ShowModal($"Are you sure you want to delete this category? NOTE: Only categories without items can be deleted.",
                () =>
                {
                    var area = GetArea(Player);
                    var propertyId = Property.GetPropertyId(area);
                    var playerId = GetObjectUUID(Player);
                    var categoryId = _categoryIds[SelectedCategoryIndex];
                    var category = DB.Get<WorldPropertyCategory>(categoryId);

                    var propertyPermission = GetPropertyPermission(playerId, propertyId);
                    if (!propertyPermission.Permissions[PropertyPermissionType.EditCategories])
                        return;

                    // Category no longer exists. May have been deleted by another player.
                    if (category == null)
                    {
                        Instructions = $"Category no longer exists.";
                        InstructionsColor = GuiColor.Red;
                        return;
                    }

                    if (category.Items.Count > 0)
                    {
                        Instructions = $"Remove all items from the category and try again.";
                        InstructionsColor = GuiColor.Red;
                        return;
                    }

                    var query = new DBQuery<WorldPropertyPermission>()
                        .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), categoryId, false);
                    
                    // Remove any permissions specific to this category.
                    var permissions = DB.Search(query);
                    foreach (var permission in permissions)
                    {
                        DB.Delete<WorldPropertyPermission>(permission.Id);
                    }

                    // Remove the category itself.
                    DB.Delete<WorldPropertyCategory>(categoryId);

                    // Remove from the UI.
                    CategoryNames.RemoveAt(SelectedCategoryIndex);
                    CategoryEnables.RemoveAt(SelectedCategoryIndex);
                    CategoryToggles.RemoveAt(SelectedCategoryIndex);
                    _categoryIds.RemoveAt(SelectedCategoryIndex);
                    SelectedCategoryIndex = -1;
                    
                    LoadCategory();

                    Instructions = $"Category deleted successfully.";
                    InstructionsColor = GuiColor.Green;
                });
        };

        public Action OnEditPermissions() => () =>
        {
            ClearInstructions();

            var playerId = GetObjectUUID(Player);
            var categoryId = _categoryIds[SelectedCategoryIndex];
            var categoryPermission = GetCategoryPermission(playerId, categoryId);

            if (!categoryPermission.GrantPermissions.Any(x => x.Value))
                return;

            var payload = new PropertyPermissionPayload(PropertyType.Category, categoryId, string.Empty, true);
            Gui.TogglePlayerWindow(Player, GuiWindowType.PermissionManagement, payload);
        };

        public Action OnSaveName() => () =>
        {
            ClearInstructions();

            if (CategoryName.Length <= 0)
            {
                Instructions = $"Categories must have a name.";
                InstructionsColor = GuiColor.Red;
                return;
            }

            var playerId = GetObjectUUID(Player);
            var area = GetArea(Player);
            var propertyId = Property.GetPropertyId(area);
            var categoryId = _categoryIds[SelectedCategoryIndex];
            var propertyPermission = GetPropertyPermission(playerId, propertyId);
            if (!propertyPermission.Permissions[PropertyPermissionType.EditCategories])
                return;

            var category = DB.Get<WorldPropertyCategory>(categoryId);

            category.Name = CategoryName;

            DB.Set(category);

            CategoryNames[SelectedCategoryIndex] = CategoryName;

            Instructions = $"Category renamed successfully.";
            InstructionsColor = GuiColor.Green;
        };

        public Action OnStoreItem() => () =>
        {
            ClearInstructions();

            Targeting.EnterTargetingMode(Player, ObjectType.Item, "Please click on an item within your inventory.", item =>
            {
                var canBeStored = Item.CanBePersistentlyStored(Player, item);
                if (!string.IsNullOrWhiteSpace(canBeStored))
                {
                    Instructions = canBeStored;
                    InstructionsColor = GuiColor.Red;
                    return;
                }

                var playerId = GetObjectUUID(Player);
                var area = GetArea(Player);
                var propertyId = Property.GetPropertyId(area);
                var categoryId = _categoryIds[SelectedCategoryIndex];
                var categoryPermission = GetCategoryPermission(playerId, categoryId);

                if (!categoryPermission.Permissions[PropertyPermissionType.AccessStorage])
                    return;

                var property = DB.Get<WorldProperty>(propertyId);
                var itemCount = GetItemCount();

                if (itemCount >= property.ItemStorageCount)
                {
                    Instructions = $"Property item storage limit reached!";
                    InstructionsColor = GuiColor.Red;
                    return;
                }

                var category = DB.Get<WorldPropertyCategory>(categoryId);
                var itemId = Guid.NewGuid().ToString();
                var dbItem = new WorldPropertyItem
                {
                    Name = GetName(item),
                    Tag = GetTag(item),
                    Resref = GetResRef(item),
                    IconResref = Item.GetIconResref(item),
                    Quantity = GetItemStackSize(item),

                    Data = ObjectPlugin.Serialize(item)
                };

                category.Items.Add(itemId, dbItem);

                DB.Set(category);

                ItemNames.Add($"{dbItem.Quantity}x {dbItem.Name}");
                ItemToggles.Add(false);
                ItemResrefs.Add(dbItem.IconResref);
                _itemIds.Add(itemId);

                RefreshItemCount(itemCount+1, property.ItemStorageCount);

                Instructions = $"Item stored successfully.";
                InstructionsColor = GuiColor.Green;

                DestroyObject(item);
            });
        };

        public Action OnRetrieveItem() => () =>
        {
            ClearInstructions();

            var area = GetArea(Player);
            var playerId = GetObjectUUID(Player);
            var propertyId = Property.GetPropertyId(area);
            var categoryId = _categoryIds[SelectedCategoryIndex];
            var categoryPermission = GetCategoryPermission(playerId, categoryId);

            if (!categoryPermission.Permissions[PropertyPermissionType.AccessStorage])
                return;

            var property = DB.Get<WorldProperty>(propertyId);
            var category = DB.Get<WorldPropertyCategory>(categoryId);
            var itemId = _itemIds[SelectedItemIndex];

            // Another player may have taken it out before this player.
            // Check and display an error if that's the case.
            if (!category.Items.ContainsKey(itemId))
            {
                Instructions = $"Item no longer exists.";
                InstructionsColor = GuiColor.Red;
                return;
            }

            var dbItem = category.Items[itemId];
            var item = ObjectPlugin.Deserialize(dbItem.Data);
            ObjectPlugin.AcquireItem(Player, item);

            category.Items.Remove(itemId);
            DB.Set(category);

            var itemCount = GetItemCount();
            RefreshItemCount(itemCount, property.ItemStorageCount);

            IsItemSelected = false;

            ItemNames.RemoveAt(SelectedItemIndex);
            ItemToggles.RemoveAt(SelectedItemIndex);
            ItemResrefs.RemoveAt(SelectedItemIndex);
            _itemIds.RemoveAt(SelectedItemIndex);
            SelectedItemIndex = -1;
        };

        public Action OnExamineItem() => () =>
        {
            ClearInstructions();

            var index = NuiGetEventArrayIndex();
            var categoryId = _categoryIds[SelectedCategoryIndex];
            var itemId = _itemIds[index];
            var category = DB.Get<WorldPropertyCategory>(categoryId);

            if (!category.Items.ContainsKey(itemId))
            {
                Instructions = "Item no longer in storage.";
                InstructionsColor = GuiColor.Red;
                return;
            }

            var dbItem = category.Items[itemId];
            var item = ObjectPlugin.Deserialize(dbItem.Data);
            var payload = new ExamineItemPayload(GetName(item), GetDescription(item), Item.BuildItemPropertyString(item));
            Gui.TogglePlayerWindow(Player, GuiWindowType.ExamineItem, payload);
            DestroyObject(item);
        };

        public Action OnSelectCategory() => () =>
        {
            ClearInstructions();

            if (SelectedCategoryIndex > -1)
                CategoryToggles[SelectedCategoryIndex] = false;

            SelectedCategoryIndex = NuiGetEventArrayIndex();
            CategoryToggles[SelectedCategoryIndex] = true;

            SelectedItemIndex = -1;
            LoadCategory();
        };

        public Action OnSelectItem() => () =>
        {
            ClearInstructions();

            if (SelectedItemIndex > -1)
                ItemToggles[SelectedItemIndex] = false;

            SelectedItemIndex = NuiGetEventArrayIndex();
            ItemToggles[SelectedItemIndex] = true;
            IsItemSelected = true;
        };
    }
}
