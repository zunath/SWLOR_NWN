using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Perk.Contracts;
using SWLOR.Component.Perk.Service;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Character.ValueObjects;
using SWLOR.Shared.Domain.Common.Contracts;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Component.Perk.Feature.PerkDefinition
{
    public class OneHandedPerkDefinition : IPerkListDefinition
    {
        private readonly IServiceProvider _serviceProvider;

        public OneHandedPerkDefinition(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        private IRandomService Random => _serviceProvider.GetRequiredService<IRandomService>();
        private IItemService ItemService => _serviceProvider.GetRequiredService<IItemService>();
        private IStatService StatService => _serviceProvider.GetRequiredService<IStatService>();
        private IPerkService PerkService => _serviceProvider.GetRequiredService<IPerkService>();
        private IAbilityService AbilityService => _serviceProvider.GetRequiredService<IAbilityService>();
                public Dictionary<PerkType, PerkDetail> BuildPerks(IPerkBuilder builder)
        {
            Doublehand(builder);
            DualWield(builder);
            WeaponFocusVibroblades(builder);
            ImprovedCriticalVibroblades(builder);
            VibrobladeProficiency(builder);
            VibrobladeMastery(builder);
            HackingBlade(builder);
            RiotBlade(builder);
            WeaponFocusFinesseVibroblades(builder);
            ImprovedCriticalFinesseVibroblades(builder);
            FinesseVibrobladeProficiency(builder);
            FinesseVibrobladeMastery(builder);
            PoisonStab(builder);
            Backstab(builder);
            WeaponFocusLightsabers(builder);
            ImprovedCriticalLightsabers(builder);
            LightsaberProficiency(builder);
            LightsaberMastery(builder);
            ForceLeap(builder);
            SaberStrike(builder);
            ImprovedTwoWeaponFighting(builder);
            StrongStyleLightsaber(builder);
            Duelist(builder);
            WailingBlows(builder);
            ShieldMaster(builder);
            ShieldBash(builder);
            Bulwark(builder);
            ShieldResistance(builder);
            Alacrity(builder);
            Clarity(builder);

            return builder.Build();
        }

        private void Doublehand(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedGeneral, PerkType.Doublehand)
                .Name("Doublehand")

                .AddPerkLevel()
                .Description("Increases DMG of one-handed weapons by 2 when no off-hand item is equipped. [Cross Skill]")
                .Price(1)
                .DroidAISlots(2)
                .GrantsFeat(FeatType.Doublehand1)

                .AddPerkLevel()
                .Description("Increases DMG of one-handed weapons by 6 when no off-hand item is equipped. [Cross Skill]")
                .Price(1)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.OneHanded, 10)
                .GrantsFeat(FeatType.Doublehand2)

                .AddPerkLevel()
                .Description("Increases DMG of one-handed weapons by 10 when no off-hand item is equipped. [Cross Skill]")
                .Price(1)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.OneHanded, 20)
                .GrantsFeat(FeatType.Doublehand3)

                .AddPerkLevel()
                .Description("Increases DMG of one-handed weapons by 14 when no off-hand item is equipped. [Cross Skill]")
                .Price(1)
                .DroidAISlots(5)
                .RequirementSkill(SkillType.OneHanded, 30)
                .GrantsFeat(FeatType.Doublehand4)

                .AddPerkLevel()
                .Description("Increases DMG of one-handed weapons by 19 when no off-hand item is equipped. [Cross Skill]")
                .Price(2)
                .DroidAISlots(6)
                .RequirementSkill(SkillType.OneHanded, 40)
                .GrantsFeat(FeatType.Doublehand5);
        }

        private void DualWield(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedGeneral, PerkType.DualWield)
                .Name("Dual Wield")

                .AddPerkLevel()
                .Description("Enables the use of two one-handed weapons at the same time at -10%/-10% to hit. [Cross Skill]")
                .Price(4)
                .DroidAISlots(1)
                .RequirementSkill(SkillType.OneHanded, 15)
                .GrantsFeat(FeatType.DualWield);
        }

        private void WailingBlows(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedGeneral, PerkType.WailingBlows)
                .Name("Wailing Blows")

                .AddPerkLevel()
                .Description("While dual-wielding one-handed weapons, you gain 15% critical chance.")
                .Price(4)
                .RequirementSkill(SkillType.OneHanded, 25)
                .GrantsFeat(FeatType.WailingBlows)

                .TriggerEquippedItem((player, item, slot, type, level) =>
                 {
                     if (slot != InventorySlot.RightHand) return;

                     StatService.ApplyCritModifier(player, item);
                 })
                .TriggerUnequippedItem((player, item, slot, type, level) =>
                {
                    if (slot != InventorySlot.RightHand) return;

                    StatService.ApplyCritModifier(player, OBJECT_INVALID);
                })
                .TriggerPurchase((player) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    StatService.ApplyCritModifier(player, item);
                })
                .TriggerRefund((player) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    StatService.ApplyCritModifier(player, item);
                });
        }

        private void Duelist(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedGeneral, PerkType.Duelist)
                .Name("Duelist")

                .AddPerkLevel()
                .Description("While wielding one-handed weapons with a shield or free hand, you gain 5% to hit and 5% critical chance. [Cross Skill]")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 15)
                .GrantsFeat(FeatType.Duelist)

                .TriggerEquippedItem((player, item, slot, type, level) =>
                 {
                     if (slot != InventorySlot.RightHand) return;

                     StatService.ApplyCritModifier(player, item);
                 })
                .TriggerUnequippedItem((player, item, slot, type, level) =>
                {
                    if (slot != InventorySlot.RightHand) return;

                    StatService.ApplyCritModifier(player, OBJECT_INVALID);
                })
                .TriggerPurchase((player) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    StatService.ApplyCritModifier(player, item);
                })
                .TriggerRefund((player) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    StatService.ApplyCritModifier(player, item);
                });
        }

        private void ShieldMaster(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedShield, PerkType.ShieldMaster)
                .Name("Shield Master")
                .TriggerEquippedItem((player, item, slot, type, level) =>
                {
                    if (slot == InventorySlot.RightHand)
                    {
                        var offHand = GetItemInSlot(InventorySlot.LeftHand, player);
                        StatService.ApplyAttacksPerRound(player, item, offHand);
                    }
                    else if (slot == InventorySlot.LeftHand)
                    {
                        var mainHand = GetItemInSlot(InventorySlot.RightHand, player);
                        StatService.ApplyAttacksPerRound(player, mainHand, item);
                    }
                })
                .TriggerUnequippedItem((player, item, slot, type, level) =>
                {
                    if (slot == InventorySlot.RightHand)
                    {
                        var offHand = GetItemInSlot(InventorySlot.LeftHand, player);
                        StatService.ApplyAttacksPerRound(player, item, offHand);
                    }
                    else if (slot == InventorySlot.LeftHand)
                    {
                        var mainHand = GetItemInSlot(InventorySlot.RightHand, player);
                        StatService.ApplyAttacksPerRound(player, mainHand, item);
                    }

                    StatService.ApplyAttacksPerRound(player, OBJECT_INVALID);
                })
                .TriggerPurchase((player) =>
                {
                    var mainHand = GetItemInSlot(InventorySlot.RightHand, player);
                    var offHand = GetItemInSlot(InventorySlot.LeftHand, player);
                    StatService.ApplyAttacksPerRound(player, mainHand, offHand);
                })
                .TriggerRefund((player) =>
                {
                    var mainHand = GetItemInSlot(InventorySlot.RightHand, player);
                    var offHand = GetItemInSlot(InventorySlot.LeftHand, player);
                    StatService.ApplyAttacksPerRound(player, mainHand, offHand);
                })

                .AddPerkLevel()
                .Description("While equipped with a shield, you gain an additional attack with your main-hand weapon.")
                .Price(4)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.OneHanded, 30)
                .GrantsFeat(FeatType.ShieldMaster);
        }

        private void ShieldBash(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedShield, PerkType.ShieldBash)
                .Name("Shield Bash")

                .AddPerkLevel()
                .Description("Bashes an enemy for 8 DMG and has a DC12 Will check to inflict Dazed for 3 seconds.")
                .Price(2)
                .DroidAISlots(1)
                .RequirementSkill(SkillType.OneHanded, 5)
                .GrantsFeat(FeatType.ShieldBash1)

                .AddPerkLevel()
                .Description("Bashes an enemy for 16 DMG and has a DC14 Will check to inflict Dazed for 3 seconds.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.OneHanded, 20)
                .GrantsFeat(FeatType.ShieldBash2)

                .AddPerkLevel()
                .Description("Bashes an enemy for 24 DMG and has a DC16 Will check to inflict Dazed for 3 seconds.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.OneHanded, 35)
                .GrantsFeat(FeatType.ShieldBash3);
        }

        private void Bulwark(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedShield, PerkType.Bulwark)
                .Name("Bulwark")

                .AddPerkLevel()
                .Description("While equipped with a shield, you automatically attempt to deflect ranged attacks once per round.")
                .Price(3)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.OneHanded, 10)
                .GrantsFeat(FeatType.Bulwark);
        }

        private void ShieldResistance(IPerkBuilder builder)
        {
            void AdjustSavingThrows(uint player, uint item)
            {
                CreaturePlugin.SetBaseSavingThrow(player, SavingThrow.Fortitude, StatService.CalculateBaseSavingThrow(player, SavingThrow.Fortitude, item));
                CreaturePlugin.SetBaseSavingThrow(player, SavingThrow.Will, StatService.CalculateBaseSavingThrow(player, SavingThrow.Will, item));
                CreaturePlugin.SetBaseSavingThrow(player, SavingThrow.Reflex, StatService.CalculateBaseSavingThrow(player, SavingThrow.Reflex, item));
            }

            builder.Create(PerkCategoryType.OneHandedShield, PerkType.ShieldResistance)
                .Name("Shield Resistance")

                .AddPerkLevel()
                .Description("Grants +1 to Will, Fortitude, and Reflex saves when equipped with a shield.")
                .Price(2)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.OneHanded, 20)

                .AddPerkLevel()
                .Description("Grants +2 to Will, Fortitude, and Reflex saves when equipped with a shield.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.OneHanded, 40)
                
                .TriggerEquippedItem((player, item, slot, type, level) =>
                {
                    var itemType = GetBaseItemType(item);
                    if (slot == InventorySlot.LeftHand &&
                        ItemService.ShieldBaseItemTypes.Contains(itemType))
                    {
                        AdjustSavingThrows(player, item);
                    }
                })
                .TriggerUnequippedItem((player, item, slot, type, level) =>
                {
                    var itemType = GetBaseItemType(item);
                    if (slot == InventorySlot.LeftHand &&
                        ItemService.ShieldBaseItemTypes.Contains(itemType))
                    {
                        AdjustSavingThrows(player, OBJECT_INVALID);
                    }
                })
                .TriggerPurchase((player) =>
                {
                    var item = GetItemInSlot(InventorySlot.LeftHand, player);
                    AdjustSavingThrows(player, item);
                })
                .TriggerRefund((player) =>
                {
                    var item = GetItemInSlot(InventorySlot.LeftHand, player);
                    AdjustSavingThrows(player, item);
                });
        }


        private void Alacrity(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedShield, PerkType.Alacrity)
                .Name("Alacrity")

                .AddPerkLevel()
                .Description("Grants a 10% chance to restore 4 STM when damaged while equipped with a shield.")
                .Price(2)
                .DroidAISlots(3)
                .RequirementCannotHavePerk(PerkType.Clarity)
                .RequirementSkill(SkillType.OneHanded, 25);
        }

        private void Clarity(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedShield, PerkType.Clarity)
                .Name("Clarity")

                .AddPerkLevel()
                .Description("Grants a 10% chance to restore 4 FP when damaged while equipped with a shield.")
                .Price(2)
                .RequirementCannotHavePerk(PerkType.Alacrity)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .RequirementSkill(SkillType.OneHanded, 25);
        }

        private void WeaponFocusVibroblades(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedVibroblade, PerkType.WeaponFocusVibroblades)
                .Name("Weapon Focus - Vibroblades")

                .AddPerkLevel()
                .Description("Your accuracy with vibroblades is increased by 5.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 5)
                .GrantsFeat(FeatType.WeaponFocusVibroblades)

                .AddPerkLevel()
                .Description("Your base damage with vibroblades is increased by 2 DMG.")
                .Price(4)
                .RequirementSkill(SkillType.OneHanded, 15)
                .GrantsFeat(FeatType.WeaponSpecializationVibroblades);
        }

        private void ImprovedCriticalVibroblades(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedVibroblade, PerkType.ImprovedCriticalVibroblades)
                .Name("Improved Critical - Vibroblades")

                .AddPerkLevel()
                .Description("Improves the chance to critically hit with vibroblades by 5%.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 25)
                .GrantsFeat(FeatType.ImprovedCriticalVibroblades);
        }

        private void VibrobladeProficiency(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedVibroblade, PerkType.VibrobladeProficiency)
                .Name("Vibroblade Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Vibroblades.")
                .Price(2)
                .GrantsFeat(FeatType.VibrobladeProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Vibroblades.")
                .Price(2)
                .RequirementSkill(SkillType.OneHanded, 10)
                .GrantsFeat(FeatType.VibrobladeProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Vibroblades.")
                .Price(2)
                .RequirementSkill(SkillType.OneHanded, 20)
                .GrantsFeat(FeatType.VibrobladeProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Vibroblades.")
                .Price(2)
                .RequirementSkill(SkillType.OneHanded, 30)
                .GrantsFeat(FeatType.VibrobladeProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Vibroblades.")
                .Price(2)
                .RequirementSkill(SkillType.OneHanded, 40)
                .GrantsFeat(FeatType.VibrobladeProficiency5);
        }

        private void VibrobladeMastery(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedVibroblade, PerkType.VibrobladeMastery)
                .Name("Vibroblade Mastery")
                .TriggerEquippedItem((player, item, slot, type, level) =>
                {
                    if (slot != InventorySlot.RightHand) return;

                    StatService.ApplyAttacksPerRound(player, item);
                })
                .TriggerUnequippedItem((player, item, slot, type, level) =>
                {
                    if (slot != InventorySlot.RightHand) return;

                    StatService.ApplyAttacksPerRound(player, OBJECT_INVALID);
                })
                .TriggerPurchase((player) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    StatService.ApplyAttacksPerRound(player, item);
                })
                .TriggerRefund((player) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    StatService.ApplyAttacksPerRound(player, item);
                })

                .AddPerkLevel()
                .Description("Grants an additional attack when equipped with a Vibroblade.")
                .Price(8)
                .RequirementSkill(SkillType.OneHanded, 25)
                .GrantsFeat(FeatType.VibrobladeMastery1)
                
                .AddPerkLevel()
                .Description("Grants an additional attack when equipped with a Vibroblade.")
                .Price(8)
                .RequirementSkill(SkillType.OneHanded, 50)
                .GrantsFeat(FeatType.VibrobladeMastery2);
        }

        private void HackingBlade(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedVibroblade, PerkType.HackingBlade)
                .Name("Hacking Blade")

                .AddPerkLevel()
                .Description("Your next attack deals an additional 6 DMG and has a DC10 Fortitude check to inflict Bleed for 30 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 15)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.HackingBlade1)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 15 DMG and has a DC15 Fortitude check to inflict Bleed for 1 minute.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 30)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.HackingBlade2)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 22 DMG and has a DC20 Fortitude check to inflict Bleed for 1 minute.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 45)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.HackingBlade3);
        }

        private void RiotBlade(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedVibroblade, PerkType.RiotBlade)
                .Name("Riot Blade")

                .AddPerkLevel()
                .Description("Instantly deals 10 DMG to your target.")
                .Price(2)
                .RequirementSkill(SkillType.OneHanded, 5)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.RiotBlade1)

                .AddPerkLevel()
                .Description("Instantly deals 20 DMG to your target.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 20)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.RiotBlade2)

                .AddPerkLevel()
                .Description("Instantly deals 30 DMG to your target.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 35)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.RiotBlade3);
        }

        private void WeaponFocusFinesseVibroblades(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedFinesseVibroblade, PerkType.WeaponFocusFinesseVibroblades)
                .Name("Weapon Focus - Finesse Vibroblades")

                .AddPerkLevel()
                .Description("Your accuracy with finesse vibroblades is increased by 5.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 5)
                .GrantsFeat(FeatType.WeaponFocusFinesseVibroblades)

                .AddPerkLevel()
                .Description("Your base damage with finesse vibroblades is increased by 2 DMG.")
                .Price(4)
                .RequirementSkill(SkillType.OneHanded, 15)
                .GrantsFeat(FeatType.WeaponSpecializationFinesseVibroblades);
        }

        private void ImprovedCriticalFinesseVibroblades(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedFinesseVibroblade, PerkType.ImprovedCriticalFinesseVibroblades)
                .Name("Improved Critical - Finesse Vibroblades")

                .AddPerkLevel()
                .Description("Improves the chance to critically hit with finesse vibroblades by 5%.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 25)
                .GrantsFeat(FeatType.ImprovedCriticalFinesseVibroblades);
        }

        private void FinesseVibrobladeProficiency(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedFinesseVibroblade, PerkType.FinesseVibrobladeProficiency)
                .Name("Finesse Vibroblade Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Finesse Vibroblades.")
                .Price(2)
                .GrantsFeat(FeatType.FinesseVibrobladeProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Finesse Vibroblades.")
                .Price(2)
                .RequirementSkill(SkillType.OneHanded, 10)
                .GrantsFeat(FeatType.FinesseVibrobladeProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Finesse Vibroblades.")
                .Price(2)
                .RequirementSkill(SkillType.OneHanded, 20)
                .GrantsFeat(FeatType.FinesseVibrobladeProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Finesse Vibroblades.")
                .Price(2)
                .RequirementSkill(SkillType.OneHanded, 30)
                .GrantsFeat(FeatType.FinesseVibrobladeProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Finesse Vibroblades.")
                .Price(2)
                .RequirementSkill(SkillType.OneHanded, 40)
                .GrantsFeat(FeatType.FinesseVibrobladeProficiency5);
        }

        private void FinesseVibrobladeMastery(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedFinesseVibroblade, PerkType.FinesseVibrobladeMastery)
                .Name("Finesse Vibroblade Mastery")
                .TriggerEquippedItem((player, item, slot, type, level) =>
                {
                    if (slot != InventorySlot.RightHand) return;

                    StatService.ApplyAttacksPerRound(player, item);
                })
                .TriggerUnequippedItem((player, item, slot, type, level) =>
                {
                    if (slot != InventorySlot.RightHand) return;

                    StatService.ApplyAttacksPerRound(player, OBJECT_INVALID);
                })
                .TriggerPurchase((player) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    StatService.ApplyAttacksPerRound(player, item);
                })
                .TriggerRefund((player) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    StatService.ApplyAttacksPerRound(player, item);
                })

                .AddPerkLevel()
                .Description("Grants an additional attack with a Finesse Vibroblade.")
                .Price(8)
                .RequirementSkill(SkillType.OneHanded, 25)
                .GrantsFeat(FeatType.FinesseVibrobladeMastery1)

                .AddPerkLevel()
                .Description("Grants an additional attack with a Finesse Vibroblade.")
                .Price(8)
                .RequirementSkill(SkillType.OneHanded, 50)
                .GrantsFeat(FeatType.FinesseVibrobladeMastery2);
        }

        private void PoisonStab(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedFinesseVibroblade, PerkType.PoisonStab)
                .Name("Poison Stab")

                .AddPerkLevel()
                .Description("Your next attack deals an additional 8 DMG and has a DC10 Fortitude check to inflict Poison for 30 seconds.")
                .Price(3)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.OneHanded, 15)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.PoisonStab1)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 18 DMG and has a DC15 Fortitude check to inflict Poison for 1 minute.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.OneHanded, 30)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.PoisonStab2)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 28 DMG and has a DC20 Fortitude check to inflict Poison for 1 minute.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.OneHanded, 45)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.PoisonStab3);
        }

        private void Backstab(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedFinesseVibroblade, PerkType.Backstab)
                .Name("Backstab")

                .AddPerkLevel()
                .Description("Deals 14 DMG to your target when dealt from behind. Damage is halved if not behind target.")
                .Price(2)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.OneHanded, 5)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.Backstab1)

                .AddPerkLevel()
                .Description("Deals 30 DMG to your target when dealt from behind. Damage is halved if not behind target.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.OneHanded, 20)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.Backstab2)

                .AddPerkLevel()
                .Description("Deals 45 DMG to your target when dealt from behind. Damage is halved if not behind target.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.OneHanded, 35)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.Backstab3);
        }

        private void WeaponFocusLightsabers(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedLightsaber, PerkType.WeaponFocusLightsabers)
                .Name("Weapon Focus - Lightsabers")

                .AddPerkLevel()
                .Description("Your accuracy with lightsabers is increased by 5.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 5)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.WeaponFocusLightsabers)

                .AddPerkLevel()
                .Description("Your base damage with lightsabers is increased by 2 DMG.")
                .Price(4)
                .RequirementSkill(SkillType.OneHanded, 15)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.WeaponSpecializationLightsabers);
        }

        private void ImprovedCriticalLightsabers(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedLightsaber, PerkType.ImprovedCriticalLightsabers)
                .Name("Improved Critical - Lightsabers")

                .AddPerkLevel()
                .Description("Improves the chance to critically hit with lightsabers by 5%.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 25)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ImprovedCriticalLightsabers);
        }

        private void LightsaberProficiency(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedLightsaber, PerkType.LightsaberProficiency)
                .Name("Lightsaber Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Lightsabers.")
                .Price(2)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.LightsaberProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Lightsabers.")
                .Price(2)
                .RequirementSkill(SkillType.OneHanded, 10)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.LightsaberProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Lightsabers.")
                .Price(2)
                .RequirementSkill(SkillType.OneHanded, 20)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.LightsaberProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Lightsabers.")
                .Price(2)
                .RequirementSkill(SkillType.OneHanded, 30)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.LightsaberProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Lightsabers.")
                .Price(2)
                .RequirementSkill(SkillType.OneHanded, 40)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.LightsaberProficiency5);
        }

        private void LightsaberMastery(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedLightsaber, PerkType.LightsaberMastery)
                .Name("Lightsaber Mastery")
                .TriggerEquippedItem((player, item, slot, type, level) =>
                {
                    if (slot != InventorySlot.RightHand) return;

                    StatService.ApplyAttacksPerRound(player, item);
                })
                .TriggerUnequippedItem((player, item, slot, type, level) =>
                {
                    if (slot != InventorySlot.RightHand) return;

                    StatService.ApplyAttacksPerRound(player, OBJECT_INVALID);
                })
                .TriggerPurchase((player) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    StatService.ApplyAttacksPerRound(player, item);
                })
                .TriggerRefund((player) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    StatService.ApplyAttacksPerRound(player, item);
                })

                .AddPerkLevel()
                .Description("Grants an additional attack when equipped with a Lightsaber.")
                .Price(8)
                .RequirementSkill(SkillType.OneHanded, 25)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.LightsaberMastery1)
                
                .AddPerkLevel()
                .Description("Grants an additional attack when equipped with a Lightsaber.")
                .Price(8)
                .RequirementSkill(SkillType.OneHanded, 50)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.LightsaberMastery2);
        }

        private void ForceLeap(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedLightsaber, PerkType.ForceLeap)
                .Name("Force Leap")

                .AddPerkLevel()
                .Description("Leap to a distant target instantly, inflicting 8 DMG and stunning for 2 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 15)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceLeap1)

                .AddPerkLevel()
                .Description("Leap to a distant target instantly, inflicting 15 DMG and stunning for 2 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 30)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceLeap2)

                .AddPerkLevel()
                .Description("Leap to a distant target instantly, inflicting 23 DMG and stunning for 2 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 45)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceLeap3);
        }

        private void SaberStrike(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedLightsaber, PerkType.SaberStrike)
                .Name("Saber Strike")

                .AddPerkLevel()
                .Description("Your next attack deals an additional 6 DMG and has a DC10 Fortitude check to inflict Breach, reducing evasion for 30 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.OneHanded, 5)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.SaberStrike1)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 15 DMG and has a DC15 Fortitude check to inflict Breach, reducing evasion for 1 minute.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 20)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.SaberStrike2)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 22 DMG and has a DC20 Fortitude check to inflict Breach, reducing evasion for 1 minute.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 35)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.SaberStrike3);
        }

        private void ImprovedTwoWeaponFighting(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedGeneral, PerkType.ImprovedTwoWeaponFightingOneHanded)
                .Name("Improved Two Weapon Fighting (One-Handed)")

                .AddPerkLevel()
                .Description("Grants an additional off-hand attack when dual wielding or using a double-sided weapon, and reduces the two-weapon fighting penalty to 0%/-10%. [Cross Skill]")
                .Price(4)
                .RequirementSkill(SkillType.OneHanded, 40)
                .RequirementCannotHavePerk(PerkType.ImprovedTwoWeaponFightingTwoHanded)
                .GrantsFeat(FeatType.ImprovedTwoWeaponFighting);
        }

        private void StrongStyleLightsaber(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedLightsaber, PerkType.StrongStyleLightsaber)
                .Name("Strong Style (Lightsaber)")
                .TriggerRefund((player) =>
                {
                    AbilityService.ToggleAbility(player, AbilityToggleType.StrongStyleLightsaber, false);
                })
                .TriggerPurchase((player) =>
                {
                    AbilityService.ToggleAbility(player, AbilityToggleType.StrongStyleLightsaber, false);
                })
                
                .AddPerkLevel()
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .Description("While active, attacks with a lightsaber use PER to-hit and MGT for damage, and gain bonus damage equal to half your MGT modifier.")
                .Price(1)
                .GrantsFeat(FeatType.StrongStyleLightsaber);
        }
    }
}
