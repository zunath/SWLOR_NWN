using Microsoft.Extensions.DependencyInjection;
using SWLOR.NWN.API.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Abstractions.Enums;
using SWLOR.Shared.Abstractions.Models;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Properties.Contracts;
using SWLOR.Shared.Domain.Properties.Entities;
using SWLOR.Shared.Domain.Properties.Enums;
using SWLOR.Shared.Domain.Repositories;
using SWLOR.Shared.Domain.UI.Payloads;
using SWLOR.Shared.UI.Component;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Model;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.Properties.UI.ViewModel
{
    public class PropertyItemStorageViewModel: GuiViewModelBase<PropertyItemStorageViewModel, IGuiPayload>
    {
        private readonly IWorldPropertyCategoryRepository _worldPropertyCategoryRepository;
        private readonly IWorldPropertyPermissionRepository _worldPropertyPermissionRepository;
        private readonly IWorldPropertyRepository _worldPropertyRepository;
        private readonly IServiceProvider _serviceProvider;
        
        // Lazy-loaded services to break circular dependencies
        private IItemService ItemService => _serviceProvider.GetRequiredService<IItemService>();
        private IPropertyService PropertyService => _serviceProvider.GetRequiredService<IPropertyService>();
        private ITargetingService TargetingService => _serviceProvider.GetRequiredService<ITargetingService>();
        private IObjectPluginService ObjectPlugin => _serviceProvider.GetRequiredService<IObjectPluginService>();

        public PropertyItemStorageViewModel(IGuiService guiService, IWorldPropertyCategoryRepository worldPropertyCategoryRepository, IWorldPropertyPermissionRepository worldPropertyPermissionRepository, IWorldPropertyRepository worldPropertyRepository, IServiceProvider serviceProvider) : base(guiService)
        {
            _worldPropertyCategoryRepository = worldPropertyCategoryRepository;
            _worldPropertyPermissionRepository = worldPropertyPermissionRepository;
            _worldPropertyRepository = worldPropertyRepository;
            _serviceProvider = serviceProvider;
        }
        
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
            var propertyId = PropertyService.GetPropertyId(area);
            var categories = _worldPropertyCategoryRepository.GetByPropertyId(propertyId).ToList();
            var itemCount = categories.Sum(x => x.Items.Count);

            return itemCount;
        }

        private WorldPropertyPermission GetPropertyPermission(string playerId, string propertyId)
        {
            var property = _worldPropertyRepository.GetById(propertyId);
            var permissions = _worldPropertyPermissionRepository.GetByPropertyIdAndPlayerId(propertyId, playerId);
            return permissions.FirstOrDefault() ?? new WorldPropertyPermission
            {
                PropertyId = propertyId,
                PlayerId = playerId,
                Permissions = PropertyService.GetPermissionsByPropertyType(property.PropertyType).ToDictionary(x => x, _ => false),
                GrantPermissions = PropertyService.GetPermissionsByPropertyType(property.PropertyType).ToDictionary(x => x, _ => false)
            };
        }

        private WorldPropertyPermission GetCategoryPermission(string playerId, string categoryId)
        {
            var permissions = _worldPropertyPermissionRepository.GetByPropertyIdAndPlayerId(categoryId, playerId);
            return permissions.FirstOrDefault() ?? new WorldPropertyPermission
            {
                PropertyId = categoryId,
                PlayerId = playerId,
                Permissions = PropertyService.GetPermissionsByPropertyType(PropertyType.Category).ToDictionary(x => x, _ => false),
                GrantPermissions = PropertyService.GetPermissionsByPropertyType(PropertyType.Category).ToDictionary(x => x, _ => false)
            };
        }

        private void RefreshItemCount(int current, int max)
        {
            ItemCount = $"Items: {current} / {max}";
        }

        protected override void Initialize(IGuiPayload initialPayload)
        {
            Instructions = string.Empty;
            CategoryName = string.Empty;

            SelectedCategoryIndex = -1;
            SelectedItemIndex = -1;
            _categoryIds.Clear();

            var area = GetArea(Player);
            var playerId = GetObjectUUID(Player);
            var propertyId = PropertyService.GetPropertyId(area);
            var property = _worldPropertyRepository.GetById(propertyId);
            var propertyPermission = GetPropertyPermission(playerId, propertyId);

            var categories = _worldPropertyCategoryRepository.GetByPropertyId(propertyId).ToList();
            var categoryIds = categories.Select(s => s.Id).ToList();

            var permissions = _worldPropertyPermissionRepository.GetByPlayerId(playerId)
                .Where(p => categoryIds.Contains(p.PropertyId))
                .ToList();

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
                var propertyId = PropertyService.GetPropertyId(area);
                var propertyPermission = GetPropertyPermission(playerId, propertyId);
                var categoryId = _categoryIds[SelectedCategoryIndex];
                var category = _worldPropertyCategoryRepository.GetById(categoryId);
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
            var propertyId = PropertyService.GetPropertyId(area);
            var property = _worldPropertyRepository.GetById(propertyId);
            var playerId = GetObjectUUID(Player);

            var propertyPermission = GetPropertyPermission(playerId, propertyId);
            if (!propertyPermission.Permissions[PropertyPermissionType.EditCategories])
                return;

            var categoryCount = _worldPropertyCategoryRepository.GetByPropertyId(propertyId).Count();

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

            var defaultPermissions = PropertyService.GetPermissionsByPropertyType(PropertyType.Category);
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

            _worldPropertyCategoryRepository.Save(category);
            _worldPropertyPermissionRepository.Save(ownerPermission);

            if(playerId != property.OwnerPlayerId)
                _worldPropertyPermissionRepository.Save(adderPermission);

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
                    var propertyId = PropertyService.GetPropertyId(area);
                    var playerId = GetObjectUUID(Player);
                    var categoryId = _categoryIds[SelectedCategoryIndex];
                    var category = _worldPropertyCategoryRepository.GetById(categoryId);

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

                    // Remove any permissions specific to this category.
                    var permissions = _worldPropertyPermissionRepository.GetByPropertyId(categoryId);
                    foreach (var permission in permissions)
                    {
                        _worldPropertyPermissionRepository.Delete(permission.Id);
                    }

                    // Remove the category itself.
                    _worldPropertyCategoryRepository.Delete(categoryId);

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
            _guiService.TogglePlayerWindow(Player, GuiWindowType.PermissionManagement, payload);
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
            var propertyId = PropertyService.GetPropertyId(area);
            var categoryId = _categoryIds[SelectedCategoryIndex];
            var propertyPermission = GetPropertyPermission(playerId, propertyId);
            if (!propertyPermission.Permissions[PropertyPermissionType.EditCategories])
                return;

            var category = _worldPropertyCategoryRepository.GetById(categoryId);

            category.Name = CategoryName;

            _worldPropertyCategoryRepository.Save(category);

            CategoryNames[SelectedCategoryIndex] = CategoryName;

            Instructions = $"Category renamed successfully.";
            InstructionsColor = GuiColor.Green;
        };

        public Action OnStoreItem() => () =>
        {
            ClearInstructions();

            TargetingService.EnterTargetingMode(Player, ObjectType.Item, "Please click on an item within your inventory.", item =>
            {
                var canBeStored = ItemService.CanBePersistentlyStored(Player, item);
                if (!string.IsNullOrWhiteSpace(canBeStored))
                {
                    Instructions = canBeStored;
                    InstructionsColor = GuiColor.Red;
                    return;
                }

                var playerId = GetObjectUUID(Player);
                var area = GetArea(Player);
                var propertyId = PropertyService.GetPropertyId(area);
                var categoryId = _categoryIds[SelectedCategoryIndex];
                var categoryPermission = GetCategoryPermission(playerId, categoryId);

                if (!categoryPermission.Permissions[PropertyPermissionType.AccessStorage])
                    return;

                var property = _worldPropertyRepository.GetById(propertyId);
                var itemCount = GetItemCount();

                if (itemCount >= property.ItemStorageCount)
                {
                    Instructions = $"Property item storage limit reached!";
                    InstructionsColor = GuiColor.Red;
                    return;
                }

                var category = _worldPropertyCategoryRepository.GetById(categoryId);
                var itemId = Guid.NewGuid().ToString();
                var dbItem = new WorldPropertyItem
                {
                    Name = GetName(item),
                    Tag = GetTag(item),
                    Resref = GetResRef(item),
                    IconResref = ItemService.GetIconResref(item),
                    Quantity = GetItemStackSize(item),

                    Data = ObjectPlugin.Serialize(item)
                };

                category.Items.Add(itemId, dbItem);

                _worldPropertyCategoryRepository.Save(category);

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
            var propertyId = PropertyService.GetPropertyId(area);
            var categoryId = _categoryIds[SelectedCategoryIndex];
            var categoryPermission = GetCategoryPermission(playerId, categoryId);

            if (!categoryPermission.Permissions[PropertyPermissionType.AccessStorage])
                return;

            var property = _worldPropertyRepository.GetById(propertyId);
            var category = _worldPropertyCategoryRepository.GetById(categoryId);
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
            _worldPropertyCategoryRepository.Save(category);

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
            var category = _worldPropertyCategoryRepository.GetById(categoryId);

            if (!category.Items.ContainsKey(itemId))
            {
                Instructions = "Item no longer in storage.";
                InstructionsColor = GuiColor.Red;
                return;
            }

            var dbItem = category.Items[itemId];
            var item = ObjectPlugin.Deserialize(dbItem.Data);
            var payload = new ExamineItemPayload(GetName(item), GetDescription(item), ItemService.BuildItemPropertyString(item));
            _guiService.TogglePlayerWindow(Player, GuiWindowType.ExamineItem, payload);
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
