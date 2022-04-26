using System;
using Discord;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.SkillService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using Skill = SWLOR.Game.Server.Service.Skill;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class CharacterSheetViewModel: GuiViewModelBase<CharacterSheetViewModel, GuiPayloadBase>,
        IGuiRefreshable<ChangePortraitRefreshEvent>,
        IGuiRefreshable<SkillXPRefreshEvent>,
        IGuiRefreshable<EquipItemRefreshEvent>,
        IGuiRefreshable<UnequipItemRefreshEvent>
    {
        private const int MaxUpgrades = 10;

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

        public int Social
        {
            get => Get<int>();
            set => Set(value);
        }

        public string Name
        {
            get => Get<string>();
            set => Set(value);
        }

        public int Attack
        {
            get => Get<int>();
            set => Set(value);
        }

        public int ForceAttack
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

        public string AP
        {
            get => Get<string>();
            set => Set(value);
        }

        public int Control
        {
            get => Get<int>();
            set => Set(value);
        }

        public int Craftsmanship
        {
            get => Get<int>();
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

        public bool IsSocialUpgradeAvailable
        {
            get => Get<bool>();
            set => Set(value);
        }

        public Action OnClickSkills() => () =>
        {
            Gui.TogglePlayerWindow(Player, GuiWindowType.Skills);
        };

        public Action OnClickPerks() => () =>
        {
            Gui.TogglePlayerWindow(Player, GuiWindowType.Perks);
        };

        public Action OnClickChangePortrait() => () =>
        {
            Gui.TogglePlayerWindow(Player, GuiWindowType.ChangePortrait);
        };

        public Action OnClickQuests() => () =>
        {
            Gui.TogglePlayerWindow(Player, GuiWindowType.Quests);
        };

        public Action OnClickRecipes() => () =>
        {
            Gui.TogglePlayerWindow(Player, GuiWindowType.Recipes);
        };

        public Action OnClickKeyItems() => () =>
        {
            Gui.TogglePlayerWindow(Player, GuiWindowType.KeyItems);
        };

        public Action OnClickAchievements() => () =>
        {
            Gui.TogglePlayerWindow(Player, GuiWindowType.Achievements);
        };

        public Action OnClickNotes() => () =>
        {
            Gui.TogglePlayerWindow(Player, GuiWindowType.Notes);
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
            Gui.TogglePlayerWindow(Player, GuiWindowType.AppearanceEditor);
        };

        public Action OnClickSettings() => () =>
        {
            Gui.TogglePlayerWindow(Player, GuiWindowType.Settings);
        };

        private void UpgradeAttribute(AbilityType ability, string abilityName)
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);

            if (dbPlayer.UnallocatedAP <= 0)
            {
                FloatingTextStringOnCreature("You do not have enough AP to purchase this upgrade.", Player, false);
                return;
            }

            if (dbPlayer.UpgradedStats[ability] >= MaxUpgrades)
            {
                FloatingTextStringOnCreature("You cannot upgrade this attribute any further.", Player, false);
                return;
            }

            dbPlayer.UnallocatedAP--;
            dbPlayer.UpgradedStats[ability]++;
            CreaturePlugin.ModifyRawAbilityScore(Player, ability, 1);

            DB.Set(dbPlayer);

            FloatingTextStringOnCreature($"Your {abilityName} attribute has increased!", Player, false);
            LoadData();
        }

        public Action OnClickUpgradeMight() => () =>
        {
            ShowModal($"Upgrading your Might attribute will consume 1 AP. Are you sure you want to upgrade it?", () =>
            {
                UpgradeAttribute(AbilityType.Might, "Might");
            });
        };

        public Action OnClickUpgradePerception() => () =>
        {
            ShowModal($"Upgrading your Perception attribute will consume 1 AP. Are you sure you want to upgrade it?", () =>
            {
                UpgradeAttribute(AbilityType.Perception, "Perception");
            });
        };

        public Action OnClickUpgradeVitality() => () =>
        {
            ShowModal($"Upgrading your Vitality attribute will consume 1 AP. Are you sure you want to upgrade it?", () =>
            {
                UpgradeAttribute(AbilityType.Vitality, "Vitality");
            });
        };

        public Action OnClickUpgradeWillpower() => () =>
        {
            ShowModal($"Upgrading your Willpower attribute will consume 1 AP. Are you sure you want to upgrade it?", () =>
            {
                UpgradeAttribute(AbilityType.Willpower, "Willpower");
            });
        };

        public Action OnClickUpgradeSocial() => () =>
        {
            ShowModal($"Upgrading your Social attribute will consume 1 AP. Are you sure you want to upgrade it?", () =>
            {
                UpgradeAttribute(AbilityType.Social, "Social");
            });
        };

        private void RefreshStats(Player dbPlayer)
        {
            HP = GetCurrentHitPoints(Player) + " / " + GetMaxHitPoints(Player);

            if (dbPlayer.CharacterType == Enumeration.CharacterType.Standard)
            {
                FP = $"0 / 0";
            }
            else
            {
                FP = Stat.GetCurrentFP(Player, dbPlayer) + " / " + Stat.GetMaxFP(Player, dbPlayer);
            }

            STM = Stat.GetCurrentStamina(Player, dbPlayer) + " / " + Stat.GetMaxStamina(Player, dbPlayer);
            Name = GetName(Player);
            Might = GetAbilityScore(Player, AbilityType.Might);
            Perception = GetAbilityScore(Player, AbilityType.Perception);
            Vitality = GetAbilityScore(Player, AbilityType.Vitality);
            Willpower = GetAbilityScore(Player, AbilityType.Willpower);
            Social = GetAbilityScore(Player, AbilityType.Social);

            IsMightUpgradeAvailable = dbPlayer.UnallocatedAP > 0 && dbPlayer.UpgradedStats[AbilityType.Might] < MaxUpgrades;
            IsPerceptionUpgradeAvailable = dbPlayer.UnallocatedAP > 0 && dbPlayer.UpgradedStats[AbilityType.Perception] < MaxUpgrades;
            IsVitalityUpgradeAvailable = dbPlayer.UnallocatedAP > 0 && dbPlayer.UpgradedStats[AbilityType.Vitality] < MaxUpgrades;
            IsWillpowerUpgradeAvailable = dbPlayer.UnallocatedAP > 0 && dbPlayer.UpgradedStats[AbilityType.Willpower] < MaxUpgrades;
            IsSocialUpgradeAvailable = dbPlayer.UnallocatedAP > 0 && dbPlayer.UpgradedStats[AbilityType.Social] < MaxUpgrades;
        }

        private void RefreshEquipmentStats(Player dbPlayer)
        {
            Attack = dbPlayer.Attack;
            ForceAttack = dbPlayer.ForceAttack;
            DefensePhysical = Stat.GetDefense(Player, CombatDamageType.Physical, AbilityType.Vitality);
            DefenseForce = Stat.GetDefense(Player, CombatDamageType.Force, AbilityType.Willpower);

            var fireDefense = dbPlayer.Defenses[CombatDamageType.Fire].ToString();
            var poisonDefense = dbPlayer.Defenses[CombatDamageType.Poison].ToString();
            var electricalDefense = dbPlayer.Defenses[CombatDamageType.Electrical].ToString();
            var iceDefense = dbPlayer.Defenses[CombatDamageType.Ice].ToString();
            DefenseElemental = $"{fireDefense}/{poisonDefense}/{electricalDefense}/{iceDefense}";
            Evasion = CreaturePlugin.GetBaseAC(Player);
            Control = dbPlayer.Control;
            Craftsmanship = dbPlayer.Craftsmanship;
        }

        private void RefreshAttributes(Player dbPlayer)
        {
            SP = $"{dbPlayer.TotalSPAcquired} / {Skill.SkillCap} ({dbPlayer.UnallocatedSP})";
            AP = $"{dbPlayer.TotalAPAcquired / 10} / 30 ({dbPlayer.UnallocatedAP})";
        }

        private void RefreshPortrait()
        {
            PortraitResref = GetPortraitResRef(Player) + "l";
        }

        private void LoadData()
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);
            CharacterType = dbPlayer.CharacterType == Enumeration.CharacterType.Standard ? "Standard" : "Force Sensitive";
            Race = GetStringByStrRef(Convert.ToInt32(Get2DAString("racialtypes", "Name", (int)GetRacialType(Player))), GetGender(Player));

            RefreshPortrait();
            RefreshStats(dbPlayer);
            RefreshEquipmentStats(dbPlayer);
            RefreshAttributes(dbPlayer);
        }
        
        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            LoadData();
        }

        public void Refresh(ChangePortraitRefreshEvent payload)
        {
            RefreshPortrait();
        }

        public void Refresh(SkillXPRefreshEvent payload)
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);

            SP = $"{dbPlayer.TotalSPAcquired} / {Skill.SkillCap} ({dbPlayer.UnallocatedSP})";
            AP = $"{dbPlayer.TotalAPAcquired / 10} / 30 ({dbPlayer.UnallocatedAP})";
        }

        public void Refresh(EquipItemRefreshEvent payload)
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);
            if (dbPlayer == null)
                return;

            RefreshEquipmentStats(dbPlayer);
        }

        public void Refresh(UnequipItemRefreshEvent payload)
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);
            if (dbPlayer == null)
                return;

            RefreshStats(dbPlayer);
            RefreshEquipmentStats(dbPlayer);
        }
    }
}
