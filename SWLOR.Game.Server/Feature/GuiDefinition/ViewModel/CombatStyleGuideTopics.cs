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

                CraftingTopic("Engineering", "Choose Engineering perks for a Circuit that runs hot and strong, or one that runs stable and precise.",
                    new[]
                    {
                        Style("Engineering - Overclocker", "Overclocker perks reward a high Load in the Circuit: more output from Wire, Tune and Discharge, with tools to survive the heat. Overclock is its style."),
                        Style("Engineering - Technician", "Technician perks reward a steady, precise Circuit: fewer mistakes, finer calibration and a safety net. Safe Mode is its style."),
                        Style("Choosing a style", "Engineering styles are picked in the Engineering craft window, one active at a time. Check the Perks window for requirements and current effects.")
                    },
                    "Perks", "Crafting", "Skills"),

                Topic("Espionage", "Choose between evasive repositioning and delayed traps in the Espionage combat trees.",
                    new[]
                    {
                        Style("Espionage - Infiltrator", "Infiltrator includes tools to reduce enmity, evade attacks, and reposition behind a hostile target. Shadow Step does not grant invisibility."),
                        Style("Espionage - Saboteur", "Saboteur places visible traps that arm after a delay and trigger damage or conditions when enemies reach them.")
                    },
                    "Perks", "Combat Basics", "Vibroknife Combat Styles", "Pistol Combat Styles"),

                CraftingTopic("Fabrication", "Choose Fabrication perks to speed up or steady the Fabricator, or to improve research experiments.",
                    new[]
                    {
                        Style("Fabrication - Builder", "Builder perks change the Fabricator. At Fabrication 20 you choose Production Run, which makes Run much stronger, or Fine Tolerance, which makes Precision Pass stronger and Run weaker. Other Builder perks make Retool and Service cheaper, reduce wear, build two sub-assemblies in one action, and improve Prime Feedstock and the final Assemble."),
                        Style("Fabrication - Researcher", "Researcher perks change research experiments. At Fabrication 20 you choose Methodical, which adds trials but lowers every Test's chance, or Intuitive, which raises every Test's chance but removes a trial. Other Researcher perks add Test chance and trials, make Refine Method cheaper and stronger, and add Peer Review and Breakthrough."),
                        Style("Choosing a style", "The Fabrication style you own is always active, and you cannot own both styles of a line. Switching costs a Perk Refund Token. Check the Perks window for requirements and current effects.")
                    },
                    "Perks", "Crafting", "Skills"),

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

                Topic("Piloting", "Choose Piloting perks for abilities that fire your ship's weapons and for evasion while you fly.",
                    new[]
                    {
                        Style("Piloting - Ace", "Ace fires your ship's energy weapons for disabling shots, double shots, and a burst around your ship, and rewards evasion, movement, and critical hits."),
                        Style("Piloting - Ordnance", "Ordnance fires your ship's launchers as area ordnance: burning volleys, blinding flares, concussion bursts, fragmentation, and saturation fire."),
                        Style("Piloting - Bulwark", "Bulwark holds enemy attention for your fleet with goads and next-hit strikes that generate threat, and adds defense, a threat stance, and protection from critical hits and low hull."),
                        Style("Piloting - Command", "Command repairs allied ships and gives your party ships damage orders, an enemy accuracy penalty, and a damage-reduction aura, and makes your repairs, orders, and crew stronger.")
                    },
                    "Perks", "Combat Basics", "Pistol Combat Styles", "Rifle Combat Styles", "Throwing Combat Styles", "Vibroblade Combat Styles", "Heavy Vibroblade Combat Styles", "Leadership Combat Styles", "First Aid Combat Styles"),

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

                CraftingTopic("Smithery", "Choose Smithery perks for fast, hot work in the Plasma Forge, or for slower, precise finishing.",
                    new[]
                    {
                        Style("Smithery - Forgemaster", "Forgemaster perks reward fast, hot work: cheaper Thermal, a slower cooling piece, and bigger Hydro-Press gains, with a once-per-craft Red Line Press as its capstone. Hot Forging is its style."),
                        Style("Smithery - Artisan", "Artisan perks reward careful finishing: more Stable Containment, Finish that ignores Thermal, steadier heat and a stronger Cryo-Quench. Cold Working is its style."),
                        Style("Choosing a style", "Smithery styles are picked in the Plasma Forge window, one active at a time. You can own both and switch between them before each craft. Check the Perks window for requirements and current effects.")
                    },
                    "Perks", "Crafting", "Skills"),

                Topic("Spear","Choose Spear for attacks that interrupt or burden a target, or for evasion and positional bonuses.",
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

                Topic("Twin Pistol", "Choose Twin Pistol for sustained fire on one enemy or rotations that chain abilities to build Cadence.",
                    new[]
                    {
                        Style("Twin Pistol - Barrage", "Barrage rewards weapon auto-attack hits on one enemy. Akimbo Stance ramps Haste while reducing all weapon auto-attack damage by 30 percent. Twin Suns strengthens your next six weapon auto-attack attempts."),
                        Style("Twin Pistol - Crossfire", "Crossfire chains different weapon abilities to build up to three Cadence. Lead Shot and Tempo Shot grant Cadence directly; Closing Shot and Final Salvo spend it. Crossfire Stance adds Haste and Movement Speed while reducing all weapon auto-attack damage by 20 percent."),
                        Style("Equipment and Cross-skilling", "Twin Pistols are one owned item. Equipping the pair automatically equips a temporary off-hand gun; removing either gun unequips the pair and destroys the temporary gun. Both guns share the item's weapon profile. Weapon auto-attacks train Twin Pistol. Purchased perks also work with other eligible weapons. Matched Rhythm grants Haste while wielding two weapons.")
                    },
                    "Perks", "Combat Basics", "Pistol Combat Styles", "Rifle Combat Styles"),

                Topic("Unarmed", "Choose Unarmed for Power Glove combinations that chain setups into finishers, or for heavy strikes and counterattacks. Both gloves strike each attack, and Unarmed abilities add the DMG of both gloves.",
                    new[]
                    {
                        Style("Unarmed - Martial Artist", "Martial Artist chains Jab into Palm Strike and finishers, with setups that also help your other weapon attacks."),
                        Style("Unarmed - Brawler", "Brawler includes pressure attacks that weaken an enemy's attacks, heavy strikes, and counterattacks earned by taking damage.")
                    },
                    "Perks", "Combat Basics", "Katar Combat Styles", "Staff Combat Styles"),

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

        /// <summary>
        /// A crafting perk tree. The Player Guide lists it with the combat trees so a search for the tree's exact name finds it; the topic is
        /// named for its perk lines and not for combat.
        /// </summary>
        private static PlayerGuideViewModel.PlayerGuideTopic CraftingTopic(
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
                $"{family} Perk Lines",
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
