using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.SpaceService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class ShipManagementViewModel: GuiViewModelBase<ShipManagementViewModel, GuiPayloadBase>
    {
        private static GuiColor _green = new GuiColor(0, 255, 0);
        private static GuiColor _white = new GuiColor(255, 255, 255);

        private int SelectedShipIndex { get; set; }
        private int ActiveShipIndex { get; set; }
        private List<string> _shipIds { get; set; } = new List<string>();

        public string ShipCountRegistered
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiBindingList<string> ShipNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> ShipToggles
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public GuiBindingList<GuiColor> ShipColors
        {
            get => Get<GuiBindingList<GuiColor>>();
            set => Set(value);
        }

        public bool IsRegisterEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsUnregisterEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string ShipName
        {
            get => Get<string>();
            set => Set(value);
        }

        public string ShipType
        {
            get => Get<string>();
            set => Set(value);
        }

        public float Shields
        {
            get => Get<float>();
            set => Set(value);
        }

        public string ShieldsTooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public float Hull
        {
            get => Get<float>();
            set => Set(value);
        }

        public string HullTooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public float Capacitor
        {
            get => Get<float>();
            set => Set(value);
        }

        public string CapacitorTooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string ShieldRechargeRate
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool HighPower1Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool HighPower2Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool HighPower3Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool HighPower4Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool HighPower5Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool HighPower6Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool HighPower7Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool HighPower8Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string HighPower1Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string HighPower2Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string HighPower3Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string HighPower4Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string HighPower5Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string HighPower6Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string HighPower7Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string HighPower8Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string HighPower1Resref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string HighPower2Resref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string HighPower3Resref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string HighPower4Resref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string HighPower5Resref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string HighPower6Resref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string HighPower7Resref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string HighPower8Resref
        {
            get => Get<string>();
            set => Set(value);
        }


        public bool LowPower1Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool LowPower2Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool LowPower3Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool LowPower4Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool LowPower5Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool LowPower6Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool LowPower7Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool LowPower8Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string LowPower1Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string LowPower2Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string LowPower3Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string LowPower4Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string LowPower5Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string LowPower6Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string LowPower7Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string LowPower8Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string LowPower1Resref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string LowPower2Resref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string LowPower3Resref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string LowPower4Resref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string LowPower5Resref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string LowPower6Resref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string LowPower7Resref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string LowPower8Resref
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsMakeActiveEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsNameEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);
            var query = new DBQuery<PlayerShip>()
                .AddFieldSearch(nameof(PlayerShip.PlayerId), playerId, false);
            var dbPlayerShips = DB.Search(query).ToList();

            _shipIds.Clear();
            var shipNames = new GuiBindingList<string>();
            var shipToggles = new GuiBindingList<bool>();
            var shipColors = new GuiBindingList<GuiColor>();

            ActiveShipIndex = -1;
            foreach (var ship in dbPlayerShips)
            {
                _shipIds.Add(ship.Id.ToString());
                shipToggles.Add(false);

                if (ship.Id.ToString() == dbPlayer.SelectedShipId)
                {
                    shipNames.Add($"{ship.Status.Name} [ACTIVE]");
                    shipColors.Add(_green);
                    ActiveShipIndex = shipNames.Count - 1;
                }
                else
                {
                    shipNames.Add(ship.Status.Name);
                    shipColors.Add(_white);
                }
            }

            ShipCountRegistered = $"Ships: {dbPlayerShips.Count} / {Space.MaxRegisteredShips}";
            ShipNames = shipNames;
            ShipToggles = shipToggles;
            ShipColors = shipColors;
            SelectedShipIndex = -1;
            LoadShip();

            WatchOnClient(model => model.ShipName);
        }

        private void LoadShip()
        {
            if (SelectedShipIndex <= -1)
            {
                ShipName = "[Select a Ship]";
                ShipType = string.Empty;

                Shields = 0f;
                ShieldsTooltip = string.Empty;

                Hull = 0f;
                HullTooltip = string.Empty;

                Capacitor = 0f;
                CapacitorTooltip = string.Empty;

                ShieldRechargeRate = string.Empty;

                HighPower1Visible = false;
                HighPower2Visible = false;
                HighPower3Visible = false;
                HighPower4Visible = false;
                HighPower5Visible = false;
                HighPower6Visible = false;
                HighPower7Visible = false;
                HighPower8Visible = false;

                HighPower1Tooltip = string.Empty;
                HighPower2Tooltip = string.Empty;
                HighPower3Tooltip = string.Empty;
                HighPower4Tooltip = string.Empty;
                HighPower5Tooltip = string.Empty;
                HighPower6Tooltip = string.Empty;
                HighPower7Tooltip = string.Empty;
                HighPower8Tooltip = string.Empty;

                HighPower1Resref = string.Empty;
                HighPower2Resref = string.Empty;
                HighPower3Resref = string.Empty;
                HighPower4Resref = string.Empty;
                HighPower5Resref = string.Empty;
                HighPower6Resref = string.Empty;
                HighPower7Resref = string.Empty;
                HighPower8Resref = string.Empty;

                LowPower1Visible = false;
                LowPower2Visible = false;
                LowPower3Visible = false;
                LowPower4Visible = false;
                LowPower5Visible = false;
                LowPower6Visible = false;
                LowPower7Visible = false;
                LowPower8Visible = false;

                LowPower1Tooltip = string.Empty;
                LowPower2Tooltip = string.Empty;
                LowPower3Tooltip = string.Empty;
                LowPower4Tooltip = string.Empty;
                LowPower5Tooltip = string.Empty;
                LowPower6Tooltip = string.Empty;
                LowPower7Tooltip = string.Empty;
                LowPower8Tooltip = string.Empty;

                LowPower1Resref = string.Empty;
                LowPower2Resref = string.Empty;
                LowPower3Resref = string.Empty;
                LowPower4Resref = string.Empty;
                LowPower5Resref = string.Empty;
                LowPower6Resref = string.Empty;
                LowPower7Resref = string.Empty;
                LowPower8Resref = string.Empty;

                IsMakeActiveEnabled = false;
            }
            else
            {
                var shipId = _shipIds[SelectedShipIndex];
                var ship = DB.Get<PlayerShip>(shipId);
                var shipDetail = Space.GetShipDetailByItemTag(ship.Status.ItemTag);
                
                ShipName = ship.Status.Name;
                ShipType = $"Type: {shipDetail.Name}";

                Shields = (float)ship.Status.Shield / ship.Status.MaxShield;
                ShieldsTooltip = $"Shields: {ship.Status.Shield} / {ship.Status.MaxShield}";

                Hull = (float)ship.Status.Hull / ship.Status.MaxHull;
                HullTooltip = $"Hull: {ship.Status.Hull} / {ship.Status.MaxHull}";

                Capacitor = (float)ship.Status.Capacitor / ship.Status.MaxCapacitor;
                CapacitorTooltip = $"Capacitor: {ship.Status.Capacitor} / {ship.Status.MaxCapacitor}";

                ShieldRechargeRate = $"{ship.Status.ShieldRechargeRate}s";

                HighPower1Visible = shipDetail.HighPowerNodes >= 1;
                HighPower2Visible = shipDetail.HighPowerNodes >= 2;
                HighPower3Visible = shipDetail.HighPowerNodes >= 3;
                HighPower4Visible = shipDetail.HighPowerNodes >= 4;
                HighPower5Visible = shipDetail.HighPowerNodes >= 5;
                HighPower6Visible = shipDetail.HighPowerNodes >= 6;
                HighPower7Visible = shipDetail.HighPowerNodes >= 7;
                HighPower8Visible = shipDetail.HighPowerNodes >= 8;

                LowPower1Visible = shipDetail.LowPowerNodes >= 1;
                LowPower2Visible = shipDetail.LowPowerNodes >= 2;
                LowPower3Visible = shipDetail.LowPowerNodes >= 3;
                LowPower4Visible = shipDetail.LowPowerNodes >= 4;
                LowPower5Visible = shipDetail.LowPowerNodes >= 5;
                LowPower6Visible = shipDetail.LowPowerNodes >= 6;
                LowPower7Visible = shipDetail.LowPowerNodes >= 7;
                LowPower8Visible = shipDetail.LowPowerNodes >= 8;


                IsMakeActiveEnabled = true;
            }

            ToggleRegisterButtons();
        }

        private void ToggleRegisterButtons()
        {
            IsRegisterEnabled = _shipIds.Count < Space.MaxRegisteredShips;
            IsUnregisterEnabled = SelectedShipIndex > -1;
        }

        public Action OnClickShip() => () =>
        {
            if (SelectedShipIndex > -1)
                ShipToggles[SelectedShipIndex] = false;

            var index = NuiGetEventArrayIndex();
            SelectedShipIndex = index;
            ShipToggles[index] = true;

            LoadShip();
        };
        
        public Action OnClickRegisterShip() => () =>
        {
            Targeting.EnterTargetingMode(Player, ObjectType.Item, item =>
            {
                if (GetItemPossessor(item) != Player)
                {
                    FloatingTextStringOnCreature("Item must be in your inventory.", Player, false);
                    return;
                }

                if (!Space.IsItemShip(item))
                {
                    FloatingTextStringOnCreature("Only ship deeds may be targeted.", Player, false);
                    return;
                }

                if (!Item.CanCreatureUseItem(Player, item))
                {
                    FloatingTextStringOnCreature("You do not meet the requirements necessary to register this ship.", Player, false);
                    return;
                }

                var playerId = GetObjectUUID(Player);
                var query = new DBQuery<PlayerShip>()
                    .AddFieldSearch(nameof(PlayerShip.PlayerId), playerId, false);
                var dbPlayerShips = DB.Search(query).ToList();

                if (dbPlayerShips.Count >= Space.MaxRegisteredShips)
                {
                    FloatingTextStringOnCreature($"You may only have {Space.MaxRegisteredShips} ships registered at a time.", Player, false);
                    return;
                }

                // Validation passed. Add the ship and register it under this player.
                var itemTag = GetTag(item);
                var shipDetail = Space.GetShipDetailByItemTag(itemTag);
                var ship = new PlayerShip
                {
                    PlayerId = playerId,
                    Status = new ShipStatus
                    {
                        ItemTag = itemTag,
                        Name = shipDetail.Name,
                        Shield = shipDetail.MaxShield,
                        MaxShield = shipDetail.MaxShield,
                        Hull = shipDetail.MaxHull,
                        MaxHull = shipDetail.MaxHull,
                        Capacitor = shipDetail.MaxCapacitor,
                        MaxCapacitor = shipDetail.MaxCapacitor,
                        EMDefense = shipDetail.EMDefense,
                        ExplosiveDefense = shipDetail.ExplosiveDefense,
                        ThermalDefense = shipDetail.ThermalDefense,
                        Accuracy = shipDetail.Accuracy,
                        Evasion = shipDetail.Evasion,
                        ShieldRechargeRate = shipDetail.ShieldRechargeRate
                    }
                };
                DB.Set(ship.Id.ToString(), ship);

                // Update the UI with the new ship details.
                ShipCountRegistered = $"Ships: {dbPlayerShips.Count+1} / {Space.MaxRegisteredShips}";
                _shipIds.Add(ship.Id.ToString());
                ShipNames.Add(ship.Status.Name);
                ShipToggles.Add(false);
                ToggleRegisterButtons();

                DestroyObject(item);

                FloatingTextStringOnCreature("Ship registered!", Player, false);
            });
        };

        public Action OnClickUnregisterShip() => () =>
        {
            ShowModal($"Unregistering a ship will convert it back into a deed item. Are you sure you wish to unregister this ship?",
                () =>
                {
                    var playerId = GetObjectUUID(Player);
                    var shipId = _shipIds[SelectedShipIndex];
                    var dbPlayer = DB.Get<Player>(playerId);
                    var dbShip = DB.Get<PlayerShip>(shipId);
                    var shipDetail = Space.GetShipDetailByItemTag(dbShip.Status.ItemTag);

                    if (dbShip.Status.HighPowerModules.Count > 0 ||
                        dbShip.Status.LowPowerModules.Count > 0)
                    {
                        FloatingTextStringOnCreature($"Please uninstall all modules before unregistering your ship.", Player, false);
                        return;
                    }

                    if (dbPlayer.SelectedShipId == shipId)
                    {
                        dbPlayer.SelectedShipId = Guid.Empty.ToString();
                        ActiveShipIndex = -1;
                    }

                    if (dbPlayer.ActiveShipId == shipId)
                        dbPlayer.ActiveShipId = Guid.Empty.ToString();

                    DB.Delete<PlayerShip>(shipId);
                    DB.Set(playerId, dbPlayer);

                    CreateItemOnObject(shipDetail.ItemResref, Player);

                    _shipIds.RemoveAt(SelectedShipIndex);
                    ShipNames.RemoveAt(SelectedShipIndex);
                    ShipToggles.RemoveAt(SelectedShipIndex);
                    ToggleRegisterButtons();
                    ShipCountRegistered = $"Ships: {_shipIds.Count} / {Space.MaxRegisteredShips}";

                    FloatingTextStringOnCreature("Ship unregistered!", Player, false);
                });
        };

        public Action OnClickSaveShipName() => () =>
        {
            if (string.IsNullOrWhiteSpace(ShipName))
            {
                FloatingTextStringOnCreature("A ship name is required.", Player, false);
                return;
            }

            var shipId = _shipIds[SelectedShipIndex];
            var dbShip = DB.Get<PlayerShip>(shipId);

            dbShip.Status.Name = ShipName;
            DB.Set(shipId, dbShip);

            if (ActiveShipIndex == SelectedShipIndex)
            {
                ShipNames[SelectedShipIndex] = ShipName + " [ACTIVE]";
            }
            else
            {
                ShipNames[SelectedShipIndex] = ShipName;
            }
        };

        private void ProcessHighPower(int slot)
        {

        }


        public Action OnClickHighPower1() => () =>
        {
            ProcessHighPower(1);
        };
        public Action OnClickHighPower2() => () =>
        {
            ProcessHighPower(2);
        };
        public Action OnClickHighPower3() => () =>
        {
            ProcessHighPower(3);
        };
        public Action OnClickHighPower4() => () =>
        {
            ProcessHighPower(4);
        };
        public Action OnClickHighPower5() => () =>
        {
            ProcessHighPower(5);
        };
        public Action OnClickHighPower6() => () =>
        {
            ProcessHighPower(6);
        };
        public Action OnClickHighPower7() => () =>
        {
            ProcessHighPower(7);
        };
        public Action OnClickHighPower8() => () =>
        {
            ProcessHighPower(8);
        };

        private void ProcessLowPower(int slot)
        {

        }

        public Action OnClickLowPower1() => () =>
        {
            ProcessLowPower(1);
        };
        public Action OnClickLowPower2() => () =>
        {
            ProcessLowPower(2);
        };
        public Action OnClickLowPower3() => () =>
        {
            ProcessLowPower(3);
        };
        public Action OnClickLowPower4() => () =>
        {
            ProcessLowPower(4);
        };
        public Action OnClickLowPower5() => () =>
        {
            ProcessLowPower(5);
        };
        public Action OnClickLowPower6() => () =>
        {
            ProcessLowPower(6);
        };
        public Action OnClickLowPower7() => () =>
        {
            ProcessLowPower(7);
        };
        public Action OnClickLowPower8() => () =>
        {
            ProcessLowPower(8);
        };

        public Action OnClickMakeActive() => () =>
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);
            var shipId = _shipIds[SelectedShipIndex];

            dbPlayer.SelectedShipId = shipId;

            DB.Set(playerId, dbPlayer);

            if (ActiveShipIndex > -1)
            {
                ShipNames[ActiveShipIndex] = ShipNames[ActiveShipIndex].Replace(" [ACTIVE]", string.Empty);
                ShipColors[ActiveShipIndex] = _white;
            }

            ActiveShipIndex = SelectedShipIndex;
            ShipNames[SelectedShipIndex] = ShipNames[SelectedShipIndex] + " [ACTIVE]";
            ShipColors[SelectedShipIndex] = _green;
        };
    }
}
