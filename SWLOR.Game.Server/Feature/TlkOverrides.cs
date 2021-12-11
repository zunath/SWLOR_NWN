using SWLOR.Game.Server.Core;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature
{
    public class TlkOverrides
    {
        [NWNEventHandler("mod_load")]
        public static void OverrideTlks()
        {
            OverrideAttributeNames();
            OverrideMenuNames();
        }

        private static void OverrideAttributeNames()
        {
            SetTlkOverride(131, "Social"); // Charisma
            SetTlkOverride(132, "Vitality"); // Constitution
            SetTlkOverride(133, "Perception"); // Dexterity
            SetTlkOverride(134, "Unused"); // Intelligence
            SetTlkOverride(135, "Might"); // Strength
            SetTlkOverride(136, "Willpower"); // Wisdom

            SetTlkOverride(328, "Increased Might By"); // Strength
            SetTlkOverride(329, "Increased Perception By"); // Dexterity
            SetTlkOverride(330, "Unused"); // Intelligence
            SetTlkOverride(331, "Increased Vitality By"); // Constitution
            SetTlkOverride(332, "Increased Willpower By"); // Wisdom
            SetTlkOverride(333, "Increased Social By"); // Charisma

            SetTlkOverride(473, "Might Information"); // Strength
            SetTlkOverride(474, "Perception Information"); // Dexterity
            SetTlkOverride(475, "Vitality Information"); // Constitution
            SetTlkOverride(476, "Willpower Information"); // Wisdom
            SetTlkOverride(477, "Unused"); // Intelligence
            SetTlkOverride(479, "Social Information"); // Charisma

            SetTlkOverride(457, BuildRecommendedButtonText());

            SetTlkOverride(459, "Might measures the physical power of your character. It improves your melee power and carrying capacity.");
            SetTlkOverride(460, "Perception measures the intuition of your character. It improves your ranged power and evasion.");
            SetTlkOverride(461, "Vitality represents the health and stamina of your character. It improves your max HP, FP, and stamina.");
            SetTlkOverride(462, "Willpower represents the attunement to the Force of your character. It improves your force attack and force defense.");
            SetTlkOverride(478, "Social measures the ability to negotiate and influence others. It improves your ability to negotiate mission rewards and improves Roleplay XP.");

            SetTlkOverride(321, "EV");
            SetTlkOverride(7099, "Evasion");

            SetTlkOverride(66751, "Holonet");
            SetTlkOverride(66755, "Comms");
        }

        private static string BuildRecommendedButtonText()
        {
            return "Your character is guided by five core attributes: Might, Vitality, Perception, Willpower, and Social.\n\n" +
                   "Might: Improves your melee power and carrying capacity.\n" +
                   "Vitality: Improves your max hit points, ether points, and stamina.\n" +
                   "Perception: Improves your ranged power and evasion.\n" +
                   "Willpower: Improves your force attack and force defense.\n" +
                   "Social: Improves your ability to negotiate mission rewards and improves Roleplay XP.\n\n";
        }

        private static void OverrideMenuNames()
        {
            // Journal - List as Quests
            SetTlkOverride(7037, "Quests");

            // Spell Book - List as Unused
            SetTlkOverride(7038, "Unused");
        }

    }
}
