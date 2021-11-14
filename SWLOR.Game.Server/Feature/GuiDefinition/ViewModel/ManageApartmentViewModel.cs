using System;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.PropertyService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class ManageApartmentViewModel: GuiViewModelBase<ManageApartmentViewModel, GuiPayloadBase>
    {
        private const int MaxLeaseDays = 30;
        private static readonly GuiColor _red = new GuiColor(255, 0, 0);
        private static readonly GuiColor _green = new GuiColor(0, 255, 0);

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

        private WorldProperty GetApartment()
        {
            var playerId = GetObjectUUID(Player);
            var query = new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.OwnerPlayerId), playerId, false)
                .AddFieldSearch(nameof(WorldProperty.PropertyType), (int)PropertyType.Apartment)
                .AddFieldSearch(nameof(WorldProperty.IsQueuedForDeletion), false);
            var apartment = DB.Search(query).Single();

            return apartment;
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            var apartment = GetApartment();
            var layout = Property.GetLayoutByType(apartment.InteriorLayout);
            var furnitureCount = apartment.ChildPropertyIds.Count;
            var leaseDate = apartment.Timers[PropertyTimerType.Lease];
            var now = DateTime.UtcNow;

            ClearInstructions();
            CustomName = apartment.CustomName;
            LayoutName = $"Layout: {layout.Name}";
            InitialPrice = $"Initial Price: {layout.InitialPrice} cr";
            PricePerDay = $"Price Per Day: {layout.PricePerDay} cr";
            FurnitureLimit = $"Furniture Limit: {furnitureCount} / {layout.FurnitureLimit}";

            // Apartment lease has expired but won't be cleaned up until the next reboot.
            // Display it differently to signify the player needs to fix it or risk losing it.
            if (now >= leaseDate)
            {
                LeasedUntil = $"Lease EXPIRED on {leaseDate.ToString("G")}";
                LeasedUntilColor = _red;
            }
            else
            {
                LeasedUntil = $"Lease Expires on {leaseDate.ToString("G")}";
                LeasedUntilColor = _green;
            }
            

            RefreshLeaseInfo();
            WatchOnClient(model => model.CustomName);
        }

        private void RefreshLeaseInfo()
        {
            var apartment = GetApartment();
            var layout = Property.GetLayoutByType(apartment.InteriorLayout);
            var leasedUntilDate = apartment.Timers[PropertyTimerType.Lease];
            var dayPrice = layout.PricePerDay;
            var weekPrice = layout.PricePerDay * 7;
            var gold = GetGold(Player);
            var now = DateTime.UtcNow;

            ExtendLease1DayText = $"Extend 1 Day ({dayPrice} cr)";
            IsExtendLease1DayEnabled = gold >= dayPrice &&
                                       leasedUntilDate.AddDays(1) < now.AddDays(MaxLeaseDays);

            ExtendLease7DaysText = $"Extend 7 Days ({weekPrice} cr)";
            IsExtendLease7DaysEnabled = gold >= weekPrice &&
                                        leasedUntilDate.AddDays(7) < now.AddDays(MaxLeaseDays);

            IsEnterEnabled = leasedUntilDate > now;
        }

        private void ClearInstructions()
        {
            Instruction = string.Empty;
        }

        public Action SaveCustomName() => () =>
        {
            if (CustomName.Length < 3)
            {
                Instruction = $"Apartment names must be at least 3 characters long.";
                InstructionColor = _red;
                return;
            }
            
            var apartment = GetApartment();
            apartment.CustomName = CustomName;

            DB.Set(apartment.Id.ToString(), apartment);

            var instance = Property.GetRegisteredInstance(apartment.Id.ToString());
            SetName(instance, CustomName);


            Instruction = $"Name saved!";
            InstructionColor = _green;
        };

        private void ExtendLease(int days)
        {
            var apartment = GetApartment();
            var layout = Property.GetLayoutByType(apartment.InteriorLayout);
            var price = days * layout.PricePerDay;
            var dayWord = days == 1 ? "day" : "days";
            var currentLease = apartment.Timers[PropertyTimerType.Lease];
            var now = DateTime.UtcNow;
            var newLeaseDate = currentLease.AddDays(days);

            if (newLeaseDate > now.AddDays(MaxLeaseDays))
            {
                Instruction = $"Leases may only paid a maximum of {MaxLeaseDays} days in advance.";
                InstructionColor = _red;
                return;
            }

            var newLeaseDateText = newLeaseDate.ToString("F");
            ShowModal($"Extending your lease by {days} {dayWord} will cost {price} credits. Your new lease will extend to {newLeaseDateText} (UTC). Are you sure you want to extend your lease?",
                () =>
                {
                    apartment = GetApartment();
                    apartment.Timers[PropertyTimerType.Lease] = newLeaseDate;

                    DB.Set(apartment.Id.ToString(), apartment);

                    Instruction = $"Lease extended by {days} {dayWord}!";
                    InstructionColor = _green;
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
                    var apartment = GetApartment();

                    // Queue the deletion for the next reboot to avoid lag while players are on.
                    apartment.IsQueuedForDeletion = true;
                    DB.Set(apartment.Id.ToString(), apartment);
                    
                    if(Gui.IsWindowOpen(Player, GuiWindowType.ManageApartment))
                        Gui.TogglePlayerWindow(Player, GuiWindowType.ManageApartment);

                    FloatingTextStringOnCreature("Your apartment's lease has been successfully revoked.", Player, false);
                });
        };

        public Action OnManagePermissions() => () =>
        {
            Gui.TogglePlayerWindow(Player, GuiWindowType.PermissionManagement);
        };

        public Action OnEnterApartment() => () =>
        {
            var apartment = GetApartment();
            Property.EnterProperty(Player, apartment.Id.ToString());

            Gui.TogglePlayerWindow(Player, GuiWindowType.ManageApartment);
        };
    }
}
