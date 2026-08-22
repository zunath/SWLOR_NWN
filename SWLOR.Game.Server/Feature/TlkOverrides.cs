using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.AttributeService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature
{
    public class TlkOverrides
    {
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void OverrideTlks()
        {
            OverrideAttributeNames();
            OverrideMenuNames();
            OverrideDamageTypeNames();
            OverrideFeatDescriptions();
            OverrideAttackBonus();
        }

        private static void OverrideDamageTypeNames()
        {
            // SWLOR routes Force damage through the engine's "Magical" damage type (damagetypes.2da row 3,
            // damagetypegroups.2da group 1). The combat-log feedback for that group renders base dialog.tlk
            // strref 5593 ("<CUSTOM0> Magical"); retype it to "Force" so the log reads e.g. "72 Force".
            // The <CUSTOM0> token is the damage amount and must be preserved.
            SetTlkOverride(5593, "<CUSTOM0> Force"); // Magical damage combat-log feedback
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
            SetTlkOverride(330, "Increased Agility By"); // Intelligence
            SetTlkOverride(331, "Increased Vitality By"); // Constitution
            SetTlkOverride(332, "Increased Willpower By"); // Wisdom
            SetTlkOverride(333, "Increased Social By"); // Charisma

            SetTlkOverride(473, "Might Information"); // Strength
            SetTlkOverride(474, "Perception Information"); // Dexterity
            SetTlkOverride(475, "Vitality Information"); // Constitution
            SetTlkOverride(476, "Willpower Information"); // Wisdom
            SetTlkOverride(477, "Agility Information"); // Intelligence
            SetTlkOverride(479, "Social Information"); // Charisma

            SetTlkOverride(457, AttributeDescription.BuildOverview());

            SetTlkOverride(459, AttributeDescription.MightDetails);
            SetTlkOverride(460, AttributeDescription.PerceptionDetails);
            SetTlkOverride(461, AttributeDescription.VitalityDetails);
            SetTlkOverride(462, AttributeDescription.WillpowerDetails);
            SetTlkOverride(463, AttributeDescription.AgilityDetails);
            SetTlkOverride(478, AttributeDescription.SocialDetails);

            SetTlkOverride(535, "Credit"); // Gold Piece

            SetTlkOverride(1027, "Poison"); // Acid

            SetTlkOverride(3593, "Give credits."); // GP
            SetTlkOverride(5025, "The Galactic Credit Standard, or simply the 'credit', is the main form of currency throughout the galaxy."); // GP desc
            SetTlkOverride(6407, "Credits"); // GP
            SetTlkOverride(7059, "Drop or give credits, etc.");

            SetTlkOverride(7099, "Evasion");

            SetTlkOverride(8035, "Resting");
            SetTlkOverride(8049, "Horrified");
            SetTlkOverride(8056, "Accuracy Increased");
            SetTlkOverride(8057, "Accuracy Decreased");
            SetTlkOverride(8060, "Defense Increased");
            SetTlkOverride(8061, "Defense Decreased");
            SetTlkOverride(8062, "Evasion Increased");
            SetTlkOverride(8063, "Evasion Decreased");
            SetTlkOverride(8077, "Force Drained");

            SetTlkOverride(58369, "Might Increased");
            SetTlkOverride(58370, "Might Decreased");
            SetTlkOverride(58371, "Perception Increased");
            SetTlkOverride(58372, "Perception Decreased");
            SetTlkOverride(58373, "Vitality Increased");
            SetTlkOverride(58374, "Vitality Decreased");
            SetTlkOverride(58375, "Agility Increased");
            SetTlkOverride(58376, "Agility Decreased");
            SetTlkOverride(58377, "Willpower Increased");
            SetTlkOverride(58378, "Willpower Decreased");
            SetTlkOverride(58379, "Social Increased");
            SetTlkOverride(58380, "Social Decreased");

            SetTlkOverride(61619, "Sell <CUSTOM0> for <CUSTOM1>cr");
            SetTlkOverride(61620, "Buy <CUSTOM0> for <CUSTOM1>cr");
            SetTlkOverride(62489, "Acquired <CUSTOM0> credits");
            SetTlkOverride(62490, "Lost <CUSTOM0> credits");

            SetTlkOverride(10303, "[Comms] ");
            SetTlkOverride(66751, "Disabled");
            SetTlkOverride(66755, "Comms");

            SetTlkOverride(83393, "Poison"); // Acid
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
                var levelOneFeatDescriptionId = -1;
                var levelOneSpellDescriptionId = -1;
                var levelOneDescription = string.Empty;

                foreach (var (level, perkLevel) in detail.PerkLevels)
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

                        if (level == 1)
                        {
                            levelOneDescription = description;
                            levelOneFeatDescriptionId = featDescriptionId;
                        }

                        // Update both the feat and the spell descriptions, if applicable
                        SetTlkOverride(featDescriptionId, description);

                        if (spellDescriptionId > 0)
                        {
                            SetTlkOverride(spellDescriptionId, description);
                            levelOneSpellDescriptionId = spellDescriptionId;
                        }
                    }

                    // Some perks only grant one feat and improve the effectiveness of that feat on each level.
                    // For these, we display every perk level on the feat & spell description (if applicable)
                    if (level > 1 &&
                        !string.IsNullOrWhiteSpace(levelOneDescription) &&
                        perkLevel.GrantedFeats.Count <= 0)
                    {
                        levelOneDescription += $"\n\nLevel #{level}: {perkLevel.Description}";
                        SetTlkOverride(levelOneFeatDescriptionId, levelOneDescription);
                        if(levelOneSpellDescriptionId > 0)
                            SetTlkOverride(levelOneSpellDescriptionId, levelOneDescription);
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
            SetTlkOverride(1086, "Accuracy vs. Racial Group: This property grants an enhancement bonus to attack rolls made with the weapon, but does not improve the damage dealt by the weapon on a successful hit. It grants this bonus only against a specific racial group, such as giants, undead, or shapeshifters.");
            SetTlkOverride(1087, "Accuracy vs. Specific Alignment: This property grants an enhancement bonus to attack rolls made with the weapon, but does not improve the damage dealt by the weapon on a successful hit. It grants this bonus only when used against creatures of a specific alignment, such as chaotic neutral, lawful evil, or neutral good.");
            SetTlkOverride(1088, "Accuracy vs. Alignment Group: This property grants an enhancement bonus to attack rolls made with the weapon, but does not improve the damage dealt by the weapon on a successful hit. It grants this bonus only when used against creatures from a specific alignment group, such as evil or lawful creatures.");
            SetTlkOverride(1458, "Accuracy Penalty: This property inflicts a penalty on the wielder's attack rolls made with the weapon.");
            SetTlkOverride(1460, "Accuracy and Damage Penalty: This property inflicts a penalty on the wielder's attack and damage rolls made with the weapon.");
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
