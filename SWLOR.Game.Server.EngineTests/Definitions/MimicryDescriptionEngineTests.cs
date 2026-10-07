using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Service.AbilityService;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class MimicryDescriptionEngineTests
    {
        [EngineTest("Mimicry hotbar descriptions show costs and recasts without duplicating menu text", Category = "MimicryDescriptions")]
        public static Task TechniqueDescriptionsIncludeActivationDetails(EngineTestContext ctx)
        {
            var techniques = Ability.GetAllAbilityDetails().Where(entry => entry.Value.IsMimicryTechnique).ToArray();
            ctx.Assert(techniques.Length > 2, "The entire learned technique registry is available");
            foreach (var (feat, detail) in techniques)
            {
                var authored = Mimicry.GetTechniqueDescription(feat);
                ctx.Assert(!string.IsNullOrWhiteSpace(authored), $"{feat}: authored effects exist");
                ctx.Assert(!authored.StartsWith("Name: "), $"{feat}: the menu retains text without a hotbar header");
                var strRef = int.Parse(Get2DAString("feat", "DESCRIPTION", (int)feat));
                var before = GetStringByStrRef(strRef);
                AssertDescription(ctx, feat.ToString(), detail, authored, before);
                if (int.TryParse(Get2DAString("feat", "SPELLID", (int)feat), out var spellId) && spellId > 0 &&
                    int.TryParse(Get2DAString("spells", "SpellDesc", spellId), out var spellDescriptionId) && spellDescriptionId > 0)
                    ctx.AssertEqual(before, GetStringByStrRef(spellDescriptionId), $"{feat}: targeting spell uses the same description");
            }

            // Publishing again must reuse authored text rather than nesting the old hotbar header.
            TlkOverrides.OverrideTlks();
            foreach (var (feat, detail) in techniques)
            {
                var strRef = int.Parse(Get2DAString("feat", "DESCRIPTION", (int)feat));
                AssertDescription(ctx, feat.ToString(), detail, Mimicry.GetTechniqueDescription(feat), GetStringByStrRef(strRef));
            }
            ctx.SetResultDetail($"All {techniques.Length} technique feat/spell descriptions include current activation costs and recasts, distinguish passive traits, and remain stable across repeated publication; authored menu descriptions retain no hotbar header. Client UI is not attached.");
            return Task.CompletedTask;
        }

        private static void AssertDescription(EngineTestContext ctx, string feat, AbilityDetail detail, string authored, string description)
        {
            ctx.Assert(description.StartsWith($"Name: {detail.Name}\n"), $"{feat}: named hotbar header");
            ctx.Assert(description.EndsWith($"Description: {authored}\n"), $"{feat}: authored effects are retained once");
            if (detail.IsMimicryTrait)
            {
                ctx.Assert(description.Contains("Type: Passive Trait\n") && !description.Contains("Recast:"), $"{feat}: passive trait");
                return;
            }
            var stamina = detail.Requirements.OfType<AbilityRequirementStamina>().LastOrDefault()?.RequiredSTM ?? 0;
            ctx.Assert(description.Contains($"STM: {stamina}\n"), $"{feat}: stamina matches its ability definition");
            ctx.Assert(description.Contains(FormattableString.Invariant($"Recast: {detail.RecastDelay?.Invoke(OBJECT_INVALID) ?? 0f}s\n")),
                $"{feat}: recast matches its ability definition");
        }
    }
}
