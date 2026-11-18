using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    /// <summary>The Player Guide topics of the Combat category, one method per topic.</summary>
    internal static class PlayerGuideCombatTopics
    {
        internal static PlayerGuideTopic CreateForceAffinity()
        {
            return new(
                "Force Affinity",
                "Combat",
                "How Light, Dark, and Universal Force powers change - and are changed by - your affinity.",
                "Light, Dark, and Universal powers",
                new[]
                {
                    new ArticleBlock("Reading Your Affinity",
                        "Force Affinity ranges from -10 Dark to +10 Light. A value of 0 is neutral. Your current value is displayed prominently in the Perks window and in the Character Sheet's detailed statistics."),
                    new ArticleBlock("How Affinity Changes",
                        "Owning any rank of a Light-aligned perk contributes +1 Light affinity. Owning any rank of a Dark-aligned perk contributes -1 Dark affinity. Additional ranks of the same perk do not contribute additional affinity. Refunding the perk removes its contribution. Universal Force perks do not change affinity."),
                    new ArticleBlock("Magnitude",
                        "Each point toward a power's side increases that power's damage, healing, shields, regeneration, or drain magnitude by 5 percent. Each point toward the opposing side reduces it by 5 percent. The multiplier is limited to 50 percent at full opposition and 150 percent at full alignment. Healing, shields, and regeneration stop at 125 percent, so alignment past +5 no longer raises them."),
                    new ArticleBlock("Hit Chance",
                        "Affinity also changes the hit chance of detrimental Light and Dark Force powers. Divide affinity toward the power's side by two and round down to a whole percentage point: +3 matching affinity grants +1 percentage point, while 3 points toward the opposing side applies -2 percentage points. At +10 Light, Light powers gain +5% hit chance and Dark powers suffer -5%. At -10 Dark, Dark powers gain +5% and Light powers suffer -5%. These are percentage-point adjustments: a 70% chance with a +5 adjustment becomes 75%. The final chance shown in the combat log already includes this adjustment."),
                    new ArticleBlock("Universal Powers and Durations",
                        "Universal Force powers use their normal Willpower scaling but neither gain nor lose magnitude or hit chance from affinity. Force Affinity does not change effect duration. Status resistance and explicit duration modifiers can still change a duration."),
                    new ArticleBlock("Example: +6 Light",
                        "At +6 Light, a Light damage power uses 130% magnitude, a Light heal or shield uses 125%, and Light powers gain +3% hit chance. A Dark power uses 70% magnitude and suffers -3% hit chance. A Universal power remains at 100% magnitude with no affinity hit adjustment.")
                },
                new[]
                {
                    new QuestionAnswer("Do extra ranks of one power add more affinity?", "No. Owning any rank contributes one point for that perk; higher ranks of the same perk do not add more."),
                    new QuestionAnswer("Why is my Dark power weaker?", "Positive Light affinity weakens Dark power magnitude and hit chance. Negative Dark affinity does the same to Light powers."),
                    new QuestionAnswer("Do Universal powers move affinity?", "No. They neither change affinity nor receive its magnitude or hit-chance modifiers."),
                    new QuestionAnswer("Does affinity change duration?", "No. Affinity changes magnitude and hit chance, while resistance and duration modifiers handle duration."),
                    new QuestionAnswer("Where can I see the exact effect?", "The Perks window shows your current affinity and the current magnitude and hit-chance effect on a selected Force perk.")
                },
                new[] { "Perks", "Abilities", "Attributes", "Combat Basics" });
        }

        internal static PlayerGuideTopic CreateAbilities()
        {
            return new(
                "Abilities",
                "Combat",
                "Ability descriptions, FP, STM, cooldowns, and perk-granted active abilities.",
                "FP, STM, recast",
                new[]
                {
                    new ArticleBlock("Where Abilities Come From",
                        "Some perk ranks and equipped Mimicry techniques grant active abilities. A newly granted active ability appears on your character and may be placed on your hotbar."),
                    new ArticleBlock("Ability Details",
                        "Ability descriptions show the FP cost, STM cost, recast time, and what the ability does."),
                    new ArticleBlock("Cooldowns",
                        "If an ability is still cooling down, the game tells you how long to wait before you can use it again. Hostile abilities also share a 0.5-second wait after each one. A hostile ability you use during that wait goes off as soon as it ends, and if you use several, only the last one goes off. Healing, support, and other abilities that aren't hostile don't wait."),
                    new ArticleBlock("Recast Groups",
                        "Some abilities share a cooldown group. Using one ability in that group can make the other abilities in the same group wait too.")
                },
                new[]
                {
                    new QuestionAnswer("Where do abilities come from?", "Some perk ranks and equipped Mimicry techniques grant active abilities."),
                    new QuestionAnswer("What is Recast?", "The cooldown seconds shown in the ability description."),
                    new QuestionAnswer("Why can't I use it yet?", "Its cooldown or shared cooldown group still has time remaining.")
                },
                new[] { StatusEffectGuideTopics.TopicName, "Perks", "Attributes", "Combat Basics", "Mimicry & Techniques", "Useful Windows", "Common Questions" });
        }

        internal static PlayerGuideTopic CreateMimicryAndTechniques()
        {
            return new(
                "Mimicry & Techniques",
                "Combat",
                "Learning creature techniques, individual rank requirements, learning chance, analyzer upgrades, and technique slots.",
                "Learn enemy techniques",
                new[]
                {
                    new ArticleBlock("Combat Analyzer",
                        "Combat Analyzer I unlocks technique learning and the Techniques window and provides 2 technique slots. Every technique has its own Mimicry rank requirement based on when its source enemy appears. Higher Combat Analyzer ranks improve equipped technique potency."),
                    new ArticleBlock("Analyzing Creatures",
                        "To analyze a technique, you must be alive, have Combat Analyzer I, be within 15 meters when an eligible creature uses it, and remain in the same area and within 40 meters when that creature dies. Analysis progress is shown in combat messages."),
                    new ArticleBlock("Learning Chance",
                        "Your chance to learn depends on your Mimicry rank above the technique's minimum, Pattern Recognition, and Perception, and is capped at 75 percent. A successful recording permanently teaches the technique and grants Mimicry XP."),
                    new ArticleBlock("Technique Slots",
                        "Learned techniques are not active until equipped in the Techniques window. Techniques have different slot costs. Each Analyzer Memory rank adds 2 slots, and Overclocked Analyzer adds another 2."),
                    new ArticleBlock("Active and Passive Techniques",
                        "Equipped active techniques are granted as usable abilities and can be placed on the hotbar. Passive traits apply while equipped but are not castable. You cannot change equipped techniques during combat.")
                },
                new[]
                {
                    new QuestionAnswer("How do I start learning techniques?", "Buy Combat Analyzer I, witness an eligible creature technique nearby, and stay near that creature until it dies."),
                    new QuestionAnswer("Why didn't I learn it?", "You must meet the technique's Mimicry rank requirement. Learning is then a chance roll based on Mimicry, Pattern Recognition, and Perception."),
                    new QuestionAnswer("Where do I equip techniques?", "Open Techniques from the Character Sheet while out of combat."),
                    new QuestionAnswer("Do learned techniques disappear?", "No. A successfully recorded technique remains learned, but it only functions while equipped.")
                },
                new[] { "Abilities", "Skills", "Perks", "Combat Basics", "Useful Windows" });
        }

        internal static PlayerGuideTopic CreatePvpCombat()
        {
            return new(
                "PvP Combat",
                "Combat",
                "How long fights between players wind down: the PvP combat timer, the healing reduction, the damage increase, and shorter hard crowd control.",
                "Long PvP fights",
                new[]
                {
                    new ArticleBlock("PvP Combat Timer",
                        "You enter PvP combat when you deal damage to, or take damage from, another player or a player's pet. Players who are only nearby don't count. The timer starts at the first hit and only resets after 30 seconds without PvP damage dealt or taken; switching targets, hiding briefly, or a fresh healer arriving does not reset it. A status icon counts down to the next step, and each step prints a line in your combat log."),
                    new ArticleBlock("Healing Reduction",
                        "From 2:00 of PvP combat, healing you receive drops by 10%, then 20% at 2:30, 30% at 3:00, 45% at 3:30, and 60% from 4:00 on, where it stays. It covers every way HP comes back: heals, heals over time, lifesteal, consumables, resting, and natural regeneration. FP and STM recovery are not affected, and shields (temporary HP) are not reduced."),
                    new ArticleBlock("Damage Increase",
                        "From 3:00 of PvP combat, damage players and their pets deal to you rises by 20%, then 40% at 3:30, 60% at 4:00, 80% at 4:30, and doubles (+100%) from 5:00 on. It follows how long you have been in PvP combat, not your attacker's timer, so NPCs take normal damage from you and players who just joined the fight take normal damage."),
                    new ArticleBlock("Hard Crowd Control",
                        "Hard crowd control that a player or a player's pet applies to another player lasts half as long, at least one second."),
                    new ArticleBlock("When It Ends",
                        "PvP combat ends 30 seconds after the last PvP damage dealt or taken, or when you die or are subdued. Healing and damage return to normal at once."),
                },
                new[]
                {
                    new QuestionAnswer("Why are my heals weaker?", "You have been in PvP combat for 2 minutes or more. Healing you receive drops in steps, down to 60% less from 4:00 on."),
                    new QuestionAnswer("Why am I taking more damage from players?", "You have been in PvP combat for 3 minutes or more. Damage players and their pets deal to you rises in steps, up to double from 5:00 on."),
                    new QuestionAnswer("How do I reset it?", "Go 30 seconds without dealing or taking damage from another player or a player's pet.")
                },
                new[] { "Combat Basics", "Death & Recovery", StatusEffectGuideTopics.TopicName });
        }

        internal static PlayerGuideTopic CreateCombatBasics()
        {
            return new(
                "Combat Basics",
                "Combat",
                "HP, FP, STM, hit chance, damage, defenses, resistances, deflection, guard, and character-sheet stats.",
                "Damage and defenses",
                new[]
                {
                    new ArticleBlock("Core Resources",
                        "HP is health. FP fuels Force abilities. STM fuels non-Force abilities. The Character Sheet shows your current and maximum values."),
                    new ArticleBlock("Hits and Misses",
                        $"Accuracy is compared against Evasion to decide whether an attack lands. Final hit chance is clamped between {CombatRules.MinimumHitRate} and {CombatRules.MaximumHitRate} percent, so very high or very low stats still leave uncertainty. Bosses always have at least a {CombatQueriesService.BossMinimumHitRate} percent chance to hit."),
                    new ArticleBlock("Damage and Defense",
                        "Physical attacks use Physical DEF and Force attacks use Force DEF; higher defense is better. Fire, poison, electrical, and ice use matching resistances. Sonic has no dedicated resistance. For percentage damage-taken modifiers, lower values are better."),
                    new ArticleBlock("Status Resistance",
                        "Mind, Mobility, Trauma, and Disruption resistance protect against matching categories of harmful status effects. The relevant resistance is shown by the effect or ability when applicable."),
                    new ArticleBlock("Critical Hits",
                        "Critical Rate raises your chance to critically hit, while Critical Damage makes critical hits stronger. The target's Vitality can affect the final critical chance."),
                    new ArticleBlock("Deflection and Guard",
                        $"Melee Deflection can negate hostile melee weapon auto-attacks, and Ranged Deflection can negate hostile ranged weapon auto-attacks. Both require a weapon and no shield. Shield Deflection covers both melee and ranged weapon auto-attacks and completely replaces weapon deflection while a shield is equipped; the chances never stack or roll in sequence. Deflection does not work against activated combat abilities or Force powers. Each incoming weapon auto-attack gets its own deflection attempt, including both hands when dual wielding. Guard is a separate damage-stage outcome that reduces incoming physical damage by {CombatDamageTakenService.BaseGuardDamageReductionPercent} to {CombatDamageTakenService.MaximumGuardDamageReductionPercent} percent and increases enmity."),
                    new ArticleBlock("Combat Readiness",
                        "Combat Readiness increases activated ability damage, healing, and temporary HP. It does not reduce cooldowns.")
                },
                new[]
                {
                    new QuestionAnswer("Why did I miss?", $"Your Accuracy was checked against the target's Evasion, and final hit chance is always between {CombatRules.MinimumHitRate} and {CombatRules.MaximumHitRate} percent."),
                    new QuestionAnswer("What defense matters?", "Physical DEF helps against physical attacks, Force DEF helps against Force attacks, and resistances help against matching elemental or status types."),
                    new QuestionAnswer("Are deflection and guard the same?", "No. Deflection can stop a hostile weapon auto-attack from landing, while Guard reduces physical damage after the hit."),
                    new QuestionAnswer("Does readiness lower cooldowns?", "No. Combat Readiness improves activated ability damage, healing, and temporary HP.")
                },
                new[] { "Stat Caps", StatusEffectGuideTopics.TopicName, "Abilities", "Attributes", "Skills", "Death & Recovery", "Espionage", "Lightsaber Combat Styles", "Force Combat Styles", "Useful Windows" });
        }

        internal static PlayerGuideTopic CreateStatCaps()
        {
            return new(
                "Stat Caps",
                "Combat",
                "Where character and combat stats stop, so you know when more of a stat no longer helps.",
                "Where stats stop",
                new[]
                {
                    new ArticleBlock("Character Progression",
                        "Skills that count toward the skill cap can reach 400 total ranks. Every 10 earned SP grants 1 AP, up to 40 AP. AP upgrades raise an attribute to at most 26; a racial bonus can take one attribute to 27."),
                    new ArticleBlock("Hit Chance",
                        $"Final hit chance always lands between {CombatRules.MinimumHitRate} and {CombatRules.MaximumHitRate} percent, however high or low Accuracy and Evasion are. Bosses always keep at least a {CombatQueriesService.BossMinimumHitRate} percent chance to hit."),
                    new ArticleBlock("Critical Hits",
                        $"Critical chance is at least {CombatRules.MinimumCriticalRate} percent and at most {CombatRules.MaximumCriticalRate} percent. Critical Damage bonuses add at most +{CombatRules.MaximumCriticalDamagePercentAdjustment} percent to a critical hit."),
                    new ArticleBlock("Damage Dealt",
                        $"Percentage damage bonuses together can raise a hit by at most +{CombatRules.MaximumDamageBonusPercent} percent, double its base damage. Flat damage bonuses are added on top of that cap, and stance changes to ordinary weapon auto-attack damage apply after it."),
                    new ArticleBlock("Damage Taken",
                        $"Percentage damage-taken reductions from your gear and effects stop at {CombatRules.MaximumCombinedDamageReductionPercent} percent combined. Reductions from Leadership abilities, Guard, and elemental resistance are applied separately on top of that, and damage immunity effects stop a hit entirely."),
                    new ArticleBlock("Guard",
                        $"Guard chance can reach {CombatRatingService.MaximumGuardChance} percent. A guarded hit loses {CombatDamageTakenService.BaseGuardDamageReductionPercent} percent of its physical damage, and bonuses can raise that to at most {CombatDamageTakenService.MaximumGuardDamageReductionPercent} percent."),
                    new ArticleBlock("Deflection",
                        $"Melee and Ranged Deflection chance each stop at {CombatRatingService.DefaultMeleeDeflectionChanceCap} percent. Some perks and effects raise that cap, up to {CombatRatingService.MaximumDeflectionChanceCap} percent. Shield Deflection stops at {CombatRatingService.MaximumShieldDeflectionChance} percent."),
                    new ArticleBlock("Resistances",
                        $"Fire, Poison, Electrical, Ice, Mind, Mobility, Trauma, and Disruption resistance stop at {ResistanceRules.MaximumResistance - 1} percent for players. Only temporary immunity effects reach {ResistanceRules.MaximumResistance} percent."),
                    new ArticleBlock("Attack Speed and Movement",
                        $"Attack speed bonuses shorten your attack delay by at most {CombatRules.MaximumAttackDelayAdjustmentPercent} percent, and slows lengthen it by at most {CombatRules.MaximumAttackDelayAdjustmentPercent} percent. Movement speed stops at {(int)Math.Round(CreatureStatService.MaximumMovementSpeedMultiplier * 100)} percent of normal, or 100 percent while walking."),
                    new ArticleBlock("Combat Readiness",
                        $"Combat Readiness stops at {StatService.MaximumCombatReadinessPercent} percent."),
                    new ArticleBlock("Healing From Damage",
                        $"Healing from damage you deal, such as lifesteal, is at most {CombatRules.MaximumDamageDerivedHealingPercentPerHit} percent of that hit's damage. An activated ability heals you at most {CombatResourcesService.MaximumActivatedDamageHealingMaxHPPercent} percent of your maximum HP this way per use. Passive on-hit healing gives at most {CombatResourcesService.MaximumPassiveDamageHealingMaxHPPercentPerHit} percent of your maximum HP per hit ({CombatResourcesService.MaximumCooldownDamageHealingMaxHPPercentPerHit} percent for effects with their own cooldown) and at most {CombatResourcesService.MaximumPassiveDamageHealingMaxHPPercentPerWindow} percent every {CombatResourcesService.CombatHealingWindowSeconds} seconds. Healing from defeating enemies gives at most {CombatResourcesService.MaximumDefeatedEnemyHealingMaxHPPercentPerWindow} percent of your maximum HP every {CombatResourcesService.CombatHealingWindowSeconds} seconds."),
                    new ArticleBlock("Hard Crowd Control",
                        $"Hard crowd control lasts at most {StatusEffectRules.MaximumHardCrowdControlDurationSeconds} seconds, however long it was meant to last. Against players it is then halved by the PvP rules.")
                },
                new[]
                {
                    new QuestionAnswer("Why does my hit chance never reach 100?", $"Final hit chance stops at {CombatRules.MaximumHitRate} percent, so more Accuracy past that point does not help."),
                    new QuestionAnswer("What is the highest critical chance?", $"{CombatRules.MaximumCriticalRate} percent."),
                    new QuestionAnswer("Can I become immune to damage?", $"Only through damage immunity effects. Otherwise percentage damage-taken reductions from your gear and effects stop at {CombatRules.MaximumCombinedDamageReductionPercent} percent combined, and resistances stop at {ResistanceRules.MaximumResistance - 1} percent outside temporary immunity effects."),
                    new QuestionAnswer("How high can attributes go?", "AP upgrades stop at 26. A racial bonus can raise one attribute to 27.")
                },
                new[] { "Combat Basics", "Attributes", "Skills", "Force Affinity", StatusEffectGuideTopics.TopicName, "PvP Combat" });
        }
    }
}
