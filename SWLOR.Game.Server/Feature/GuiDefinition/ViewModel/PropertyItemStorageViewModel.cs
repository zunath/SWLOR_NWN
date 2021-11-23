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
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class PropertyItemStorageViewModel: GuiViewModelBase<PropertyItemStorageViewModel, GuiPayloadBase>
    {
        private static readonly GuiColor _green = new(0, 255, 0);
        private static readonly GuiColor _red = new(255, 0, 0);

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

        public bool IsCategorySelected
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsItemSelected
        {
            get => Get<bool>();
            set => Set(value);
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
                var permission = permissions.Single(x => x.PropertyId == category.Id);
                
                _categoryIds.Add(category.Id);
                categoryNames.Add(category.Name);
                categoryToggles.Add(false);
                categoryEnables.Add(permission.Permissions[PropertyPermissionType.AccessStorage]);
            }
            
            RefreshItemCount(
                categories.Sum(x => x.Items.Count), 
                property.ItemStorageCount);
            CategoryNames = categoryNames;
            CategoryToggles = categoryToggles;
            CategoryEnables = categoryEnables;

            LoadCategory();

            WatchOnClient(model => model.CategoryName);
        }

        private void LoadCategory()
        {
            _itemIds.Clear();
            var itemNames = new GuiBindingList<string>();
            var itemToggles = new GuiBindingList<bool>();
            var itemResrefs = new GuiBindingList<string>();

            if (SelectedCategoryIndex > -1)
            {
                var categoryId = _categoryIds[SelectedCategoryIndex];
                var category = DB.Get<WorldPropertyCategory>(categoryId);

                foreach (var (itemId, item) in category.Items)
                {
                    _itemIds.Add(itemId);
                    itemNames.Add($"{item.Quantity}x {item.Name}");
                    itemToggles.Add(false);
                    itemResrefs.Add(item.IconResref);
                }

                IsCategorySelected = true;
                CategoryName = category.Name;
            }
            else
            {
                IsCategorySelected = false;
                CategoryName = string.Empty;
            }

            ItemNames = itemNames;
            ItemToggles = itemToggles;
            ItemResrefs = itemResrefs;
        }

        public Action OnAddCategory() => () =>
        {
            var area = GetArea(Player);
            var propertyId = Property.GetPropertyId(area);
            var property = DB.Get<WorldProperty>(propertyId);
            var playerId = GetObjectUUID(Player);

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
            ShowModal($"Are you sure you want to delete this category? NOTE: Only categories without items can be deleted.",
                () =>
                {
                    var categoryId = _categoryIds[SelectedCategoryIndex];
                    var category = DB.Get<WorldPropertyCategory>(categoryId);

                    // Category no longer exists. May have been deleted by another player.
                    if (category == null)
                    {
                        Instructions = $"Category no longer exists.";
                        InstructionsColor = _red;
                        return;
                    }

                    if (category.Items.Count > 0)
                    {
                        Instructions = $"Remove all items from the category and try again.";
                        InstructionsColor = _red;
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
                });
        };

        public Action OnEditPermissions() => () =>
        {
            var categoryId = _categoryIds[SelectedCategoryIndex];
            var availablePermissions = Property.GetPermissionsByPropertyType(PropertyType.Category);

            var payload = new PropertyPermissionPayload(PropertyType.Category, categoryId, true, availablePermissions);
            Gui.TogglePlayerWindow(Player, GuiWindowType.PermissionManagement, payload);
        };

        public Action OnSaveName() => () =>
        {
            if (CategoryName.Length <= 0)
            {
                Instructions = $"Categories must have a name.";
                InstructionsColor = _red;
                return;
            }

            var categoryId = _categoryIds[SelectedCategoryIndex];
            var category = DB.Get<WorldPropertyCategory>(categoryId);

            category.Name = CategoryName;

            DB.Set(category);
        };

        public Action OnStoreItem() => () =>
        {
            Targeting.EnterTargetingMode(Player, ObjectType.Item, item =>
            {
                var area = GetArea(Player);
                var propertyId = Property.GetPropertyId(area);
                var property = DB.Get<WorldProperty>(propertyId);
                var itemCount = GetItemCount();

                if (itemCount >= property.ItemStorageCount)
                {
                    Instructions = $"Property item storage limit reached!";
                    InstructionsColor = _red;
                    return;
                }

                var categoryId = _categoryIds[SelectedCategoryIndex];
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
                InstructionsColor = _green;

                DestroyObject(item);
            });

            
        };

        public Action OnRetrieveItem() => () =>
        {
            var area = GetArea(Player);
            var propertyId = Property.GetPropertyId(area);
            var property = DB.Get<WorldProperty>(propertyId);
            var categoryId = _categoryIds[SelectedCategoryIndex];
            var category = DB.Get<WorldPropertyCategory>(categoryId);
            var itemId = _itemIds[SelectedItemIndex];

            // Another player may have taken it out before this player.
            // Check and display an error if that's the case.
            if (!category.Items.ContainsKey(itemId))
            {
                Instructions = $"Item no longer exists.";
                InstructionsColor = _red;
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
            var index = NuiGetEventArrayIndex();
            var categoryId = _categoryIds[SelectedCategoryIndex];
            var itemId = _itemIds[index];
            var category = DB.Get<WorldPropertyCategory>(categoryId);

            if (!category.Items.ContainsKey(itemId))
            {
                Instructions = "Item no longer in storage.";
                InstructionsColor = _red;
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
            if (SelectedCategoryIndex > -1)
                CategoryToggles[SelectedCategoryIndex] = false;

            SelectedCategoryIndex = NuiGetEventArrayIndex();
            CategoryToggles[SelectedCategoryIndex] = true;

            SelectedItemIndex = -1;
            LoadCategory();
        };

        public Action OnSelectItem() => () =>
        {
            if (SelectedItemIndex > -1)
                ItemToggles[SelectedItemIndex] = false;

            SelectedItemIndex = NuiGetEventArrayIndex();
            ItemToggles[SelectedItemIndex] = true;
            IsItemSelected = true;
        };
    }
}
