using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class ManageCitizenshipViewModel: GuiViewModelBase<ManageCitizenshipViewModel, GuiPayloadBase>
    {
        private string _cityPropertyId;
        private string _electionId;

        public GuiBindingList<string> CityDetails
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public string RegisterRevokeButtonName
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiColor RegisterRevokeButtonColor
        {
            get => Get<GuiColor>();
            set => Set(value);
        }

        public string PayTaxesButtonName
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsPayTaxesEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsElectionActive
        {
            get => Get<bool>();
            set => Set(value);
        }

        private void LoadData()
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);
            var area = GetArea(TetherObject);
            var propertyId = Property.GetPropertyId(area);
            var dbProperty = DB.Get<WorldProperty>(propertyId);
            var dbBuilding = DB.Get<WorldProperty>(dbProperty.ParentPropertyId);
            var dbCity = DB.Get<WorldProperty>(dbBuilding.ParentPropertyId);
            var dbMayorPlayer = DB.Get<Player>(dbCity.OwnerPlayerId);
            var dbElection = DB.Search(new DBQuery<Election>()
                .AddFieldSearch(nameof(Election.PropertyId), dbCity.Id, false))
                .SingleOrDefault();
            var dbCitizenCount = DB.SearchCount(new DBQuery<Player>()
                .AddFieldSearch(nameof(Entity.Player.CitizenPropertyId), dbCity.Id, false)
                .AddFieldSearch(nameof(Entity.Player.IsDeleted), false));

            var cityDetails = new GuiBindingList<string>();

            cityDetails.Add($"City Name: {dbCity.CustomName}");
            cityDetails.Add($"Mayor: {dbMayorPlayer.Name}");
            cityDetails.Add($"# Citizens: {dbCitizenCount}");
            cityDetails.Add($"Level: {Property.GetCityLevelName(dbCity.Upgrades[PropertyUpgradeType.CityLevel])}");
            cityDetails.Add($"Established: {dbCity.DateCreated:yyyy-MM-dd hh:mm:ss}");

            cityDetails.Add($"Taxes & Fees:");
            cityDetails.Add($"   Citizenship: {(int)dbCity.Taxes[PropertyTaxType.Citizenship]} cr per week");
            cityDetails.Add($"   Transportation: {dbCity.Taxes[PropertyTaxType.Transportation]:F1}%");

            // Election has not started.
            if (dbElection == null)
            {
                cityDetails.Add($"Next Election [After server restart]:");
                cityDetails.Add($"    {dbCity.Dates[PropertyDateType.ElectionStart]:yyyy-MM-dd hh:mm:ss}");
            }
            // Election has started and we're in the registration process.
            else if (dbElection.Stage == ElectionStageType.Registration)
            {
                var openUntil = dbCity.Dates[PropertyDateType.ElectionStart]
                    .AddDays(Property.ElectionRegistrationDays);
                cityDetails.Add($"Election registrations are currently OPEN to citizens until:");
                cityDetails.Add($"    {openUntil:yyyy-MM-dd hh:mm:ss}");
            }
            // Election has started and we're in the voting process.
            else if (dbElection.Stage == ElectionStageType.Voting)
            {
                var openUntil = dbCity.Dates[PropertyDateType.ElectionStart]
                    .AddDays(Property.ElectionRegistrationDays + Property.ElectionVotingDays);
                cityDetails.Add($"Election voting is currently OPEN to citizens until:");
                cityDetails.Add($"    {openUntil:yyyy-MM-dd hh:mm:ss}");
            }


            CityDetails = cityDetails;
            _cityPropertyId = dbCity.Id;
            _electionId = dbElection == null ? string.Empty : dbElection.Id;
            IsElectionActive = !string.IsNullOrWhiteSpace(_electionId);

            // Player is already a citizen. Provide the "Revoke Citizenship" option.
            if (dbPlayer.CitizenPropertyId == dbCity.Id)
            {
                RegisterRevokeButtonName = "Revoke Citizenship";
                RegisterRevokeButtonColor = GuiColor.Red;
                IsPayTaxesEnabled = dbPlayer.PropertyOwedTaxes > 0;
                PayTaxesButtonName = $"Pay Taxes ({dbPlayer.PropertyOwedTaxes} cr)";
            }
            else
            {
                RegisterRevokeButtonName = "Register Citizenship";
                RegisterRevokeButtonColor = GuiColor.White;
                PayTaxesButtonName = "Pay Taxes";
            }
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            LoadData();
        }

        public Action RegisterRevoke() => () =>
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);

            // Chose to revoke citizenship.
            if (dbPlayer.CitizenPropertyId == _cityPropertyId)
            {
                ShowModal("Revoking your citizenship will remove your ability to access all public facilities. You will also lose any permissions granted to you. If you are a candidate for an election, you will be withdrawn from the race. Are you sure you want to revoke your citizenship?",
                    () =>
                    {
                        var dbCity = DB.Get<WorldProperty>(_cityPropertyId);
                        if (playerId == dbCity.OwnerPlayerId)
                        {
                            FloatingTextStringOnCreature("You are the mayor of this city and cannot revoke your citizenship.", Player, false);
                            return;
                        }

                        // Remove the player's citizenship property Id.
                        dbPlayer = DB.Get<Player>(playerId);
                        dbPlayer.CitizenPropertyId = string.Empty;
                        dbPlayer.PropertyOwedTaxes = 0;
                        DB.Set(dbPlayer);

                        // If there's an active election going, remove this player from the running.
                        if (!string.IsNullOrWhiteSpace(_electionId))
                        {
                            var dbElection = DB.Get<Election>(_electionId);
                            dbElection.CandidatePlayerIds.Remove(playerId);

                            var toRemove = dbElection.VoterSelections.Where(x => x.Value.CandidatePlayerId == playerId);
                            foreach (var vote in toRemove)
                            {
                                dbElection.VoterSelections.Remove(vote.Key);
                                Log.Write(LogGroup.Property, $"Removed vote from player '{vote.Key}' because chosen candidate '{playerId}' has dropped out of the race.");
                            }

                            DB.Set(dbElection);
                        }

                        // Retrieve all structure Ids
                        var structureIds = dbCity.ChildPropertyIds.ContainsKey(PropertyChildType.Structure)
                            ? dbCity.ChildPropertyIds[PropertyChildType.Structure]
                            : new List<string>();

                        // Pull back the full structure information
                        var structures = DB.Search(new DBQuery<WorldProperty>()
                            .AddFieldSearch(nameof(WorldProperty.Id), structureIds));

                        // Look for any structures that have interior children and return their Ids
                        var interiorPropertyIds = structures.SelectMany(s =>
                            s.ChildPropertyIds.ContainsKey(PropertyChildType.Interior)
                                ? s.ChildPropertyIds[PropertyChildType.Interior]
                                : new List<string>()).ToList();
                        
                        // Always remove any city permissions
                        interiorPropertyIds.Add(_cityPropertyId);

                        // Retrieve any permissions associated to this player across any structures in the city.
                        var permissions = DB.Search(new DBQuery<WorldPropertyPermission>()
                            .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), interiorPropertyIds)
                            .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), playerId, false));

                        // Remove the player's permissions, if any.
                        foreach (var permission in permissions)
                        {
                            DB.Delete<WorldPropertyPermission>(permission.Id);
                        }

                        FloatingTextStringOnCreature("Your citizenship has been revoked!", Player, false);
                        Log.Write(LogGroup.Property, $"Player '{GetName(Player)}' ({GetPCPlayerName(Player)} / {GetPCPublicCDKey(Player)}) revoked citizenship from city '{dbCity.CustomName}' ({dbCity.Id}).");
                        LoadData();
                    });
            }
            // Chose to register citizenship.
            else
            {
                ShowModal($"Registering as a citizen will grant you access to all public facilities within the city. Registration costs 5000 credits and is paid to the planetary government for taxes and fees. Your character must be older than 30 days old and have a minimum of 100 cumulative skill ranks. You will be expected to pay all taxes and fees as mandated by the mayor. You will also be entitled to run for mayor yourself during the next election. Are you sure you want to become a citizen here?",
                    () =>
                    {
                        if (GetGold(Player) < 5000)
                        {
                            FloatingTextStringOnCreature("5000 credits are needed to register as a citizen.", Player, false);
                            return;
                        }

                        dbPlayer = DB.Get<Player>(playerId);
                        if (dbPlayer.DateCreated.AddDays(30) > DateTime.UtcNow)
                        {
                            FloatingTextStringOnCreature("Your character must be 30 days or older to become a citizen of a city.", Player, false);
                            return;
                        }

                        if (dbPlayer.TotalSPAcquired < 100)
                        {
                            FloatingTextStringOnCreature("You must have acquired a minimum of 100 skill ranks to become a citizen of a city.", Player, false);
                            return;
                        }

                        AssignCommand(Player, () => TakeGoldFromCreature(5000, Player, true));

                        var dbCity = DB.Get<WorldProperty>(_cityPropertyId);
                        dbPlayer.CitizenPropertyId = _cityPropertyId;

                        DB.Set(dbPlayer);

                        Log.Write(LogGroup.Property, $"Player '{GetName(Player)}' ({GetPCPlayerName(Player)} / {GetPCPublicCDKey(Player)}) became a citizen of '{dbCity.CustomName}' ({dbCity.Id}).");
                        FloatingTextStringOnCreature($"You became a citizen of {dbCity.CustomName}!", Player, false);

                        LoadData();
                    });
            }
        };

        public Action PayTaxes() => () =>
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);
            ShowModal($"You owe {dbPlayer.PropertyOwedTaxes} credits. Will you pay this now?", () =>
            {
                var dbCity = DB.Get<WorldProperty>(_cityPropertyId);
                dbPlayer = DB.Get<Player>(playerId);
                var gold = GetGold(Player);

                if (gold <= 0)
                {
                    FloatingTextStringOnCreature("You do not have any credits.", Player, false);
                    return;
                }

                // Not enough to cover everything, but take what they have.
                if (gold < dbPlayer.PropertyOwedTaxes)
                {
                    AssignCommand(Player, () => TakeGoldFromCreature(gold, Player, true));
                    Log.Write(LogGroup.Property, $"{GetName(Player)} paid {gold} credits towards taxes for property '{_cityPropertyId}'");
                    dbCity.Treasury += gold;
                    dbPlayer.PropertyOwedTaxes -= gold;
                }
                // Can cover everything. Take what's required.
                else
                {
                    var amount = dbPlayer.PropertyOwedTaxes;
                    AssignCommand(Player, () => TakeGoldFromCreature(amount, Player, true));
                    Log.Write(LogGroup.Property, $"{GetName(Player)} paid {dbPlayer.PropertyOwedTaxes} credits towards taxes for property '{_cityPropertyId}'.");
                    dbCity.Treasury += dbPlayer.PropertyOwedTaxes;
                    dbPlayer.PropertyOwedTaxes = 0;
                }

                DB.Set(dbPlayer);
                DB.Set(dbCity);

                IsPayTaxesEnabled = dbPlayer.PropertyOwedTaxes > 0;
                PayTaxesButtonName = $"Pay Taxes ({dbPlayer.PropertyOwedTaxes} cr)";
            });
        };

        public Action OpenElectionMenu() => () =>
        {
            Gui.TogglePlayerWindow(Player, GuiWindowType.Election, null, TetherObject);
        };
    }
}
