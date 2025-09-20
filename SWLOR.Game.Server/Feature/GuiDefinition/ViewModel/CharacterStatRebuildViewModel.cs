using System;

using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CurrencyService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Core.Event;
using SWLOR.Shared.Core.Service;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class CharacterStatRebuildViewModel: GuiViewModelBase<CharacterStatRebuildViewModel, GuiPayloadBase>
    {
        [ScriptHandler(ScriptName.OnBuyStatRebuild)]
        public static void LoadCharacterStatRebuild()
        {
            var terminal = OBJECT_SELF;
            var player = GetPCSpeaker();

            if (!GetIsPC(player))
            {
                SendMessageToPC(player, ColorToken.Red("This terminal may only be used by players."));
                return;
            }

            if (Currency.GetCurrency(player, CurrencyType.StatRefundToken) <= 0)
            {
                FloatingTextStringOnCreature(ColorToken.Red("Insufficient stat refund tokens!"), player, false);
                return;
            }

            var (isOnDelay, timeToWait) = Recast.IsOnRecastDelay(player, RecastGroup.StatRebuild);
            if (isOnDelay)
            {
                FloatingTextStringOnCreature(ColorToken.Red($"Another stat rebuild can be performed in {timeToWait}."), player, false);
                return;
            }

            Gui.TogglePlayerWindow(player, GuiWindowType.StatRebuild, null, terminal);
        }

        private const int MaxAbilityIncreases = 15;
        private const int CooldownDays = 14;

        private int _remainingAbilityPoints;
        private int _might;
        private int _perception;
        private int _vitality;
        private int _willpower;
        private int _agility;
        private int _social;

        public string RemainingAbilityPoints
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Might
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Perception
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Vitality
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Willpower
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Agility
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Social
        {
            get => Get<string>();
            set => Set(value);
        }

        private void ResetControls()
        {
            _might = 0;
            _perception = 0;
            _vitality = 0;
            _willpower = 0;
            _agility = 0;
            _social = 0;
            RecalculateAvailableAbilityPoints();
            Might = $"MGT [{_might}]";
            Perception = $"PER [{_perception}]";
            Vitality = $"VIT [{_vitality}]";
            Willpower = $"WIL [{_willpower}]";
            Agility = $"AGI [{_agility}]";
            Social = $"SOC [{_social}]";
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            ResetControls();
        }

        private void RecalculateAvailableAbilityPoints()
        {
            _remainingAbilityPoints = MaxAbilityIncreases - _might - _perception - _vitality - _willpower - _agility - _social;
            RemainingAbilityPoints = $"Attributes - {_remainingAbilityPoints} Points Remaining";
        }

        public Action OnClickSubtractMight() => () =>
        {
            _might--;
            if (_might < 0)
                _might = 0;

            RecalculateAvailableAbilityPoints();
            Might = $"MGT [{_might}]";
        };

        public Action OnClickAddMight() => () =>
        {
            if (_remainingAbilityPoints <= 0)
                return;

            _might++;
            if (_might > 6)
                _might = 6;

            RecalculateAvailableAbilityPoints();
            Might = $"MGT [{_might}]";
        };

        public Action OnClickSubtractPerception() => () =>
        {
            _perception--;
            if (_perception < 0)
                _perception = 0;

            RecalculateAvailableAbilityPoints();
            Perception = $"PER [{_perception}]";
        };

        public Action OnClickAddPerception() => () =>
        {
            if (_remainingAbilityPoints <= 0)
                return;

            _perception++;
            if (_perception > 6)
                _perception = 6;

            RecalculateAvailableAbilityPoints();
            Perception = $"PER [{_perception}]";
        };


        public Action OnClickSubtractVitality() => () =>
        {
            _vitality--;
            if (_vitality < 0)
                _vitality = 0;

            RecalculateAvailableAbilityPoints();
            Vitality = $"VIT [{_vitality}]";
        };

        public Action OnClickAddVitality() => () =>
        {
            if (_remainingAbilityPoints <= 0)
                return;

            _vitality++;
            if (_vitality > 6)
                _vitality = 6;

            RecalculateAvailableAbilityPoints();
            Vitality = $"VIT [{_vitality}]";
        };

        public Action OnClickSubtractWillpower() => () =>
        {
            _willpower--;
            if (_willpower < 0)
                _willpower = 0;

            RecalculateAvailableAbilityPoints();
            Willpower = $"WIL [{_willpower}]";
        };

        public Action OnClickAddWillpower() => () =>
        {
            if (_remainingAbilityPoints <= 0)
                return;

            _willpower++;
            if (_willpower > 6)
                _willpower = 6;

            RecalculateAvailableAbilityPoints();
            Willpower = $"WIL [{_willpower}]";
        };

        public Action OnClickSubtractAgility() => () =>
        {
            _agility--;
            if (_agility < 0)
                _agility = 0;

            RecalculateAvailableAbilityPoints();
            Agility = $"AGI [{_agility}]";
        };

        public Action OnClickAddAgility() => () =>
        {
            if (_remainingAbilityPoints <= 0)
                return;

            _agility++;
            if (_agility > 6)
                _agility = 6;

            RecalculateAvailableAbilityPoints();
            Agility = $"AGI [{_agility}]";
        };

        public Action OnClickSubtractSocial() => () =>
        {
            _social--;
            if (_social < 0)
                _social = 0;

            RecalculateAvailableAbilityPoints();
            Social = $"SOC [{_social}]";
        };

        public Action OnClickAddSocial() => () =>
        {
            if (_remainingAbilityPoints <= 0)
                return;

            _social++;
            if (_social > 6)
                _social = 6;

            RecalculateAvailableAbilityPoints();
            Social = $"SOC [{_social}]";
        };

        public Action OnClickSave() => () =>
        {
            void UnequipAllItems()
            {
                AssignCommand(Player, () => ClearAllActions());
                for (var index = 0; index < NumberOfInventorySlots; index++)
                {
                    var slot = (InventorySlot)index;
                    var item = GetItemInSlot(slot, Player);
                    if (GetIsObjectValid(item)
                        && slot != InventorySlot.CreatureArmor
                        && slot != InventorySlot.CreatureBite
                        && slot != InventorySlot.CreatureLeft
                        && slot != InventorySlot.CreatureRight)
                    {
                        AssignCommand(Player, () =>
                        {
                            ActionUnequipItem(item);
                        });
                    }
                }
            }

            ShowModal($"Are you sure you'd like to save these changes?", () =>
            {
                if (Currency.GetCurrency(Player, CurrencyType.StatRefundToken) <= 0)
                {
                    Gui.CloseWindow(Player, GuiWindowType.StatRebuild, Player);
                    FloatingTextStringOnCreature(ColorToken.Red("Insufficient stat refund tokens!"), Player, false);
                    return;
                }

                var (isOnDelay, timeToWait) = Recast.IsOnRecastDelay(Player, RecastGroup.StatRebuild);
                if (isOnDelay)
                {
                    FloatingTextStringOnCreature(ColorToken.Red($"Another stat rebuild can be performed in {timeToWait}."), Player, false);
                    return;
                }

                UnequipAllItems();

                var playerId = GetObjectUUID(Player);
                var dbPlayer = DB.Get<Player>(playerId);

                CreaturePlugin.SetRawAbilityScore(Player, AbilityType.Might, 10 + _might);
                CreaturePlugin.SetRawAbilityScore(Player, AbilityType.Perception, 10 + _perception);
                CreaturePlugin.SetRawAbilityScore(Player, AbilityType.Vitality, 10 + _vitality);
                CreaturePlugin.SetRawAbilityScore(Player, AbilityType.Willpower, 10 + _willpower);
                CreaturePlugin.SetRawAbilityScore(Player, AbilityType.Agility, 10 + _agility);
                CreaturePlugin.SetRawAbilityScore(Player, AbilityType.Social, 10 + _social);

                dbPlayer.BaseStats[AbilityType.Might] = CreaturePlugin.GetRawAbilityScore(Player, AbilityType.Might);
                dbPlayer.BaseStats[AbilityType.Perception] = CreaturePlugin.GetRawAbilityScore(Player, AbilityType.Perception);
                dbPlayer.BaseStats[AbilityType.Vitality] = CreaturePlugin.GetRawAbilityScore(Player, AbilityType.Vitality);
                dbPlayer.BaseStats[AbilityType.Willpower] = CreaturePlugin.GetRawAbilityScore(Player, AbilityType.Willpower);
                dbPlayer.BaseStats[AbilityType.Agility] = CreaturePlugin.GetRawAbilityScore(Player, AbilityType.Agility);
                dbPlayer.BaseStats[AbilityType.Social] = CreaturePlugin.GetRawAbilityScore(Player, AbilityType.Social);

                dbPlayer.UpgradedStats[AbilityType.Might] = 0;
                dbPlayer.UpgradedStats[AbilityType.Perception] = 0;
                dbPlayer.UpgradedStats[AbilityType.Vitality] = 0;
                dbPlayer.UpgradedStats[AbilityType.Willpower] = 0;
                dbPlayer.UpgradedStats[AbilityType.Agility] = 0;
                dbPlayer.UpgradedStats[AbilityType.Social] = 0;

                dbPlayer.UnallocatedAP = dbPlayer.TotalAPAcquired;
                dbPlayer.RacialStat = AbilityType.Invalid;

                FloatingTextStringOnCreature(ColorToken.Green("Character stat rebuild complete!"), Player, false);

                DB.Set(dbPlayer);
                Gui.CloseWindow(Player, GuiWindowType.StatRebuild, Player);
                Gui.CloseWindow(Player, GuiWindowType.CharacterSheet, Player);

                Currency.TakeCurrency(Player, CurrencyType.StatRefundToken, 1);

                const int DelaySeconds = CooldownDays * 86400;
                Recast.ApplyRecastDelay(Player, RecastGroup.StatRebuild, DelaySeconds, true);
            });
        };
    }
}
