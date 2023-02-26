using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class ManageApartmentViewModel: GuiViewModelBase<ManageApartmentViewModel, ManageApartmentPayload>
    {
        public const int MaxNameLength = 50;
        public const int MaxDescriptionLength = 200;
        private const int MaxLeaseDays = 30;

        public GuiBindingList<string> ApartmentNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> ApartmentToggles
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public bool IsApartmentSelected
        {
            get => Get<bool>();
            set => Set(value);
        }

        private int SelectedApartmentIndex { get; set; }

        private readonly List<string> _propertyIds = new List<string>();

        public string Instruction
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiColor InstructionColor
        {
            get => Get<GuiColor>();
            set => Set(value);
        }

        public string CustomName
        {
            get => Get<string>();
            set => Set(value);
        }

        public string CustomDescription
        {
            get => Get<string>();
            set => Set(value);
        }

        public string LayoutName
        {
            get => Get<string>();
            set => Set(value);
        }

        public string InitialPrice
        {
            get => Get<string>();
            set => Set(value);
        }

        public string PricePerDay
        {
            get => Get<string>();
            set => Set(value);
        }

        public string FurnitureLimit
        {
            get => Get<string>();
            set => Set(value);
        }

        public string LeasedUntil
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiColor LeasedUntilColor
        {
            get => Get<GuiColor>();
            set => Set(value);
        }

        public string ExtendLease1DayText
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsExtendLease1DayEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string ExtendLease7DaysText
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsExtendLease7DaysEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsEnterEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsManagePermissionsEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsCancelLeaseEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsDescriptionEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsPropertyRenameEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsSaveEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsAtTerminal
        {
            get => Get<bool>();
            set => Set(value);
        }

        private WorldProperty GetApartment()
        {
            var selectedPropertyId = _propertyIds[SelectedApartmentIndex];
            var query = new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.Id), selectedPropertyId, false);
            var apartment = DB.Search(query).Single();

            return apartment;
        }

        private WorldPropertyPermission GetPermissions()
        {
            var playerId = GetObjectUUID(Player);
            var selectedPropertyId = _propertyIds[SelectedApartmentIndex];
            var query = new DBQuery<WorldPropertyPermission>()
                .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), playerId, false)
                .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), selectedPropertyId, false);
            var permission = DB.Search(query).FirstOrDefault()
                             ?? new WorldPropertyPermission();

            return permission;
        }

        protected override void Initialize(ManageApartmentPayload initialPayload)
        {
            _propertyIds.Clear();
            var playerId = GetObjectUUID(Player);
            var apartmentNames = new GuiBindingList<string>();
            var apartmentToggles = new GuiBindingList<bool>();
            var selectedApartmentIndex = -1;

            if (initialPayload != null && !string.IsNullOrWhiteSpace(initialPayload.SpecificPropertyId))
            {
                var property = DB.Get<WorldProperty>(initialPayload.SpecificPropertyId);
                apartmentNames.Add(property.CustomName);
                apartmentToggles.Add(true);
                _propertyIds.Add(property.Id);
                selectedApartmentIndex = 0;
                IsAtTerminal = false;
            }
            else
            {
                var permissionQuery = new DBQuery<WorldPropertyPermission>()
                    .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), playerId, false);
                var permissionCount = (int)DB.SearchCount(permissionQuery);
                var dbPermissions = DB.Search(permissionQuery
                        .AddPaging(permissionCount, 0))
                    .ToList();

                if (dbPermissions.Count > 0)
                {
                    var propertyIds = dbPermissions.Select(s => s.PropertyId);
                    var propertyQuery = new DBQuery<WorldProperty>()
                        .AddFieldSearch(nameof(WorldProperty.PropertyType), (int)PropertyType.Apartment)
                        .AddFieldSearch(nameof(WorldProperty.Id), propertyIds)
                        .AddFieldSearch(nameof(WorldProperty.IsQueuedForDeletion), false);
                    var propertyCount = (int)DB.SearchCount(propertyQuery);

                    var properties = DB.Search(propertyQuery
                        .AddPaging(propertyCount, 0));

                    foreach (var property in properties)
                    {
                        _propertyIds.Add(property.Id);
                        apartmentNames.Add(property.CustomName);
                        apartmentToggles.Add(false);
                    }
                }

                IsAtTerminal = true;
            }
            
            ApartmentNames = apartmentNames;
            ApartmentToggles = apartmentToggles;
            SelectedApartmentIndex = selectedApartmentIndex;

            LoadApartment();
            WatchOnClient(model => model.CustomName);
            WatchOnClient(model => model.CustomDescription);
        }

        private void LoadApartment()
        {
            if (SelectedApartmentIndex <= -1)
            {
                CustomName = string.Empty;
                CustomDescription = string.Empty;
                LayoutName = $"Layout: [SELECT]";
                InitialPrice = $"Initial Price: [SELECT]";
                PricePerDay = $"Price Per Day: [SELECT]";
                FurnitureLimit = $"Furniture Limit: [SELECT]";
                LeasedUntil = string.Empty;

                IsEnterEnabled = false;
                IsManagePermissionsEnabled = false;
                IsCancelLeaseEnabled = false;
                IsPropertyRenameEnabled = false;
                IsDescriptionEnabled = false;
                IsSaveEnabled = false;
            }
            else
            {
                var apartment = GetApartment();
                var permissions = GetPermissions();
                var layout = Property.GetLayoutByType(apartment.Layout);
                var furnitureCount = 
                    apartment.ChildPropertyIds.ContainsKey(PropertyChildType.Structure)
                    ? apartment.ChildPropertyIds[PropertyChildType.Structure].Count
                    : 0;
                var leaseDate = apartment.Dates[PropertyDateType.Lease];
                var now = DateTime.UtcNow;

                ClearInstructions();
                CustomName = apartment.CustomName;
                CustomDescription = apartment.CustomDescription;
                LayoutName = $"Layout: {layout.Name}";
                InitialPrice = $"Initial Price: {layout.InitialPrice} cr";
                PricePerDay = $"Price Per Day: {layout.PricePerDay} cr";
                FurnitureLimit = $"Structure Limit: {furnitureCount} / {layout.StructureLimit}";
                IsEnterEnabled = permissions.Permissions[PropertyPermissionType.EnterProperty];
                IsManagePermissionsEnabled = permissions.GrantPermissions.Any(x => x.Value);
                IsCancelLeaseEnabled = permissions.Permissions[PropertyPermissionType.CancelLease];
                IsPropertyRenameEnabled = permissions.Permissions[PropertyPermissionType.RenameProperty];
                IsDescriptionEnabled = permissions.Permissions[PropertyPermissionType.ChangeDescription];
                IsSaveEnabled = IsPropertyRenameEnabled || IsDescriptionEnabled;

                // Apartment lease has expired but won't be cleaned up until the next reboot.
                // Display it differently to signify the player needs to fix it or risk losing it.
                if (now >= leaseDate)
                {
                    LeasedUntil = $"Lease EXPIRED on {leaseDate.ToString("G")}";
                    LeasedUntilColor = GuiColor.Red;
                }
                else
                {
                    LeasedUntil = $"Lease Expires on {leaseDate.ToString("G")}";
                    LeasedUntilColor = GuiColor.Green;
                }
            }

            RefreshLeaseInfo();
        }

        private void RefreshLeaseInfo()
        {
            if (SelectedApartmentIndex <= -1)
            {
                ExtendLease1DayText = $"Extend 1 Day";
                IsExtendLease1DayEnabled = false;

                ExtendLease7DaysText = "Extend 7 Days";
                IsExtendLease7DaysEnabled = false;

                IsEnterEnabled = false;
                return;
            }

            var apartment = GetApartment();
            var layout = Property.GetLayoutByType(apartment.Layout);
            var leasedUntilDate = apartment.Dates[PropertyDateType.Lease];
            var dayPrice = layout.PricePerDay;
            var weekPrice = layout.PricePerDay * 7;
            var gold = GetGold(Player);
            var now = DateTime.UtcNow;
            var permission = GetPermissions();

            ExtendLease1DayText = $"Extend 1 Day ({dayPrice} cr)";
            IsExtendLease1DayEnabled = gold >= dayPrice &&
                                       leasedUntilDate.AddDays(1) < now.AddDays(MaxLeaseDays) &&
                                       permission.Permissions[PropertyPermissionType.ExtendLease];

            ExtendLease7DaysText = $"Extend 7 Days ({weekPrice} cr)";
            IsExtendLease7DaysEnabled = gold >= weekPrice &&
                                        leasedUntilDate.AddDays(7) < now.AddDays(MaxLeaseDays) &&
                                        permission.Permissions[PropertyPermissionType.ExtendLease];

            IsEnterEnabled = leasedUntilDate > now;
        }

        private void ClearInstructions()
        {
            Instruction = string.Empty;
        }

        public Action OnSelectApartment() => () =>
        {
            if (SelectedApartmentIndex > -1)
                ApartmentToggles[SelectedApartmentIndex] = false;

            SelectedApartmentIndex = NuiGetEventArrayIndex();
            ApartmentToggles[SelectedApartmentIndex] = true;

            LoadApartment();
        };

        public Action SaveChanges() => () =>
        {
            var permissions = GetPermissions();
            var apartment = GetApartment();
            var hasChange = false;

            // Rename Property
            if (permissions.Permissions[PropertyPermissionType.RenameProperty])
            {
                if (CustomName.Length < 3)
                {
                    Instruction = $"Name must be at least 3 characters long.";
                    InstructionColor = GuiColor.Red;
                    return;
                }

                if (CustomName.Length > MaxNameLength)
                    CustomName = CustomName.Substring(0, MaxNameLength);

                apartment.CustomName = CustomName;
                hasChange = true;
            }

            // Change Description
            if (permissions.Permissions[PropertyPermissionType.ChangeDescription])
            {
                if (CustomDescription.Length > MaxDescriptionLength)
                    CustomDescription = CustomDescription.Substring(0, MaxDescriptionLength);

                apartment.CustomDescription = CustomDescription;
                hasChange = true;
            }

            if (hasChange)
            {
                DB.Set(apartment);

                var instance = Property.GetRegisteredInstance(apartment.Id);
                SetName(instance.Area, "{PC} " + CustomName);

                Instruction = $"Saved successfully.";
                InstructionColor = GuiColor.Green;
            }
        };

        private void ExtendLease(int days)
        {
            var apartment = GetApartment();
            var permissions = GetPermissions();

            if (!permissions.Permissions[PropertyPermissionType.ExtendLease])
                return;

            var layout = Property.GetLayoutByType(apartment.Layout);
            var price = days * layout.PricePerDay;
            var dayWord = days == 1 ? "day" : "days";
            var currentLease = apartment.Dates[PropertyDateType.Lease];
            var now = DateTime.UtcNow;
            var newLeaseDate = currentLease.AddDays(days);

            if (newLeaseDate > now.AddDays(MaxLeaseDays))
            {
                Instruction = $"Leases may only paid a maximum of {MaxLeaseDays} days in advance.";
                InstructionColor = GuiColor.Red;
                return;
            }

            var newLeaseDateText = newLeaseDate.ToString("F");
            ShowModal($"Extending your lease by {days} {dayWord} will cost {price} credits. Your new lease will extend to {newLeaseDateText} (UTC). Are you sure you want to extend your lease?",
                () =>
                {
                    if (price > GetGold(Player))
                    {
                        Instruction = $"Insufficient credits!";
                        InstructionColor = GuiColor.Red;
                        return;
                    }

                    AssignCommand(Player, () => TakeGoldFromCreature(price, Player, true));

                    apartment = GetApartment();
                    apartment.Dates[PropertyDateType.Lease] = newLeaseDate;

                    DB.Set(apartment);

                    Instruction = $"Lease extended by {days} {dayWord}!";
                    InstructionColor = GuiColor.Green;
                    RefreshLeaseInfo();
                });
        }

        public Action OnExtendLease1Day() => () =>
        {
            ExtendLease(1);
        };

        public Action OnExtendLease7Days() => () =>
        {
            ExtendLease(7);
        };

        public Action OnCancelLease() => () =>
        {
            ShowModal($"WARNING: Cancelling your lease will forfeit your apartment and ALL objects contained inside. These items will be permanently lost. You will NOT receive any credits back that have already been paid toward the lease. Are you sure you want to revoke your lease?",
                () =>
                {
                    var permissions = GetPermissions();
                    if (!permissions.Permissions[PropertyPermissionType.CancelLease])
                        return;

                    var apartment = GetApartment();

                    // Queue the deletion for the next reboot to avoid lag while players are on.
                    apartment.IsQueuedForDeletion = true;
                    DB.Set(apartment);
                    
                    if(Gui.IsWindowOpen(Player, GuiWindowType.ManageApartment))
                        Gui.TogglePlayerWindow(Player, GuiWindowType.ManageApartment);

                    FloatingTextStringOnCreature("Your apartment's lease has been successfully revoked.", Player, false);
                });
        };

        public Action OnManagePermissions() => () =>
        {
            var permissions = GetPermissions();
            if (!permissions.GrantPermissions.Any(x => x.Value))
                return;

            var apartment = GetApartment();
            var payload = new PropertyPermissionPayload(PropertyType.Apartment, apartment.Id, string.Empty, false);

            Gui.TogglePlayerWindow(Player, GuiWindowType.PermissionManagement, payload, TetherObject);
        };

        public Action OnEnterApartment() => () =>
        {
            var permissions = GetPermissions();
            if (!permissions.Permissions[PropertyPermissionType.EnterProperty])
                return;

            var apartment = GetApartment();
            Property.EnterProperty(Player, apartment.Id);

            Gui.TogglePlayerWindow(Player, GuiWindowType.ManageApartment);
        };

        public Action OnBuyApartment() => () =>
        {
            Gui.TogglePlayerWindow(Player, GuiWindowType.RentApartment, null, TetherObject);
        };
    }
}
