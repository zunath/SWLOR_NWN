using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;

namespace SWLOR.Game.Server.Feature
{
    public class TlkOverrides
    {
        [NWNEventHandler("mod_load")]
        public static void OverrideTlks()
        {
            OverrideAttributeNames();
            OverrideMenuNames();
            OverrideFeatDescriptions();
            OverrideAttackBonus();
        }

        private static void OverrideAttributeNames()
        {
            SetTlkOverride(131, "Social"); // Charisma
            SetTlkOverride(132, "Vitality"); // Constitution
            SetTlkOverride(133, "Perception"); // Dexterity
            SetTlkOverride(134, "Agility"); // Intelligence
            SetTlkOverride(135, "Might"); // Strength
            SetTlkOverride(136, "Willpower"); // Wisdom

            SetTlkOverride(328, "Increased Might By"); // Strength
            SetTlkOverride(329, "Increased Perception By"); // Dexterity
            SetTlkOverride(330, "Agility"); // Intelligence
            SetTlkOverride(331, "Increased Vitality By"); // Constitution
            SetTlkOverride(332, "Increased Willpower By"); // Wisdom
            SetTlkOverride(333, "Increased Social By"); // Charisma

            SetTlkOverride(473, "Might Information"); // Strength
            SetTlkOverride(474, "Perception Information"); // Dexterity
            SetTlkOverride(475, "Vitality Information"); // Constitution
            SetTlkOverride(476, "Willpower Information"); // Wisdom
            SetTlkOverride(477, "Agility"); // Intelligence
            SetTlkOverride(479, "Social Information"); // Charisma

            SetTlkOverride(457, BuildRecommendedButtonText());

            SetTlkOverride(459, "Might improves damage dealt by melee weapons and increases carrying capacity.");
            SetTlkOverride(460, "Perception improves damage dealt by ranged and finesse weapons and increases physical accuracy.");
            SetTlkOverride(461, "Vitality improves your max hit points and reduces damage received.");
            SetTlkOverride(462, "Willpower improves your force attack, force defense, and max force points.");
            SetTlkOverride(463, "Agility improves ranged accuracy, evasion, and max stamina.");
            SetTlkOverride(478, "Social improves your XP gain and leadership capabilities.");
            
            SetTlkOverride(1027, "Poison"); // Acid

            SetTlkOverride(7099, "Evasion");

            SetTlkOverride(66751, "Holonet");
            SetTlkOverride(66755, "Comms");

            SetTlkOverride(83393, "Poison"); // Acid
        }

        private static string BuildRecommendedButtonText()
        {
            return "Your character is guided by six core attributes: Might, Vitality, Perception, Willpower, Agility, and Social.\n\n" +
                   "Might: Improves damage dealt by melee weapons and increases carrying capacity.\n" +
                   "Vitality: Improves your max hit points and reduces damage received.\n" +
                   "Perception: Improves damage dealt by ranged and finesse weapons and increases physical accuracy.\n" +
                   "Willpower: Improves your force attack, force defense, and max force points.\n" +
                   "Agility: Improves ranged accuracy, evasion, and max stamina.\n" +
                   "Social: Improves your XP gain and leadership capabilities.\n\n";
        }

        private static void OverrideMenuNames()
        {
            // Journal - List as Quests
            SetTlkOverride(7037, "Quests");

            // Spell Book - List as Player Guide
            SetTlkOverride(7038, "Player Guide");
        }

        private static void OverrideFeatDescriptions()
        {
            var template = "Name: {0}\n" +
                           "FP: {1}\n" +
                           "STM: {2}\n" +
                           "Recast: {3}s\n" +
                           "Description: {4}\n";

            foreach (var (_, detail) in Perk.GetAllPerks())
            {
                foreach (var (_, perkLevel) in detail.PerkLevels)
                {
                    foreach (var feat in perkLevel.GrantedFeats)
                    {
                        if (feat == FeatType.Invalid)
                            continue;
                        if (!int.TryParse(Get2DAString("feat", "DESCRIPTION", (int)feat), out var featDescriptionId))
                            continue;
                        if (!Ability.IsFeatRegistered(feat))
                            continue;

                        var spellDescriptionId = 0;
                        int.TryParse(Get2DAString("feat", "SPELLID", (int)feat), out var spellId);

                        if (spellId > 0)
                        {
                            int.TryParse(Get2DAString("spells", "SpellDesc", spellId), out spellDescriptionId);
                        }

                        var abilityDetail = Ability.GetAbilityDetail(feat);
                        var fp = 0;
                        var stm = 0;
                        var recast = abilityDetail.RecastDelay?.Invoke(OBJECT_INVALID) ?? 0f;

                        foreach (var requirement in abilityDetail.Requirements)
                        {
                            if (requirement.GetType() == typeof(AbilityRequirementFP))
                            {
                                var req = (AbilityRequirementFP)requirement;
                                fp = req.RequiredFP;
                            }
                            else if (requirement.GetType() == typeof(AbilityRequirementStamina))
                            {
                                var req = (AbilityRequirementStamina)requirement;
                                stm = req.RequiredSTM;
                            }
                        }

                        var description = string.Format(template,
                            abilityDetail.Name,
                            fp,
                            stm,
                            recast,
                            perkLevel.Description);

                        // Update both the feat and the spell descriptions, if applicable
                        SetTlkOverride(featDescriptionId, description);

                        if(spellDescriptionId > 0)
                            SetTlkOverride(spellDescriptionId, description);
                    }
                }
            }
        }

        private static void OverrideAttackBonus()
        {
            SetTlkOverride(660, "Accuracy and Damage Penalty");
            SetTlkOverride(734, "Accuracy vs. Alignment Group");
            SetTlkOverride(735, "Accuracy");
            SetTlkOverride(736, "Decreased Accuracy");
            SetTlkOverride(737, "Accuracy vs. Racial Group");
            SetTlkOverride(738, "Accuracy vs. Specific Alignment");
            SetTlkOverride(757, "Select a race for this creature. This is used for preferred enemy and/or accuracy bonus vs race calculations.");
            SetTlkOverride(1085, "Accuracy: This property grants an enhancement bonus to attack rolls made with the weapon, but does not improve the damage dealt by the weapon on a successful hit.");
            SetTlkOverride(1086, "Accuracy vs. Monster Type: This property grants an enhancement bonus to attack rolls made with the weapon, but does not improve the damage dealt by the weapon on a successful hit. It grants this bonus only against a specific monster type, such as giants, undead, or shapeshifters.");
            SetTlkOverride(1087, "Accuracy vs. Specific Alignment: This property grants an enhancement bonus to attack rolls made with the weapon, but does not improve the damage dealt by the weapon on a successful hit. It grants this bonus only when used against creatures of a specific alignment, such as chaotic neutral, lawful evil, or neutral good.");
            SetTlkOverride(1088, "Accuracy vs. Alignment Group: This property grants an enhancement bonus to attack rolls made with the weapon, but does not improve the damage dealt by the weapon on a successful hit. It grants this bonus only when used against creatures from a specific alignment group, such as evil or lawful creatures.");
            SetTlkOverride(1458, "Accuracy Penalty: This property inflicts a penalty on the wielder's attack rolls made with the weapon.");
            SetTlkOverride(1460, "Accuracy Penalty: This property inflicts a penalty on the wielder's attack and damage rolls made with the weapon.");
            SetTlkOverride(2227, "Accuracy Bonus");
            SetTlkOverride(5519, "Accuracy Bonus:");
            SetTlkOverride(5520, "Accuracy Bonus vs.:");
            SetTlkOverride(5521, "Accuracy Penalty:");
            SetTlkOverride(8038, "Enemy Accuracy Bonus");
            SetTlkOverride(40557, "Total Base Accuracy Bonus is %d, but must have at least %d");
            SetTlkOverride(58292, "Accuracy bonus: ");
        }

    }
}
