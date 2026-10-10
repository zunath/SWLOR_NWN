namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    internal static class StatusEffectGuideTopics
    {
        internal const string TopicName = "Control & Harmful Effects";
        internal const string ControlEffects = "Adhesive Grenade; Blind; Confusion; Dazed; Disoriented; Foggy Mind; Force Disruption; Hamstring; Hobble; Immobilized; Knockdown; Shadow Strike; Stunned; Tranquilized";

        internal static PlayerGuideViewModel.PlayerGuideTopic Create()
        {
            return new PlayerGuideViewModel.PlayerGuideTopic(
                TopicName,
                "Combat",
                "Control and harmful status lists, Bleed and Toxin damage caps, Fragmentation, cleansing, and Staff - Crusher / Spear - Disabler synergies.",
                "Control and debuff synergies",
                new[]
                {
                    new PlayerGuideViewModel.ArticleBlock("How the Categories Relate",
                        "A harmful effect is a negative status effect, including control effects, damage over time, and other debuffs. All control effects also count as harmful effects. A condition requiring a target affected by control effects needs one of the statuses in the complete control list below to be active. A trigger that says \"after you apply\" instead checks successful application. Taking damage or having any other harmful effect is not enough. The lists below cover named combat statuses used by effect-based perk conditions. The category belongs to the applied status, so judge it by the status name rather than the attack's name or animation."),
                    new PlayerGuideViewModel.ArticleBlock("Complete Control Effect List", ControlEffects),
                    new PlayerGuideViewModel.ArticleBlock("Hard Crowd Control",
                        "Blind, Confusion, Dazed, Immobilized, Knockdown, Stunned, and Tranquilized are hard crowd control. They also count as control and harmful effects. Hard crowd control has shared overlap and immunity rules. The remaining control effects can qualify for control synergies without being hard crowd control: slows, Disoriented, Foggy Mind, and Force Disruption still count."),
                    new PlayerGuideViewModel.ArticleBlock("Other Harmful Effects (A-F)",
                        "Bleed; Breach; Burn; Containment Net; Covering Claws; Covering Strike; Creeping Terror; Crippled Defense; Crushing Blow; Decoy; Diagnostic Sweep; Disarming Shot; Disease; Disruption Field; Disruption Pulse; Duelist's Challenge; Duelist's Distance; Eclipse of Resolve; Essence Drain; Exhausted; Expose Weak Point; Exposed; Flanking Barrage; Flash; Force Choke; Force Erosion; Force Judgment I; Force Judgment II; Force Judgment III; Force Suppression; Forcebane; Fractured Focus; Fragmentation; Freezing"),
                    new PlayerGuideViewModel.ArticleBlock("Other Harmful Effects (H-P)",
                        "Hemorrhage; Incapacitate; Infection; Kill Box; Last Bastion; Last Word; Marked; Marked for Death; Marking Toss; Nightmare Field; Pacification Field; Poison; Poison Resistance; Predator's Mark I"),
                    new PlayerGuideViewModel.ArticleBlock("Other Harmful Effects (S-W)",
                        "Saturation Toss; Shock; Signal Jammer; Smoke Bomb; Smoke Round; Stasis Volley; Sunder; Suppression; Tempest Mark; Terrified; Toxin; Unstable Pressure; Venom; Vital Strike; Vulnerable; Weakened; Worldbreaker"),
                    new PlayerGuideViewModel.ArticleBlock("Bleed Damage",
                        "Bleed deals physical damage every 6 seconds. Its base damage is 4% of the target's maximum HP, rounded up, capped at 20 + twice the higher of the source's Might or Perception modifiers. Negative modifiers count as zero. The cap applies before outgoing Bleed bonuses, Trauma resistance, and the target's damage modifiers. For example, a +8 relevant modifier caps the base tick at 36 damage. These rules also apply to Bleed from Piercing Toss and other Throwing or Shuriken abilities."),
                    new PlayerGuideViewModel.ArticleBlock("Toxin Damage",
                        "Toxin deals poison damage every 6 seconds. Its base damage is 6% of the target's maximum HP, rounded up, capped at 30 + three times the source's Agility modifier. Negative modifiers count as zero. The cap applies before Poison resistance and the target's damage modifiers. For example, a +8 Agility modifier caps the base tick at 54 damage."),
                    new PlayerGuideViewModel.ArticleBlock("Percentage Damage Caps",
                        "Bleed and Toxin use the lower of their HP percentage and source-based cap, with a minimum base tick of 1 damage. A modifier is the bonus from an attribute, not its full score. If the source is no longer available, its modifiers count as zero. The same caps apply against players, ordinary enemies, and bosses. Damage bonuses and vulnerabilities can raise the final damage above the base cap; resistance can reduce it or prevent damage entirely."),
                    new PlayerGuideViewModel.ArticleBlock("Fragmentation Damage",
                        "Fragmentation deals fixed physical damage at the tick interval listed by the ability or perk that applies it. Trauma resistance and the target's damage modifiers affect each tick. Its damage does not grow with the target's maximum HP."),
                    new PlayerGuideViewModel.ArticleBlock("Damage Output Penalties",
                        "Temporary effects that reduce damage dealt, such as Duelist's Distance, are harmful debuffs rather than control effects. A damage-increasing version is a beneficial effect. The damage dealt by an attack itself is not a status effect."),
                    new PlayerGuideViewModel.ArticleBlock("Staff - Crusher",
                        "Slam, Crushing Mastery, Heavy Hands, Crusher Stance, Break Posture, and Worldbreaker check whether the enemy currently has a control effect. Another player's control effect can satisfy these conditions. Charged Blows and Skull Rattle instead trigger when YOU successfully apply a control effect; an ally applying one does not trigger them. An effect rejected by resistance or immunity does not count as successfully applied. Charged Blows stores its next-attack bonus for the listed window even if the original control effect ends first."),
                    new PlayerGuideViewModel.ArticleBlock("Spear - Disabler",
                        "Fracture Strike requires a control effect YOU applied to still be active on the target. An ally's control effect alone does not qualify. Your Foggy Mind and Force Disruption both qualify. Erosion Strike specifically requires Foggy Mind, while Force Piercing specifically requires Foggy Mind or Force Disruption; another control status does not substitute for those named conditions. Those named conditions accept an effect applied by any player; only Fracture Strike requires your own control effect."),
                    new PlayerGuideViewModel.ArticleBlock("Examples and Common Confusions",
                        "An ally's Hamstring enables Crusher's controlled-target bonuses, but not your Fracture Strike. Your Foggy Mind enables both. Bleed, Poison, Burn, Sunder, Exposed, Force Suppression, Fractured Focus, Incapacitate, and Terrified are harmful effects but do not satisfy control-effect conditions. Disoriented does count as control even though it reduces combat stats. A momentary interrupt alone leaves no control status for later attacks to exploit."),
                    new PlayerGuideViewModel.ArticleBlock("Cleansing and Resistance",
                        "An ability that names particular effects can only remove those effects; a specific cleanse does not necessarily remove every harmful effect. Mind, Mobility, Trauma, and Disruption resistance describe which resistance an effect tests, rather than whether it counts as control. Buffs and beneficial effects do not count as harmful effects. A stance's cost or self-imposed drawback is not automatically a harmful status.")
                },
                new[]
                {
                    new PlayerGuideViewModel.QuestionAnswer("Does every harmful effect count as control?",
                        "No. Control is a subset of harmful effects. Use the complete control list; damage over time and most stat penalties do not qualify."),
                    new PlayerGuideViewModel.QuestionAnswer("Do slows and ability disruption count as control?",
                        "Adhesive Grenade, Hamstring, Hobble, Shadow Strike, Foggy Mind, and Force Disruption do. Other effects only qualify if they are in the control list."),
                    new PlayerGuideViewModel.QuestionAnswer("Can I use an ally's control effects?",
                        "Crusher's controlled-target bonuses can. Fracture Strike requires your own active control effect, and Charged Blows / Skull Rattle require you to apply one."),
                    new PlayerGuideViewModel.QuestionAnswer("Why does Incapacitate not count as control?",
                        "Incapacitate reduces Evasion. It is classified as a harmful debuff, not a control effect.")
                },
                new[] { "Combat Basics", "Abilities", "Perks", "Staff Combat Styles", "Spear Combat Styles" });
        }
    }
}
