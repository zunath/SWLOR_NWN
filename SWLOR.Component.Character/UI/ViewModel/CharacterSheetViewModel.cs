// todo: Migrate this file to the new system


using Microsoft.Extensions.DependencyInjection;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Component.Character.UI.Payload;
using SWLOR.NWN.API.Contracts;
using SWLOR.Shared.Abstractions.Enums;
using SWLOR.Shared.Domain.Ability.Contracts;
using SWLOR.Shared.Domain.Associate.Contracts;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Communication.Contracts;
using SWLOR.Shared.Domain.Crafting.Enums;
using SWLOR.Shared.Domain.Crafting.Payloads;
using SWLOR.Shared.Domain.Dialog.Contracts;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Skill.Contracts;
using SWLOR.Shared.Domain.Skill.Enums;
using SWLOR.Shared.Domain.Space.Contracts;
using SWLOR.Shared.Domain.UI.Events;
using SWLOR.Shared.Domain.UI.Payloads;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.Character.UI.ViewModel
{
    public class CharacterSheetViewModel: GuiViewModelBase<CharacterSheetViewModel, CharacterSheetPayload>,
        IGuiRefreshable<ChangePortraitRefreshEvent>,
        IGuiRefreshable<SkillXPRefreshEvent>,
        IGuiRefreshable<EquipItemRefreshEvent>,
        IGuiRefreshable<UnequipItemRefreshEvent>,
        IGuiRefreshable<BeastGainXPRefreshEvent>
    {
        private readonly IDatabaseService _db;
        private readonly ICreaturePluginService _creaturePlugin;
        private readonly IStatCalculationService _statService;

        public CharacterSheetViewModel(
            IGuiService guiService, 
            IDatabaseService db, 
            IServiceProvider serviceProvider,
            ICreaturePluginService creaturePlugin,
            IStatCalculationService statService) 
            : base(guiService)
        {
            _db = db;
            _creaturePlugin = creaturePlugin;
            _statService = statService;
            
            // Initialize lazy services
            _skillService = new Lazy<ISkillService>(serviceProvider.GetRequiredService<ISkillService>);
            _itemService = new Lazy<IItemService>(serviceProvider.GetRequiredService<IItemService>);
            _combatService = new Lazy<ICombatService>(serviceProvider.GetRequiredService<ICombatService>);
            _abilityService = new Lazy<IAbilityService>(serviceProvider.GetRequiredService<IAbilityService>);
            _spaceService = new Lazy<ISpaceService>(serviceProvider.GetRequiredService<ISpaceService>);
            _beastMasteryService = new Lazy<IBeastMasteryService>(serviceProvider.GetRequiredService<IBeastMasteryService>);
            _dialogService = new Lazy<IDialogService>(serviceProvider.GetRequiredService<IDialogService>);
            _holoComService = new Lazy<IHoloComService>(serviceProvider.GetRequiredService<IHoloComService>);
            _guiService = new Lazy<IGuiService>(serviceProvider.GetRequiredService<IGuiService>);
        }

        // Lazy-loaded services to break circular dependencies
        private readonly Lazy<ISkillService> _skillService;
        private readonly Lazy<IItemService> _itemService;
        private readonly Lazy<ICombatService> _combatService;
        private readonly Lazy<IAbilityService> _abilityService;
        private readonly Lazy<ISpaceService> _spaceService;
        private readonly Lazy<IBeastMasteryService> _beastMasteryService;
        private readonly Lazy<IDialogService> _dialogService;
        
        private ISkillService SkillService => _skillService.Value;
        private IItemService ItemService => _itemService.Value;
        private ICombatService CombatService => _combatService.Value;
        private IAbilityService AbilityService => _abilityService.Value;
        private ISpaceService SpaceService => _spaceService.Value;
        private IBeastMasteryService BeastMasteryService => _beastMasteryService.Value;
        private IDialogService DialogService => _dialogService.Value;
        private readonly Lazy<IHoloComService> _holoComService;
        private readonly Lazy<IGuiService> _guiService;
        
        private IHoloComService HoloComService => _holoComService.Value;
        private IGuiService GuiService => _guiService.Value;
        
        private const int MaxUpgrades = 10;
        private uint _target;

        public bool IsPlayerMode
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool ShowSP
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool ShowAPOrLevel
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string PortraitResref
        {
            get => Get<string>();
            set => Set(value);
        }

        public string HP
        {
            get => Get<string>();
            set => Set(value);
        }
        public string FP
        {
            get => Get<string>();
            set => Set(value);
        }

        public string STM
        {
            get => Get<string>();
            set => Set(value);
        }

        public string APOrLevelLabel
        {
            get => Get<string>();
            set => Set(value);
        }

        public int Might
        {
            get => Get<int>();
            set => Set(value);
        }

        public int Perception
        {
            get => Get<int>();
            set => Set(value);
        }

        public int Vitality
        {
            get => Get<int>();
            set => Set(value);
        }

        public int Willpower
        {
            get => Get<int>();
            set => Set(value);
        }

        public int Agility
        {
            get => Get<int>();
            set => Set(value);
        }

        public int Social
        {
            get => Get<int>();
            set => Set(value);
        }

        public string SavingThrows
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Name
        {
            get => Get<string>();
            set => Set(value);
        }

        public string MainHandDMG
        {
            get => Get<string>();
            set => Set(value);
        }

        public string OffHandDMG
        {
            get => Get<string>();
            set => Set(value);
        }

        public string MainHandTooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string OffHandTooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public int Attack
        {
            get => Get<int>();
            set => Set(value);
        }

        public int DefensePhysical
        {
            get => Get<int>();
            set => Set(value);
        }

        public int DefenseForce
        {
            get => Get<int>();
            set => Set(value);
        }

        public string DefenseElemental
        {
            get => Get<string>();
            set => Set(value);
        }

        public int Accuracy
        {
            get => Get<int>();
            set => Set(value);
        }

        public int Evasion
        {
            get => Get<int>();
            set => Set(value);
        }

        public string CharacterType
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Race
        {
            get => Get<string>();
            set => Set(value);
        }

        public string SP
        {
            get => Get<string>();
            set => Set(value);
        }

        public string APOrLevel
        {
            get => Get<string>();
            set => Set(value);
        }

        public string APOrLevelTooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Control
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Craftsmanship
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsMightUpgradeAvailable
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsPerceptionUpgradeAvailable
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsVitalityUpgradeAvailable
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsWillpowerUpgradeAvailable
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsAgilityUpgradeAvailable
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsSocialUpgradeAvailable
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsHolocomEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public Action OnClickSkills() => () =>
        {
            GuiService.TogglePlayerWindow(Player, GuiWindowType.Skills);
        };

        public Action OnClickPerks() => () =>
        {
            GuiService.TogglePlayerWindow(Player, GuiWindowType.Perks);
        };

        public Action OnClickChangePortrait() => () =>
        {
            var payload = new CustomizeCharacterPayload(_target);
            GuiService.TogglePlayerWindow(Player, GuiWindowType.CustomizeCharacter, payload);
        };

        public Action OnClickQuests() => () =>
        {
            GuiService.TogglePlayerWindow(Player, GuiWindowType.Quests);
        };

        public Action OnClickRecipes() => () =>
        {
            var payload = new RecipesPayload(RecipesUIModeType.Recipes, SkillType.Invalid);
            GuiService.TogglePlayerWindow(Player, GuiWindowType.Recipes, payload);
        };

        public Action OnClickHoloCom() => () =>
        {
            if (SpaceService.IsPlayerInSpaceMode(Player))
            {
                SendMessageToPC(Player, ColorToken.Red("Holocom cannot be used in space."));
                return;
            }

            HoloComService.StartHoloComDialog(Player);
        };

        public Action OnClickKeyItems() => () =>
        {
            GuiService.TogglePlayerWindow(Player, GuiWindowType.KeyItems);
        };

        public Action OnClickCurrencies() => () =>
        {
            GuiService.TogglePlayerWindow(Player, GuiWindowType.Currencies);
        };

        public Action OnClickAchievements() => () =>
        {
            GuiService.TogglePlayerWindow(Player, GuiWindowType.Achievements);
        };

        public Action OnClickNotes() => () =>
        {
            GuiService.TogglePlayerWindow(Player, GuiWindowType.Notes);
        };

        public Action OnClickOpenTrash() => () =>
        {
            var location = GetLocation(Player);
            var trash = CreateObject(ObjectType.Placeable, "reo_trash_can", location);
            AssignCommand(Player, () => ActionInteractObject(trash));
            DelayCommand(0.2f, () => SetUseableFlag(trash, false));
        };

        public Action OnClickAppearance() => () =>
        {
            var payload = new AppearanceEditorPayload(Player);
            GuiService.TogglePlayerWindow(Player, GuiWindowType.AppearanceEditor, payload);
        };

        public Action OnClickSettings() => () =>
        {
            GuiService.TogglePlayerWindow(Player, GuiWindowType.Settings);
        };

        private void UpgradeAttribute(AbilityType ability, string abilityName)
        {
            var playerId = GetObjectUUID(_target);
            var dbPlayer = _db.Get<Player>(playerId);
            var isRacial = dbPlayer.RacialStat == AbilityType.Invalid;
            var promptMessage = isRacial
                ? "WARNING: You are about to spend your one-time racial stat bonus. Once spent, this action can only be undone with a stat rebuild. Are you SURE you want to upgrade this stat?"
                : $"Upgrading your {abilityName} attribute will consume 1 AP. Are you sure you want to upgrade it?";

            ShowModal(promptMessage, () =>
            {
                if (GetResRef(GetArea(_target)) == "char_migration")
                {
                    FloatingTextStringOnCreature($"Stats cannot be upgraded in this area.", _target, false);
                    return;
                }

                playerId = GetObjectUUID(_target);
                dbPlayer = _db.Get<Player>(playerId);
                isRacial = dbPlayer.RacialStat == AbilityType.Invalid;

                // Racial upgrades do not count toward the 10 cap and they don't reduce AP.
                if (!isRacial)
                {
                    if (dbPlayer.UnallocatedAP <= 0)
                    {
                        FloatingTextStringOnCreature("You do not have enough AP to purchase this upgrade.", _target, false);
                        return;
                    }

                    if (dbPlayer.UpgradedStats[ability] >= MaxUpgrades)
                    {
                        FloatingTextStringOnCreature("You cannot upgrade this attribute any further.", _target, false);
                        return;
                    }

                    dbPlayer.UnallocatedAP--;
                    dbPlayer.UpgradedStats[ability]++;
                }
                else
                {
                    dbPlayer.RacialStat = ability;
                }

                _creaturePlugin.ModifyRawAbilityScore(_target, ability, 1);

                _db.Set(dbPlayer);

                FloatingTextStringOnCreature($"Your {abilityName} attribute has increased!", _target, false);
                LoadData();
            });
        }

        public Action OnClickUpgradeMight() => () =>
        {
            UpgradeAttribute(AbilityType.Might, "Might");
        };

        public Action OnClickUpgradePerception() => () =>
        {
            UpgradeAttribute(AbilityType.Perception, "Perception");
        };

        public Action OnClickUpgradeVitality() => () =>
        {
            UpgradeAttribute(AbilityType.Vitality, "Vitality");
        };

        public Action OnClickUpgradeWillpower() => () =>
        {
            UpgradeAttribute(AbilityType.Willpower, "Willpower");
        };

        public Action OnClickUpgradeAgility() => () =>
        {
            UpgradeAttribute(AbilityType.Agility, "Agility");
        };

        public Action OnClickUpgradeSocial() => () =>
        {
            UpgradeAttribute(AbilityType.Social, "Social");
        };


        private void RefreshStats()
        {
            //HP = GetCurrentHitPoints(_target) + " / " + GetMaxHitPoints(_target);

            //if (GetClassByPosition(1, _target) == ClassType.Standard)
            //{
            //    FP = $"0 / 0";
            //}
            //else
            //{
            //    var currentFP = StatService.GetCurrentFP(_target);
            //    var maxFP = StatService.GetMaxFP(_target);
            //    if (currentFP < 0)
            //        currentFP = 0;
            //    if (maxFP < 0)
            //        maxFP = 0;

            //    FP = $"{currentFP} / {maxFP}";
            //}

            //var currentSTM = StatService.GetCurrentStamina(_target);
            //var maxSTM = StatService.GetMaxStamina(_target);
            //if (currentSTM < 0)
            //    currentSTM = 0;
            //if (maxSTM < 0)
            //    maxSTM = 0;

            //STM = $"{currentSTM} / {maxSTM}";
            //Name = GetName(_target);
            //Might = GetAbilityScore(_target, AbilityType.Might);
            //Perception = GetAbilityScore(_target, AbilityType.Perception);
            //Vitality = GetAbilityScore(_target, AbilityType.Vitality);
            //Willpower = GetAbilityScore(_target, AbilityType.Willpower);
            //Agility = GetAbilityScore(_target, AbilityType.Agility);
            //Social = GetAbilityScore(_target, AbilityType.Social);
            //SavingThrows = GetFortitudeSavingThrow(_target) + "/" +
            //               GetReflexSavingThrow(_target) + "/" +
            //               GetWillSavingThrow(_target);

            //if (IsPlayerMode)
            //{
            //    var playerId = GetObjectUUID(_target);
            //    var dbPlayer = _db.Get<Player>(playerId);

            //    var isRacialBonusAvailable = dbPlayer.RacialStat == AbilityType.Invalid;
            //    IsMightUpgradeAvailable = (dbPlayer.UnallocatedAP > 0 && dbPlayer.UpgradedStats[AbilityType.Might] < MaxUpgrades) || isRacialBonusAvailable;
            //    IsPerceptionUpgradeAvailable = (dbPlayer.UnallocatedAP > 0 && dbPlayer.UpgradedStats[AbilityType.Perception] < MaxUpgrades) || isRacialBonusAvailable;
            //    IsVitalityUpgradeAvailable = (dbPlayer.UnallocatedAP > 0 && dbPlayer.UpgradedStats[AbilityType.Vitality] < MaxUpgrades) || isRacialBonusAvailable;
            //    IsWillpowerUpgradeAvailable = (dbPlayer.UnallocatedAP > 0 && dbPlayer.UpgradedStats[AbilityType.Willpower] < MaxUpgrades) || isRacialBonusAvailable;
            //    IsAgilityUpgradeAvailable = (dbPlayer.UnallocatedAP > 0 && dbPlayer.UpgradedStats[AbilityType.Agility] < MaxUpgrades) || isRacialBonusAvailable;
            //    IsSocialUpgradeAvailable = (dbPlayer.UnallocatedAP > 0 && dbPlayer.UpgradedStats[AbilityType.Social] < MaxUpgrades) || isRacialBonusAvailable;
            //}
        }

        private void RefreshEquipmentStats()
        {
            // Builds a damage estimate using the player's stats as a baseline.
            //(string, string) GetCombatInfo( uint item)
            //{
            //    var itemType = GetBaseItemType(item);
            //    var skill = SkillService.GetSkillTypeByBaseItem(itemType);
            //    int skillRank;

            //    if (GetIsPC(_target))
            //    {
            //        var playerId = GetObjectUUID(_target);
            //        var dbPlayer = _db.Get<Player>(playerId);
            //        skillRank = dbPlayer.Skills[skill].Rank;
            //    }
            //    else
            //    {
            //        var npcStats = StatService.GetNPCStats(_target);
            //        skillRank = npcStats.Level;
            //    }

            //    var damageAbility = ItemService.GetWeaponDamageAbilityType(itemType);
            //    var damageStat = GetAbilityScore(_target, damageAbility);
            //    var dmg = ItemService.GetDMG(item) + CombatService.GetMiscDMGBonus(_target, itemType);
            //    var dmgText = $"{dmg} DMG";
            //    var attack = _statService.CalculateAttack(_target);
            //    var defense = _statService.CalculateDefense(_target);
            //    var (min, max) = CombatService.CalculateDamageRange(attack, dmg, damageStat, defense, damageStat, 0);
            //    var tooltip = $"Est. Damage: {min} - {max}";

            //    return (dmgText, tooltip);
            //}

            //var mainHand = GetItemInSlot(InventorySlotType.RightHand, _target);
            //var offHand = GetItemInSlot(InventorySlotType.LeftHand, _target);
            //var mainHandType = GetBaseItemType(mainHand);

            //if (GetIsObjectValid(mainHand))
            //{
            //    var dmgInfo = GetCombatInfo(mainHand);
            //    MainHandDMG = dmgInfo.Item1;
            //    MainHandTooltip = dmgInfo.Item2;
            //}
            //else
            //{
            //    MainHandDMG = "-";
            //    MainHandTooltip = "Est. Damage: N/A";
            //}

            //if (GetIsObjectValid(offHand))
            //{
            //    var dmgInfo = GetCombatInfo(offHand);
            //    OffHandDMG = dmgInfo.Item1;
            //    OffHandTooltip = dmgInfo.Item2;
            //}
            //else
            //{
            //    OffHandDMG = "-";
            //    OffHandTooltip = "Est. Damage: N/A";
            //}

            //AbilityType damageStat;
            //AbilityType accuracyStatOverride;

            //if (BeastMasteryService.IsPlayerBeast(_target))
            //{
            //    var beastType = BeastMasteryService.GetBeastType(_target);
            //    var beastDetails = BeastMasteryService.GetBeastDetail(beastType);
            //    damageStat = beastDetails.DamageStat;
            //    accuracyStatOverride = beastDetails.AccuracyStat;
            //    mainHand = GetItemInSlot(InventorySlotType.CreatureArmor, _target);
            //}
            //else
            //{
            //    damageStat = ItemService.GetWeaponDamageAbilityType(mainHandType);
            //    accuracyStatOverride = AbilityType.Invalid;

            //    // Strong Style (Lightsaber)
            //    if (ItemService.LightsaberBaseItemTypes.Contains(mainHandType) &&
            //        AbilityService.IsAbilityToggled(_target, AbilityToggleType.StrongStyleLightsaber))
            //    {
            //        damageStat = AbilityType.Might;
            //        accuracyStatOverride = AbilityType.Perception;
            //    }
            //    // Strong Style (Saberstaff)
            //    if (ItemService.SaberstaffBaseItemTypes.Contains(mainHandType) &&
            //        AbilityService.IsAbilityToggled(_target, AbilityToggleType.StrongStyleSaberstaff))
            //    {
            //        damageStat = AbilityType.Might;
            //        accuracyStatOverride = AbilityType.Perception;
            //    }

            //    // Flurry Style (Staff)
            //    if (ItemService.StaffBaseItemTypes.Contains(mainHandType) &&
            //        GetHasFeat(FeatType.FlurryStyle, _target))
            //    {
            //        damageStat = AbilityType.Perception;
            //        accuracyStatOverride = AbilityType.Agility;
            //    }
            //}
            
            //var mainHandSkill = SkillService.GetSkillTypeByBaseItem(mainHandType);
            //Attack = StatService.GetAttack(_target, damageStat, mainHandSkill);
            //DefensePhysical = StatService.GetDefense(_target, CombatDamageType.Physical, AbilityType.Vitality);
            //DefenseForce = StatService.GetDefense(_target, CombatDamageType.Force, AbilityType.Willpower);
            
            //if (GetIsPC(_target))
            //{
            //    var playerId = GetObjectUUID(_target);
            //    var dbPlayer = _db.Get<Player>(playerId);

            //    var fireDefense = (dbPlayer.Defenses[CombatDamageType.Fire] + food.DefenseFire).ToString();
            //    var poisonDefense = (dbPlayer.Defenses[CombatDamageType.Poison] + food.DefensePoison).ToString();
            //    var electricalDefense = (dbPlayer.Defenses[CombatDamageType.Electrical + food.DefenseElectrical]).ToString();
            //    var iceDefense = (dbPlayer.Defenses[CombatDamageType.Ice] + food.DefenseIce).ToString();

            //    DefenseElemental = $"{fireDefense}/{poisonDefense}/{electricalDefense}/{iceDefense}";
            //}
            //else
            //{
            //    var npcStats = StatService.GetNPCStats(_target);
            //    var fireDefense = npcStats.Defenses.ContainsKey(CombatDamageType.Fire) ? npcStats.Defenses[CombatDamageType.Fire] : 0;
            //    var poisonDefense = npcStats.Defenses.ContainsKey(CombatDamageType.Poison) ? npcStats.Defenses[CombatDamageType.Poison] : 0;
            //    var electricalDefense = npcStats.Defenses.ContainsKey(CombatDamageType.Electrical) ? npcStats.Defenses[CombatDamageType.Electrical] : 0;
            //    var iceDefense = npcStats.Defenses.ContainsKey(CombatDamageType.Ice) ? npcStats.Defenses[CombatDamageType.Ice] : 0;

            //    DefenseElemental = $"{fireDefense}/{poisonDefense}/{electricalDefense}/{iceDefense}";
            //}

            //Accuracy = StatService.GetAccuracy(_target, mainHand, accuracyStatOverride, SkillType.Invalid);
            //Evasion = StatService.GetEvasion(_target, SkillType.Invalid);

            //var smithery = StatService.CalculateControl(_target, SkillType.Smithery);
            //var engineering = StatService.CalculateControl(_target, SkillType.Engineering);
            //var fabrication = StatService.CalculateControl(_target, SkillType.Fabrication);
            //var agriculture = StatService.CalculateControl(_target, SkillType.Agriculture);

            //Control = $"{smithery}/{engineering}/{fabrication}/{agriculture}";

            //smithery = StatService.CalculateCraftsmanship(_target, SkillType.Smithery);
            //engineering = StatService.CalculateCraftsmanship(_target, SkillType.Engineering);
            //fabrication = StatService.CalculateCraftsmanship(_target, SkillType.Fabrication);
            //agriculture = StatService.CalculateCraftsmanship(_target, SkillType.Agriculture);
            //Craftsmanship = $"{smithery}/{engineering}/{fabrication}/{agriculture}";
        }

        private void RefreshAttributes()
        {
            if (GetIsPC(_target))
            {
                var playerId = GetObjectUUID(_target);
                var dbPlayer = _db.Get<Player>(playerId);

                SP = $"{dbPlayer.TotalSPAcquired} / {SkillService.SkillCap} ({dbPlayer.UnallocatedSP})";
                APOrLevel = $"{dbPlayer.TotalAPAcquired} / {SkillService.APCap} ({dbPlayer.UnallocatedAP})";
            }
            else if (BeastMasteryService.IsPlayerBeast(_target))
            {
                var beastId = BeastMasteryService.GetBeastId(_target);
                var dbBeast = _db.Get<Beast>(beastId);

                SP = $"{dbBeast.Level} / {BeastMasteryService.MaxLevel} ({dbBeast.UnallocatedSP})";
                APOrLevel = $"{dbBeast.Level} / {BeastMasteryService.MaxLevel}";
                APOrLevelTooltip = $"XP: {dbBeast.XP} / {BeastMasteryService.GetRequiredXP(dbBeast.Level, dbBeast.XPPenaltyPercent)}";
            }
        }

        private void RefreshPortrait()
        {
            PortraitResref = GetPortraitResRef(_target) + "l";
        }

        private void LoadData()
        {
            CharacterType = GetClassByPosition(1, _target) == ClassType.Standard ? "Standard" : "Force Sensitive";
            Race = GetStringByStrRef(Convert.ToInt32(Get2DAString("racialtypes", "Name", (int)GetRacialType(_target))), GetGender(_target));
            IsHolocomEnabled = !SpaceService.IsPlayerInSpaceMode(_target);

            if (IsPlayerMode)
            {
                APOrLevelLabel = "AP";
                APOrLevelTooltip = "Ability Points - Used to increase your attributes.";
            }
            else
            {
                APOrLevelLabel = "Level";
            }

            RefreshPortrait();
            RefreshStats();
            RefreshEquipmentStats();
            RefreshAttributes();
        }
        
        protected override void Initialize(CharacterSheetPayload initialPayload)
        {
            _target = GetIsObjectValid(initialPayload.Target) ? initialPayload.Target : Player;
            IsPlayerMode = initialPayload.IsPlayerMode;
            ShowSP = IsPlayerMode || BeastMasteryService.IsPlayerBeast(_target);
            ShowAPOrLevel = ShowSP;

            LoadData();
        }

        public void Refresh(ChangePortraitRefreshEvent payload)
        {
            RefreshPortrait();
        }

        public void Refresh(SkillXPRefreshEvent payload)
        {
            if (!GetIsPC(_target))
                return;

            var playerId = GetObjectUUID(_target);
            var dbPlayer = _db.Get<Player>(playerId);

            SP = $"{dbPlayer.TotalSPAcquired} / {SkillService.SkillCap} ({dbPlayer.UnallocatedSP})";
            APOrLevel = $"{dbPlayer.TotalAPAcquired} / {SkillService.APCap} ({dbPlayer.UnallocatedAP})";
            
            RefreshStats();
        }

        public void Refresh(BeastGainXPRefreshEvent payload)
        {
            if (!BeastMasteryService.IsPlayerBeast(_target))
                return;

            var beastId = BeastMasteryService.GetBeastId(_target);
            var dbBeast = _db.Get<Beast>(beastId);

            SP = $"{dbBeast.Level} / {BeastMasteryService.MaxLevel} ({dbBeast.UnallocatedSP})";
            APOrLevel = $"{dbBeast.Level} / {BeastMasteryService.MaxLevel}";
            APOrLevelTooltip = $"XP: {dbBeast.XP} / {BeastMasteryService.GetRequiredXP(dbBeast.Level, dbBeast.XPPenaltyPercent)}";
        }

        public void Refresh(EquipItemRefreshEvent payload)
        {
            RefreshEquipmentStats();
        }

        public void Refresh(UnequipItemRefreshEvent payload)
        {
            RefreshStats();
            RefreshEquipmentStats();
        }

        //public void Refresh(StatusEffectReceivedRefreshEvent payload)
        //{
        //    RefreshStats();
        //    RefreshEquipmentStats();
        //}

        //public void Refresh(StatusEffectRemovedRefreshEvent payload)
        //{
        //    RefreshStats();
        //    RefreshEquipmentStats();
        //}
    }
}
