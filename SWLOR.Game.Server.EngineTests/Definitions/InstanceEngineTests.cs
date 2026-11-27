using System.Threading.Tasks;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.EngineTests.Framework;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// An engine test class with instance tests is made content: the runner builds it with the content factory it was handed, for
    /// each run, so the class takes its services through its constructor and reads them under their old names
    /// (MaintainabilityRefactorContentInjection.md, "Engine tests").
    /// </summary>
    public sealed class InstanceEngineTests
    {
        private const int RenewalMaxLevel = 3;

        public InstanceEngineTests(IContentFactory content, IPerkService perk)
        {
            Content = content;
            Perk = perk;
        }

        private IContentFactory Content { get; }

        private IPerkService Perk { get; }

        [EngineTest("An instance engine test gets its services through its constructor", Category = "EngineRunner")]
        public async Task InstanceTestGetsItsServicesThroughItsConstructor(EngineTestContext ctx)
        {
            ctx.Assert(Content != null && Perk != null, "The runner built the test class with its services");
            ctx.Assert(Content.Create<InstanceEngineTests>() is { } second && !ReferenceEquals(second, this),
                "The factory the class was given builds a new instance of a made type");

            var npc = ctx.SpawnCreature("nw_rat001");
            await ctx.WaitFrameAsync();

            ctx.AssertEqual(RenewalMaxLevel, Perk.GetPerkLevel(npc, PerkType.Renewal), "Uncapped NPC Renewal perk level through the injected service");
        }
    }
}
