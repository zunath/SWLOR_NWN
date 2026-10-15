using System.Collections.Generic;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Native;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// The native effect checks may skip a script effect walk only when that walk could not find the effect,
    /// and replacing a status effect's native effect must leave exactly one copy.
    /// </summary>
    public static class AppliedEffectsEngineTests
    {
        private const string Resref = "nw_bandit001";
        private const string TagPrefix = "APPLIED_EFFECTS_TEST_";

        [EngineTest("Native effect checks find every effect type and tag script iteration finds", Category = "AppliedEffects", TimeoutSeconds = 60f)]
        public static async Task NativeChecksCoverScriptIteration(EngineTestContext ctx)
        {
            var creature = ctx.SpawnCreature(Resref);
            await ctx.DelaySecondsAsync(1f);
            SetAILevel(creature, AILevel.VeryLow);

            ctx.Assert(!AppliedEffects.MayHaveScriptType(creature, EffectTypeScript.Invisibility, EffectTypeScript.ImprovedInvisibility),
                "a creature without invisibility skips the invisibility walk");
            ctx.Assert(!AppliedEffects.MayHaveScriptType(creature, EffectTypeScript.Petrify), "a creature without Petrify skips the Petrify walk");
            ctx.Assert(!AppliedEffects.MayHaveTag(creature, TagPrefix + "0"), "an absent tag skips the tag walk");

            var effects = new[]
            {
                EffectInvisibility(InvisibilityType.Normal),
                EffectInvisibility(InvisibilityType.Improved),
                EffectInvisibility(InvisibilityType.Darkness),
                EffectSeeInvisible(),
                EffectTrueSeeing(),
                EffectUltravision(),
                EffectDarkness(),
                EffectConcealment(20),
                EffectSanctuary(10),
                EffectEthereal(),
                EffectHaste(),
                EffectAbilityIncrease(AbilityType.Might, 2),
                EffectTemporaryHitpoints(10),
                EffectIcon(EffectIconType.Haste),
                EffectVisualEffect(VisualEffect.Vfx_Dur_Glow_White),
                EffectCutsceneGhost(),
                EffectPetrify(),
                SupernaturalEffect(EffectLinkEffects(EffectDamageIncrease(1), EffectACIncrease(1))),
            };
            for (var index = 0; index < effects.Length; index++)
                ApplyEffectToObject(DurationType.Temporary, TagEffect(effects[index], TagPrefix + index), creature, 60f);
            ctx.Assert(StatusEffect.ApplyStatusEffect(creature, creature, typeof(HobbleStatusEffect), 30f), "Hobble applies");

            var scriptTypes = new HashSet<EffectTypeScript>();
            var scriptTags = new HashSet<string>();
            for (var effect = GetFirstEffect(creature); GetIsEffectValid(effect); effect = GetNextEffect(creature))
            {
                var type = GetEffectType(effect);
                scriptTypes.Add(type);
                ctx.Assert(AppliedEffects.MayHaveScriptType(creature, type), $"the native check finds script effect type {type}");

                var tag = GetEffectTag(effect);
                if (string.IsNullOrEmpty(tag))
                    continue;
                scriptTags.Add(tag);
                ctx.Assert(AppliedEffects.MayHaveTag(creature, tag), $"the native check finds tag {tag}");
            }

            foreach (var type in new[] { EffectTypeScript.Invisibility, EffectTypeScript.Petrify, EffectTypeScript.SeeInvisible, EffectTypeScript.TrueSeeing })
                ctx.Assert(scriptTypes.Contains(type), $"script iteration lists {type}");
            ctx.Assert(scriptTags.Count >= effects.Length, $"script iteration lists every tagged effect ({scriptTags.Count} tags)");
            ctx.Assert(Stealth.IsInvisible(creature), "an invisible creature is still reported invisible");
            ctx.SetResultDetail($"Found by both checks: {scriptTags.Count} tags and script types {string.Join(", ", scriptTypes)}.");
        }

        [EngineTest("A removed effect stays hidden while the engine finishes removing it", Category = "AppliedEffects", TimeoutSeconds = 60f)]
        public static async Task RemovedEffectStaysHidden(EngineTestContext ctx)
        {
            var creature = ctx.SpawnCreature(Resref);
            await ctx.DelaySecondsAsync(1f);
            SetAILevel(creature, AILevel.VeryLow);

            var tag = TagPrefix + "PENDING";
            ApplyEffectToObject(DurationType.Temporary, TagEffect(EffectInvisibility(InvisibilityType.Normal), tag), creature, 60f);
            ctx.Assert(Stealth.IsInvisible(creature), "the creature is invisible");
            ctx.Assert(HasEffectByTag(creature, tag), "script iteration finds the tagged effect");

            RemoveEffectByTag(creature, tag);
            // The engine can keep a removed effect in its list for a moment, so a native match is only a hint.
            var stillListed = AppliedEffects.MayHaveTag(creature, tag);
            ctx.Assert(!Stealth.IsInvisible(creature), "the creature is visible as soon as its invisibility is removed");
            ctx.Assert(!HasEffectByTag(creature, tag), "script iteration no longer finds the removed effect");

            await ctx.WaitUntilAsync(() => !AppliedEffects.MayHaveTag(creature, tag) &&
                                           !AppliedEffects.MayHaveScriptType(creature, EffectTypeScript.Invisibility, EffectTypeScript.ImprovedInvisibility),
                5f, "the engine to finish removing the effect");
            ctx.SetResultDetail(stillListed
                ? "The engine still listed the effect right after removal; the script check decided."
                : "The engine dropped the effect immediately.");
        }

        [EngineTest("Restarting, extending and refreshing a status effect leaves one native copy that stays active", Category = "AppliedEffects", TimeoutSeconds = 60f)]
        public static async Task ReplacedNativeStatusEffectIsSingle(EngineTestContext ctx)
        {
            var caster = ctx.SpawnCreature(Resref);
            var target = ctx.SpawnCreature(Resref, 2f);
            var control = ctx.SpawnCreature(Resref, 4f);
            await ctx.DelaySecondsAsync(1f);
            ctx.MakeHostile(target);
            ctx.MakeHostile(control);
            foreach (var creature in new[] { caster, target, control })
                SetAILevel(creature, AILevel.VeryLow);

            ctx.Assert(StatusEffect.ApplyStatusEffect(caster, control, typeof(HobbleStatusEffect), 6f), "Hobble applies to the control");
            var expected = CountTagged(control, StatusEffect.GetStatusEffect<HobbleStatusEffect>(control).Id);
            ctx.Assert(expected > 0, "a fresh Hobble has a native effect");

            ctx.Assert(StatusEffect.ApplyStatusEffect(caster, target, typeof(HobbleStatusEffect), 6f), "Hobble applies");
            var hobble = StatusEffect.GetStatusEffect<HobbleStatusEffect>(target);
            var steps = new (string Name, Func<bool> Run)[]
            {
                ("an in-place refresh", () => StatusEffect.ApplyStatusEffect(caster, target, typeof(HobbleStatusEffect), 6f)),
                ("a restart", () => StatusEffect.RestartStatusEffectDuration(target, typeof(HobbleStatusEffect), caster, 6f)),
                ("an extension", () => StatusEffect.ExtendStatusEffectDuration(target, typeof(HobbleStatusEffect), caster, 1f)),
                ("a refresh", () => StatusEffect.RefreshStatusEffectDuration(target, typeof(HobbleStatusEffect), caster, 6f)),
            };
            foreach (var (name, run) in steps)
            {
                await ctx.DelaySecondsAsync(0.5f);
                ctx.Assert(run(), $"{name} succeeds");
                ctx.Assert(ReferenceEquals(hobble, StatusEffect.GetStatusEffect<HobbleStatusEffect>(target)), $"{name} keeps the active Hobble");
                ctx.AssertEqual(expected, CountTagged(target, hobble.Id), $"{name} leaves one native copy");

                // The old copy's removal callback arrives after the engine finishes removing it.
                await ctx.WaitFrameAsync();
                await ctx.WaitFrameAsync();
                ctx.Assert(ReferenceEquals(hobble, StatusEffect.GetStatusEffect<HobbleStatusEffect>(target)), $"Hobble stays active after {name}");
                ctx.AssertEqual(expected, CountTagged(target, hobble.Id), $"one native copy remains after {name} settles");
                ctx.Assert(AppliedEffects.MayHaveTag(target, hobble.Id), $"the native check still finds Hobble after {name}");
            }
        }

        private static int CountTagged(uint creature, string tag)
        {
            var count = 0;
            for (var effect = GetFirstEffect(creature); GetIsEffectValid(effect); effect = GetNextEffect(creature))
            {
                if (GetEffectTag(effect) == tag)
                    count++;
            }

            return count;
        }
    }
}
