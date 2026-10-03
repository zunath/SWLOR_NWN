using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    internal static class CombatStyleGuideTopics
    {
        public static IReadOnlyList<PlayerGuideViewModel.PlayerGuideTopic> Create()
        {
            return new[]
            {
                Topic("Armor", "Choose Armor perks for enemy control, defensive options, or weapon use.",
                    new[] { Style("General", "Within the Armor skill, the General category includes Provoke to draw enemy attention and options such as Dual Wield and defensive traits. This describes the Armor tree; the General category label is shared elsewhere. Check the Perks window for requirements and current effects.") },
                    "Perks", "Combat Basics", "Vibroblade Combat Styles", "Katar Combat Styles"),

                Topic("Beast Mastery", "Choose Beast combat perks to shape your active beast's commands, attacks, and defenses.",
                    new[]
                    {
                        Style("Beast Mastery - Training", "Training includes taming, calling, reviving, and directing beasts, with command perks that can add attacks or support effects."),
                        Style("Beast - Damage", "Damage perks add physical attacks and effects such as Bleed to your beast's next strikes."),
                        Style("Beast - Tank", "Tank perks include damage reduction and commands that draw an enemy's attention to your beast."),
                        Style("Beast - Balanced", "Balanced perks mix beast attacks and sustained combat bonuses."),
                        Style("Beast - Bruiser", "Bruiser perks include cone attacks and other beast attacks that can apply conditions."),
                        Style("Beast - Evasion", "Evasion perks improve your beast's evasion and pair attacks with defensive windows."),
                        Style("Beast - Force", "Force perks add Force damage and healing options to your beast's commands.")
                    },
                    "Perks", "Combat Basics", "Leadership Combat Styles", "First Aid Combat Styles"),

                Topic("Devices", "Choose device perks for explosives, deployed fields, automated beacons, or ally support.",
                    new[]
                    {
                        Style("Devices - Grenadier", "Grenadier uses consumable explosives for area damage and effects such as knockdown."),
                        Style("Devices - Field Engineer", "Field Engineer places beacons and fields that pulse damage or other effects over time."),
                        Style("Devices - Field Support", "Field Support includes ally protection and tools that weaken an opponent's attacks."),
                        Style("Devices - Assault Gadgets", "Assault Gadgets offers direct attacks, including cones and targeted rockets.")
                    },
                    "Perks", "Combat Basics", "First Aid Combat Styles", "Rifle Combat Styles"),

                Topic("Espionage", "Choose between evasive repositioning and delayed traps in the Espionage combat trees.",
                    new[]
                    {
                        Style("Espionage - Infiltrator", "Infiltrator includes tools to reduce enmity, evade attacks, and reposition behind a hostile target. Shadow Step does not grant invisibility."),
                        Style("Espionage - Saboteur", "Saboteur places visible traps that arm after a delay and trigger damage or conditions when enemies reach them.")
                    },
                    "Perks", "Combat Basics", "Vibroknife Combat Styles", "Pistol Combat Styles"),

                Topic("First Aid", "Choose First Aid perks to heal allies, clear conditions, or prepare with combat consumables.",
                    new[]
                    {
                        Style("First Aid - Trauma Medic", "Trauma Medic uses medical supplies to heal one target and remove conditions such as Bleed or Poison."),
                        Style("First Aid - Combat Pharmacology", "Combat Pharmacology uses stim packs for resource recovery and temporary defenses.")
                    },
                    "Perks", "Combat Basics", "Leadership Combat Styles", "Beast Mastery Combat Styles"),

                Topic("Force", "Choose Force powers for hostile control, healing, protection, or movement to an ally.",
                    new[]
                    {
                        Style("Force - Alter", "Alter includes direct Force damage, area attacks, control effects, and attacks that can restore health."),
                        Style("Force - Control", "Control includes healing, temporary protection, and movement or control powers."),
                        Style("Force - Sense", "Sense includes attacks and effects that weaken enemies, plus ways to intercept harm aimed at an ally.")
                    },
                    "Perks", "Combat Basics", "Lightsaber Combat Styles", "Saberstaff Combat Styles"),

                Topic("Heavy Vibroblade", "Choose Heavy Vibroblade for self-recovery through attacks or for defense and control that draw enemy attention.",
                    new[]
                    {
                        Style("Heavy Vibroblade - Immortal", "Immortal includes attacks that generate extra enmity, defensive bonuses, and effects that hinder nearby enemies."),
                        Style("Heavy Vibroblade - Berserker", "Berserker includes attacks that trade health for damage and strikes that restore health from damage dealt.")
                    },
                    "Perks", "Combat Basics", "Vibroblade Combat Styles", "Katar Combat Styles"),

                Topic("Katar", "Choose Katar for Guard-based counterattacks and ally protection, or for attacks that hinder a target.",
                    new[]
                    {
                        Style("Katar - Iron Guard", "Iron Guard includes a counterattack enhanced by a recent Guard and a link that shares some of your Guard with an ally."),
                        Style("Katar - Scrapper", "Scrapper attacks can hinder targets with effects such as Hamstring or Dazed.")
                    },
                    "Perks", "Combat Basics", "Vibroblade Combat Styles", "Heavy Vibroblade Combat Styles"),

                Topic("Leadership", "Choose Leadership perks to bolster allies or improve nearby party attacks.",
                    new[]
                    {
                        Style("Leadership - Vanguard Command", "Vanguard Command includes an aura that increases nearby party members' damage for a limited time."),
                        Style("Leadership - Field Steward", "Field Steward includes ally-focused temporary protection and stronger protection for an ally at low health.")
                    },
                    "Perks", "Combat Basics", "First Aid Combat Styles", "Beast Mastery Combat Styles"),

                Topic("Lightsaber", "Choose Lightsaber for strikes that weaken enemy defenses or for wards that protect an ally.",
                    new[]
                    {
                        Style("Lightsaber - Severance", "Severance favors pressure with Force-enhanced next-hit attacks and strikes that reduce an enemy's defenses."),
                        Style("Lightsaber - Ward", "Ward favors protecting an ally: Saber Ward improves your defenses, while its link redirects part of that ally's incoming damage to you.")
                    },
                    "Perks", "Combat Basics", "Saberstaff Combat Styles", "Twin Blade Combat Styles"),

                Topic("Mimicry", "Mimicry gives you combat actions learned from creatures you have analyzed.",
                    new[]
                    {
                        Style("Mimicry", "Mimicry techniques include attacks and control effects learned from analyzed creatures. The available techniques depend on what you have learned.")
                    },
                    "Perks", "Combat Basics", "Beasts & Stables", "Abilities", "Mimicry & Techniques"),

                Topic("Pistol", "Choose Pistol for shots that reward changing targets or disrupt opponents.",
                    new[]
                    {
                        Style("Pistol - Gambler", "Gambler rewards switching targets with a critical chance; critical hits can also restore Stamina."),
                        Style("Pistol - Skirmisher", "Skirmisher favors disrupting one target with shots that can disarm or interrupt.")
                    },
                    "Perks", "Combat Basics", "Rifle Combat Styles", "Throwing Combat Styles"),

                Topic("Rifle", "Choose Rifle for shots that reward careful timing or suppress enemy evasion and movement.",
                    new[]
                    {
                        Style("Rifle - Marksman", "Marksman rewards pauses between attacks and includes a defense-piercing shot in a line."),
                        Style("Rifle - Suppression", "Suppression builds effects that reduce enemy evasion and includes attacks that hinder movement.")
                    },
                    "Perks", "Combat Basics", "Pistol Combat Styles", "Devices Combat Styles"),

                Topic("Saberstaff", "Choose Saberstaff for attacks with resource or defensive benefits, or for repeated strikes against nearby enemies.",
                    new[]
                    {
                        Style("Saberstaff - Conduit", "Conduit attacks gain benefits when both Force and Stamina are above stated thresholds, including a defensive bonus."),
                        Style("Saberstaff - Tempest", "Tempest includes multi-hit attacks, nearby-area attacks, and benefits when attacks connect.")
                    },
                    "Perks", "Combat Basics", "Lightsaber Combat Styles", "Twin Blade Combat Styles"),

                Topic("Spear", "Choose Spear for attacks that interrupt or burden a target, or for evasion and positional bonuses.",
                    new[]
                    {
                        Style("Spear - Vigor", "Vigor pairs attacks with evasion and includes a sweep that gains damage when you are beside or behind a target."),
                        Style("Spear - Disabler", "Disabler attacks can increase an enemy's ability costs or drain resources when interrupting an active ability. Fracture Strike benefits from control effects you applied, including Foggy Mind and Force Disruption.")
                    },
                    "Perks", "Combat Basics", StatusEffectGuideTopics.TopicName, "Staff Combat Styles", "Katar Combat Styles"),

                Topic("Staff", "Choose Staff for extra pressure on controlled targets or for attacks that disorient and knock down.",
                    new[]
                    {
                        Style("Staff - Crusher", "Crusher gains extra damage against targets with control effects, including those applied by allies, and includes attacks that can daze. Charged Blows and Skull Rattle require you to apply the control effect yourself."),
                        Style("Staff - Sentinel", "Sentinel includes a line attack and strikes that can knock targets down or disorient them.")
                    },
                    "Perks", "Combat Basics", StatusEffectGuideTopics.TopicName, "Spear Combat Styles", "Twin Blade Combat Styles"),

                Topic("Throwing", "Choose Throwing for hindering attacks or explosive area damage.",
                    new[]
                    {
                        Style("Throwing - Ordnance", "Ordnance includes explosive area attacks and thrown attacks that can inflict effects such as Blind."),
                        Style("Throwing - Flurry", "Flurry attacks can inflict Bleed or Hamstring, with additional effects against bleeding targets.")
                    },
                    "Perks", "Combat Basics", "Pistol Combat Styles", "Devices Combat Styles"),

                Topic("Twin Blade", "Choose Twin Blade for repeated attacks that build momentum or spread bleeding.",
                    new[]
                    {
                        Style("Twin Blade - Cyclone", "Cyclone includes quick repeated attacks and strikes against several nearby enemies."),
                        Style("Twin Blade - Lacerator", "Lacerator attacks apply Bleed and can spread it to another nearby enemy.")
                    },
                    "Perks", "Combat Basics", "Saberstaff Combat Styles", "Staff Combat Styles"),

                Topic("Vibroblade", "Choose Vibroblade for attacks that scale with defense and draw enemy attention, or for nearby area damage.",
                    new[]
                    {
                        Style("Vibroblade - Bulwark", "Bulwark includes a strike based on your Physical Defense and attacks that increase enmity toward you."),
                        Style("Vibroblade - Frenzy", "Frenzy includes next-hit attacks and nearby area damage.")
                    },
                    "Perks", "Combat Basics", "Heavy Vibroblade Combat Styles", "Katar Combat Styles"),

                Topic("Vibroknife", "Choose Vibroknife for attacks that exploit Exposed targets or extend Venom and Infection.",
                    new[]
                    {
                        Style("Vibroknife - Saboteur", "Saboteur attacks apply Exposed and gain extra damage against targets already affected by it."),
                        Style("Vibroknife - Shadow", "Shadow attacks apply Venom and can extend Venom or Infection already affecting a target.")
                    },
                    "Perks", "Combat Basics", "Espionage", "Twin Blade Combat Styles")
            };
        }

        private static PlayerGuideViewModel.PlayerGuideTopic Topic(
            string family,
            string summary,
            IReadOnlyList<PlayerGuideViewModel.ArticleBlock> styles,
            params string[] related)
        {
            var questions = new[]
            {
                new PlayerGuideViewModel.QuestionAnswer(
                    $"What does {family} focus on?",
                    summary)
            };

            return new PlayerGuideViewModel.PlayerGuideTopic(
                $"{family} Combat Styles",
                "Combat Styles",
                summary,
                family,
                styles,
                questions,
                related);
        }

        private static PlayerGuideViewModel.ArticleBlock Style(string title, string description)
        {
            return new PlayerGuideViewModel.ArticleBlock(title, description);
        }
    }
}