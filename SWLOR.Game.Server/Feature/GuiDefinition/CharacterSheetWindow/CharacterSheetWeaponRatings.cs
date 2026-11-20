using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using static SWLOR.Game.Server.Feature.GuiDefinition.CharacterSheetWindow.CharacterSheetFormat;

namespace SWLOR.Game.Server.Feature.GuiDefinition.CharacterSheetWindow
{
    /// <summary>
    /// The sheet's weapon and rating bindings: main-hand and off-hand damage, attack delay, Attack, defenses, accuracies and Evasion.
    /// </summary>
    public class CharacterSheetWeaponRatings
    {
        private IBeastMasteryService BeastMastery { get; }
        private CharacterSheetDamageEstimate CharacterSheetDamageEstimate { get; }
        private CharacterSheetWeaponTiming CharacterSheetWeaponTiming { get; }
        private ICombatRatingService CombatRating { get; }
        private IItemService Item { get; }
        private IPairedRangedTraitsService PairedRangedTraits { get; }
        private ISkillService Skill { get; }

        public CharacterSheetWeaponRatings(IBeastMasteryService beastMastery, CharacterSheetDamageEstimate characterSheetDamageEstimate, CharacterSheetWeaponTiming characterSheetWeaponTiming, ICombatRatingService combatRating, IItemService item, IPairedRangedTraitsService pairedRangedTraits, ISkillService skill)
        {
            BeastMastery = beastMastery;
            CharacterSheetDamageEstimate = characterSheetDamageEstimate;
            CharacterSheetWeaponTiming = characterSheetWeaponTiming;
            CombatRating = combatRating;
            Item = item;
            PairedRangedTraits = pairedRangedTraits;
            Skill = skill;
        }


        public void Refresh(ICharacterSheetHost host)
        {
            var mainHand = GetItemInSlot(InventorySlot.RightHand, host.Target);
            var offHand = GetItemInSlot(InventorySlot.LeftHand, host.Target);
            var forceAccuracyWeapon = CharacterSheetWeaponTiming.SelectForceAccuracyWeapon(mainHand,
                GetIsObjectValid(offHand) && GetBaseItemType(offHand) == BaseItem.CompanionPistol ? OBJECT_INVALID : offHand,
                GetIsObjectValid(mainHand));
            var mainHandType = GetBaseItemType(mainHand);
            var attackDelayInfo = CharacterSheetWeaponTiming.GetAttackDelayInfo(host);
            host.AttackDelay = attackDelayInfo.Value;
            host.AttackDelayTooltip = attackDelayInfo.Tooltip;

            if (GetIsObjectValid(mainHand))
            {
                var dmgInfo = CharacterSheetDamageEstimate.GetCombatInfo(host, mainHand);
                host.MainHandDMG = dmgInfo.Item1;
                host.MainHandTooltip = dmgInfo.Item2;
            }
            else
            {
                host.MainHandDMG = "-";
                host.MainHandTooltip = "Est. Damage: N/A";
            }

            var offhandAttackWeapon = CharacterSheetWeaponTiming.GetOffhandAttackWeapon(host);
            var hasOffhandAttack = GetIsObjectValid(offhandAttackWeapon);
            if (hasOffhandAttack)
            {
                var dmgInfo = CharacterSheetDamageEstimate.GetCombatInfo(host, offhandAttackWeapon);
                host.OffHandDMG = dmgInfo.Item1;
                host.OffHandTooltip = dmgInfo.Item2;
            }
            else
            {
                var inactiveCompanion = GetIsObjectValid(offHand) && GetBaseItemType(offHand) == BaseItem.CompanionPistol;
                host.OffHandDMG = inactiveCompanion ? "Inactive" : "-";
                host.OffHandTooltip = inactiveCompanion
                    ? "Requires an eligible main-hand pistol and the Twin Pistol ranks required by both weapons."
                    : "Est. Damage: N/A";
            }

            AbilityType damageStat;
            AbilityType accuracyStatOverride;

            if (BeastMastery.IsPlayerBeast(host.Target))
            {
                var beastType = BeastMastery.GetBeastType(host.Target);
                var beastDetails = BeastMastery.GetBeastDetail(beastType);
                damageStat = beastDetails.DamageStat;
                accuracyStatOverride = beastDetails.AccuracyStat;
                // The skin's Accuracy is already included through the NPC stats, so read the claw combat attacks with.
                mainHand = GetItemInSlot(InventorySlot.CreatureLeft, host.Target);
            }
            else
            {
                damageStat = CharacterSheetDamageEstimate.GetEquipmentDamageAbility(host.Target, mainHand);
                accuracyStatOverride = AbilityType.Invalid;

            }

            var mainHandSkill = Skill.GetWeaponSkill(host.Target, mainHand);
            host.Attack = CombatRating.GetAttack(host.Target, damageStat, mainHandSkill);
            host.AttackTooltip = FormatOffHandTooltip(host.AttackDescription, hasOffhandAttack
                ? CombatRating.GetAttack(host.Target, CharacterSheetDamageEstimate.GetEquipmentDamageAbility(host.Target, offhandAttackWeapon),
                    Skill.GetWeaponSkill(host.Target, offhandAttackWeapon)).ToString()
                : null);
            host.ForceAttack = CombatRating.GetAttack(host.Target, AbilityType.Willpower, SkillType.Force);
            host.PhysicalDefense = CombatRating.GetDefense(host.Target, CombatDamageType.Physical, AbilityType.Vitality);
            host.ForceDefense = CombatRating.GetDefense(host.Target, CombatDamageType.Force, AbilityType.Willpower);

            host.WeaponAccuracy = CombatRating.GetAccuracy(host.Target, mainHand, accuracyStatOverride, SkillType.Invalid);
            if (GetIsObjectValid(mainHand) && Item.IsAttackWeaponType(GetBaseItemType(mainHand)))
                host.WeaponAccuracy += PairedRangedTraits.GetOrdinaryAccuracy(host.Target);
            var offhandAccuracy = hasOffhandAttack
                ? CombatRating.GetAccuracy(host.Target, offhandAttackWeapon, AbilityType.Invalid, SkillType.Invalid) : 0;
            if (hasOffhandAttack)
                offhandAccuracy += PairedRangedTraits.GetOrdinaryAccuracy(host.Target);
            host.OffHandAccuracy = hasOffhandAttack ? offhandAccuracy.ToString() : "-";
            host.ForceAccuracy = CombatRating.GetAccuracy(
                host.Target,
                forceAccuracyWeapon,
                AbilityType.Willpower,
                SkillType.Force,
                ignoreWeaponAccuracyStatOverride: true);
            host.Evasion = CombatRating.GetEvasion(host.Target, SkillType.Invalid);
        }
    }
}
